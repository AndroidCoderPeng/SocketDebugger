using System;
using System.Collections.Generic;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using Prism.Regions;
using SocketDebugger.Events;
using SocketDebugger.Model;
using SocketDebugger.Services;

namespace SocketDebugger.ViewModels
{
    public class MainWindowViewModel : BindableBase
    {
        #region VM

        public List<MainMenuModel> MainMenuModels { get; }

        #endregion

        #region DelegateCommand

        public DelegateCommand<object> MenuSelectedCommand { set; get; }

        #endregion

        private readonly IRegionManager _regionManager;
        private readonly IEventAggregator _eventAggregator;

        public MainWindowViewModel(IApplicationDataService dataService, IRegionManager regionManager,
            IEventAggregator eventAggregator)
        {
            _regionManager = regionManager;
            _eventAggregator = eventAggregator;

            MainMenuModels = dataService.GetMainMenu();

            MenuSelectedCommand = new DelegateCommand<object>(MenuSelected);
        }

        private void MenuSelected(object selectedIndex)
        {
            if (selectedIndex == null)
            {
                return;
            }

            var region = _regionManager.Regions["ContentRegion"];
            switch (selectedIndex)
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

            var mainMenuModel = MainMenuModels[Convert.ToInt32(selectedIndex)];
            _eventAggregator.GetEvent<ChangeViewByMainMenuEvent>().Publish(mainMenuModel?.MainMenuName);
        }
    }
}