// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MediaLibrary.Tagging
{
    using System.Collections.Immutable;

    public class TagInfo
    {
        public TagInfo(string tag, bool isAbstract, ImmutableHashSet<string> aliases, ImmutableHashSet<string> ancestors, ImmutableHashSet<string> descendants, ImmutableList<string> properties)
        {
            this.Tag = tag;
            this.IsAbstract = isAbstract;
            this.Ancestors = ancestors;
            this.Aliases = aliases;
            this.Descendants = descendants;
            this.Properties = properties;
        }

        public ImmutableHashSet<string> Aliases { get; }

        public ImmutableHashSet<string> Ancestors { get; }

        public ImmutableHashSet<string> Descendants { get; }

        public bool IsAbstract { get; }

        public ImmutableList<string> Properties { get; }

        public string Tag { get; }
    }
}
