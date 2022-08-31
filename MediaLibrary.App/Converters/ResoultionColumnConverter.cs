using System.Globalization;
using ByteSizeLib;
using MediaLibrary.Storage.FileTypes;
using MediaLibrary.Storage.Search;

namespace MediaLibrary.App.Converters
{
    internal class ResolutionColumnConverter : DetailsConverter<long?>
    {
        public override string? Convert(SearchResult searchResult, object? parameter, CultureInfo culture)
        {
            var width = GetDetails(searchResult, ImageDetailRecognizer.Properties.Width, value => System.Convert.ToInt64(value, CultureInfo.InvariantCulture));
            var height = GetDetails(searchResult, ImageDetailRecognizer.Properties.Height, value => System.Convert.ToInt64(value, CultureInfo.InvariantCulture));
            if (width == null || height == null)
            {
                return string.Empty;
            }

            var byteSize = ByteSize.FromBytes(width.Value * height.Value);
            var byteSizeValue = byteSize.LargestWholeNumberDecimalValue;
            var byteSizeSymbol = byteSize.LargestWholeNumberDecimalSymbol.Replace('B', 'P');
            return $"{byteSizeValue:0.0} {byteSizeSymbol} ({width}×{height})";
        }
    }
}
