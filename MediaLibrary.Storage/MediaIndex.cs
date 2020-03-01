// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MediaLibrary.Storage
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Data;
    using System.Data.SQLite;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Security.Cryptography;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Dapper;
    using MediaLibrary.Storage.Search;
    using NeoSmart.AsyncLock;

    public class MediaIndex
    {
        private readonly string indexPath;
        private AsyncLock dbLock = new AsyncLock();

        public MediaIndex(string indexPath)
        {
            this.indexPath = indexPath;
        }

        public async Task AddHashTag(HashTag hashTag)
        {
            await this.UpdateIndex(HashTag.Queries.AddHashTag, hashTag).ConfigureAwait(false);
        }

        public async Task AddIndexedPath(string path, IProgress<RescanProgress> progress = null)
        {
            await this.UpdateIndex(Queries.AddIndexedPath, new { Path = path }).ConfigureAwait(false);
            await this.RescanIndexedPath(path, progress).ConfigureAwait(false);
        }

        public Task<List<string>> GetAllTags() =>
            this.QueryIndex(async conn =>
                (await conn.QueryAsync<string>(HashTag.Queries.GetAllTags).ConfigureAwait(false)).ToList());

        public async Task<SQLiteConnection> GetConnection(bool readOnly = false)
        {
            var connectionString = new SQLiteConnectionStringBuilder
            {
                DataSource = this.indexPath,
                ReadOnly = readOnly,
            };

            SQLiteConnection connection = null;
            try
            {
                connection = new SQLiteConnection(connectionString.ToString());
                await connection.OpenAsync().ConfigureAwait(false);

                var result = connection;
                connection = null;
                return result;
            }
            finally
            {
                if (connection != null)
                {
                    connection.Dispose();
                }
            }
        }

        public async Task Initialize()
        {
            await this.UpdateIndex(Queries.CreateSchema).ConfigureAwait(false);
        }

        public async Task RemoveIndexedPath(string path)
        {
            await this.UpdateIndex(Queries.RemoveIndexedPath, new { Path = path }).ConfigureAwait(false);
        }

        public async Task Rescan(IProgress<RescanProgress> progress = null)
        {
            var indexedPaths = await this.GetIndexedPaths().ConfigureAwait(false);

            var progresses = new RescanProgress[indexedPaths.Count];
            for (var i = 0; i < indexedPaths.Count; i++)
            {
                progresses[i] = new RescanProgress(0, 0, 0, false);
            }

            var tasks = new Task[indexedPaths.Count];

            var progressSync = new object();
            var lastProgress = 0.0;
            for (var i = 0; i < indexedPaths.Count; i++)
            {
                var p = i; // Closure copy.
                tasks[p] = this.RescanIndexedPath(indexedPaths[p], progress == null ? null : OnProgress.Do<RescanProgress>(prog =>
                {
                    lock (progressSync)
                    {
                        progresses[p] = prog;
                        progress?.Report(RescanProgress.Aggregate(ref lastProgress, progresses));
                    }
                }));
            }

            await Task.WhenAll(tasks);
        }

        public async Task<List<SearchResult>> SearchIndex(string query)
        {
            var term = new SearchGrammar().Parse(query);
            var dialect = new SearchDialect();
            var sqlQuery = dialect.Compile(term);

            using (await this.dbLock.LockAsync().ConfigureAwait(false))
            using (var conn = await this.GetConnection(readOnly: false).ConfigureAwait(false))
            {
                var reader = await conn.QueryMultipleAsync(sqlQuery).ConfigureAwait(false);
                var tags = (await reader.ReadAsync<HashTag>(buffered: false).ConfigureAwait(false)).ToLookup(f => f.Hash);
                var fileNames = (await reader.ReadAsync<FilePath>(buffered: false).ConfigureAwait(false)).ToLookup(f => f.LastHash);
                var hashes = (await reader.ReadAsync<HashInfo>(buffered: false).ConfigureAwait(false)).ToList();

                var results = new List<SearchResult>();
                foreach (var hash in hashes)
                {
                    results.Add(new SearchResult(
                        hash.Hash,
                        hash.FileType,
                        hash.FileSize,
                        tags[hash.Hash].Select(t => t.Tag).ToArray(),
                        fileNames[hash.Hash].Select(t => t.Path).ToArray()));
                }

                return results;
            }
        }

        private static async Task<HashInfo> HashFileAsync(string path)
        {
            var fileSize = 0L;
            var recognizerState = FileTypeRecognizer.Initialize();
            byte[] hash;
            using (var hashAlgorithm = new SHA256Managed())
            using (var file = File.OpenRead(path))
            {
                var buffer = new byte[4096];
                hashAlgorithm.Initialize();
                while (true)
                {
                    var count = await file.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false);
                    if (count == 0)
                    {
                        hashAlgorithm.TransformFinalBlock(buffer, 0, 0);
                        hash = hashAlgorithm.Hash;
                        break;
                    }
                    else
                    {
                        hashAlgorithm.TransformBlock(buffer, 0, count, buffer, 0);
                        FileTypeRecognizer.Advance(recognizerState, buffer, 0, count);
                        fileSize += count;
                    }
                }
            }

            var sb = new StringBuilder(hash.Length * 2);

            for (var i = 0; i < hash.Length; i++)
            {
                sb.Append(hash[i].ToString("x2", CultureInfo.InvariantCulture));
            }

            return new HashInfo(sb.ToString(), fileSize, FileTypeRecognizer.GetType(recognizerState));
        }

        private Task<FilePath> GetFilePath(string path) =>
            this.QueryIndex(async conn =>
                (await conn.QueryAsync<FilePath>(FilePath.Queries.GetFilePathByPath, new { Path = path }).ConfigureAwait(false)).SingleOrDefault());

        private Task<FilePath> GetFilePaths(string hash) =>
            this.QueryIndex(async conn =>
                (await conn.QueryAsync<FilePath>(FilePath.Queries.GetFilePathsByHash, new { Hash = hash }).ConfigureAwait(false)).SingleOrDefault());

        private Task<HashInfo> GetHashInfo(string hash) =>
            this.QueryIndex(async conn =>
                (await conn.QueryAsync<HashInfo>(HashInfo.Queries.GetHashInfo, new { Hash = hash }).ConfigureAwait(false)).SingleOrDefault());

        private Task<List<string>> GetIndexedPaths() =>
            this.QueryIndex(async conn =>
                (await conn.QueryAsync<string>(Queries.GetIndexedPaths).ConfigureAwait(false)).ToList());

        private async Task<T> QueryIndex<T>(Func<SQLiteConnection, Task<T>> query)
        {
            using (await this.dbLock.LockAsync().ConfigureAwait(false))
            using (var conn = await this.GetConnection(readOnly: false).ConfigureAwait(false))
            {
                return await query(conn).ConfigureAwait(false);
            }
        }

        private async Task<string> RescanFile(string path)
        {
            var filePath = await this.GetFilePath(path).ConfigureAwait(false);
            var modifiedTime = File.GetLastWriteTimeUtc(path).Ticks;

            HashInfo hashInfo = null;
            if (filePath != null && filePath.LastModifiedTime == modifiedTime)
            {
                hashInfo = await this.GetHashInfo(filePath.LastHash).ConfigureAwait(false);
            }

            if (hashInfo == null)
            {
                hashInfo = await HashFileAsync(path).ConfigureAwait(false);
                await this.UpdateIndex(HashInfo.Queries.AddHashInfo, hashInfo).ConfigureAwait(false);
                filePath = new FilePath(path, hashInfo.Hash, modifiedTime);
                await this.UpdateIndex(FilePath.Queries.AddFilePath, filePath).ConfigureAwait(false);
            }

            return hashInfo.Hash;
        }

        private async Task RescanIndexedPath(string path, IProgress<RescanProgress> progress = null)
        {
            var sync = new object();
            var lastProgress = 0.0;
            var discoveryComplete = false;
            var discovered = 0;
            var processed = 0;
            var queue = new ConcurrentQueue<string>();
            var pendingTasks = new List<Task>();

            var progressTimer = Stopwatch.StartNew();
            void ReportProgress(bool force = false)
            {
                lock (sync)
                {
                    if (force || progressTimer.Elapsed.TotalMilliseconds > 250)
                    {
                        progress?.Report(RescanProgress.Aggregate(ref lastProgress, new RescanProgress(0, discovered, processed, discoveryComplete)));
                        progressTimer.Restart();
                    }
                }
            }

            var enumerateTask = Task.Run(() =>
            {
                try
                {
                    foreach (var file in Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories))
                    {
                        Interlocked.Increment(ref discovered);
                        queue.Enqueue(file);
                        ReportProgress();
                    }
                }
                finally
                {
                    discoveryComplete = true;
                }

                ReportProgress(force: true);
            });

            var populateTask = Task.Run(async () =>
            {
                while (true)
                {
                    if (!queue.TryDequeue(out var file))
                    {
                        if (discoveryComplete && queue.Count == 0)
                        {
                            break;
                        }
                        else
                        {
                            await Task.Delay(TimeSpan.FromMilliseconds(10)).ConfigureAwait(false);
                            continue;
                        }
                    }

                    pendingTasks.Add(this.RescanFile(file).ContinueWith(result =>
                    {
                        Interlocked.Increment(ref processed);
                        ReportProgress();
                    }));
                }
            });

            await Task.WhenAll(enumerateTask, populateTask).ConfigureAwait(false);
            await Task.WhenAll(pendingTasks).ConfigureAwait(false);
            ReportProgress(force: true);
        }

        private async Task UpdateIndex(string query, object param = null)
        {
            using (await this.dbLock.LockAsync().ConfigureAwait(false))
            using (var conn = await this.GetConnection(readOnly: false).ConfigureAwait(false))
            {
                await conn.ExecuteAsync(query, param).ConfigureAwait(false);
            }
        }

        private static class Queries
        {
            public static readonly string AddIndexedPath = @"
                INSERT INTO IndexedPaths (Path) VALUES (@Path)
            ";

            public static readonly string CreateSchema = @"
                CREATE TABLE IF NOT EXISTS IndexedPaths
                (
                    Path text NOT NULL,
                    PRIMARY KEY (Path)
                );

                CREATE TABLE IF NOT EXISTS Paths
                (
                    Path text NOT NULL,
                    LastHash text NOT NULL,
                    LastModifiedTime INTEGER NOT NULL,
                    PRIMARY KEY (Path)
                );

                CREATE UNIQUE INDEX IF NOT EXISTS IX_Paths_Path ON Paths (Path);
                CREATE INDEX IF NOT EXISTS IX_Paths_LastHash ON Paths (LastHash, Path);

                CREATE TABLE IF NOT EXISTS HashInfo
                (
                    Hash text NOT NULL,
                    FileSize integer NOT NULL,
                    FileType text NOT NULL,
                    PRIMARY KEY (Hash)
                );

                CREATE UNIQUE INDEX IF NOT EXISTS IX_HashInfo_Hash ON HashInfo (Hash);
                CREATE INDEX IF NOT EXISTS IX_HashInfo_FileType ON HashInfo (FileType);

                CREATE TABLE IF NOT EXISTS HashTags
                (
                    Hash text NOT NULL,
                    Tag text NOT NULL,
                    PRIMARY KEY (Hash, Tag),
                    FOREIGN KEY (Hash) REFERENCES HashInfo (Hash)
                );

                CREATE UNIQUE INDEX IF NOT EXISTS IX_HashTags_Hash_Tag ON HashTags (Hash, Tag);
            ";

            public static readonly string GetIndexedPaths = @"
                SELECT
                    Path
                FROM IndexedPaths
            ";

            public static readonly string RemoveIndexedPath = @"
                DELETE FROM IndexedPaths WHERE Path = @Path
            ";
        }
    }
}
