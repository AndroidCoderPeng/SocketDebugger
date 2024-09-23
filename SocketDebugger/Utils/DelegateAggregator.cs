using System.Net.WebSockets;
using DotNetty.Codecs.Http.WebSockets;

namespace SocketDebugger.Utils
{
    public class DelegateAggregator
    {
        public delegate void WebSocketStateDelegate(WebSocketState state);

        public delegate void WebSocketMessageDelegate(WebSocketFrame dataFrame);
    }
}