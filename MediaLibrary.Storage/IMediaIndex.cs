// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MediaLibrary.Storage
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Threading.Tasks;
    using MediaLibrary.Storage.Search;
    using TaggingLibrary;

    public interface IMediaIndex : IDisposable
    {
        event EventHandler<HashInvalidatedEventArgs> HashInvalidated;

        event EventHandler<ItemAddedEventArgs<(HashPerson hash, Person person)>> HashPersonAdded;

        event EventHandler<ItemRemovedEventArgs<HashPerson>> HashPersonRemoved;

        event EventHandler<ItemAddedEventArgs<HashTag>> HashTagAdded;

        event EventHandler<ItemRemovedEventArgs<HashTag>> HashTagRemoved;

        event EventHandler<ItemUpdatedEventArgs<Rating>> RatingUpdated;

        event EventHandler<ItemUpdatedEventArgs<TagRuleEngine>> TagRulesUpdated;

        TagRuleEngine TagEngine { get; }

        Task<Alias> AddAlias(Alias alias);

        Task AddHashPerson(HashPerson hashPerson);

        Task AddHashTag(HashTag hashTag);

        Task AddIndexedPath(string path);

        Task<Person> AddPerson(string name);

        Task<SavedSearch> AddSavedSearch(string name, string query);

        Task<List<Alias>> GetAliases(int personId);

        Task<List<Alias>> GetAllAliasesForSite(string site);

        Task<List<Alias>> GetAllAliasesForSite(string site, string name);

        Task<string[]> GetAllAliasSites();

        Task<List<Person>> GetAllPeople();

        Task<List<string>> GetAllRatingCategories();

        Task<List<RuleCategory>> GetAllRuleCategories();

        Task<List<SavedSearch>> GetAllSavedSearches();

        Task<List<string>> GetAllHashTags();

        Task<ImmutableDictionary<string, object>> GetHashDetails(string hash);

        Task<List<HashTag>> GetHashTags(string hash);

        Task<Person> GetPersonById(long personId);

        Task<Rating> GetRating(string hash, string category);

        Task<List<HashTag>> GetRejectedHashTags(string hash);

        Task MergePeople(int targetId, int duplicateId);

        Task RemoveAlias(Alias alias);

        Task RemoveFilePath(string path);

        Task RemoveHashPerson(HashPerson hashPerson, bool rejectPerson = false);

        Task RemoveHashTag(HashTag hashTag, bool rejectTag = false);

        Task RemoveIndexedPath(string path);

        Task RemovePerson(Person person);

        Task RemoveSavedSearch(SavedSearch savedSearch);

        Task Rescan(bool forceRehash = false);

        Task<List<SearchResult>> SearchIndex(string query, bool excludeHidden = true);

        Task UpdatePerson(Person person);

        Task UpdateRating(Rating rating);

        Task UpdateSavedSearch(SavedSearch savedSearch);

        Task UpdateTagRules(IList<RuleCategory> ruleCategories);
    }
}
