using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Windows;
using System.Windows.Controls;
using Prism.Commands;
using Prism.Mvvm;
using SocketDebugger.Model;
using SocketDebugger.Pages;
using SocketDebugger.Services;
using SocketDebugger.Utils;
using WebSocket4Net;
using MessageBox = HandyControl.Controls.MessageBox;

namespace SocketDebugger.ViewModels
{
    public class WebSocketClientViewModel : BindableBase
    {
        #region VM

        private ObservableCollection<ConnectionConfigModel> _configModels;

        public ObservableCollection<ConnectionConfigModel> ConfigModels
        {
            get => _configModels;
            private set
            {
                _configModels = value;
                RaisePropertyChanged();
            }
        }

        private ConnectionConfigModel _configModel;

        public ConnectionConfigModel ConfigModel
        {
            get => _configModel;
            private set
            {
                _configModel = value;
                RaisePropertyChanged();
            }
        }

        private string _connectColorBrush = "DarkGray";

        public string ConnectColorBrush
        {
            get => _connectColorBrush;
            private set
            {
                _connectColorBrush = value;
                RaisePropertyChanged();
            }
        }

        private string _connectState = "未连接";

        public string ConnectState
        {
            get => _connectState;
            private set
            {
                _connectState = value;
                RaisePropertyChanged();
            }
        }

        private string _connectButtonState = "连接";

        public string ConnectButtonState
        {
            get => _connectButtonState;
            private set
            {
                _connectButtonState = value;
                RaisePropertyChanged();
            }
        }

        private string _userInputText;

        public string UserInputText
        {
            get => _userInputText;
            set
            {
                _userInputText = value;
                RaisePropertyChanged();
            }
        }

        private ObservableCollection<ChatMessageModel> _chatMessages = new ObservableCollection<ChatMessageModel>();

        public ObservableCollection<ChatMessageModel> ChatMessages
        {
            get => _chatMessages;
            set
            {
                _chatMessages = value;
                RaisePropertyChanged();
            }
        }

        #endregion

        #region DelegateCommand

        public DelegateCommand<WebSocketClientView> PageLoadedCommand { get; }
        public DelegateCommand<ListView> ConfigItemSelectedCommand { get; }
        public DelegateCommand AddConfigCommand { get; }
        public DelegateCommand DeleteConfigCommand { get; }
        public DelegateCommand EditConfigCommand { get; }
        public DelegateCommand ConnectServerCommand { get; }
        public DelegateCommand ClearMessageCommand { get; }
        public DelegateCommand SendMessageCommand { get; }

        #endregion

        private WebSocketClientView _viewPage;
        private readonly IApplicationDataService _applicationDataService;
        private WebSocket _webSocketClient;

        public WebSocketClientViewModel(IApplicationDataService applicationDataService)
        {
            _applicationDataService = applicationDataService;
            PageLoadedCommand = new DelegateCommand<WebSocketClientView>(it =>
            {
                Debug.WriteLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " WebSocketClientViewModel => 加载");
                _viewPage = it;
            });
            ConfigModels = _applicationDataService.GetConfigModels();
            if (ConfigModels.Any())
            {
                ConfigModel = ConfigModels[0];
            }

            ConfigItemSelectedCommand = new DelegateCommand<ListView>(it =>
            {
                if (it.SelectedIndex == -1)
                {
                    return;
                }

                ConfigModel = (ConnectionConfigModel)it.SelectedItem;
            });

            AddConfigCommand = new DelegateCommand(delegate
            {
                var configModel = new ConnectionConfigModel
                {
                    Comment = "",
                    ConnType = "WebSocket客户端",
                    ConnHost = SystemHelper.GetHostAddress(),
                    ConnPort = "8080"
                };
                // var dialog = new ConfigDialog(configModel) { Owner = Window.GetWindow(_viewPage) };
                // dialog.AddConfigEventHandler += AddConfigResult;
                // dialog.ShowDialog();
            });

            DeleteConfigCommand = new DelegateCommand(delegate
            {
                if (ConfigModels.Any())
                {
                    var result = MessageBox.Show("是否删除当前配置？", "温馨提示", MessageBoxButton.OKCancel,
                        MessageBoxImage.Warning);
                    if (result != MessageBoxResult.OK) return;
                    using (var manager = new DataBaseManager())
                    {
                        manager.Delete(ConfigModel);
                    }

                    ConfigModels = _applicationDataService.GetConfigModels();
                    if (ConfigModels.Any())
                    {
                        ConfigModel = ConfigModels[0];
                    }
                }
                else
                {
                    MessageBox.Show("没有配置，无法删除", "温馨提示", MessageBoxButton.OKCancel, MessageBoxImage.Warning);
                }
            });

            EditConfigCommand = new DelegateCommand(delegate
            {
                if (ConfigModel == null)
                {
                    MessageBox.Show("无配置项，无法编辑", "温馨提示", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                else
                {
                    // var dialog = new ConfigDialog(ConfigModel) { Owner = Window.GetWindow(_viewPage) };
                    // dialog.AddConfigEventHandler += AddConfigResult;
                    // dialog.ShowDialog();
                }
            });

            ConnectServerCommand = new DelegateCommand(delegate
            {
                if (_webSocketClient == null)
                {
                    _webSocketClient = new WebSocket("ws://" + ConfigModel.ConnHost + ":" + ConfigModel.ConnPort);
                }

                _webSocketClient.Opened += WebSocketOpened;
                _webSocketClient.MessageReceived += WebSocketMessageReceived;
                _webSocketClient.Closed += WebSocketClosed;

                try
                {
                    if (_connectButtonState == "连接")
                    {
                        _webSocketClient.Open();
                    }
                    else
                    {
                        _webSocketClient.Opened -= WebSocketOpened;
                        _webSocketClient.MessageReceived -= WebSocketMessageReceived;
                        _webSocketClient.Closed -= WebSocketClosed;

                        _webSocketClient.Close();
                        _webSocketClient = null;
                    }
                }
                catch (SocketException e)
                {
                    MessageBox.Show(e.Message, "温馨提示", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            });

            ClearMessageCommand = new DelegateCommand(() => { ChatMessages.Clear(); });

            SendMessageCommand = new DelegateCommand(delegate
            {
                if (string.IsNullOrEmpty(_userInputText))
                {
                    MessageBox.Show("不能发送空消息", "温馨提示", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (ConnectState == "未连接")
                {
                    MessageBox.Show("未连接成功，无法发送消息", "温馨提示", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                _webSocketClient?.Send(_userInputText);

                ChatMessages.Add(new ChatMessageModel
                {
                    MessageTime = DateTime.Now.ToString("yyyy年MM月dd HH时mm分ss秒"),
                    Message = _userInputText,
                    IsSend = true
                });
            });
        }

        private void AddConfigResult(object sender, ConnectionConfigModel model)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                ConfigModels = _applicationDataService.GetConfigModels();
                ConfigModel = model;
            });
        }

        private void WebSocketOpened(object sender, EventArgs e)
        {
            ConnectColorBrush = "LimeGreen";
            ConnectState = "已连接";
            ConnectButtonState = "断开";
        }

        private void WebSocketClosed(object sender, EventArgs e)
        {
            ConnectColorBrush = "DarkGray";
            ConnectState = "未连接";
            ConnectButtonState = "连接";
        }

        private void WebSocketMessageReceived(object sender, MessageReceivedEventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                ChatMessages.Add(new ChatMessageModel
                {
                    MessageTime = DateTime.Now.ToString("yyyy年MM月dd HH时mm分ss秒"),
                    Message = e.Message,
                    IsSend = false
                });
            });
        }
    }
}