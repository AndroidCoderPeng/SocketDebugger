using System.Windows;
using Prism.DryIoc;
using Prism.Ioc;
using Prism.Regions;
using SocketDebugger.Dialogs;
using SocketDebugger.Services;
using SocketDebugger.Utils;
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
            var mainWindow = Container.Resolve<MainWindow>();
            mainWindow.Loaded += delegate
            {
                //显示默认View
                MemoryCacheManager.SelectedMainMenu = "TCP客户端";

                var regionManager = Container.Resolve<IRegionManager>();
                regionManager.RequestNavigate("ContentRegion", "TcpClientView");
            };
            return mainWindow;
        }

        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            //Data
            containerRegistry.Register<IApplicationDataService, ApplicationDataServiceImpl>();

            //Navigation
            containerRegistry.RegisterForNavigation<TcpClientView, TcpClientViewModel>();
            containerRegistry.RegisterForNavigation<TcpServerView, TcpServerViewModel>();
            containerRegistry.RegisterForNavigation<UdpClientView, UdpClientViewModel>();
            containerRegistry.RegisterForNavigation<UdpServerView, UdpServerViewModel>();
            containerRegistry.RegisterForNavigation<WebSocketClientView, WebSocketClientViewModel>();
            containerRegistry.RegisterForNavigation<WebSocketServerView, WebSocketServerViewModel>();
            containerRegistry.RegisterForNavigation<SerialPortView, SerialPortViewModel>();

            //Dialog or Window
            containerRegistry.RegisterDialog<ConfigDialog, ConfigDialogViewModel>();
            containerRegistry.RegisterDialog<AlertMessageDialog, AlertMessageDialogViewModel>();
            containerRegistry.RegisterDialog<AlertControlDialog, AlertControlDialogViewModel>();
        }
    }
}