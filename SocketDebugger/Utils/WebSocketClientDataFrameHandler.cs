using System;
using System.Net.WebSockets;
using DotNetty.Codecs.Http.WebSockets;
using DotNetty.Transport.Channels;

namespace SocketDebugger.Utils
{
    public class WebSocketClientDataFrameHandler : SimpleChannelInboundHandler<WebSocketFrame>
    {
        private readonly DelegateAggregator.WebSocketStateDelegate _stateDelegate;
        private readonly DelegateAggregator.WebSocketMessageDelegate _messageDelegate;

        public WebSocketClientDataFrameHandler(DelegateAggregator.WebSocketStateDelegate stateDelegate,
            DelegateAggregator.WebSocketMessageDelegate messageDelegate)
        {
            _stateDelegate = stateDelegate;
            _messageDelegate = messageDelegate;
        }

        protected override void ChannelRead0(IChannelHandlerContext ctx, WebSocketFrame msg)
        {
            _messageDelegate(msg);
        }

        public override void ChannelActive(IChannelHandlerContext context)
        {
            base.ChannelActive(context);
            _stateDelegate(WebSocketState.Open);
        }

        public override void ChannelInactive(IChannelHandlerContext context)
        {
            base.ChannelInactive(context);
            _stateDelegate(WebSocketState.Closed);
        }

        public override void ExceptionCaught(IChannelHandlerContext ctx, Exception exception)
        {
            Console.WriteLine($@"Exception: {exception}");
            ctx.CloseAsync();
            _stateDelegate(WebSocketState.Aborted);
        }
    }
}