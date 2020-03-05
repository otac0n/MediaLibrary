// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

using System.Collections.Immutable;

namespace MediaLibrary.Storage.Search
{
    public class SearchResult
    {
        public SearchResult(string hash, string fileType, long fileSize, ImmutableHashSet<string> tags, ImmutableHashSet<string> paths)
        {
            this.Hash = hash;
            this.FileType = fileType;
            this.FileSize = fileSize;
            this.Tags = tags;
            this.Paths = paths;
        }

        public long FileSize { get; }

        public string FileType { get; }

        public string Hash { get; }

        public ImmutableHashSet<string> Paths { get; }

        public ImmutableHashSet<string> Tags { get; }
    }
}
