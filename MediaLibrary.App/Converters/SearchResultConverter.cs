using System;
using System.Globalization;
using Avalonia.Data.Converters;
using MediaLibrary.Storage.Search;

namespace MediaLibrary.App.Converters
{
    internal abstract class SearchResultConverter<T> : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (targetType == typeof(T))
            {
                if (value is null)
                {
                    return null;
                }

                if (value is SearchResult r)
                {
                    return this.Convert(r, parameter, culture);
                }
            }

            throw new NotSupportedException();
        }

        public abstract T Convert(SearchResult searchResult, object? parameter, CultureInfo culture);


        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
            throw new NotSupportedException();
    }
}
