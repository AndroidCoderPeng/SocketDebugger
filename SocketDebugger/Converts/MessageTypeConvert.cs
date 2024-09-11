using System;
using System.Globalization;
using System.Windows.Data;

namespace SocketDebugger.Converts
{
    public class MessageTypeConvert : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Console.WriteLine(value);
            return value == null || ReferenceEquals(value, "文本");
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}