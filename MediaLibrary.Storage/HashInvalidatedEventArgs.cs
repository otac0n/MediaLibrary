// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MediaLibrary.Storage
{
    using System;

    public class HashInvalidatedEventArgs : EventArgs
    {
        public HashInvalidatedEventArgs(string hash)
        {
            this.Hash = hash;
        }

        public string Hash { get; }
    }
}
