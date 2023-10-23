using System.Windows;
using Prism.DryIoc;
using Prism.Ioc;
using SocketDebugger.Dialogs;
using SocketDebugger.Pages;
using SocketDebugger.Services;
using SocketDebugger.ViewModels;
using SocketDebugger.Views;

namespace SocketDebugger
{
    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    public partial class App : PrismApplication
    {
        protected override Window CreateShell()
        {
            return Container.Resolve<MainWindow>();
        }

        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            //Data
            containerRegistry.Register<IApplicationDataService, ApplicationDataServiceImpl>();
            
            //Navigation
            containerRegistry.RegisterForNavigation<EmptyView>();
            containerRegistry.RegisterForNavigation<TcpClientView>();
            containerRegistry.RegisterForNavigation<TcpServerView>();
            containerRegistry.RegisterForNavigation<UdpClientView>();
            containerRegistry.RegisterForNavigation<UdpServerView>();
            containerRegistry.RegisterForNavigation<WebSocketClientView>();
            containerRegistry.RegisterForNavigation<WebSocketServerView>();
            containerRegistry.RegisterForNavigation<HttpServerView>();
            
            //Dialog or Window
            containerRegistry.RegisterDialog<ConfigDialog, ConfigDialogViewModel>();
        }
    }
}