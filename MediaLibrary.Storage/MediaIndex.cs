// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MediaLibrary.Storage
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Collections.Immutable;
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

    public class MediaIndex : IDisposable
    {
        public static readonly char[] PathSeparators = new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar };

        private readonly string indexPath;
        private AsyncLock dbLock = new AsyncLock();
        private Dictionary<string, FileSystemWatcher> fileSystemWatchers = new Dictionary<string, FileSystemWatcher>();

        public MediaIndex(string indexPath)
        {
            this.indexPath = indexPath;
        }

        public event EventHandler<HashInvalidatedEventArgs> HashInvalidated;

        public event EventHandler<ItemAddedEventArgs<(HashPerson hash, Person person)>> HashPersonAdded;

        public event EventHandler<ItemRemovedEventArgs<HashPerson>> HashPersonRemoved;

        public event EventHandler<ItemAddedEventArgs<HashTag>> HashTagAdded;

        public event EventHandler<ItemRemovedEventArgs<HashTag>> HashTagRemoved;

        public static async Task<HashInfo> HashFileAsync(string path)
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

        public async Task AddHashPerson(HashPerson hashPerson)
        {
            if (hashPerson == null)
            {
                throw new ArgumentNullException(nameof(hashPerson));
            }

            await this.UpdateIndex(HashPerson.Queries.AddHashPerson, hashPerson).ConfigureAwait(false);
            this.HashInvalidated?.Invoke(this, new HashInvalidatedEventArgs(hashPerson.Hash));
            this.HashPersonAdded?.Invoke(this, new ItemAddedEventArgs<(HashPerson, Person)>((hashPerson, await this.GetPerson(hashPerson.PersonId).ConfigureAwait(false))));
        }

        public async Task AddHashTag(HashTag hashTag)
        {
            if (hashTag == null)
            {
                throw new ArgumentNullException(nameof(hashTag));
            }

            await this.UpdateIndex(HashTag.Queries.AddHashTag, hashTag).ConfigureAwait(false);
            this.HashInvalidated?.Invoke(this, new HashInvalidatedEventArgs(hashTag.Hash));
            this.HashTagAdded?.Invoke(this, new ItemAddedEventArgs<HashTag>(hashTag));
        }

        public async Task AddIndexedPath(string path, IProgress<RescanProgress> progress = null)
        {
            await this.UpdateIndex(Queries.AddIndexedPath, new { Path = path }).ConfigureAwait(false);
            await this.RescanIndexedPath(path, progress).ConfigureAwait(false);
            this.AddFileSystemWatcher(path);
        }

        public Task<Person> AddPerson(string name) =>
            this.QueryIndex(
                async conn => (await conn.QueryAsync<Person>(Person.Queries.AddPerson, new { Name = name }).ConfigureAwait(false)).Single());

        public void Dispose()
        {
            lock (this.fileSystemWatchers)
            {
                foreach (var watcher in this.fileSystemWatchers.Values)
                {
                    watcher.EnableRaisingEvents = false;
                    watcher.Dispose();
                }
            }
        }

        public Task<List<Person>> GetAllPeople() =>
            this.QueryIndex(async conn =>
                (await conn.QueryAsync<Person>(Person.Queries.GetAllPeople).ConfigureAwait(false)).ToList());

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
            var indexedPaths = await this.GetIndexedPaths().ConfigureAwait(false);
            lock (this.fileSystemWatchers)
            {
                foreach (var path in indexedPaths)
                {
                    this.AddFileSystemWatcher(path);
                }
            }
        }

        public async Task RemoveFilePath(string path) =>
            await this.UpdateIndex(FilePath.Queries.RemoveFilePathByPath, new { Path = path });

        public async Task RemoveHashPerson(HashPerson hashPerson)
        {
            if (hashPerson == null)
            {
                throw new ArgumentNullException(nameof(hashPerson));
            }

            await this.UpdateIndex(HashPerson.Queries.RemoveHashPerson, hashPerson).ConfigureAwait(false);
            this.HashInvalidated?.Invoke(this, new HashInvalidatedEventArgs(hashPerson.Hash));
            this.HashPersonRemoved?.Invoke(this, new ItemRemovedEventArgs<HashPerson>(hashPerson));
        }

        public async Task RemoveHashTag(HashTag hashTag)
        {
            if (hashTag == null)
            {
                throw new ArgumentNullException(nameof(hashTag));
            }

            await this.UpdateIndex(HashTag.Queries.RemoveHashTag, hashTag).ConfigureAwait(false);
            this.HashInvalidated?.Invoke(this, new HashInvalidatedEventArgs(hashTag.Hash));
            this.HashTagRemoved?.Invoke(this, new ItemRemovedEventArgs<HashTag>(hashTag));
        }

        public async Task RemoveIndexedPath(string path)
        {
            this.RemoveFileSystemWatcher(path);
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
                var people = (await reader.ReadAsync<Person>(buffered: false).ConfigureAwait(false)).ToDictionary(f => f.PersonId);
                var hashPeople = (await reader.ReadAsync<HashPerson>(buffered: false).ConfigureAwait(false)).ToLookup(f => f.Hash);
                var hashes = (await reader.ReadAsync<HashInfo>(buffered: false).ConfigureAwait(false)).ToList();

                var results = new List<SearchResult>();
                foreach (var hash in hashes)
                {
                    results.Add(new SearchResult(
                        hash.Hash,
                        hash.FileType,
                        hash.FileSize,
                        tags[hash.Hash].Select(t => t.Tag).ToImmutableHashSet(),
                        fileNames[hash.Hash].Select(t => t.Path).ToImmutableHashSet(),
                        hashPeople[hash.Hash].Select(p => people[p.PersonId]).ToImmutableList()));
                }

                return results;
            }
        }

        private void AddFileSystemWatcher(string path)
        {
            var watcher = new FileSystemWatcher(path);
            this.fileSystemWatchers.Add(path, watcher);
            watcher.Changed += this.Watcher_Changed;
            watcher.Deleted += this.Watcher_Deleted;
            watcher.Created += this.Watcher_Created;
            watcher.Renamed += this.Watcher_Renamed;
            watcher.EnableRaisingEvents = true;
        }

        private Task<FilePath> GetFilePath(string path) =>
            this.QueryIndex(async conn =>
                (await conn.QueryAsync<FilePath>(FilePath.Queries.GetFilePathByPath, new { Path = path }).ConfigureAwait(false)).SingleOrDefault());

        private Task<FilePath> GetFilePaths(string hash) =>
            this.QueryIndex(async conn =>
                (await conn.QueryAsync<FilePath>(FilePath.Queries.GetFilePathsByHash, new { Hash = hash }).ConfigureAwait(false)).SingleOrDefault());

        private Task<List<FilePath>> GetFilePathsUnder(string path) =>
            this.QueryIndex(async conn =>
                (await conn.QueryAsync<FilePath>(FilePath.Queries.GetFilePathsUnder, new { Path = QueryBuilder.EscapeLike(path) }).ConfigureAwait(false)).ToList());

        private Task<HashInfo> GetHashInfo(string hash) =>
            this.QueryIndex(async conn =>
                (await conn.QueryAsync<HashInfo>(HashInfo.Queries.GetHashInfo, new { Hash = hash }).ConfigureAwait(false)).SingleOrDefault());

        private Task<List<string>> GetIndexedPaths() =>
            this.QueryIndex(async conn =>
                (await conn.QueryAsync<string>(Queries.GetIndexedPaths).ConfigureAwait(false)).ToList());

        private Task<Person> GetPerson(int personId) =>
            this.QueryIndex(async conn =>
                (await conn.QueryAsync<Person>(Person.Queries.GetPersonById, new { PersonId = personId }).ConfigureAwait(false)).SingleOrDefault());

        private async Task<T> QueryIndex<T>(Func<SQLiteConnection, Task<T>> query)
        {
            using (await this.dbLock.LockAsync().ConfigureAwait(false))
            using (var conn = await this.GetConnection(readOnly: false).ConfigureAwait(false))
            {
                return await query(conn).ConfigureAwait(false);
            }
        }

        private void RemoveFileSystemWatcher(string path)
        {
            lock (this.fileSystemWatchers)
            {
                if (this.fileSystemWatchers.TryGetValue(path, out var watcher))
                {
                    this.fileSystemWatchers.Remove(path);
                    watcher.Dispose();
                }
            }
        }

        private async Task<string> RescanFile(string path, FilePath filePath = null)
        {
            filePath = filePath ?? await this.GetFilePath(path).ConfigureAwait(false);

            var fileInfo = new FileInfo(path);
            if (fileInfo.Exists)
            {
                var modifiedTime = fileInfo.LastWriteTimeUtc.Ticks;

                HashInfo hashInfo = null;
                if (filePath != null && filePath.LastModifiedTime == modifiedTime)
                {
                    hashInfo = await this.GetHashInfo(filePath.LastHash).ConfigureAwait(false);
                    if (hashInfo != null && hashInfo.FileSize != fileInfo.Length)
                    {
                        hashInfo = null;
                    }
                }

                if (hashInfo == null)
                {
                    hashInfo = await HashFileAsync(path).ConfigureAwait(false);
                    await this.UpdateIndex(HashInfo.Queries.AddHashInfo, hashInfo).ConfigureAwait(false);
                    filePath = new FilePath(path, hashInfo.Hash, modifiedTime, missingSince: null);
                    await this.UpdateIndex(FilePath.Queries.AddFilePath, filePath).ConfigureAwait(false);
                }
                else if (filePath.MissingSince != null)
                {
                    filePath = filePath.With(missingSince: null);
                    await this.UpdateIndex(FilePath.Queries.AddFilePath, filePath).ConfigureAwait(false);
                }

                return hashInfo.Hash;
            }
            else
            {
                if (filePath != null)
                {
                    var now = DateTime.UtcNow.Ticks;
                    if (filePath.MissingSince < now)
                    {
                        if (TimeSpan.FromTicks(now - filePath.MissingSince.Value).TotalDays > 30)
                        {
                            await this.RemoveFilePath(path).ConfigureAwait(false);
                        }
                    }
                    else
                    {
                        await this.UpdateIndex(FilePath.Queries.AddFilePath, filePath.With(missingSince: now)).ConfigureAwait(false);
                    }
                }

                return null;
            }
        }

        private async Task RescanIndexedPath(string path, IProgress<RescanProgress> progress = null)
        {
            var sync = new object();
            var lastProgress = 0.0;
            var discoveryComplete = false;
            var discovered = 0;
            var processed = 0;
            var queue = new ConcurrentQueue<(string path, FilePath filePath)>();
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

            var enumerateTask = Task.Run(async () =>
            {
                try
                {
                    var seen = new HashSet<string>();
                    foreach (var filePath in await this.GetFilePathsUnder(path).ConfigureAwait(false))
                    {
                        seen.Add(filePath.Path);
                        Interlocked.Increment(ref discovered);
                        queue.Enqueue((filePath.Path, filePath));
                        ReportProgress();
                    }

                    foreach (var file in Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories))
                    {
                        if (seen.Add(file))
                        {
                            Interlocked.Increment(ref discovered);
                            queue.Enqueue((file, null));
                            ReportProgress();
                        }
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

                    pendingTasks.Add(this.RescanFile(file.path, file.filePath).ContinueWith(result =>
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

        private void Watcher_Changed(object sender, FileSystemEventArgs e)
        {
            ////await this.RescanFile(e.FullPath).ConfigureAwait(false);
        }

        private void Watcher_Created(object sender, FileSystemEventArgs e)
        {
            ////await this.RescanFile(e.FullPath).ConfigureAwait(false);
        }

        private async void Watcher_Deleted(object sender, FileSystemEventArgs e)
        {
            await this.RemoveFilePath(e.FullPath).ConfigureAwait(false);
        }

        private async void Watcher_Renamed(object sender, RenamedEventArgs e)
        {
            await this.RemoveFilePath(e.OldFullPath).ConfigureAwait(false);
            ////await this.RescanFile(e.OldFullPath).ConfigureAwait(false);
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
                    MissingSince INTEGER NULL,
                    PRIMARY KEY (Path)
                );

                CREATE UNIQUE INDEX IF NOT EXISTS IX_Paths_Path ON Paths (Path);
                CREATE INDEX IF NOT EXISTS IX_Paths_LastHash ON Paths (LastHash);

                CREATE TABLE IF NOT EXISTS HashInfo
                (
                    Hash text NOT NULL,
                    FileSize integer NOT NULL,
                    FileType text NOT NULL,
                    PRIMARY KEY (Hash)
                );

                CREATE UNIQUE INDEX IF NOT EXISTS IX_HashInfo_Hash ON HashInfo (Hash);
                CREATE INDEX IF NOT EXISTS IX_HashInfo_FileType ON HashInfo (FileType);

                CREATE TABLE IF NOT EXISTS HashTag
                (
                    Hash text NOT NULL,
                    Tag text NOT NULL,
                    PRIMARY KEY (Hash, Tag),
                    FOREIGN KEY (Hash) REFERENCES HashInfo (Hash) ON DELETE CASCADE
                );

                CREATE UNIQUE INDEX IF NOT EXISTS IX_HashTag_Hash_Tag ON HashTag (Hash, Tag);

                CREATE TABLE IF NOT EXISTS Person
                (
                    PersonId integer NOT NULL,
                    Name text NOT NULL,
                    PRIMARY KEY (PersonId)
                );

                CREATE TABLE IF NOT EXISTS HashPerson
                (
                    Hash text NOT NULL,
                    PersonId integer NOT NULL,
                    PRIMARY KEY (Hash, PersonId),
                    FOREIGN KEY (Hash) REFERENCES HashInfo (Hash) ON DELETE CASCADE,
                    FOREIGN KEY (PersonId) REFERENCES Person (PersonId) ON DELETE CASCADE
                );
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
