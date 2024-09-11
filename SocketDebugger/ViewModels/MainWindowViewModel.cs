using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using Prism.Regions;
using Prism.Services.Dialogs;
using SocketDebugger.Events;
using SocketDebugger.Model;
using SocketDebugger.Services;

namespace SocketDebugger.ViewModels
{
    public class MainWindowViewModel : BindableBase
    {
        #region VM

        public List<MainMenuModel> MainMenuModels { get; }

        private ObservableCollection<ConnectionConfigModel> _connectionObservableCollection;

        public ObservableCollection<ConnectionConfigModel> ConnectionObservableCollection
        {
            get => _connectionObservableCollection;
            set
            {
                _connectionObservableCollection = value;
                RaisePropertyChanged();
            }
        }

        private int _currentIndex;

        public int CurrentIndex
        {
            get => _currentIndex;
            set
            {
                _currentIndex = value;
                RaisePropertyChanged();
            }
        }

        #endregion

        #region DelegateCommand

        public DelegateCommand<object> MenuSelectedCommand { set; get; }
        public DelegateCommand<ConnectionConfigModel> ConnectionItemSelectedCommand { set; get; }
        public DelegateCommand<ConnectionConfigModel> DeleteConnectionConfigCommand { set; get; }
        public DelegateCommand<MainMenuModel> AddConnectionConfigCommand { set; get; }

        #endregion

        private readonly IApplicationDataService _dataService;
        private readonly IRegionManager _regionManager;
        private readonly IDialogService _dialogService;
        private readonly IEventAggregator _eventAggregator;

        public MainWindowViewModel(IApplicationDataService dataService, IRegionManager regionManager,
            IDialogService dialogService, IEventAggregator eventAggregator)
        {
            _dataService = dataService;
            _regionManager = regionManager;
            _dialogService = dialogService;
            _eventAggregator = eventAggregator;

            MainMenuModels = dataService.GetMainMenu();
            UpdateConnectionView(MainMenuModels[0].MainMenuName, false);

            MenuSelectedCommand = new DelegateCommand<object>(MenuSelected);
            ConnectionItemSelectedCommand = new DelegateCommand<ConnectionConfigModel>(ConnectionItemSelected);
            DeleteConnectionConfigCommand = new DelegateCommand<ConnectionConfigModel>(DeleteConnectionConfig);
            AddConnectionConfigCommand = new DelegateCommand<MainMenuModel>(AddConnectionConfig);
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
            UpdateConnectionView(mainMenuModel?.MainMenuName, false);
        }

        private void ConnectionItemSelected(ConnectionConfigModel configModel)
        {
            if (configModel == null)
            {
                return;
            }

            //切换列表时候更新信息
            _eventAggregator.GetEvent<UpdateConnectionDetailEvent>().Publish(configModel);
        }

        private void DeleteConnectionConfig(ConnectionConfigModel configModel)
        {
            if (configModel == null)
            {
                return;
            }

            var result = MessageBox.Show("是否删除当前配置？", "温馨提示", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                _dataService.DeleteConnectionById(configModel.Uuid);
                //刷新界面
                // UpdateConnectionView(configModel.ConnectionType, false);
            }
        }

        private void AddConnectionConfig(MainMenuModel menuModel)
        {
            if (menuModel == null)
            {
                return;
            }

            var configModel = new ConnectionConfigModel
            {
                ConnectionTitle = "",
                ConnectionType = menuModel.MainMenuName,
                ConnectionHost = _dataService.GetHostAddress(),
                ConnectionPort = "8080",
                MessageType = "16进制"
            };

            _dialogService.ShowDialog("ConfigDialog", new DialogParameters
                {
                    { "Title", "添加配置" }, { "ConnectionConfigModel", configModel }
                },
                delegate(IDialogResult result)
                {
                    if (result.Result == ButtonResult.OK)
                    {
                        //更新列表
                        // UpdateConnectionView(menuModel.MainMenuName, true);
                    }
                }
            );
        }

        private void UpdateConnectionView(string type, bool isAdd)
        {
            ConnectionObservableCollection = _dataService.GetConnectionCollection(type);
            if (_connectionObservableCollection.Any())
            {
                if (isAdd)
                {
                    //选中最新添加的数据
                    CurrentIndex = _connectionObservableCollection.Count - 1;
                }
                else
                {
                    //删除或者切换类别选中第一个
                    CurrentIndex = 0;
                }

                //设置最右侧默认面板
                _eventAggregator.GetEvent<UpdateConnectionDetailEvent>().Publish(
                    _connectionObservableCollection[_currentIndex]
                );
            }
        }
    }
}