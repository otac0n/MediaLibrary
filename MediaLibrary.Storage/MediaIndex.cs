// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MediaLibrary.Storage
{
    using System;
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
    using MediaLibrary.Search.Terms;
    using MediaLibrary.Storage.FileTypes;
    using MediaLibrary.Storage.Search;
    using MediaLibrary.Storage.Search.Expressions;
    using Nito.AsyncEx;
    using TaggingLibrary;

    public class MediaIndex
    {
        public static readonly char[] PathSeparators = new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar };
        private static readonly TimeSpan BufferOverflowDelay = TimeSpan.FromSeconds(2);
        private static readonly TimeSpan FileChangeDelay = TimeSpan.FromSeconds(0.3);
        private static readonly TimeSpan FileCreatedDelay = TimeSpan.FromSeconds(0.5);

        private readonly AsyncReaderWriterLock dbLock = new AsyncReaderWriterLock();
        private readonly HashSet<string> detailsColumns = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);
        private readonly CancellationTokenSource disposeCancel = new CancellationTokenSource();
        private readonly ChangeQueue<string, (int attempt, string path, FilePath filePath, HashInfo hashInfo, bool? hasDetails, bool forceRehash)> fileChangeQueue;
        private readonly Dictionary<string, FileSystemWatcher> fileSystemWatchers = new Dictionary<string, FileSystemWatcher>();
        private readonly List<string> indexedPaths = new List<string>();
        private readonly string indexPath;
        private readonly WeakReferenceCache<long, Person> personCache = new WeakReferenceCache<long, Person>();
        private readonly ChangeQueue<string, (int attempt, string path, bool forceRehash)> rescanQueue;
        private readonly WeakReferenceCache<string, SearchResult> searchResultsCache = new WeakReferenceCache<string, SearchResult>();
        private readonly TagRulesParser tagRuleParser;
        private Task fileScanTask;
        private Task indexRescanTask;

        public MediaIndex(string indexPath)
        {
            this.indexPath = indexPath;
            this.tagRuleParser = new TagRulesParser();
            this.fileChangeQueue = new ChangeQueue<string, (int attempt, string path, FilePath filePath, HashInfo hashInfo, bool? hasDetails, bool forceRehash)>(
                getKey: item => item.path,
                merge: (a, b) =>
                {
                    a.attempt += b.attempt;
                    a.filePath = b.filePath ?? a.filePath;
                    a.hashInfo = b.hashInfo ?? a.hashInfo;
                    a.hasDetails = b.hasDetails ?? a.hasDetails;
                    a.forceRehash |= b.forceRehash;
                    return a;
                });

            this.rescanQueue = new ChangeQueue<string, (int attempt, string path, bool forceRehash)>(
                getKey: item => item.path,
                merge: (a, b) =>
                {
                    a.attempt += b.attempt;
                    a.forceRehash |= b.forceRehash;
                    return a;
                });
        }

        public event EventHandler<HashInvalidatedEventArgs> HashInvalidated;

        public event EventHandler<ItemAddedEventArgs<(HashPerson hash, Person person)>> HashPersonAdded;

        public event EventHandler<ItemRemovedEventArgs<HashPerson>> HashPersonRemoved;

        public event EventHandler<ItemAddedEventArgs<HashTag>> HashTagAdded;

        public event EventHandler<ItemRemovedEventArgs<HashTag>> HashTagRemoved;

        public event EventHandler<ItemAddedEventArgs<PersonTag>> PersonTagAdded;

        public event EventHandler<ItemRemovedEventArgs<PersonTag>> PersonTagRemoved;

        public event EventHandler<ItemUpdatedEventArgs<Rating>> RatingUpdated;

        public event EventHandler<ItemUpdatedEventArgs<RescanProgress>> RescanProgressUpdated;

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

            return new HashInfo(sb.ToString(), fileSize, FileTypeRecognizer.GetType(recognizerState), FileTypeRecognizer.Version);
        }

        public Task<Alias> AddAlias(Alias alias) =>
            this.IndexUpdateAsync(async conn => (await conn.QueryAsync<Alias>(Alias.Queries.AddAlias, alias).ConfigureAwait(false)).Single());

        public async Task AddHashPerson(HashPerson hashPerson)
        {
            if (hashPerson == null)
            {
                throw new ArgumentNullException(nameof(hashPerson));
            }

            await this.IndexWriteAsync(conn => conn.ExecuteAsync(HashPerson.Queries.AddHashPerson, hashPerson)).ConfigureAwait(false);

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

            await this.IndexWriteAsync(conn => conn.ExecuteAsync(HashTag.Queries.AddHashTag, hashTag)).ConfigureAwait(false);

            var hash = hashTag.Hash;
            if (this.searchResultsCache.TryGetValue(hash, out var searchResult) && !searchResult.Tags.Contains(hashTag.Tag))
            {
                searchResult.Tags = searchResult.Tags.Add(hashTag.Tag);
            }

            this.HashInvalidated?.Invoke(this, new HashInvalidatedEventArgs(hash));
            this.HashTagAdded?.Invoke(this, new ItemAddedEventArgs<HashTag>(hashTag));
        }

        public async Task AddIndexedPath(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentNullException(nameof(path));
            }

            if (!Path.IsPathRooted(path))
            {
                throw new ArgumentOutOfRangeException(nameof(path));
            }

            await this.IndexWriteAsync(conn => conn.ExecuteAsync(Queries.AddIndexedPath, new { Path = path, PathRaw = PathEncoder.GetPathRaw(path) })).ConfigureAwait(false);

            lock (this.indexedPaths)
            {
                this.indexedPaths.Add(path);
            }

            this.rescanQueue.Enqueue((0, path, false));
            this.StartIndexRescanTask();

            lock (this.fileSystemWatchers)
            {
                this.AddFileSystemWatcher(path);
            }
        }

        public Task<Person> AddPerson(string name) =>
            this.IndexUpdateAsync(async conn =>
            {
                var person = (await conn.QueryAsync<Person>(Person.Queries.AddPerson, new { Name = name }).ConfigureAwait(false)).Single();
                person.Aliases = ImmutableHashSet<Alias>.Empty;
                return this.personCache.AddOrUpdate(
                    person.PersonId,
                    id => person,
                    (id, p) =>
                    {
                        p.Name = person.Name;
                        p.Aliases = person.Aliases;
                    });
            });

        public async Task AddPersonTag(PersonTag personTag)
        {
            if (personTag == null)
            {
                throw new ArgumentNullException(nameof(personTag));
            }

            await this.IndexWriteAsync(conn => conn.ExecuteAsync(PersonTag.Queries.AddPersonTag, personTag)).ConfigureAwait(false);

            this.PersonTagAdded?.Invoke(this, new ItemAddedEventArgs<PersonTag>(personTag));
        }

        public Task<SavedSearch> AddSavedSearch(string name, string query) =>
            this.IndexUpdateAsync(async conn => (await conn.QueryAsync<SavedSearch>(SavedSearch.Queries.AddSavedSearch, new { Name = name, Query = query }).ConfigureAwait(false)).Single());

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
            this.IndexReadAsync(async conn =>
                (await conn.QueryAsync<Alias>(Alias.Queries.GetAliasesByPersonId, new { PersonId = personId }).ConfigureAwait(false)).ToList());

        public Task<List<Alias>> GetAllAliasesForSite(string site, string name) =>
            this.IndexReadAsync(async conn =>
                (await conn.QueryAsync<Alias>(Alias.Queries.GetAliasesBySiteAndName, new { Name = name, Site = site }).ConfigureAwait(false)).ToList());

        public Task<List<Alias>> GetAllAliasesForSite(string site) =>
            this.IndexReadAsync(async conn =>
                (await conn.QueryAsync<Alias>(Alias.Queries.GetAliasesBySite, new { Site = site }).ConfigureAwait(false)).ToList());

        public Task<string[]> GetAllAliasSites() =>
            this.IndexReadAsync(async conn =>
                (await conn.QueryAsync<string>(Alias.Queries.GetAllSites).ConfigureAwait(false)).ToArray());

        public Task<List<string>> GetAllHashTags() =>
            this.IndexReadAsync(async conn =>
                (await conn.QueryAsync<string>(HashTag.Queries.GetAllHashTags).ConfigureAwait(false)).ToList());

        public Task<List<Person>> GetAllPeople() =>
            this.IndexReadAsync(async conn =>
                (await this.ReadPeopleAsync(conn.QueryMultiple(Person.Queries.GetAllPeople)).ConfigureAwait(false)).ToList());

        public Task<List<string>> GetAllPersonTags() =>
            this.IndexReadAsync(async conn =>
                (await conn.QueryAsync<string>(PersonTag.Queries.GetAllPersonTags).ConfigureAwait(false)).ToList());

        public Task<List<string>> GetAllRatingCategories() =>
            this.IndexReadAsync(async conn =>
                (await conn.QueryAsync<string>(Rating.Queries.GetRatingCategories).ConfigureAwait(false)).ToList());

        public Task<List<RuleCategory>> GetAllRuleCategories() =>
            this.IndexReadAsync(async conn =>
                (await conn.QueryAsync<RuleCategory>(RuleCategory.Queries.GetAllRuleCategories).ConfigureAwait(false)).ToList());

        public Task<List<SavedSearch>> GetAllSavedSearches() =>
             this.IndexReadAsync(async conn =>
                (await conn.QueryAsync<SavedSearch>(SavedSearch.Queries.GetSavedSearches).ConfigureAwait(false)).ToList());

        public Task<ImmutableDictionary<string, object>> GetHashDetails(string hash) =>
            this.IndexReadAsync(async conn =>
            {
                var row = (await conn.QueryAsync(Queries.GetHashDetails, new { Hash = hash }).ConfigureAwait(false)).SingleOrDefault();
                return ((IDictionary<string, object>)row)?.ToImmutableDictionary() ?? ImmutableDictionary<string, object>.Empty;
            });

        public Task<List<HashTag>> GetHashTags(string hash) =>
            this.IndexReadAsync(async conn =>
                (await conn.QueryAsync<HashTag>(HashTag.Queries.GetHashTags, new { Hash = hash }).ConfigureAwait(false)).ToList());

        public Task<Person> GetPersonById(long personId) =>
            this.personCache.TryGetValue(personId, out var person)
                ? Task.FromResult(person)
                : this.IndexReadAsync(async conn =>
                {
                    var reader = await conn.QueryMultipleAsync(Person.Queries.GetPersonById, new { PersonId = personId }).ConfigureAwait(false);
                    person = (await this.ReadPeopleAsync(reader).ConfigureAwait(false)).SingleOrDefault();
                    return this.personCache.GetOrAdd(personId, _ => person);
                });

        public Task<List<PersonTag>> GetPersonTags(int personId) =>
            this.IndexReadAsync(async conn =>
                (await conn.QueryAsync<PersonTag>(PersonTag.Queries.GetPersonTags, new { PersonId = personId }).ConfigureAwait(false)).ToList());

        public Task<Rating> GetRating(string hash, string category) =>
            this.IndexReadAsync(conn =>
                conn.QuerySingleOrDefaultAsync<Rating>(Rating.Queries.GetRating, new { Hash = hash, Category = category ?? string.Empty }));

        public Task<List<HashTag>> GetRejectedHashTags(string hash) =>
            this.IndexReadAsync(async conn =>
                (await conn.QueryAsync<HashTag>(HashTag.Queries.GetRejectedHashTags, new { Hash = hash }).ConfigureAwait(false)).ToList());

        public Task<List<PersonTag>> GetRejectedPersonTags(int personId) =>
            this.IndexReadAsync(async conn =>
                (await conn.QueryAsync<PersonTag>(PersonTag.Queries.GetRejectedPersonTags, new { PersonId = personId }).ConfigureAwait(false)).ToList());

        public async Task Initialize()
        {
            await this.IndexWriteAsync(async conn =>
            {
                await conn.ExecuteAsync(Queries.CreateSchema).ConfigureAwait(false);
                if (!(await conn.QueryAsync<string>("SELECT name FROM pragma_table_info('TagRules') WHERE name = 'Category'").ConfigureAwait(false)).Any())
                {
                    using (var tran = conn.BeginTransaction())
                    {
                        await conn.ExecuteAsync(Queries.CreateSchema_01_AddTagRuleCategories, transaction: tran).ConfigureAwait(false);
                        tran.Commit();
                    }
                }

                if (!(await conn.QueryAsync<string>("SELECT name FROM pragma_table_info('HashInfo') WHERE name = 'Version'").ConfigureAwait(false)).Any())
                {
                    using (var tran = conn.BeginTransaction())
                    {
                        await conn.ExecuteAsync(Queries.CreateSchema_02_AddHashInfoVersion, transaction: tran).ConfigureAwait(false);
                        tran.Commit();
                    }
                }
            }).ConfigureAwait(false);

            var ruleCategories = await this.GetAllRuleCategories().ConfigureAwait(false);
            this.TagEngine = new TagRuleEngine(ruleCategories.SelectMany(c => this.tagRuleParser.Parse(c.Rules)));
            var indexedPaths = await this.GetIndexedPaths().ConfigureAwait(false);
            lock (this.indexedPaths)
            {
                lock (this.fileSystemWatchers)
                {
                    foreach (var path in indexedPaths)
                    {
                        this.indexedPaths.Add(path);
                        this.AddFileSystemWatcher(path);
                    }
                }
            }
        }

        public bool IsPathIndexed(string fullPath)
        {
            lock (this.indexedPaths)
            {
                // TODO: Convert to a Trie.
                // TODO: Case insensitive on such filesystems.
                if (this.indexedPaths.Any(p => fullPath.StartsWith(p)))
                {
                    return true;
                }
            }

            return false;
        }

        public async Task MergePeople(int targetId, int duplicateId)
        {
            if (targetId == duplicateId)
            {
                throw new ArgumentOutOfRangeException(nameof(duplicateId));
            }

            await this.IndexWriteAsync(conn => conn.ExecuteAsync(Person.Queries.MergePeople, new { TargetId = targetId, DuplicateId = duplicateId })).ConfigureAwait(false);
            this.personCache.Remove(duplicateId);
            this.personCache.Remove(targetId);
            var target = await this.GetPersonById(targetId).ConfigureAwait(false);
            var updatedResults = new HashSet<string>();
            this.searchResultsCache.UpdateAll((key, value) =>
            {
                var toReplace = value.People.Where(p => p.PersonId == targetId || p.PersonId == duplicateId).ToList();
                if (toReplace.Count > 0)
                {
                    var newPeople = value.People.Except(toReplace).Add(target);
                    value.People = newPeople;
                    updatedResults.Add(key);
                }
            });

            foreach (var hash in updatedResults)
            {
                this.HashInvalidated?.Invoke(this, new HashInvalidatedEventArgs(hash));
            }
        }

        public Task RemoveAlias(Alias alias) => this.IndexWriteAsync(conn => conn.ExecuteAsync(Alias.Queries.RemoveAlias, alias));

        public Task RemoveFilePath(string path) =>
            this.IndexWriteAsync(conn =>
                conn.ExecuteAsync(FilePath.Queries.RemoveFilePathByPath, new { Path = path, PathRaw = PathEncoder.GetPathRaw(path) }));

        public async Task RemoveHashPerson(HashPerson hashPerson, bool rejectPerson = false)
        {
            if (hashPerson == null)
            {
                throw new ArgumentNullException(nameof(hashPerson));
            }

            await this.IndexWriteAsync(conn => conn.ExecuteAsync(rejectPerson ? HashPerson.Queries.RejectHashPerson : HashPerson.Queries.RemoveHashPerson, hashPerson)).ConfigureAwait(false);

            var hash = hashPerson.Hash;
            if (this.searchResultsCache.TryGetValue(hash, out var searchResult) && searchResult.People.FirstOrDefault(p => p.PersonId == hashPerson.PersonId) is Person person)
            {
                // TODO: Use Whisk to do this atomically.
                searchResult.People = searchResult.People.Remove(person);
                if (rejectPerson)
                {
                    searchResult.RejectedPeople = searchResult.People.Add(person);
                }
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

            await this.IndexWriteAsync(conn => conn.ExecuteAsync(rejectTag ? HashTag.Queries.RejectHashTag : HashTag.Queries.RemoveHashTag, hashTag)).ConfigureAwait(false);

            var hash = hashTag.Hash;
            if (this.searchResultsCache.TryGetValue(hash, out var searchResult) && searchResult.Tags.Contains(hashTag.Tag))
            {
                // TODO: Use Whisk to do this atomically.
                searchResult.Tags = searchResult.Tags.Remove(hashTag.Tag);
                if (rejectTag)
                {
                    searchResult.RejectedTags.Add(hashTag.Tag);
                }
            }

            this.HashInvalidated?.Invoke(this, new HashInvalidatedEventArgs(hash));
            this.HashTagRemoved?.Invoke(this, new ItemRemovedEventArgs<HashTag>(hashTag));
        }

        public async Task RemoveRejectedHashTag(HashTag hashTag)
        {
            if (hashTag == null)
            {
                throw new ArgumentNullException(nameof(hashTag));
            }

            await this.IndexWriteAsync(conn => conn.ExecuteAsync(HashTag.Queries.RemoveRejectedHashTag, hashTag)).ConfigureAwait(false);

            var hash = hashTag.Hash;
            if (this.searchResultsCache.TryGetValue(hash, out var searchResult) && searchResult.RejectedTags.Contains(hashTag.Tag))
            {
                searchResult.RejectedTags = searchResult.RejectedTags.Remove(hashTag.Tag);
            }

            this.HashInvalidated?.Invoke(this, new HashInvalidatedEventArgs(hash));
            this.HashTagRemoved?.Invoke(this, new ItemRemovedEventArgs<HashTag>(hashTag));
        }

        public async Task RemoveIndexedPath(string path)
        {
            this.RemoveFileSystemWatcher(path);
            await this.IndexWriteAsync(conn => conn.ExecuteAsync(Queries.RemoveIndexedPath, new { Path = path, PathRaw = PathEncoder.GetPathRaw(path) })).ConfigureAwait(false);
        }

        public Task RemovePerson(Person person) => this.IndexWriteAsync(conn => conn.ExecuteAsync(Person.Queries.RemovePerson, new { person.PersonId }));

        public async Task RemovePersonTag(PersonTag personTag, bool rejectTag = false)
        {
            if (personTag == null)
            {
                throw new ArgumentNullException(nameof(personTag));
            }

            await this.IndexWriteAsync(conn => conn.ExecuteAsync(rejectTag ? PersonTag.Queries.RejectPersonTag : PersonTag.Queries.RemovePersonTag, personTag)).ConfigureAwait(false);

            this.PersonTagRemoved?.Invoke(this, new ItemRemovedEventArgs<PersonTag>(personTag));
        }

        public Task RemoveSavedSearch(SavedSearch savedSearch) =>
            this.IndexWriteAsync(conn =>
                conn.ExecuteAsync(SavedSearch.Queries.RemoveSavedSearch, new { savedSearch.SearchId }));

        public async Task Rescan(bool forceRehash = false)
        {
            var indexedPaths = await this.GetIndexedPaths().ConfigureAwait(false);

            foreach (var path in indexedPaths)
            {
                this.rescanQueue.Enqueue((0, path, forceRehash));
            }

            await this.StartIndexRescanTask().ConfigureAwait(false);
        }

        public async Task<Expression> CompileQuery(string query, bool excludeHidden = true)
        {
            var grammar = new SearchGrammar();
            var term = grammar.Parse(query ?? string.Empty);
            var containsSavedSearches = new ContainsSavedSearchTermCompiler().Compile(term);
            var savedSearches = containsSavedSearches
                ? (await this.GetAllSavedSearches().ConfigureAwait(false)).ToDictionary(s => s.Name, StringComparer.CurrentCultureIgnoreCase)
                : null;
            var dialect = new SearchDialect(this.TagEngine, name => savedSearches.TryGetValue(name, out var search) ? grammar.Parse(search.Query) : null);
            return dialect.CompileQuery(term, excludeHidden);
        }

        public async Task<TQuery> CompileQuery<TCompiler, TQuery>(string query, bool excludeHidden = true)
            where TCompiler : SearchCompiler<TQuery>, new() =>
                await this.CompileQuery<TCompiler, TQuery>(await this.CompileQuery(query, excludeHidden).ConfigureAwait(false)).ConfigureAwait(false);

        public async Task<TQuery> CompileQuery<TCompiler, TQuery>(Expression query)
            where TCompiler : SearchCompiler<TQuery>, new()
        {
            return new TCompiler().CompileQuery(query);
        }

        public async Task<List<SearchResult>> SearchIndex(string query, bool excludeHidden = true) =>
            await this.SearchIndex(await this.CompileQuery(query, excludeHidden).ConfigureAwait(false)).ConfigureAwait(false);

        public Task<List<SearchResult>> SearchIndex(Expression query)
        {
            return Task.Run(async () =>
            {
                var sqlQuery = await this.CompileQuery<SqlSearchCompiler, string>(query).ConfigureAwait(false);

                using (var conn = await this.GetConnection().ConfigureAwait(false))
                {
                    ILookup<string, HashTag> tags;
                    ILookup<string, HashTag> rejectedTags;
                    ILookup<string, FilePath> fileNames;
                    Dictionary<int, Person> peopleLookup;
                    ILookup<string, HashPerson> people;
                    ILookup<string, HashPerson> rejectedPeople;
                    ILookup<string, IDictionary<string, object>> hashDetails;
                    ILookup<string, Rating> hashRatings;
                    IList<HashInfo> hashes;
                    using (await this.dbLock.ReaderLockAsync())
                    {
                        var reader = await conn.QueryMultipleAsync(sqlQuery).ConfigureAwait(false);
                        tags = (await reader.ReadAsync<HashTag>(buffered: false).ConfigureAwait(false)).ToLookup(f => f.Hash);
                        rejectedTags = (await reader.ReadAsync<HashTag>(buffered: false).ConfigureAwait(false)).ToLookup(f => f.Hash);
                        fileNames = (await reader.ReadAsync<FilePath>(buffered: false).ConfigureAwait(false)).ToLookup(f => f.LastHash);
                        peopleLookup = (await this.ReadPeopleAsync(reader).ConfigureAwait(false)).ToDictionary(p => p.PersonId);
                        people = (await reader.ReadAsync<HashPerson>(buffered: false).ConfigureAwait(false)).ToLookup(f => f.Hash);
                        rejectedPeople = (await reader.ReadAsync<HashPerson>(buffered: false).ConfigureAwait(false)).ToLookup(f => f.Hash);
                        hashDetails = (await reader.ReadAsync(buffered: false).ConfigureAwait(false)).ToLookup(r => (string)r.Hash, r => (IDictionary<string, object>)r);
                        hashRatings = (await reader.ReadAsync<Rating>(buffered: false).ConfigureAwait(false)).ToLookup(r => r.Hash);
                        hashes = (await reader.ReadAsync<HashInfo>(buffered: false).ConfigureAwait(false)).ToList();
                    }

                    var results = new List<SearchResult>();
                    foreach (var hash in hashes)
                    {
                        var updatedTags = tags[hash.Hash].Select(t => t.Tag).ToImmutableHashSet();
                        var updatedRejectedTags = rejectedTags[hash.Hash].Select(t => t.Tag).ToImmutableHashSet();
                        var updatedPaths = fileNames[hash.Hash].Select(t => t.Path).ToImmutableHashSet();
                        var updatedPeople = people[hash.Hash].Select(p => peopleLookup[p.PersonId]).ToImmutableHashSet();
                        var updatedRejectedPeople = rejectedPeople[hash.Hash].Select(p => peopleLookup[p.PersonId]).ToImmutableHashSet();
                        var updatedDetails = hashDetails[hash.Hash].SingleOrDefault()?.ToImmutableDictionary() ?? ImmutableDictionary<string, object>.Empty;
                        var updatedRating = hashRatings[hash.Hash].Where(p => string.IsNullOrEmpty(p.Category)).SingleOrDefault();
                        results.Add(this.searchResultsCache.AddOrUpdate(
                            hash.Hash,
                            key => new SearchResult(
                                key,
                                hash.FileSize,
                                hash.FileType,
                                updatedDetails,
                                updatedRating,
                                updatedTags,
                                updatedRejectedTags,
                                updatedPaths,
                                updatedPeople,
                                updatedRejectedPeople),
                            (key, searchResult) =>
                            {
                                // TODO: Deeper inspection of changes.
                                // TODO: Trigger Change Events?
                                if (searchResult.Details != updatedDetails)
                                {
                                    searchResult.Details = updatedDetails;
                                }

                                if (searchResult.Rating != updatedRating)
                                {
                                    searchResult.Rating = updatedRating;
                                }

                                if (!searchResult.Tags.SetEquals(updatedTags))
                                {
                                    searchResult.Tags = updatedTags;
                                }

                                if (!searchResult.RejectedTags.SetEquals(updatedRejectedTags))
                                {
                                    searchResult.RejectedTags = updatedRejectedTags;
                                }

                                if (!searchResult.Paths.SetEquals(updatedPaths))
                                {
                                    searchResult.Paths = updatedPaths;
                                }

                                if (!searchResult.People.SetEquals(updatedPeople))
                                {
                                    searchResult.People = updatedPeople;
                                }

                                if (!searchResult.RejectedPeople.SetEquals(updatedRejectedPeople))
                                {
                                    searchResult.RejectedPeople = updatedRejectedPeople;
                                }
                            }));
                    }

                    return results;
                }
            });
        }

        public Task UpdatePerson(Person person) =>
            this.IndexWriteAsync(conn => conn.ExecuteAsync(Person.Queries.UpdatePerson, new { person.PersonId, person.Name }));

        public async Task UpdateRating(Rating rating)
        {
            await this.IndexWriteAsync(conn => conn.ExecuteAsync(Rating.Queries.UpdateRating, rating)).ConfigureAwait(false);

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
            this.IndexWriteAsync(conn =>
                conn.ExecuteAsync(SavedSearch.Queries.UpdateSavedSearch, savedSearch));

        public async Task UpdateTagRules(IList<RuleCategory> ruleCategories)
        {
            var updatedEngine = new TagRuleEngine(ruleCategories.SelectMany(c => this.tagRuleParser.Parse(c.Rules)));
            await this.IndexWriteAsync(async conn =>
            {
                using (var tran = conn.BeginTransaction())
                {
                    await conn.ExecuteAsync(RuleCategory.Queries.ClearRuleCategories, transaction: tran).ConfigureAwait(false);
                    foreach (var c in ruleCategories)
                    {
                        await conn.ExecuteAsync(RuleCategory.Queries.AddRuleCategory, c).ConfigureAwait(false);
                    }

                    tran.Commit();
                }
            }).ConfigureAwait(false);
            this.TagEngine = updatedEngine;
            this.TagRulesUpdated?.Invoke(this, new ItemUpdatedEventArgs<TagRuleEngine>(updatedEngine));
        }

        private static Task StartQueueScan<TKey, TValue>(ChangeQueue<TKey, TValue> changeQueue, Func<Task> getTask, Action<Task> setTask, Func<TValue, Task> run)
        {
            lock (changeQueue)
            {
                var existingTask = getTask();
                if (existingTask != null)
                {
                    return existingTask;
                }

                var maxTasks = Math.Max(1, Environment.ProcessorCount - 1);
                var maxDelay = TimeSpan.FromSeconds(1);
                existingTask = Task.Run(async () =>
                {
                    var tasks = new Dictionary<Task, TValue>();
                    while (true)
                    {
                        while (tasks.Count < maxTasks && changeQueue.Dequeue(out var next))
                        {
                            var task = run(next);
                            tasks.Add(task, next);
                        }

                        if (tasks.Count > 0)
                        {
                            var timeoutTask = Task.Delay(maxDelay);
                            var completed = await Task.WhenAny(tasks.Keys.Concat(new[] { timeoutTask }).ToArray()).ConfigureAwait(false);

                            if (completed != timeoutTask)
                            {
                                var item = tasks[completed];
                                tasks.Remove(completed);

                                if (completed.IsFaulted)
                                {
                                    // TODO: Retry.
                                    ////if (item.attempt < 4)
                                    ////{
                                    ////    var delay = TimeSpan.FromSeconds(maxDelay.TotalSeconds * Math.Pow(2, item.attempt));
                                    ////    item.attempt++;
                                    ////    this.fileChangeQueue.Enqueue(item, delay);
                                    ////}
                                }
                            }
                        }
                        else
                        {
                            var delay = changeQueue.CurrentDelay;
                            if (delay != null)
                            {
                                if (delay > maxDelay)
                                {
                                    delay = maxDelay;
                                }

                                await Task.Delay(delay.Value).ConfigureAwait(false);
                            }
                            else
                            {
                                // TODO: Trigger cancellation when the changeQueue has a new item.
                                await Task.Delay(maxDelay).ConfigureAwait(false);
                                if (changeQueue.Count == 0)
                                {
                                    return;
                                }
                            }
                        }
                    }
                });

                existingTask = existingTask.ContinueWith(t =>
                {
                    if (t.Status == TaskStatus.Faulted)
                    {
                        Debug.WriteLine($"ScanTask Exception:{Environment.NewLine}{t.Exception}");
                    }

                    lock (changeQueue)
                    {
                        setTask(null);
                    }
                }, TaskScheduler.Current);
                setTask(existingTask);

                return existingTask;
            }
        }

        private async Task AddFilePath(FilePath filePath) =>
            await this.IndexWriteAsync(conn =>
                conn.ExecuteAsync(FilePath.Queries.AddFilePath, filePath)).ConfigureAwait(false);

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
                watcher.NotifyFilter =
                    NotifyFilters.Attributes |
                    NotifyFilters.CreationTime |
                    NotifyFilters.DirectoryName |
                    NotifyFilters.FileName |
                    NotifyFilters.LastWrite |
                    NotifyFilters.Security |
                    NotifyFilters.Size;
                watcher.Changed += this.Watcher_Changed;
                watcher.Deleted += this.Watcher_Deleted;
                watcher.Created += this.Watcher_Created;
                watcher.Renamed += this.Watcher_Renamed;
                watcher.Error += this.Watcher_Error;
                watcher.EnableRaisingEvents = true;
                this.fileSystemWatchers.Add(path, watcher);
                watcher = null;
            }
            finally
            {
                watcher?.Dispose();
            }
        }

        private async Task<SQLiteConnection> GetConnection(bool readOnly = false)
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

        private Task<FilePath> GetFilePath(string path) =>
            this.IndexReadAsync(conn =>
                conn.QuerySingleOrDefaultAsync<FilePath>(FilePath.Queries.GetFilePathByPath, new { Path = path, PathRaw = PathEncoder.GetPathRaw(path) }));

        private Task<FilePath> GetFilePaths(string hash) =>
            this.IndexReadAsync(conn =>
                conn.QuerySingleOrDefaultAsync<FilePath>(FilePath.Queries.GetFilePathsByHash, new { Hash = hash }));

        private Task<HashInfo> GetHashInfo(string hash) =>
            this.IndexReadAsync(conn =>
                conn.QuerySingleOrDefaultAsync<HashInfo>(HashInfo.Queries.GetHashInfo, new { Hash = hash }));

        private Task<List<string>> GetIndexedPaths() =>
            this.IndexReadAsync(async conn =>
                conn.Query<string, byte[], string>(Queries.GetIndexedPaths, (path, pathRaw) => PathEncoder.GetPath(path, pathRaw), splitOn: "PathRaw", buffered: false).ToList());

        private Task<List<(FilePath filePath, HashInfo hashInfo, bool hasDetails)>> GetIndexInfoUnder(string path) =>
            this.IndexReadAsync(async conn =>
            {
                var reader = await conn.QueryMultipleAsync(FilePath.Queries.GetFilePathsUnder, new { Path = QueryBuilder.EscapeLike(path) }).ConfigureAwait(false);
                var hashInfo = reader.Read<HashInfo, long, (HashInfo, bool)>((hash, hasDetails) => (hash, hasDetails != 0), splitOn: "HasHashDetails", buffered: false).ToDictionary(h => h.Item1.Hash);
                return (await reader.ReadAsync<FilePath>(buffered: false).ConfigureAwait(false)).Select(p =>
                {
                    if (hashInfo.TryGetValue(p.LastHash, out var hash))
                    {
                        return (p, hash.Item1, hash.Item2);
                    }

                    return (p, null, false);
                }).ToList();
            });

        private async Task<T> IndexReadAsync<T>(Func<SQLiteConnection, Task<T>> query)
        {
            using (var conn = await this.GetConnection(readOnly: true).ConfigureAwait(false))
            using (await this.dbLock.ReaderLockAsync().ConfigureAwait(false))
            {
                return await query(conn).ConfigureAwait(false);
            }
        }

        private async Task<T> IndexUpdateAsync<T>(Func<SQLiteConnection, Task<T>> query)
        {
            await Task.Yield();
            using (var conn = await this.GetConnection(readOnly: false).ConfigureAwait(false))
            using (await this.dbLock.WriterLockAsync().ConfigureAwait(false))
            {
                return await query(conn).ConfigureAwait(false);
            }
        }

        private async Task IndexWriteAsync(Func<SQLiteConnection, Task> query)
        {
            using (var conn = await this.GetConnection(readOnly: false).ConfigureAwait(false))
            using (await this.dbLock.WriterLockAsync().ConfigureAwait(false))
            {
                await query(conn).ConfigureAwait(false);
            }
        }

        private async Task<IEnumerable<Person>> ReadPeopleAsync(SqlMapper.GridReader reader)
        {
            var aliases = (await reader.ReadAsync<Alias>(buffered: false).ConfigureAwait(false)).ToLookup(f => f.PersonId);
            var people = await reader.ReadAsync<Person>(buffered: false).ConfigureAwait(false);

            return people.Select(f =>
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

        private async Task RemoveFile(string fullPath)
        {
            await this.RemoveFilePath(fullPath).ConfigureAwait(false);
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
            await Task.Yield();

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

                var previousHash = hashInfo?.Hash;
                if (hashInfo == null || hashInfo.Version < FileTypeRecognizer.Version || forceRehash || !(hasDetails ?? false))
                {
                    using (var file = File.Open(extendedPath, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        hashInfo = await HashFileAsync(file).ConfigureAwait(false);
                        await this.IndexWriteAsync(conn => conn.ExecuteAsync(HashInfo.Queries.AddHashInfo, hashInfo)).ConfigureAwait(false); // TODO: Get hasDetails here.

                        if (hashInfo.Hash == previousHash)
                        {
                            this.searchResultsCache.TryUpdate(
                                hashInfo.Hash,
                                (_, searchResult) =>
                                {
                                    searchResult.FileType = hashInfo.FileType;
                                });
                        }
                        else
                        {
                            if (previousHash != null)
                            {
                                this.searchResultsCache.TryUpdate(
                                    previousHash,
                                    (_, searchResult) =>
                                    {
                                        searchResult.Paths = searchResult.Paths.Remove(path);
                                    });
                            }

                            this.searchResultsCache.TryUpdate(
                                hashInfo.Hash,
                                (_, searchResult) =>
                                {
                                    searchResult.Paths = searchResult.Paths.Add(path);
                                });
                        }

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

        private Task RescanIndexedPath(string path, bool forceRehash = false)
        {
            return Task.Run(async () =>
            {
                var started = false;
                var seen = new HashSet<string>();
                void Scan(int attempt, string file, FilePath filePath, HashInfo hashInfo, bool? hasDetails)
                {
                    this.fileChangeQueue.Enqueue((0, file, filePath, hashInfo, hasDetails, forceRehash));
                    if (!started)
                    {
                        this.StartFileScanTask();
                        started = true;
                    }
                }

                foreach (var (filePath, hashInfo, hasDetails) in await this.GetIndexInfoUnder(path).ConfigureAwait(false))
                {
                    seen.Add(filePath.Path);
                    Scan(0, filePath.Path, filePath, hashInfo, hasDetails);
                }

                try
                {
                    foreach (var file in FileEnumerable.EnumerateFiles(path, "*", SearchOption.AllDirectories))
                    {
                        if (seen.Add(file))
                        {
                            Scan(0, file, null, null, null);
                        }
                    }
                }
                catch (IOException)
                {
                }
            });
        }

        private Task StartFileScanTask()
        {
            lock (this.fileChangeQueue)
            {
                if (this.fileScanTask != null)
                {
                    return this.fileScanTask;
                }

                var maxTasks = Math.Max(1, Environment.ProcessorCount - 1);
                var maxDelay = TimeSpan.FromSeconds(1);
                var progress = new RescanProgress(0, 0, 0, false);
                var progressStopwatch = Stopwatch.StartNew();
                var progressThreshold = TimeSpan.FromSeconds(1 / 15.0);
                void UpdateProgress(Func<RescanProgress, RescanProgress> update, bool force = false)
                {
                    progress = update(progress);
                    if (force || progressStopwatch.Elapsed > progressThreshold)
                    {
                        this.RescanProgressUpdated?.Invoke(this, new ItemUpdatedEventArgs<RescanProgress>(progress));
                        progressStopwatch.Restart();
                    }
                }

                this.fileScanTask = Task.Run(async () =>
                {
                    UpdateProgress(p => p, force: true);

                    var tasks = new Dictionary<Task, (int attempt, string path, FilePath filePath, HashInfo hashInfo, bool? hasDetails, bool forceRehash)>();
                    while (true)
                    {
                        while (tasks.Count < maxTasks && this.fileChangeQueue.Dequeue(out var next))
                        {
                            var task = this.RescanFile(next.path, next.filePath, next.hashInfo, next.hasDetails, next.forceRehash);
                            tasks.Add(task, next);
                        }

                        UpdateProgress(p =>
                            p.Update(
                                pathsDiscovered: p.PathsProcessed + tasks.Count + this.fileChangeQueue.Count));

                        if (tasks.Count > 0)
                        {
                            var timeoutTask = Task.Delay(maxDelay);
                            var completed = await Task.WhenAny(tasks.Keys.Concat(new[] { timeoutTask }).ToArray()).ConfigureAwait(false);

                            if (completed != timeoutTask)
                            {
                                var item = tasks[completed];
                                tasks.Remove(completed);

                                try
                                {
                                    await completed.ConfigureAwait(false);
                                    UpdateProgress(p =>
                                        p.Update(
                                            pathsProcessed: p.PathsProcessed + 1));
                                }
                                catch (SystemException ex)
                                {
                                    if (item.attempt < 4)
                                    {
                                        var delay = TimeSpan.FromSeconds(maxDelay.TotalSeconds * Math.Pow(2, item.attempt));
                                        item.attempt++;
                                        this.fileChangeQueue.Enqueue(item, delay);
                                    }
                                    else
                                    {
                                        UpdateProgress(p =>
                                            p.Update(
                                                pathsProcessed: p.PathsProcessed + 1));
                                    }
                                }
                            }
                        }
                        else
                        {
                            var delay = this.fileChangeQueue.CurrentDelay;
                            if (delay != null)
                            {
                                if (delay > maxDelay)
                                {
                                    delay = maxDelay;
                                }

                                await Task.Delay(delay.Value).ConfigureAwait(false);
                            }
                            else
                            {
                                // TODO: Trigger cancellation when the fileChangeQueue has a new item.
                                await Task.Delay(maxDelay).ConfigureAwait(false);
                                if (this.fileChangeQueue.Count == 0)
                                {
                                    UpdateProgress(p =>
                                        p.Update(
                                            pathsDiscovered: p.PathsProcessed,
                                            discoveryComplete: true),
                                        force: true);

                                    return;
                                }
                            }
                        }
                    }
                });

                this.fileScanTask.ContinueWith(t =>
                {
                    lock (this.fileChangeQueue)
                    {
                        this.fileScanTask = null;
                    }
                }, TaskScheduler.Current);

                return this.fileScanTask;
            }
        }

        private Task StartIndexRescanTask() =>
            StartQueueScan(
                this.rescanQueue,
                () => this.indexRescanTask,
                task => this.indexRescanTask = task,
                value => this.RescanIndexedPath(value.path, value.forceRehash));

        private void TouchFile(string fullPath, bool forceRehash = true, TimeSpan delay = default)
        {
            // TODO: This should detect if the path is inside some indexed folder.
            this.fileChangeQueue.Enqueue((0, fullPath, null, null, null, forceRehash), delay);
            this.StartFileScanTask();
        }

        private async Task UpdateHashDetails(HashInfo hashInfo, FileStream file)
        {
            Dictionary<string, object> details = null;
            try
            {
                if (FileTypeHelper.IsImage(hashInfo.FileType))
                {
                    using (var image = Image.FromStream(file))
                    {
                        details = ImageDetailRecognizer.Recognize(image);
                    }
                }
                else if (FileTypeHelper.IsVideo(hashInfo.FileType))
                {
                    details = IsoBaseMediaFormatRecognizer.Recognize(file);
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

            var keys = details.Keys.ToList();
            var detailsColumns = string.Concat(keys.Select(k => $", {QueryBuilder.EscapeName(k)}"));
            var parameterNames = string.Concat(Enumerable.Range(0, keys.Count).Select(i => $", @p{i}"));
            var param = new DynamicParameters();
            param.Add("Hash", hashInfo.Hash);
            for (var i = 0; i < keys.Count; i++)
            {
                param.Add($"p{i}", details[keys[i]]);
            }

            await this.IndexWriteAsync(async conn =>
            {
                if (this.detailsColumns.Count == 0)
                {
                    this.detailsColumns.UnionWith(
                        await conn.QueryAsync<string>("SELECT name FROM pragma_table_info('HashDetails')").ConfigureAwait(false));

                    if (this.detailsColumns.Count == 0)
                    {
                        await conn.ExecuteAsync($"CREATE TABLE HashDetails (Hash text NOT NULL{detailsColumns}, PRIMARY KEY (Hash), FOREIGN KEY (Hash) REFERENCES HashInfo (Hash) ON DELETE CASCADE)").ConfigureAwait(false);
                        this.detailsColumns.Add("Hash");
                        this.detailsColumns.UnionWith(keys);
                    }
                }

                foreach (var key in keys)
                {
                    if (!this.detailsColumns.Contains(key))
                    {
                        await conn.ExecuteAsync($"ALTER TABLE HashDetails ADD COLUMN {QueryBuilder.EscapeName(key)}").ConfigureAwait(false);
                        this.detailsColumns.Add(key);
                    }
                }

                await conn.ExecuteAsync($"INSERT OR REPLACE INTO HashDetails (Hash{detailsColumns}) VALUES (@Hash{parameterNames})", param).ConfigureAwait(false);
            }).ConfigureAwait(false);
        }

        private void Watcher_Changed(object sender, FileSystemEventArgs e)
        {
            var path = e.FullPath;
            if (this.IsPathIndexed(path))
            {
                // Avoid attempting to hash a newly created directory.
                if (Directory.Exists(path))
                {
                    return;
                }

                this.TouchFile(path, delay: FileChangeDelay);
            }
        }

        private void Watcher_Created(object sender, FileSystemEventArgs e)
        {
            var path = e.FullPath;
            if (this.IsPathIndexed(path))
            {
                if (Directory.Exists(path))
                {
                    this.rescanQueue.Enqueue((0, path, false));
                    this.StartIndexRescanTask();
                }
                else
                {
                    this.TouchFile(path, delay: FileCreatedDelay);
                }
            }
        }

        private async void Watcher_Deleted(object sender, FileSystemEventArgs e)
        {
            if (this.IsPathIndexed(e.FullPath))
            {
                await this.RemoveFile(e.FullPath).ConfigureAwait(false);
            }
        }

        private void Watcher_Error(object sender, ErrorEventArgs e)
        {
            var exception = e.GetException();
            if (exception is InternalBufferOverflowException && sender is FileSystemWatcher fsw)
            {
                this.rescanQueue.Enqueue((0, fsw.Path, false), BufferOverflowDelay);
                this.StartIndexRescanTask();
            }
        }

        private async void Watcher_Renamed(object sender, RenamedEventArgs e)
        {
            if (this.IsPathIndexed(e.OldFullPath))
            {
                await this.RemoveFile(e.OldFullPath).ConfigureAwait(false);
            }

            if (this.IsPathIndexed(e.FullPath))
            {
                this.TouchFile(e.FullPath);
            }
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
                    Version integer NOT NULL DEFAULT (0),
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
                    Category text NOT NULL,
                    [Order] integer NOT NULL,
                    Rules text NOT NULL,
                    PRIMARY KEY (Category)
                );

                CREATE TABLE IF NOT EXISTS Person
                (
                    PersonId integer NOT NULL,
                    Name text NOT NULL,
                    PRIMARY KEY (PersonId)
                );
                CREATE INDEX IF NOT EXISTS IX_Person_Name ON Person (Name);

                CREATE TABLE IF NOT EXISTS Alias
                (
                    PersonId integer NOT NULL,
                    Name text NOT NULL,
                    Site text NULL,
                    FOREIGN KEY (PersonId) REFERENCES Person (PersonId) ON DELETE CASCADE
                );
                CREATE INDEX IF NOT EXISTS IX_Alias_Name ON Alias (Name);

                CREATE UNIQUE INDEX IF NOT EXISTS IX_Alias_Site_Name ON Alias (Site, Name) WHERE Site NOT NULL;

                CREATE VIEW IF NOT EXISTS Names AS
                SELECT DISTINCT PersonId, Name FROM (
                    SELECT PersonId, Name FROM Person
                    UNION ALL
                    SELECT Personid, Name FROM Alias
                );

                CREATE TABLE IF NOT EXISTS PersonTag
                (
                    PersonId text NOT NULL,
                    Tag text NOT NULL,
                    PRIMARY KEY (PersonId, Tag),
                    FOREIGN KEY (PersonId) REFERENCES Person (PersonId) ON DELETE CASCADE
                );

                CREATE UNIQUE INDEX IF NOT EXISTS IX_PersonTag_PersonId_Tag ON PersonTag (PersonId, Tag);

                CREATE TABLE IF NOT EXISTS RejectedPersonTags
                (
                    PersonId text NOT NULL,
                    Tag text NOT NULL,
                    PRIMARY KEY (PersonId, Tag),
                    FOREIGN KEY (PersonId) REFERENCES Person (PersonId) ON DELETE CASCADE
                );

                CREATE UNIQUE INDEX IF NOT EXISTS IX_RejectedPersonTags_PersonId_Tag ON RejectedPersonTags (PersonId, Tag);

                CREATE TABLE IF NOT EXISTS HashPerson
                (
                    Hash text NOT NULL,
                    PersonId integer NOT NULL,
                    PRIMARY KEY (Hash, PersonId),
                    FOREIGN KEY (Hash) REFERENCES HashInfo (Hash) ON DELETE CASCADE,
                    FOREIGN KEY (PersonId) REFERENCES Person (PersonId) ON DELETE CASCADE
                );

                CREATE TABLE IF NOT EXISTS RejectedPerson
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

            public static readonly string CreateSchema_01_AddTagRuleCategories = @"
                ALTER TABLE TagRules ADD COLUMN Category text NOT NULL DEFAULT '';
                ALTER TABLE TagRules ADD COLUMN [Order] integer NOT NULL DEFAULT (0);
                CREATE UNIQUE INDEX PK_Category ON TagRules(Category);
            ";

            public static readonly string CreateSchema_02_AddHashInfoVersion = @"
                ALTER TABLE HashInfo ADD COLUMN Version integer NOT NULL DEFAULT (0);
            ";

            public static readonly string GetHashDetails = @"
                SELECT
                    *
                FROM HashDetails
                WHERE Hash = @Hash
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
        }

        private class ContainsSavedSearchTermCompiler : QueryCompiler<bool>
        {
            public ContainsSavedSearchTermCompiler()
                : base(null)
            {
            }

            public override bool CompileConjunction(IEnumerable<bool> query) => query.Any(x => x);

            public override bool CompileDisjunction(IEnumerable<bool> query) => query.Any(x => x);

            public override bool CompileField(FieldTerm field) => false;

            public override bool CompileNegation(bool query) => query;

            public override bool CompilePropertyConjunction(PropertyConjunctionTerm propertyConjunction) => false;

            public override bool CompileSavedSearch(SavedSearchTerm savedSearch) => true;
        }
    }
}
