using System;
using System.Globalization;
using Microsoft.UI.Xaml.Data;

namespace MermaidDiagramApp.Converters
{
    public class DoubleToStringFormatConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is null)
            {
                return string.Empty;
            }

            var format = parameter as string;
            if (value is IFormattable formattable)
            {
                return formattable.ToString(string.IsNullOrWhiteSpace(format) ? "G" : format, CultureInfo.InvariantCulture);
            }

            return value.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            if (value is string s && double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var d))
            {
                return d;
            }
            return value;
        }
    }
}
