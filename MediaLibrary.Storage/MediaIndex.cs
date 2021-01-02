// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MediaLibrary.Storage
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Data.SQLite;
    using System.Diagnostics;
    using System.Drawing;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Security.Cryptography;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Dapper;
    using MediaLibrary.Search;
    using MediaLibrary.Search.Sql;
    using MediaLibrary.Storage.FileTypes;
    using MediaLibrary.Storage.Search;
    using TaggingLibrary;

    public class MediaIndex : IDisposable
    {
        public static readonly char[] PathSeparators = new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar };

        private readonly ReaderWriterLockSlim dbLock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);
        private readonly HashSet<string> detailsColumns = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);
        private readonly CancellationTokenSource disposeCancel = new CancellationTokenSource();
        private readonly Dictionary<string, FileSystemWatcher> fileSystemWatchers = new Dictionary<string, FileSystemWatcher>();
        private readonly string indexPath;
        private readonly WeakReferenceCache<long, Person> personCache = new WeakReferenceCache<long, Person>();
        private readonly WeakReferenceCache<string, SearchResult> searchResultsCache = new WeakReferenceCache<string, SearchResult>();
        private readonly TagRulesParser tagRuleParser;

        public MediaIndex(string indexPath)
        {
            this.indexPath = indexPath;
            this.tagRuleParser = new TagRulesParser();
        }

        public event EventHandler<HashInvalidatedEventArgs> HashInvalidated;

        public event EventHandler<ItemAddedEventArgs<(HashPerson hash, Person person)>> HashPersonAdded;

        public event EventHandler<ItemRemovedEventArgs<HashPerson>> HashPersonRemoved;

        public event EventHandler<ItemAddedEventArgs<HashTag>> HashTagAdded;

        public event EventHandler<ItemRemovedEventArgs<HashTag>> HashTagRemoved;

        public event EventHandler<ItemUpdatedEventArgs<Rating>> RatingUpdated;

        public event EventHandler<ItemUpdatedEventArgs<TagRuleEngine>> TagRulesUpdated;

        public TagRuleEngine TagEngine { get; private set; }

        public static async Task<HashInfo> HashFileAsync(string path)
        {
            using (var file = File.Open(PathEncoder.ExtendPath(path), FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                return await HashFileAsync(file).ConfigureAwait(false);
            }
        }

        public static async Task<HashInfo> HashFileAsync(FileStream file)
        {
            if (file == null)
            {
                throw new ArgumentNullException(nameof(file));
            }

            var position = file.Position;

            byte[] hash;
            var fileSize = 0L;

            var recognizerState = FileTypeRecognizer.Initialize();
            try
            {
                file.Seek(0, SeekOrigin.Begin);

                var recognized = false;
                using (var hashAlgorithm = new SHA256Managed())
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

                            if (!recognized)
                            {
                                recognized = FileTypeRecognizer.Advance(recognizerState, buffer, 0, count);
                            }

                            fileSize += count;
                        }
                    }
                }
            }
            finally
            {
                file.Seek(position, SeekOrigin.Begin);
            }

            var sb = new StringBuilder(hash.Length * 2);

            for (var i = 0; i < hash.Length; i++)
            {
                sb.Append(hash[i].ToString("x2", CultureInfo.InvariantCulture));
            }

            return new HashInfo(sb.ToString(), fileSize, FileTypeRecognizer.GetType(recognizerState));
        }

        public Task<Alias> AddAlias(Alias alias) =>
            this.IndexUpdate(conn => conn.Query<Alias>(Alias.Queries.AddAlias, alias).Single());

        public async Task AddHashPerson(HashPerson hashPerson)
        {
            if (hashPerson == null)
            {
                throw new ArgumentNullException(nameof(hashPerson));
            }

            await this.IndexWrite(conn => conn.Execute(HashPerson.Queries.AddHashPerson, hashPerson)).ConfigureAwait(false);

            var hash = hashPerson.Hash;
            var personId = hashPerson.PersonId;
            Person person = null;
            if (this.searchResultsCache.TryGetValue(hash, out var searchResult) && (person = searchResult.People.FirstOrDefault(p => p.PersonId == personId)) == null)
            {
                if (!this.personCache.TryGetValue(personId, out person))
                {
                    person = await this.GetPersonById(personId).ConfigureAwait(false);
                }

                searchResult.People = searchResult.People.Add(person);
            }

            this.HashInvalidated?.Invoke(this, new HashInvalidatedEventArgs(hash));
            this.HashPersonAdded?.Invoke(this, new ItemAddedEventArgs<(HashPerson, Person)>((hashPerson, person ?? await this.GetPersonById(personId).ConfigureAwait(false))));
        }

        public async Task AddHashTag(HashTag hashTag)
        {
            if (hashTag == null)
            {
                throw new ArgumentNullException(nameof(hashTag));
            }

            await this.IndexWrite(conn => conn.Execute(HashTag.Queries.AddHashTag, hashTag)).ConfigureAwait(false);

            var hash = hashTag.Hash;
            if (this.searchResultsCache.TryGetValue(hash, out var searchResult) && !searchResult.Tags.Contains(hashTag.Tag))
            {
                searchResult.Tags = searchResult.Tags.Add(hashTag.Tag);
            }

            this.HashInvalidated?.Invoke(this, new HashInvalidatedEventArgs(hash));
            this.HashTagAdded?.Invoke(this, new ItemAddedEventArgs<HashTag>(hashTag));
        }

        public async Task AddIndexedPath(string path, IProgress<RescanProgress> progress = null)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentNullException(nameof(path));
            }

            if (!Path.IsPathRooted(path))
            {
                throw new ArgumentOutOfRangeException(nameof(path));
            }

            await this.IndexWrite(conn => conn.Execute(Queries.AddIndexedPath, new { Path = path, PathRaw = PathEncoder.GetPathRaw(path) })).ConfigureAwait(false);
            await this.RescanIndexedPath(path, progress).ConfigureAwait(false);
            this.AddFileSystemWatcher(path);
        }

        public Task<Person> AddPerson(string name) =>
            this.IndexUpdate(conn => conn.Query<Person>(Person.Queries.AddPerson, new { Name = name }).Single());

        public Task<SavedSearch> AddSavedSearch(string name, string query) =>
            this.IndexUpdate(conn => conn.Query<SavedSearch>(SavedSearch.Queries.AddSavedSearch, new { Name = name, Query = query }).Single());

        public void Dispose()
        {
            this.disposeCancel.Cancel();

            lock (this.fileSystemWatchers)
            {
                foreach (var watcher in this.fileSystemWatchers.Values)
                {
                    watcher.Dispose();
                }
            }

            this.disposeCancel.Dispose();
        }

        public Task<List<Alias>> GetAliases(int personId) =>
            this.IndexRead(conn =>
                conn.Query<Alias>(Alias.Queries.GetAliasesByPersonId, new { PersonId = personId }).ToList());

        public Task<List<Alias>> GetAllAliasesForSite(string site, string name) =>
            this.IndexRead(conn =>
                conn.Query<Alias>(Alias.Queries.GetAliasesBySiteAndName, new { Name = name, Site = site }).ToList());

        public Task<List<Alias>> GetAllAliasesForSite(string site) =>
            this.IndexRead(conn =>
                conn.Query<Alias>(Alias.Queries.GetAliasesBySite, new { Site = site }).ToList());

        public Task<string[]> GetAllAliasSites() =>
            this.IndexRead(conn =>
                conn.Query<string>(Alias.Queries.GetAllSites).ToArray());

        public Task<List<Person>> GetAllPeople() =>
            this.IndexRead(conn => this.ReadPeople(conn.QueryMultiple(Person.Queries.GetAllPeople)).ToList());

        public Task<List<string>> GetAllRatingCategories() =>
            this.IndexRead(conn =>
                conn.Query<string>(Rating.Queries.GetRatingCategories).ToList());

        public Task<List<SavedSearch>> GetAllSavedSearches() =>
            this.IndexRead(conn =>
                conn.Query<SavedSearch>(SavedSearch.Queries.GetSavedSearches).ToList());

        public Task<string> GetAllTagRules() =>
            this.IndexRead(conn =>
                string.Join(Environment.NewLine, conn.Query<string>(Queries.GetAllTagRules)));

        public Task<List<string>> GetAllTags() =>
            this.IndexRead(conn =>
                conn.Query<string>(HashTag.Queries.GetAllTags).ToList());

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

        public Task<List<HashTag>> GetHashTags(string hash) =>
            this.IndexRead(conn =>
                conn.Query<HashTag>(HashTag.Queries.GetHashTags, new { Hash = hash }).ToList());

        public Task<Person> GetPersonById(int personId) =>
            this.IndexRead(conn =>
            {
                var reader = conn.QueryMultiple(Person.Queries.GetPersonById, new { PersonId = personId });
                return this.ReadPeople(reader).SingleOrDefault();
            });

        public Task<Rating> GetRating(string hash, string category) =>
            this.IndexRead(conn =>
                conn.QuerySingleOrDefault<Rating>(Rating.Queries.GetRating, new { Hash = hash, Category = category ?? string.Empty }));

        public Task<List<HashTag>> GetRejectedTags(string hash) =>
            this.IndexRead(conn =>
                conn.Query<HashTag>(HashTag.Queries.GetRejectedTags, new { Hash = hash }).ToList());

        public async Task Initialize()
        {
            await this.IndexWrite(conn => conn.Execute(Queries.CreateSchema)).ConfigureAwait(false);
            this.TagEngine = new TagRuleEngine(this.tagRuleParser.Parse(await this.GetAllTagRules().ConfigureAwait(false)));
            var indexedPaths = await this.GetIndexedPaths().ConfigureAwait(false);
            lock (this.fileSystemWatchers)
            {
                foreach (var path in indexedPaths)
                {
                    this.AddFileSystemWatcher(path);
                }
            }
        }

        public async Task MergePeople(int targetId, int duplicateId)
        {
            if (targetId == duplicateId)
            {
                throw new ArgumentOutOfRangeException(nameof(duplicateId));
            }

            await this.IndexWrite(conn => conn.Execute(Person.Queries.MergePeople, new { TargetId = targetId, DuplicateId = duplicateId })).ConfigureAwait(false);
        }

        public Task RemoveAlias(Alias alias) => this.IndexWrite(conn => conn.Execute(Alias.Queries.RemoveAlias, alias));

        public Task RemoveFilePath(string path) =>
            this.IndexWrite(conn =>
                conn.Execute(FilePath.Queries.RemoveFilePathByPath, new { Path = path, PathRaw = PathEncoder.GetPathRaw(path) }));

        public async Task RemoveHashPerson(HashPerson hashPerson)
        {
            if (hashPerson == null)
            {
                throw new ArgumentNullException(nameof(hashPerson));
            }

            await this.IndexWrite(conn => conn.Execute(HashPerson.Queries.RemoveHashPerson, hashPerson)).ConfigureAwait(false);

            var hash = hashPerson.Hash;
            if (this.searchResultsCache.TryGetValue(hash, out var searchResult) && searchResult.People.FirstOrDefault(p => p.PersonId == hashPerson.PersonId) is Person person)
            {
                searchResult.People = searchResult.People.Remove(person);
            }

            this.HashInvalidated?.Invoke(this, new HashInvalidatedEventArgs(hash));
            this.HashPersonRemoved?.Invoke(this, new ItemRemovedEventArgs<HashPerson>(hashPerson));
        }

        public async Task RemoveHashTag(HashTag hashTag, bool rejectTag = false)
        {
            if (hashTag == null)
            {
                throw new ArgumentNullException(nameof(hashTag));
            }

            await this.IndexWrite(conn => conn.Execute(rejectTag ? HashTag.Queries.RejectHashTag : HashTag.Queries.RemoveHashTag, hashTag)).ConfigureAwait(false);

            var hash = hashTag.Hash;
            if (this.searchResultsCache.TryGetValue(hash, out var searchResult) && searchResult.Tags.Contains(hashTag.Tag))
            {
                searchResult.Tags = searchResult.Tags.Remove(hashTag.Tag);
            }

            this.HashInvalidated?.Invoke(this, new HashInvalidatedEventArgs(hash));
            this.HashTagRemoved?.Invoke(this, new ItemRemovedEventArgs<HashTag>(hashTag));
        }

        public async Task RemoveIndexedPath(string path)
        {
            this.RemoveFileSystemWatcher(path);
            await this.IndexWrite(conn => conn.Execute(Queries.RemoveIndexedPath, new { Path = path, PathRaw = PathEncoder.GetPathRaw(path) })).ConfigureAwait(false);
        }

        public Task RemovePerson(Person person) => this.IndexWrite(conn => conn.Execute(Person.Queries.RemovePerson, new { person.PersonId }));

        public Task RemoveSavedSearch(SavedSearch savedSearch) =>
            this.IndexWrite(conn =>
                conn.Execute(SavedSearch.Queries.RemoveSavedSearch, new { savedSearch.SearchId }));

        public async Task Rescan(IProgress<RescanProgress> progress = null, bool forceRehash = false)
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
                var pathProgress = progress == null ? null : OnProgress.Do<RescanProgress>(prog =>
                {
                    lock (progressSync)
                    {
                        progresses[p] = prog;
                        progress?.Report(RescanProgress.Aggregate(ref lastProgress, progresses));
                    }
                });
                tasks[p] = this.RescanIndexedPath(indexedPaths[p], pathProgress, forceRehash);
            }

            await Task.WhenAll(tasks).ConfigureAwait(false);
        }

        public async Task<List<SearchResult>> SearchIndex(string query, bool excludeHidden = true)
        {
            var term = new SearchGrammar().Parse(query ?? string.Empty);
            var dialect = new SqlSearchCompiler(this.TagEngine, excludeHidden);
            var sqlQuery = dialect.Compile(term);

            using (var conn = await this.GetConnection().ConfigureAwait(false))
            {
                ILookup<string, HashTag> tags;
                ILookup<string, FilePath> fileNames;
                Dictionary<int, Person> people;
                ILookup<string, HashPerson> hashPeople;
                ILookup<string, Rating> hashRatings;
                IList<HashInfo> hashes;
                try
                {
                    this.dbLock.EnterReadLock();
                    var reader = conn.QueryMultiple(sqlQuery);
                    tags = reader.Read<HashTag>(buffered: false).ToLookup(f => f.Hash);
                    fileNames = reader.Read<FilePath>(buffered: false).ToLookup(f => f.LastHash);
                    people = this.ReadPeople(reader).ToDictionary(p => p.PersonId);
                    hashPeople = reader.Read<HashPerson>(buffered: false).ToLookup(f => f.Hash);
                    hashRatings = reader.Read<Rating>(buffered: false).ToLookup(r => r.Hash);
                    hashes = reader.Read<HashInfo>(buffered: false).ToList();
                }
                finally
                {
                    this.dbLock.ExitReadLock();
                }

                var results = new List<SearchResult>();
                foreach (var hash in hashes)
                {
                    var updatedTags = tags[hash.Hash].Select(t => t.Tag).ToImmutableHashSet();
                    var updatedPaths = fileNames[hash.Hash].Select(t => t.Path).ToImmutableHashSet();
                    var updatedPeople = hashPeople[hash.Hash].Select(p => people[p.PersonId]).ToImmutableHashSet();
                    var updatedRating = hashRatings[hash.Hash].Where(p => string.IsNullOrEmpty(p.Category)).SingleOrDefault();
                    results.Add(this.searchResultsCache.AddOrUpdate(
                        hash.Hash,
                        key => new SearchResult(
                            key,
                            hash.FileType,
                            hash.FileSize,
                            updatedRating,
                            updatedTags,
                            updatedPaths,
                            updatedPeople),
                        (key, searchResult) =>
                        {
                            if (searchResult.Rating != updatedRating)
                            {
                                searchResult.Rating = updatedRating;
                            }

                            if (!searchResult.Tags.SetEquals(updatedTags))
                            {
                                searchResult.Tags = updatedTags;
                            }

                            if (!searchResult.Paths.SetEquals(updatedPaths))
                            {
                                searchResult.Paths = updatedPaths;
                            }

                            if (!searchResult.People.SetEquals(updatedPeople))
                            {
                                searchResult.People = updatedPeople;
                            }
                        }));
                }

                return results;
            }
        }

        public Task UpdatePerson(Person person) =>
            this.IndexWrite(conn => conn.Execute(Person.Queries.UpdatePerson, new { person.PersonId, person.Name }));

        public async Task UpdateRating(Rating rating)
        {
            await this.IndexWrite(conn => conn.Execute(Rating.Queries.UpdateRating, rating)).ConfigureAwait(false);

            if (string.IsNullOrEmpty(rating.Category))
            {
                var hash = rating.Hash;
                if (this.searchResultsCache.TryGetValue(hash, out var searchResult))
                {
                    searchResult.Rating = rating;
                }

                this.HashInvalidated?.Invoke(this, new HashInvalidatedEventArgs(hash));
                this.RatingUpdated?.Invoke(this, new ItemUpdatedEventArgs<Rating>(rating));
            }
        }

        public Task UpdateSavedSearch(SavedSearch savedSearch) =>
            this.IndexWrite(conn =>
                conn.Execute(SavedSearch.Queries.UpdateSavedSearch, savedSearch));

        public async Task UpdateTagRules(string rules)
        {
            var updatedEngine = new TagRuleEngine(this.tagRuleParser.Parse(rules));
            await this.IndexWrite(conn => conn.Execute(Queries.UdateTagRules, new { Rules = rules })).ConfigureAwait(false);
            this.TagEngine = updatedEngine;
            this.TagRulesUpdated?.Invoke(this, new ItemUpdatedEventArgs<TagRuleEngine>(updatedEngine));
        }

        private async Task AddFilePath(FilePath filePath) =>
            await this.IndexWrite(conn =>
                conn.Execute(FilePath.Queries.AddFilePath, filePath)).ConfigureAwait(false);

        private void AddFileSystemWatcher(string path)
        {
            FileSystemWatcher watcher = null;
            try
            {
                try
                {
                    watcher = new FileSystemWatcher(path);
                }
                catch (ArgumentException)
                {
                    // TODO: Poll for existence.
                    return;
                }

                watcher.IncludeSubdirectories = true;
                watcher.Changed += this.Watcher_Changed;
                watcher.Deleted += this.Watcher_Deleted;
                watcher.Created += this.Watcher_Created;
                watcher.Renamed += this.Watcher_Renamed;
                watcher.EnableRaisingEvents = true;
                this.fileSystemWatchers.Add(path, watcher);
                watcher = null;
            }
            finally
            {
                watcher?.Dispose();
            }
        }

        private Task<FilePath> GetFilePath(string path) =>
            this.IndexRead(conn =>
                conn.QuerySingleOrDefault<FilePath>(FilePath.Queries.GetFilePathByPath, new { Path = path, PathRaw = PathEncoder.GetPathRaw(path) }));

        private Task<FilePath> GetFilePaths(string hash) =>
            this.IndexRead(conn =>
                conn.QuerySingleOrDefault<FilePath>(FilePath.Queries.GetFilePathsByHash, new { Hash = hash }));

        private Task<HashInfo> GetHashInfo(string hash) =>
            this.IndexRead(conn =>
                conn.QuerySingleOrDefault<HashInfo>(HashInfo.Queries.GetHashInfo, new { Hash = hash }));

        private Task<List<string>> GetIndexedPaths() =>
            this.IndexRead(conn =>
                conn.Query<string, byte[], string>(Queries.GetIndexedPaths, (path, pathRaw) => PathEncoder.GetPath(path, pathRaw), splitOn: "PathRaw", buffered: false).ToList());

        private Task<List<(FilePath filePath, HashInfo hashInfo, bool hasDetails)>> GetIndexInfoUnder(string path) =>
            this.IndexRead(conn =>
            {
                var reader = conn.QueryMultiple(FilePath.Queries.GetFilePathsUnder, new { Path = QueryBuilder.EscapeLike(path) });
                var hashInfo = reader.Read<HashInfo, long, (HashInfo, bool)>((hash, hasDetails) => (hash, hasDetails != 0), splitOn: "HasHashDetails", buffered: false).ToDictionary(h => h.Item1.Hash);
                return reader.Read<FilePath>(buffered: false).Select(p =>
                {
                    if (hashInfo.TryGetValue(p.LastHash, out var hash))
                    {
                        return (p, hash.Item1, hash.Item2);
                    }

                    return (p, null, false);
                }).ToList();
            });

        private async Task<T> IndexRead<T>(Func<SQLiteConnection, T> query)
        {
            using (var conn = await this.GetConnection(readOnly: true).ConfigureAwait(false))
            {
                try
                {
                    this.dbLock.EnterReadLock();
                    return query(conn);
                }
                finally
                {
                    this.dbLock.ExitReadLock();
                }
            }
        }

        private async Task<T> IndexUpdate<T>(Func<SQLiteConnection, T> query)
        {
            await Task.Yield();
            using (var conn = await this.GetConnection(readOnly: false).ConfigureAwait(false))
            {
                try
                {
                    this.dbLock.EnterWriteLock();
                    return query(conn);
                }
                finally
                {
                    this.dbLock.ExitWriteLock();
                }
            }
        }

        private async Task IndexWrite(Action<SQLiteConnection> query)
        {
            using (var conn = await this.GetConnection(readOnly: false).ConfigureAwait(false))
            {
                try
                {
                    this.dbLock.EnterWriteLock();
                    query(conn);
                }
                finally
                {
                    this.dbLock.ExitWriteLock();
                }
            }
        }

        private IEnumerable<Person> ReadPeople(SqlMapper.GridReader reader)
        {
            var aliases = reader.Read<Alias>(buffered: false).ToLookup(f => f.PersonId);

            return reader.Read<Person>(buffered: false).Select(f =>
            {
                var updatedAliases = aliases[f.PersonId].ToImmutableHashSet();
                void UpdatePerson(Person person)
                {
                    if (person.Aliases == null || !person.Aliases.SetEquals(updatedAliases))
                    {
                        person.Aliases = updatedAliases;
                    }
                }

                return this.personCache.AddOrUpdate(f.PersonId,
                    _ =>
                    {
                        UpdatePerson(f);
                        return f;
                    },
                    (_, person) =>
                    {
                        UpdatePerson(person);
                    });
            });
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

        private async Task<string> RescanFile(string path, FilePath filePath = null, HashInfo hashInfo = null, bool? hasDetails = null, bool forceRehash = false)
        {
            if (filePath != null)
            {
                if (filePath.Path != path)
                {
                    throw new ArgumentOutOfRangeException(nameof(filePath));
                }
            }
            else
            {
                filePath = await this.GetFilePath(path).ConfigureAwait(false);
            }

            var extendedPath = PathEncoder.ExtendPath(path);
            var fileInfo = new FileInfo(extendedPath);
            if (fileInfo.Exists)
            {
                var modifiedTime = fileInfo.LastWriteTimeUtc.Ticks;

                if (filePath != null && filePath.LastModifiedTime == modifiedTime)
                {
                    if (hashInfo == null)
                    {
                        hasDetails = null;
                    }
                    else if (hashInfo.Hash != filePath.LastHash)
                    {
                        hashInfo = null;
                        hasDetails = null;
                    }

                    hashInfo = hashInfo ?? await this.GetHashInfo(filePath.LastHash).ConfigureAwait(false); // TODO: Get hasDetails here.
                    if (hashInfo != null && hashInfo.FileSize != fileInfo.Length)
                    {
                        hashInfo = null;
                        hasDetails = null;
                    }
                }
                else
                {
                    hashInfo = null;
                    hasDetails = null;
                }

                if (hashInfo == null || forceRehash || !(hasDetails ?? false))
                {
                    using (var file = File.Open(extendedPath, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        hashInfo = await HashFileAsync(file).ConfigureAwait(false);
                        await this.IndexWrite(conn => conn.Execute(HashInfo.Queries.AddHashInfo, hashInfo)).ConfigureAwait(false); // TODO: Get hasDetails here.
                        filePath = new FilePath(path, hashInfo.Hash, modifiedTime, missingSince: null);
                        await this.AddFilePath(filePath).ConfigureAwait(false);

                        await this.UpdateHashDetails(hashInfo, file).ConfigureAwait(false);
                    }
                }
                else if (filePath.MissingSince != null)
                {
                    filePath = filePath.With(missingSince: null);
                    await this.AddFilePath(filePath).ConfigureAwait(false);
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
                        await this.AddFilePath(filePath.With(missingSince: now)).ConfigureAwait(false);
                    }
                }

                return null;
            }
        }

        private async Task RescanIndexedPath(string path, IProgress<RescanProgress> progress = null, bool forceRehash = false)
        {
            var sync = new object();
            var lastProgress = 0.0;
            var discoveryComplete = false;
            var discovered = 0;
            var processed = 0;
            var queue = new ConcurrentQueue<(string path, FilePath filePath, HashInfo hashInfo, bool? hasDetails)>();
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
                    foreach (var (filePath, hashInfo, hasDetails) in await this.GetIndexInfoUnder(path).ConfigureAwait(false))
                    {
                        seen.Add(filePath.Path);
                        Interlocked.Increment(ref discovered);
                        queue.Enqueue((filePath.Path, filePath, hashInfo, hasDetails));
                        ReportProgress();
                    }

                    foreach (var file in Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories))
                    {
                        if (seen.Add(file))
                        {
                            Interlocked.Increment(ref discovered);
                            queue.Enqueue((file, null, null, null));
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

                    pendingTasks.Add(this.RescanFile(file.path, file.filePath, file.hashInfo, file.hasDetails, forceRehash).ContinueWith(result =>
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

        private async Task TouchFile(string fullPath)
        {
            // TODO: This should act as a queue that shares resources with full-Rescans.
            // TODO: This should detect if the path is inside some indexed folder.
            var delay = TimeSpan.FromSeconds(1);
            var attempts = 0;
            while (true)
            {
                // Avoid attempting to hash a newly created directory.
                if (!File.Exists(fullPath))
                {
                    break;
                }

                try
                {
                    await this.RescanFile(fullPath).ConfigureAwait(false);
                    break;
                }
                catch (IOException)
                {
                    attempts += 1;
                    delay += delay;
                }

                if (attempts < 4)
                {
                    await Task.Delay(delay).ConfigureAwait(false);
                }
                else
                {
                    break;
                }
            }
        }

        private async Task UpdateHashDetails(HashInfo hashInfo, FileStream file)
        {
            Dictionary<string, object> details = null;
            try
            {
                if (hashInfo.FileType == "image" || hashInfo.FileType.StartsWith("image/", StringComparison.Ordinal))
                {
                    using (var image = Image.FromStream(file))
                    {
                        details = ImageDetailRecognizer.Recognize(image);
                    }
                }
                else
                {
                    details = new Dictionary<string, object>();
                }
            }
            catch (IOException)
            {
            }

            if (details == null)
            {
                return;
            }

            details.Remove("Hash");

            string EscapeKey(string key) => $"[{key.Replace("]", "]]")}]";
            var keys = details.Keys.ToList();
            var detailsColumns = string.Concat(keys.Select(k => $", {EscapeKey(k)}"));
            var parameterNames = string.Concat(Enumerable.Range(0, keys.Count).Select(i => $", @p{i}"));
            var param = new DynamicParameters();
            param.Add("Hash", hashInfo.Hash);
            for (var i = 0; i < keys.Count; i++)
            {
                param.Add($"p{i}", details[keys[i]]);
            }

            await this.IndexWrite(conn =>
            {
                if (this.detailsColumns.Count == 0)
                {
                    this.detailsColumns.UnionWith(
                        conn.Query<string>("SELECT name FROM pragma_table_info('HashDetails')"));

                    if (this.detailsColumns.Count == 0)
                    {
                        conn.Execute($"CREATE TABLE HashDetails (Hash text NOT NULL{detailsColumns}, PRIMARY KEY (Hash), FOREIGN KEY (Hash) REFERENCES HashInfo (Hash) ON DELETE CASCADE)");
                        this.detailsColumns.Add("Hash");
                        this.detailsColumns.UnionWith(keys);
                    }
                }

                foreach (var key in keys)
                {
                    if (!this.detailsColumns.Contains(key))
                    {
                        conn.Execute($"ALTER TABLE HashDetails ADD COLUMN {EscapeKey(key)}");
                        this.detailsColumns.Add(key);
                    }
                }

                conn.Execute($"INSERT OR REPLACE INTO HashDetails (Hash{detailsColumns}) VALUES (@Hash{parameterNames})", param);
            }).ConfigureAwait(false);
        }

        private async void Watcher_Changed(object sender, FileSystemEventArgs e)
        {
            await this.TouchFile(e.FullPath).ConfigureAwait(false);
        }

        private async void Watcher_Created(object sender, FileSystemEventArgs e)
        {
            await this.TouchFile(e.FullPath).ConfigureAwait(false);
        }

        private async void Watcher_Deleted(object sender, FileSystemEventArgs e)
        {
            await this.RemoveFilePath(e.FullPath).ConfigureAwait(false);
        }

        private async void Watcher_Renamed(object sender, RenamedEventArgs e)
        {
            // TODO: Race condition. Better is to set missing-since and rescan the file.
            await this.RemoveFilePath(e.OldFullPath).ConfigureAwait(false);
            await this.TouchFile(e.FullPath).ConfigureAwait(false);
        }

        private static class Queries
        {
            public static readonly string AddIndexedPath = @"
                INSERT INTO IndexedPaths (Path, PathRaw) VALUES (@Path, @PathRaw)
            ";

            public static readonly string CreateSchema = @"
                CREATE TABLE IF NOT EXISTS IndexedPaths
                (
                    Path text NOT NULL,
                    PathRaw blob NULL,
                    PRIMARY KEY (Path)
                );

                CREATE TABLE IF NOT EXISTS Paths
                (
                    Path text NOT NULL,
                    PathRaw blob NULL,
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

                CREATE TABLE IF NOT EXISTS HashDetails
                (
                    Hash text NOT NULL,
                    PRIMARY KEY (Hash),
                    FOREIGN KEY (Hash) REFERENCES HashInfo (Hash) ON DELETE CASCADE
                );

                CREATE TABLE IF NOT EXISTS HashTag
                (
                    Hash text NOT NULL,
                    Tag text NOT NULL,
                    PRIMARY KEY (Hash, Tag),
                    FOREIGN KEY (Hash) REFERENCES HashInfo (Hash) ON DELETE CASCADE
                );

                CREATE UNIQUE INDEX IF NOT EXISTS IX_HashTag_Hash_Tag ON HashTag (Hash, Tag);

                CREATE TABLE IF NOT EXISTS RejectedTags
                (
                    Hash text NOT NULL,
                    Tag text NOT NULL,
                    PRIMARY KEY (Hash, Tag),
                    FOREIGN KEY (Hash) REFERENCES HashInfo (Hash) ON DELETE CASCADE
                );

                CREATE UNIQUE INDEX IF NOT EXISTS IX_RejectedTags_Hash_Tag ON RejectedTags (Hash, Tag);

                CREATE TABLE IF NOT EXISTS TagRules
                (
                    Rules text NOT NULL
                );

                CREATE TABLE IF NOT EXISTS Person
                (
                    PersonId integer NOT NULL,
                    Name text NOT NULL,
                    PRIMARY KEY (PersonId)
                );

                CREATE TABLE IF NOT EXISTS Alias
                (
                    PersonId integer NOT NULL,
                    Name text NOT NULL,
                    Site text NULL,
                    FOREIGN KEY (PersonId) REFERENCES Person (PersonId) ON DELETE CASCADE
                );

                CREATE UNIQUE INDEX IF NOT EXISTS IX_Alias_Site_Name ON Alias (Site, Name) WHERE Site NOT NULL;

                CREATE VIEW IF NOT EXISTS Names AS
                SELECT DISTINCT PersonId, Name FROM (
                    SELECT PersonId, Name FROM Person
                    UNION ALL
                    SELECT Personid, Name FROM Alias
                );

                CREATE TABLE IF NOT EXISTS HashPerson
                (
                    Hash text NOT NULL,
                    PersonId integer NOT NULL,
                    PRIMARY KEY (Hash, PersonId),
                    FOREIGN KEY (Hash) REFERENCES HashInfo (Hash) ON DELETE CASCADE,
                    FOREIGN KEY (PersonId) REFERENCES Person (PersonId) ON DELETE CASCADE
                );

                CREATE TABLE IF NOT EXISTS SavedSearch
                (
                    SearchId integer NOT NULL,
                    Name text NOT NULL,
                    Query text NOT NULL,
                    PRIMARY KEY (SearchId)
                );

                CREATE TABLE IF NOT EXISTS Rating
                (
                    Hash text NOT NULL,
                    Category text NOT NULL,
                    Value real NOT NULL,
                    Count integer NOT NULL,
                    PRIMARY KEY (Hash, Category)
                );
            ";

            public static readonly string GetAllTagRules = @"
                SELECT
                    Rules
                FROM TagRules
            ";

            public static readonly string GetIndexedPaths = @"
                SELECT
                    Path,
                    PathRaw
                FROM IndexedPaths
            ";

            public static readonly string RemoveIndexedPath = @"
                DELETE FROM IndexedPaths
                WHERE Path = @Path
                AND ((@PathRaw IS NULL AND PathRaw IS NULL) OR (@PathRaw IS NOT NULL AND PathRaw = @PathRaw))
            ";

            public static readonly string UdateTagRules = @"
                DELETE FROM TagRules;
                INSERT INTO TagRules (Rules) VALUES (@Rules);
            ";
        }
    }
}
