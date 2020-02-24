// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MediaLibrary.Storage
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Data.SQLite;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Security.Cryptography;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Dapper;

    public class MediaIndex
    {
        private readonly string indexPath;

        public MediaIndex(string indexPath)
        {
            this.indexPath = indexPath;
        }

        public async Task AddIndexedPath(string path, IProgress<RescanProgress> progress = null)
        {
            using (var conn = await this.GetConnection().ConfigureAwait(false))
            {
                await conn.ExecuteAsync(Queries.AddIndexedPath, new { Path = path }).ConfigureAwait(false);
                await this.RescanIndexedPath(path, progress).ConfigureAwait(false);
            }
        }

        public async Task<SQLiteConnection> GetConnection()
        {
            var connectionString = new SQLiteConnectionStringBuilder
            {
                DataSource = this.indexPath,
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
            using (var conn = await this.GetConnection().ConfigureAwait(false))
            {
                await conn.ExecuteAsync(Queries.CreateSchema).ConfigureAwait(false);
            }
        }

        public async Task RemoveIndexedPath(string path)
        {
            using (var conn = await this.GetConnection().ConfigureAwait(false))
            {
                await conn.ExecuteAsync(Queries.RemoveIndexedPath, new { Path = path }).ConfigureAwait(false);
            }
        }

        public async Task Rescan(IProgress<RescanProgress> progress = null)
        {
            using (var conn = await this.GetConnection().ConfigureAwait(false))
            {
                var indexedPaths = await this.GetIndexedPaths(conn).ConfigureAwait(false);
                var progresses = new RescanProgress[indexedPaths.Count];
                var tasks = new Task[indexedPaths.Count];

                var progressSync = new object();
                var lastProgress = 0.0;
                for (var i = 0; i < indexedPaths.Count; i++)
                {
                    var p = i; // Closure copy.
                    progresses[p] = new RescanProgress(0, 0, 0, false);
                    tasks[p] = this.RescanIndexedPath(indexedPaths[p], progress == null ? null : OnProgress.Do<RescanProgress>(prog =>
                    {
                        lock (progressSync)
                        {
                            progresses[p] = prog;
                            progress?.Report(RescanProgress.Aggregate(ref lastProgress, progresses));
                        }
                    }));
                }
            }
        }

        private static async Task<string> HashFileAsync(string path)
        {
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
                        hash = hashAlgorithm.TransformFinalBlock(buffer, 0, 0);
                        break;
                    }
                    else
                    {
                        hashAlgorithm.TransformBlock(buffer, 0, count, buffer, 0);
                    }
                }
            }

            var sb = new StringBuilder(hash.Length * 2);

            for (var i = 0; i < hash.Length; i++)
            {
                sb.Append(hash[i].ToString("x2", CultureInfo.InvariantCulture));
            }

            return sb.ToString();
        }

        private async Task<List<string>> GetIndexedPaths(SQLiteConnection conn)
        {
            return (await conn.QueryAsync<string>(Queries.GetIndexedPaths).ConfigureAwait(false)).ToList();
        }

        private async Task RescanFile(string path)
        {
            var hash = await HashFileAsync(path).ConfigureAwait(false);
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

            void ReportProgress()
            {
                lock (sync)
                {
                    progress?.Report(RescanProgress.Aggregate(ref lastProgress, new RescanProgress(0, discovered, processed, discoveryComplete)));
                }
            }

            var enumerateTask = Task.Run(() =>
            {
                try
                {
                    foreach (var file in Directory.EnumerateFiles(path))
                    {
                        if (Interlocked.Increment(ref discovered) % 100 == 0)
                        {
                            ReportProgress();
                        }

                        queue.Enqueue(file);
                    }
                }
                finally
                {
                    discoveryComplete = true;
                }

                ReportProgress();
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
                            await Task.Delay(100).ConfigureAwait(false);
                            continue;
                        }
                    }

                    pendingTasks.Add(this.RescanFile(file).ContinueWith(result =>
                    {
                        if (Interlocked.Increment(ref processed) % 100 == 0)
                        {
                            ReportProgress();
                        }
                    }));
                }
            });

            await Task.WhenAll(enumerateTask, populateTask).ConfigureAwait(false);
            await Task.WhenAll(pendingTasks).ConfigureAwait(false);
            ReportProgress();
        }

        public class RescanProgress
        {
            public RescanProgress(
                double estimate,
                int pathsDiscovered,
                int pathsProcessed,
                bool discoveryComplete)
            {
                this.Estimate = estimate;
                this.PathsDiscovered = pathsDiscovered;
                this.PathsProcessed = pathsProcessed;
                this.DiscoveryComplete = discoveryComplete;
            }

            public bool DiscoveryComplete { get; }

            public double Estimate { get; }

            public int PathsDiscovered { get; }

            public int PathsProcessed { get; }

            internal static RescanProgress Aggregate(ref double lastProgress, params RescanProgress[] progresses)
            {
                var weight = 0.0;
                var pathsDiscovered = 0;
                var pathsProcessed = 0;
                var discoveryComplete = true;
                for (var i = 0; i < progresses.Length; i++)
                {
                    var p = progresses[i];
                    pathsDiscovered += p.PathsDiscovered;
                    pathsProcessed += p.PathsProcessed;
                    discoveryComplete &= p.DiscoveryComplete;
                    weight += p.DiscoveryComplete ? p.PathsDiscovered : Math.Max(p.PathsDiscovered + 100, p.PathsDiscovered * 2);
                }

                var progress = pathsProcessed / weight;
                return new RescanProgress(
                    lastProgress = Math.Max(lastProgress, progress * (discoveryComplete ? 1 : 0.99)),
                    pathsDiscovered,
                    pathsProcessed,
                    discoveryComplete);
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
                    Path text PRIMARY KEY
                )
            ";

            public static readonly string GetIndexedPaths = @"
                SELECT Path FROM IndexedPaths
            ";

            public static readonly string RemoveIndexedPath = @"
                DELETE FROM IndexedPaths WHERE Path = @Path
            ";
        }
    }
}
