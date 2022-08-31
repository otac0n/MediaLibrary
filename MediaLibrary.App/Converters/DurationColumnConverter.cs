using System;
using System.Globalization;
using System.Text;
using MediaLibrary.Storage.Search;

namespace MediaLibrary.App.Converters
{
    internal class DurationColumnConverter : DetailsConverter<TimeSpan?>
    {
        public override string? Convert(SearchResult searchResult, object? parameter, CultureInfo culture)
        {
            var value = GetDetails(searchResult, "Duration", value => TimeSpan.FromSeconds(System.Convert.ToDouble(value, CultureInfo.InvariantCulture)));
            return FormatTimeSpan(value);
        }

        private static string FormatTimeSpan(TimeSpan? value)
        {
            if (value is TimeSpan duration)
            {
                var formatted = new StringBuilder();
                if (duration.TotalDays >= 1)
                {
                    formatted.Append(duration.Days).Append("d");
                }

                if (duration.TotalHours >= 1)
                {
                    formatted.AppendFormat("{0:d2}", duration.Hours).Append("h");
                }

                if (duration.TotalMinutes >= 1)
                {
                    formatted.AppendFormat("{0:d2}", duration.Minutes).Append("m");
                }

                formatted.AppendFormat("{0:d2}", duration.Seconds).Append("s");

                return formatted.ToString();
            }

            return string.Empty;
        }
    }
}
