// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MediaLibrary.Storage.Search
{
    using System.Collections.Immutable;

    public class SearchResult
    {
        public SearchResult(string hash, string fileType, long fileSize, Rating rating, ImmutableHashSet<string> tags, ImmutableHashSet<string> paths, ImmutableHashSet<Person> people)
        {
            this.Hash = hash;
            this.FileType = fileType;
            this.FileSize = fileSize;
            this.Rating = rating;
            this.Tags = tags;
            this.Paths = paths;
            this.People = people;
        }

        public long FileSize { get; }

        public string FileType { get; }

        public string Hash { get; }

        public ImmutableHashSet<string> Paths { get; set; }

        public ImmutableHashSet<Person> People { get; set; }

        public Rating Rating { get; set; }

        public ImmutableHashSet<string> Tags { get; set; }

        public SearchResult With(
            Rating rating = null,
            ImmutableHashSet<string> tags = null,
            ImmutableHashSet<string> paths = null,
            ImmutableHashSet<Person> people = null) =>
            new SearchResult(
                this.Hash,
                this.FileType,
                this.FileSize,
                rating ?? this.Rating,
                tags ?? this.Tags,
                paths ?? this.Paths,
                people ?? this.People);
    }
}
