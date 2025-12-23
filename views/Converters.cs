using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace nnunet_client.views
{
    public class NullToVisibilityConverter : IValueConverter
    {
        public static readonly NullToVisibilityConverter Instance = new NullToVisibilityConverter();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
                return Visibility.Collapsed;
            return Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

