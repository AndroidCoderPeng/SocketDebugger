using System;
using System.Diagnostics;
using System.Globalization;
using System.Windows.Data;

namespace SocketDebugger.Converts
{
    internal class SendMessageButtonStateConvert : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Debug.Assert(value != null, nameof(value) + " != null");
            var isChecked = (bool)value;
            return !isChecked;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}