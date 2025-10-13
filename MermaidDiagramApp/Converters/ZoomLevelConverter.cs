using Microsoft.UI.Xaml.Data;
using System;

namespace MermaidDiagramApp.Converters
{
    public class ZoomLevelConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is double zoomLevel)
            {
                return $"{(int)(zoomLevel * 100)}%";
            }
            return "100%";
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
