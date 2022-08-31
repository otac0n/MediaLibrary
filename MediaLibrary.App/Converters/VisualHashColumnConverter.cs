using System.Globalization;
using MediaLibrary.Storage.FileTypes;
using MediaLibrary.Storage.Search;

namespace MediaLibrary.App.Converters
{
    internal class VisualHashColumnConverter : DetailsConverter<long?>
    {
        public override string? Convert(SearchResult searchResult, object? parameter, CultureInfo culture)
        {
            var value = GetDetails(searchResult, ImageDetailRecognizer.Properties.AverageIntensityHash, value => System.Convert.ToInt64(value, CultureInfo.InvariantCulture));
            return value != null ? $"0x{value:X16}" : string.Empty;
        }
    }
}
