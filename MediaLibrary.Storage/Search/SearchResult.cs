// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

using System.Collections.Immutable;

namespace MediaLibrary.Storage.Search
{
    public class SearchResult
    {
        public SearchResult(string hash, string fileType, long fileSize, ImmutableHashSet<string> tags, ImmutableHashSet<string> paths, ImmutableList<Person> people)
        {
            this.Hash = hash;
            this.FileType = fileType;
            this.FileSize = fileSize;
            this.Tags = tags;
            this.Paths = paths;
            this.People = people;
        }

        public long FileSize { get; }

        public string FileType { get; }

        public string Hash { get; }

        public ImmutableHashSet<string> Paths { get; }

        public ImmutableList<Person> People { get; }

        public ImmutableHashSet<string> Tags { get; }

        public SearchResult With(
            ImmutableHashSet<string> tags = null,
            ImmutableHashSet<string> paths = null,
            ImmutableList<Person> people = null) =>
            new SearchResult(
                this.Hash,
                this.FileType,
                this.FileSize,
                tags ?? this.Tags,
                paths ?? this.Paths,
                people ?? this.People);
    }
}
