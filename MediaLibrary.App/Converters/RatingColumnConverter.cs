using System;
using System.Globalization;
using MediaLibrary.Storage.Search;

namespace MediaLibrary.App.Converters
{
    internal class RatingColumnConverter : SearchResultConverter<string?>
    {
        public override string? Convert(SearchResult searchResult, object? parameter, CultureInfo culture)
        {
            var value = searchResult.Rating;
            return value != null ? $"{Math.Round(value.Value)}{(value.Count < 15 ? "?" : string.Empty)}" : string.Empty;
        }
    }
}
