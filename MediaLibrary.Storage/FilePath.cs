// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MediaLibrary.Storage
{
    public class FilePath
    {
        public FilePath(string path, string lastHash, long lastModifiedTime)
        {
            this.Path = path;
            this.LastHash = lastHash;
            this.LastModifiedTime = lastModifiedTime;
        }

        public string LastHash { get; }

        public long LastModifiedTime { get; }

        public string Path { get; }
    }
}
