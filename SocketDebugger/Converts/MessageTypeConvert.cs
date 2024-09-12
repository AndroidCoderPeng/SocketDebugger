using System;
using System.Globalization;
using System.Windows.Data;

namespace SocketDebugger.Converts
{
    public class MessageTypeConvert : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value != null && value.ToString() == (string)parameter;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value != null && value.Equals(true) ? parameter : Binding.DoNothing;
        }
    }
}