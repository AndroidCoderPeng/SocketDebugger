using System;
using System.Diagnostics;
using System.Globalization;
using System.Windows.Data;

namespace SocketDebugger.Converts
{
    public class WebSocketPathStateConvert : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Debug.Assert(value != null, nameof(value) + " != null");
            var type = (string)value;
            return type.Contains("WebSocket");
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}