using System;
using System.Diagnostics;
using System.Globalization;
using System.Windows.Data;

namespace SocketDebugger.Converts
{
    internal class SerialPortConfigStateConvert : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Debug.Assert(value != null, nameof(value) + " != null");
            var isOpen = (bool)value;
            return !isOpen;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}