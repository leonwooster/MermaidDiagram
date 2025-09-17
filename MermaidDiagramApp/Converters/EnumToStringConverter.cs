using Microsoft.UI.Xaml.Data;
using System;

namespace MermaidDiagramApp.Converters
{
    public class EnumToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return value?.ToString() ?? string.Empty;
        }

        public object? ConvertBack(object value, Type targetType, object parameter, string language)
        {
            if (value is string strValue && targetType.IsEnum)
            {
                return Enum.Parse(targetType, strValue);
            }
            return null;
        }
    }
}
