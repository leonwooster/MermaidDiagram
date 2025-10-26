using Microsoft.UI.Xaml.Data;
using System;

namespace MermaidDiagramApp.Converters
{
    public class InverseBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is bool b)
            {
                return !b;
            }
            return true; // default to enabled if not a bool
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            if (value is bool b)
            {
                return !b;
            }
            return Microsoft.UI.Xaml.DependencyProperty.UnsetValue;
        }
    }
}
