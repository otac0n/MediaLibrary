using System.Globalization;
using System.Linq;
using MediaLibrary.Storage;
using MediaLibrary.Storage.Search;

namespace MediaLibrary.App.Converters
{
    internal abstract class BestPathConverter : SearchResultConverter<string?>
    {
        public override string? Convert(SearchResult searchResult, object? parameter, CultureInfo culture)
        {
            return searchResult.Paths.OrderBy(p => p, PathComparer.Instance).FirstOrDefault();
        }
    }
}
