using System;
using System.Globalization;
using System.Windows.Data;

namespace SocketDebugger.Converts
{
    //TODO 不生效
    public class MessageTypeConvert : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Console.WriteLine(value);
            var equals = value == null || ReferenceEquals(value, "文本");
            Console.WriteLine(equals);
            return equals;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}