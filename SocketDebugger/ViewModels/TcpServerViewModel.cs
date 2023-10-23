using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Sockets;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Services.Dialogs;
using SocketDebugger.Model;
using SocketDebugger.Services;
using SocketDebugger.Utils;
using TouchSocket.Core;
using TouchSocket.Sockets;
using MessageBox = HandyControl.Controls.MessageBox;

namespace SocketDebugger.ViewModels
{
    public class TcpServerViewModel : BindableBase
    {
        #region DelegateCommand

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

        private ConnectedClientModel _selectedClientModel;
        private readonly TcpService _tcpService = new TcpService();
        private readonly DispatcherTimer _timer = new DispatcherTimer();

        public TcpServerViewModel(IApplicationDataService dataService, IDialogService dialogService)
        {
            ConfigModels = dataService.GetConfigModels();
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

            _tcpService.Connected = (client, e) =>
            {
                Application.Current.Dispatcher.Invoke(delegate
                {
                    ConnectedClients.Add(new ConnectedClientModel
                    {
                        ClientId = client.ID,
                        ClientConnectColorBrush = "LimeGreen",
                        ClientHostAddress = client.IP + ":" + client.Port
                    });
                });
            };
            _tcpService.Disconnected = (client, e) =>
            {
                Application.Current.Dispatcher.Invoke(delegate
                {
                    var clientModel = ConnectedClients.FirstOrDefault(x => x.ClientId == $"{client.ID}");

                    if (clientModel != null)
                    {
                        //改变连接颜色
                        clientModel.ClientConnectColorBrush = "DarkGray";

                        //没有update函数，只能先删除再添加
                        ConnectedClients.Remove(clientModel);
                        ConnectedClients.Add(clientModel);
                    }
                });
            };
            _tcpService.Received = (client, byteBlock, requestInfo) =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    // var message = _viewPage.TextRadioButton.IsChecked == true
                    //     ? Encoding.UTF8.GetString(byteBlock.Buffer, 0, byteBlock.Len)
                    //     : BitConverter.ToString(byteBlock.Buffer, 0, byteBlock.Len).Replace("-", " ");
                    //
                    // ChatMessages.Add(new ChatMessageModel
                    // {
                    //     MessageTime = DateTime.Now.ToString("yyyy年MM月dd HH时mm分ss秒"),
                    //     Message = message,
                    //     IsSend = false
                    // });
                });
            };

            AddConfigCommand = new DelegateCommand(delegate
            {
                var configModel = new ConnectionConfigModel
                {
                    Comment = "",
                    ConnType = "TCP服务端",
                    ConnHost = SystemHelper.GetHostAddress(),
                    ConnPort = "8080"
                };

                dialogService.ShowDialog("ConfigDialog", new DialogParameters
                {
                    { "Title", "添加配置" }, { "ConfigModel", configModel }
                }, delegate(IDialogResult result)
                {
                    if (result.Result == ButtonResult.OK)
                    {
                        //更新列表
                        ConfigModels = dataService.GetConfigModels();

                        ConfigModel = result.Parameters.GetValue<ConnectionConfigModel>("ConfigModel");

                        if (string.IsNullOrEmpty(ConfigModel.Message))
                        {
                            //停止循环
                            _timer.Stop();
                        }
                        else
                        {
                            _timer.Interval = TimeSpan.FromMilliseconds(double.Parse(ConfigModel.TimePeriod));
                            _timer.Start();
                        }
                    }
                });
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

                    ConfigModels = dataService.GetConfigModels();
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
                    dialogService.ShowDialog("ConfigDialog", new DialogParameters
                    {
                        { "Title", "编辑配置" }, { "ConfigModel", _configModel }
                    }, delegate(IDialogResult result)
                    {
                        if (result.Result == ButtonResult.OK)
                        {
                            //更新列表
                            ConfigModels = dataService.GetConfigModels();

                            ConfigModel = result.Parameters.GetValue<ConnectionConfigModel>("ConfigModel");

                            if (string.IsNullOrEmpty(ConfigModel.Message))
                            {
                                //停止循环
                                _timer.Stop();
                            }
                            else
                            {
                                _timer.Interval = TimeSpan.FromMilliseconds(double.Parse(ConfigModel.TimePeriod));
                                _timer.Start();
                            }
                        }
                    });
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
                    config.SetListenIPHosts(new[] { new IPHost(ConfigModel.ConnHost + ":" + ConfigModel.ConnPort) });

                    //载入配置
                    _tcpService.Setup(config);
                    try
                    {
                        if (_connectButtonState == "开始监听")
                        {
                            _tcpService.Start();

                            ConnectColorBrush = "LimeGreen";
                            ConnectState = "监听中";
                            ConnectButtonState = "停止监听";
                        }
                        else
                        {
                            _tcpService.Stop();

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
                        // if (_viewPage.TextRadioButton.IsChecked == true)
                        // {
                        //     try
                        //     {
                        //         _tcpService.Send(_selectedClientModel.ClientId, _userInputText);
                        //
                        //         ChatMessages.Add(new ChatMessageModel
                        //         {
                        //             MessageTime = DateTime.Now.ToString("yyyy年MM月dd HH时mm分ss秒"),
                        //             Message = _userInputText,
                        //             IsSend = true
                        //         });
                        //     }
                        //     catch (ClientNotFindException)
                        //     {
                        //         MessageBox.Show("客户端已断开，无法发送消息", "温馨提示", MessageBoxButton.OK, MessageBoxImage.Error);
                        //     }
                        // }
                        // else
                        // {
                        //     if (_userInputText.IsHex())
                        //     {
                        //         try
                        //         {
                        //             var buffer = Encoding.UTF8.GetBytes(_userInputText);
                        //             //以UTF-8的编码同步发送字符串
                        //             _tcpService.Send(_selectedClientModel.ClientId, buffer);
                        //
                        //             ChatMessages.Add(new ChatMessageModel
                        //             {
                        //                 MessageTime = DateTime.Now.ToString("yyyy年MM月dd HH时mm分ss秒"),
                        //                 Message = _userInputText,
                        //                 IsSend = true
                        //             });
                        //         }
                        //         catch (ClientNotFindException)
                        //         {
                        //             MessageBox.Show("客户端已断开，无法发送消息", "温馨提示", MessageBoxButton.OK,
                        //                 MessageBoxImage.Error);
                        //         }
                        //     }
                        //     else
                        //     {
                        //         MessageBox.Show("数据格式错误，无法发送", "温馨提示", MessageBoxButton.OK, MessageBoxImage.Error);
                        //     }
                        // }
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
    }
}