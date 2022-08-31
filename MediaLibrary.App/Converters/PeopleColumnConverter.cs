using System.Globalization;
using System.Linq;
using MediaLibrary.Storage.Search;

namespace MediaLibrary.App.Converters
{
    internal class PeopleColumnConverter : SearchResultConverter<string?>
    {
        public override string? Convert(SearchResult searchResult, object? parameter, CultureInfo culture)
        {
            var value = searchResult.People;
            return string.Join("; ", value.Select(p => p.Name));
        }
    }
}
