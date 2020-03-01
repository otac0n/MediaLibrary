// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MediaLibrary.Storage.Search
{
    public class SearchResult
    {
        public SearchResult(string hash, string fileType, long fileSize, string[] tags, string[] fileNames)
        {
            this.Hash = hash;
            this.FileType = fileType;
            this.FileSize = fileSize;
            this.Tags = tags;
            this.Paths = fileNames;
        }

        public string[] Paths { get; }

        public long FileSize { get; }

        public string FileType { get; }

        public string Hash { get; }

        public string[] Tags { get; }
    }
}
