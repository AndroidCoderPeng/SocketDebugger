using System.Collections.Generic;
using System.Windows.Controls;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Regions;
using SocketDebugger.Model;
using SocketDebugger.Services;
using SocketDebugger.Utils;

namespace SocketDebugger.ViewModels
{
    public class MainWindowViewModel : BindableBase
    {
        public List<MainMenuModel> MainMenuModels { get; }

        #region DelegateCommand

        public DelegateCommand<ListView> MenuSelectedCommand { set; get; }

        #endregion

        public MainWindowViewModel(IApplicationDataService dataService, IRegionManager regionManager)
        {
            MainMenuModels = dataService.GetMainMenu();

            MenuSelectedCommand = new DelegateCommand<ListView>(delegate(ListView view)
            {
                MemoryCacheManager.SelectedMainMenu = MainMenuModels[view.SelectedIndex].MainMenuName;

                var region = regionManager.Regions["ContentRegion"];
                switch (view.SelectedIndex)
                {
                    case 0:
                        region.RequestNavigate("TcpClientView");
                        break;
                    case 1:
                        region.RequestNavigate("TcpServerView");
                        break;
                    case 2:
                        region.RequestNavigate("UdpClientView");
                        break;
                    case 3:
                        region.RequestNavigate("UdpServerView");
                        break;
                    case 4:
                        region.RequestNavigate("WebSocketClientView");
                        break;
                    case 5:
                        region.RequestNavigate("WebSocketServerView");
                        break;
                    case 6:
                        region.RequestNavigate("SerialPortView");
                        break;
                }
            });
        }
    }
}