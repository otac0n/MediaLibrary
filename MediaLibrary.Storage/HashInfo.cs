// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MediaLibrary.Storage
{
    public class HashInfo
    {
        public HashInfo(string hash, long fileSize, string fileType)
        {
            this.Hash = hash;
            this.FileSize = fileSize;
            this.FileType = fileType;
        }

        public long FileSize { get; }

        public string FileType { get; }

        public string Hash { get; }
    }
}
