using System.Globalization;
using MediaLibrary.Storage.Search;

namespace MediaLibrary.App.Converters
{
    internal class TagsColumnConverter : SearchResultConverter<string?>
    {
        public override string? Convert(SearchResult searchResult, object? parameter, CultureInfo culture)
        {
            var value = searchResult.Tags;
            return string.Join("; ", value);
        }
    }
}
