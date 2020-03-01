// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MediaLibrary.Storage
{
    public class HashTag
    {
        public HashTag(string hash, string tag)
        {
            this.Hash = hash;
            this.Tag = tag;
        }

        public string Hash { get; }

        public string Tag { get; }
    }
}
