// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MediaLibrary.Storage.Search
{
    using System.Collections.Immutable;

    public class SearchResult
    {
        public SearchResult(string hash, long fileSize, string fileType, ImmutableDictionary<string, object> details, Rating rating, ImmutableHashSet<string> tags, ImmutableHashSet<string> rejectedTags, ImmutableHashSet<string> paths, ImmutableHashSet<Person> people, ImmutableHashSet<Person> rejectedPeople)
        {
            this.Hash = hash;
            this.FileType = fileType;
            this.FileSize = fileSize;
            this.Details = details;
            this.Rating = rating;
            this.Tags = tags;
            this.RejectedTags = rejectedTags;
            this.Paths = paths;
            this.People = people;
            this.RejectedPeople = rejectedPeople;
        }

        public ImmutableDictionary<string, object> Details { get; set; }

        public long FileSize { get; }

        public string FileType { get; set; }

        public string Hash { get; }

        public ImmutableHashSet<string> Paths { get; set; }

        public ImmutableHashSet<Person> People { get; set; }

        public Rating Rating { get; set; }

        public ImmutableHashSet<Person> RejectedPeople { get; set; }

        public ImmutableHashSet<string> RejectedTags { get; set; }

        public ImmutableHashSet<string> Tags { get; set; }

        public SearchResult With(
            string fileType = null,
            ImmutableDictionary<string, object> details = null,
            Rating rating = null,
            ImmutableHashSet<string> tags = null,
            ImmutableHashSet<string> rejectedTags = null,
            ImmutableHashSet<string> paths = null,
            ImmutableHashSet<Person> people = null,
            ImmutableHashSet<Person> rejectedPeople = null) =>
            new SearchResult(
                this.Hash,
                this.FileSize,
                fileType ?? this.FileType,
                details ?? this.Details,
                rating ?? this.Rating,
                tags ?? this.Tags,
                rejectedTags ?? this.RejectedTags,
                paths ?? this.Paths,
                people ?? this.People,
                rejectedPeople ?? this.RejectedPeople);
    }
}
