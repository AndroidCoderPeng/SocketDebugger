using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using Prism.Commands;
using Prism.Mvvm;
using SocketDebugger.Model;
using SocketDebugger.Pages;
using SocketDebugger.Services;
using SocketDebugger.Utils;
using TouchSocket.Core;
using TouchSocket.Sockets;
using MessageBox = HandyControl.Controls.MessageBox;

namespace SocketDebugger.ViewModels
{
    public class UdpServerViewModel : BindableBase
    {
        #region DelegateCommand

        public DelegateCommand<UdpServerView> PageLoadedCommand { get; }
        public DelegateCommand<ListView> ConfigItemSelectedCommand { get; }
        public DelegateCommand AddConfigCommand { get; }
        public DelegateCommand DeleteConfigCommand { get; }
        public DelegateCommand EditConfigCommand { get; }
        public DelegateCommand StartListenCommand { get; }
        public DelegateCommand ClearMessageCommand { get; }
        public DelegateCommand<ListView> ClientItemSelectedCommand { get; }
        public DelegateCommand SendMessageCommand { get; }

        #endregion

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

        private string _connectState = "未在监听";

        public string ConnectState
        {
            get => _connectState;
            private set
            {
                _connectState = value;
                RaisePropertyChanged();
            }
        }

        private string _connectButtonState = "开始监听";

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

        private ObservableCollection<ConnectedClientModel> _connectedClients =
            new ObservableCollection<ConnectedClientModel>();

        public ObservableCollection<ConnectedClientModel> ConnectedClients
        {
            get => _connectedClients;
            set
            {
                _connectedClients = value;
                RaisePropertyChanged();
            }
        }

        #endregion

        private UdpServerView _viewPage;
        private ConnectedClientModel _selectedClientModel;
        private readonly IApplicationDataService _applicationDataService;
        private readonly UdpSession _udpSession = new UdpSession();

        public UdpServerViewModel(IApplicationDataService applicationDataService)
        {
            _applicationDataService = applicationDataService;
            PageLoadedCommand = new DelegateCommand<UdpServerView>(it =>
            {
                Debug.WriteLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " TcpServerViewModel => 加载");
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

            _udpSession.Received += delegate(EndPoint endpoint, ByteBlock byteBlock, IRequestInfo info)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    var message = _viewPage.TextRadioButton.IsChecked == true
                        ? Encoding.UTF8.GetString(byteBlock.Buffer, 0, byteBlock.Len)
                        : BitConverter.ToString(byteBlock.Buffer, 0, byteBlock.Len).Replace("-", " ");

                    ChatMessages.Add(new ChatMessageModel
                    {
                        MessageTime = DateTime.Now.ToString("yyyy年MM月dd HH时mm分ss秒"),
                        Message = message,
                        IsSend = false
                    });

                    if (ConnectedClients.All(x => x.ClientId != endpoint.GetHashCode().ToString()))
                    {
                        ConnectedClients.Add(new ConnectedClientModel
                        {
                            ClientId = endpoint.GetHashCode().ToString(),
                            ClientConnectColorBrush = "LimeGreen",
                            ClientHostAddress = endpoint.GetIP() + ":" + endpoint.GetPort()
                        });
                    }
                });
            };

            AddConfigCommand = new DelegateCommand(delegate
            {
                var configModel = new ConnectionConfigModel
                {
                    Comment = "",
                    ConnType = "UDP服务端",
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

            StartListenCommand = new DelegateCommand(delegate
            {
                if (ConfigModel == null)
                {
                    MessageBox.Show("无配置项，无法启动监听", "温馨提示", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                else
                {
                    var config = new TouchSocketConfig();
                    config.SetBindIPHost(new IPHost(ConfigModel.ConnHost + ":" + ConfigModel.ConnPort));

                    //载入配置
                    _udpSession.Setup(config);
                    try
                    {
                        if (_connectButtonState == "开始监听")
                        {
                            _udpSession.Start();

                            ConnectColorBrush = "LimeGreen";
                            ConnectState = "监听中";
                            ConnectButtonState = "停止监听";
                        }
                        else
                        {
                            _udpSession.Stop();

                            ConnectColorBrush = "DarkGray";
                            ConnectState = "未在监听";
                            ConnectButtonState = "开始监听";
                        }
                    }
                    catch (SocketException e)
                    {
                        MessageBox.Show(e.Message, "温馨提示", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            });

            ClearMessageCommand = new DelegateCommand(() => { ChatMessages.Clear(); });

            ClientItemSelectedCommand = new DelegateCommand<ListView>(it =>
            {
                if (it.SelectedIndex == -1)
                {
                    return;
                }

                _selectedClientModel = (ConnectedClientModel)it.SelectedItem;
            });

            SendMessageCommand = new DelegateCommand(delegate
            {
                if (string.IsNullOrEmpty(_userInputText))
                {
                    MessageBox.Show("不能发送空消息", "温馨提示", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (_selectedClientModel != null)
                {
                    try
                    {
                        if (_viewPage.TextRadioButton.IsChecked == true)
                        {
                            var endPoint = new IPHost(_selectedClientModel.ClientHostAddress).EndPoint;
                            _udpSession.Send(endPoint, _userInputText);

                            ChatMessages.Add(new ChatMessageModel
                            {
                                MessageTime = DateTime.Now.ToString("yyyy年MM月dd HH时mm分ss秒"),
                                Message = _userInputText,
                                IsSend = true
                            });
                        }
                        else
                        {
                            if (_userInputText.IsHex())
                            {
                                var buffer = Encoding.UTF8.GetBytes(_userInputText);
                                //以UTF-8的编码同步发送字符串
                                var endPoint = new IPHost(_selectedClientModel.ClientHostAddress).EndPoint;
                                _udpSession.Send(endPoint, buffer);

                                ChatMessages.Add(new ChatMessageModel
                                {
                                    MessageTime = DateTime.Now.ToString("yyyy年MM月dd HH时mm分ss秒"),
                                    Message = _userInputText,
                                    IsSend = true
                                });
                            }
                            else
                            {
                                MessageBox.Show("数据格式错误，无法发送", "温馨提示", MessageBoxButton.OK, MessageBoxImage.Error);
                            }
                        }
                    }
                    catch (NotConnectedException e)
                    {
                        MessageBox.Show(e.Message, "温馨提示", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                else
                {
                    MessageBox.Show("请指定接收消息的客户端", "温馨提示", MessageBoxButton.OK, MessageBoxImage.Error);
                }
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
    }
}