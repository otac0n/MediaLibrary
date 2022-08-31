using System;
using System.Collections.Immutable;
using MediaLibrary.Storage.Search;

namespace MediaLibrary.App.Converters
{
    internal abstract class DetailsConverter<T> : SearchResultConverter<string?>
    {
        protected static T? GetDetails(SearchResult result, string name, Func<object, T> converter) =>
            result.Details.TryGetValue(name, out var value) && value is object ? converter(value) : default;
    }
}
