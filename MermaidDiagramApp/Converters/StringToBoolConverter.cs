using Microsoft.UI.Xaml.Data;
using System;

namespace MermaidDiagramApp.Converters
{
    /// <summary>
    /// Converts a string to a boolean value
    /// </summary>
    public class StringToBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is string str)
            {
                return !string.IsNullOrEmpty(str);
            }
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
