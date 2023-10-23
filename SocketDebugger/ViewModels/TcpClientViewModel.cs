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
using TcpClient = TouchSocket.Sockets.TcpClient;

namespace SocketDebugger.ViewModels
{
    public class TcpClientViewModel : BindableBase
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

        public DelegateCommand<ListView> ConfigItemSelectedCommand { get; set; }
        public DelegateCommand AddConfigCommand { get; set; }
        public DelegateCommand DeleteConfigCommand { get; set; }
        public DelegateCommand EditConfigCommand { get; set; }
        public DelegateCommand ConnectServerCommand { get; set; }
        public DelegateCommand ClearMessageCommand { get; set; }
        public DelegateCommand SendMessageCommand { get; set; }

        #endregion

        private readonly IDialogService _dialogService;
        private readonly TcpClient _tcpClient = new TcpClient();
        private readonly DispatcherTimer _timer = new DispatcherTimer();

        public TcpClientViewModel(IApplicationDataService dataService, IDialogService dialogService)
        {
            _dialogService = dialogService;

            ConfigModels = dataService.GetConfigModels();
            if (ConfigModels.Any())
            {
                ConfigModel = ConfigModels[0];
            }

            _tcpClient.Connected += delegate
            {
                ConnectColorBrush = "LimeGreen";
                ConnectState = "已连接";
                ConnectButtonState = "断开";
            };
            _tcpClient.Disconnected += delegate
            {
                ConnectColorBrush = "DarkGray";
                ConnectState = "未连接";
                ConnectButtonState = "连接";

                if (_timer.IsEnabled)
                {
                    _timer.Stop();
                }
            };
            _tcpClient.Received += delegate(TcpClient client, ByteBlock block, IRequestInfo info)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    // var message = _clientView.TextRadioButton.IsChecked == true
                    //     ? Encoding.UTF8.GetString(block.Buffer, 0, block.Len)
                    //     : BitConverter.ToString(block.Buffer, 0, block.Len).Replace("-", " ");
                    //
                    // ChatMessages.Add(new ChatMessageModel
                    // {
                    //     MessageTime = DateTime.Now.ToString("yyyy年MM月dd HH时mm分ss秒"),
                    //     Message = message,
                    //     IsSend = false
                    // });
                });
            };

            ConfigItemSelectedCommand = new DelegateCommand<ListView>(delegate(ListView view)
            {
                ConfigModel = (ConnectionConfigModel)view.SelectedItem;
            });

            AddConfigCommand = new DelegateCommand(delegate
            {
                var configModel = new ConnectionConfigModel
                {
                    Comment = "",
                    ConnType = "TCP客户端",
                    ConnHost = dataService.GetHostAddress(),
                    ConnPort = "8080"
                };

                _dialogService.ShowDialog("ConfigDialog", new DialogParameters
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
                    _dialogService.ShowDialog("AlertControlDialog", new DialogParameters
                        {
                            { "AlertType", AlertType.Warning }, { "Message", "是否删除当前配置？" }
                        },
                        delegate(IDialogResult dialogResult)
                        {
                            if (dialogResult.Result == ButtonResult.OK)
                            {
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
                        }
                    );
                }
                else
                {
                    _dialogService.ShowDialog("AlertMessageDialog", new DialogParameters
                        {
                            { "AlertType", AlertType.Error }, { "Message", "没有配置，无法删除" }
                        },
                        delegate { }
                    );
                }
            });

            EditConfigCommand = new DelegateCommand(delegate
            {
                if (ConfigModel == null)
                {
                    _dialogService.ShowDialog("AlertMessageDialog", new DialogParameters
                        {
                            { "AlertType", AlertType.Error }, { "Message", "无配置项，无法编辑" }
                        },
                        delegate { }
                    );
                }
                else
                {
                    _dialogService.ShowDialog("ConfigDialog", new DialogParameters
                        {
                            { "Title", "编辑配置" }, { "ConfigModel", _configModel }
                        },
                        delegate(IDialogResult result)
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

            ConnectServerCommand = new DelegateCommand(delegate
            {
                if (ConfigModel == null)
                {
                    _dialogService.ShowDialog("AlertMessageDialog", new DialogParameters
                        {
                            { "AlertType", AlertType.Error }, { "Message", "无配置项，无法启动监听" }
                        },
                        delegate { }
                    );
                }
                else
                {
                    //声明配置
                    var config = new TouchSocketConfig();
                    config.SetRemoteIPHost(new IPHost(ConfigModel.ConnHost + ":" + ConfigModel.ConnPort))
                        .UsePlugin()
                        .ConfigurePlugins(manager => { manager.UseReconnection(5, true, 3000); });

                    //载入配置
                    _tcpClient.Setup(config);
                    try
                    {
                        if (_connectButtonState == "连接")
                        {
                            _tcpClient.Connect();
                        }
                        else
                        {
                            _tcpClient.Close();
                        }
                    }
                    catch (SocketException e)
                    {
                        _dialogService.ShowDialog("AlertMessageDialog", new DialogParameters
                            {
                                { "AlertType", AlertType.Error }, { "Message", e.Message }
                            },
                            delegate { }
                        );
                    }
                }
            });

            ClearMessageCommand = new DelegateCommand(delegate { ChatMessages.Clear(); });

            SendMessageCommand = new DelegateCommand(delegate { SendMessage(_userInputText); });

            //自动发消息
            _timer.Tick += delegate { SendMessage(ConfigModel.Message); };
        }

        /// <summary>
        /// 发送消息
        /// </summary>
        private void SendMessage(string message)
        {
            if (string.IsNullOrEmpty(message))
            {
                _dialogService.ShowDialog("AlertMessageDialog", new DialogParameters
                    {
                        { "AlertType", AlertType.Error }, { "Message", "不能发送空消息" }
                    },
                    delegate { }
                );
                return;
            }

            // if (_clientView.TextRadioButton.IsChecked == true)
            // {
            //     if (ConnectState == "未连接")
            //     {
            //         MessageBox.Show("未连接成功，无法发送消息", "温馨提示", MessageBoxButton.OK, MessageBoxImage.Error);
            //         return;
            //     }
            //
            //     _tcpClient.Send(message);
            //
            //     ChatMessages.Add(new ChatMessageModel
            //     {
            //         MessageTime = DateTime.Now.ToString("yyyy年MM月dd HH时mm分ss秒"),
            //         Message = message,
            //         IsSend = true
            //     });
            // }
            // else
            // {
            //     if (message.IsHex())
            //     {
            //         if (ConnectState == "未连接")
            //         {
            //             MessageBox.Show("未连接成功，无法发送消息", "温馨提示", MessageBoxButton.OK, MessageBoxImage.Error);
            //             return;
            //         }
            //
            //         var buffer = Encoding.UTF8.GetBytes(message);
            //         //以UTF-8的编码同步发送字符串
            //         _tcpClient.Send(buffer);
            //
            //         ChatMessages.Add(new ChatMessageModel
            //         {
            //             MessageTime = DateTime.Now.ToString("yyyy年MM月dd HH时mm分ss秒"),
            //             Message = message,
            //             IsSend = true
            //         });
            //     }
            //     else
            //     {
            //         MessageBox.Show("数据格式错误，无法发送", "温馨提示", MessageBoxButton.OK, MessageBoxImage.Error);
            //     }
            // }
        }
    }
}