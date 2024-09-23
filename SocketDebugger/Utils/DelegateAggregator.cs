using System.Net.WebSockets;
using DotNetty.Codecs.Http.WebSockets;
using DotNetty.Transport.Channels;

namespace SocketDebugger.Utils
{
    public class DelegateAggregator
    {
        public delegate void WebSocketStateDelegate(IChannelHandlerContext context, WebSocketState state);

        public delegate void WebSocketMessageDelegate(WebSocketFrame dataFrame);
    }
}