// Copyright © John & Katie Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MediaLibrary.FormsClerk
{
    using System.Collections.Generic;
    using System.Collections.Immutable;

    public class Scope
    {
        public Scope(string path, object value, string name = null, IDictionary<string, object> properties = null)
        {
            this.Path = path ?? string.Empty;
            this.Value = value;
            this.Name = name;
            this.Properties = properties?.ToImmutableDictionary();
        }

        private Scope(Scope parent, string name, object value, IDictionary<string, object> properties)
        {
            this.Parent = parent;
            this.Value = value;
            this.Path = string.IsNullOrEmpty(this.Path)
                ? name
                : name.StartsWith("[")
                    ? $"{this.Path}{name}"
                    : $"{this.Path}.{name}";
            this.Name = name;
            this.Properties = properties?.ToImmutableDictionary();
        }

        public string Name { get; }

        public Scope Parent { get; }

        public string Path { get; }

        public ImmutableDictionary<string, object> Properties { get; }

        public object Value { get; }

        public Scope Extend(string name, object value, IDictionary<string, object> properties = null) => new Scope(this, name, value, properties);

        public T GetPropertyOrDefault<T>(string key, T @default = default)
        {
            if (this.Properties != null && this.Properties.TryGetValue(key, out var value) && value is T result)
            {
                return result;
            }
            else if (this.Parent != null)
            {
                return this.Parent.GetPropertyOrDefault(key, @default);
            }

            return @default;
        }

        public static class SharedProperties
        {
            public static readonly string Key = "Key";
        }
    }
}
