using System.Windows;
using System.Windows.Controls;
using SocketDebugger.Model;

namespace SocketDebugger.Utils
{
    public class ClientChatBubbleSelector : DataTemplateSelector
    {
        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            var element = (FrameworkElement)container;
            var message = (ChatMessageModel)item;
            if (message != null && message.IsSend)
            {
                return element.FindResource("ClientSendDataTemplateKey") as DataTemplate;
            }

            return element.FindResource("ClientReceiveDataTemplateKey") as DataTemplate;
        }
    }
}