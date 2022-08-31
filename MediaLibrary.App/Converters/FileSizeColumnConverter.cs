using System.Globalization;
using ByteSizeLib;
using MediaLibrary.Storage.Search;

namespace MediaLibrary.App.Converters
{
    internal class FileSizeColumnConverter : SearchResultConverter<string?>
    {
        public override string? Convert(SearchResult searchResult, object? parameter, CultureInfo culture)
        {
            var value = searchResult.FileSize;
            return ByteSize.FromBytes(value).ToString();
        }
    }
}
