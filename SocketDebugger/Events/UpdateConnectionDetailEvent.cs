using Prism.Events;
using SocketDebugger.Model;

namespace SocketDebugger.Events
{
    public class UpdateConnectionDetailEvent : PubSubEvent<ConnectionConfigModel>
    {
    }
}