using System.Globalization;
using System.IO;
using MediaLibrary.Storage.Search;

namespace MediaLibrary.App.Converters
{
    internal class PathColumnConverter : BestPathConverter
    {
        public override string? Convert(SearchResult searchResult, object? parameter, CultureInfo culture)
        {
            var path = base.Convert(searchResult, parameter, culture);
            return path == null ? null : Path.GetDirectoryName(path);
        }
    }
}
