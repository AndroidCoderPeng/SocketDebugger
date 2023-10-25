using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
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

namespace SocketDebugger.ViewModels
{
    public class UdpServerViewModel : BindableBase
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

        private bool _isTextChecked = true;

        public bool IsTextChecked
        {
            get => _isTextChecked;
            set
            {
                _isTextChecked = value;
                RaisePropertyChanged();
            }
        }

        private bool _isHexChecked = true;

        public bool IsHexChecked
        {
            get => _isHexChecked;
            set
            {
                _isHexChecked = value;
                RaisePropertyChanged();
            }
        }

        #endregion

        private readonly IDialogService _dialogService;
        private ConnectedClientModel _selectedClientModel;
        private readonly UdpSession _udpSession = new UdpSession();
        private readonly DispatcherTimer _timer = new DispatcherTimer();

        public UdpServerViewModel(IApplicationDataService dataService, IDialogService dialogService)
        {
            _dialogService = dialogService;
            
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

            _udpSession.Received += delegate(EndPoint endpoint, ByteBlock block, IRequestInfo info)
            {
                var message = _isTextChecked
                    ? Encoding.UTF8.GetString(block.Buffer, 0, block.Len)
                    : BitConverter.ToString(block.Buffer, 0, block.Len).Replace("-", " ");

                Application.Current.Dispatcher.Invoke(() =>
                {
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
                
                dialogService.ShowDialog("ConfigDialog", new DialogParameters
                    {
                        { "Title", "添加配置" }, { "ConfigModel", configModel }
                    },
                    delegate(IDialogResult result)
                    {
                        if (result.Result == ButtonResult.OK)
                        {
                            //更新列表
                            ConfigModels = dataService.GetConfigModels();

                            ConfigModel = result.Parameters.GetValue<ConnectionConfigModel>("ConfigModel");

                            if (ConfigModel.MsgType == "文本")
                            {
                                IsTextChecked = true;
                            }
                            else
                            {
                                IsHexChecked = true;
                            }
                            
                            if (_timer.IsEnabled)
                            {
                                _timer.Stop();
                            }

                            if (ConfigModel.TimePeriod == null)
                            {
                                return;
                            }

                            _timer.Interval = TimeSpan.FromMilliseconds(double.Parse(ConfigModel.TimePeriod));
                            _timer.Start();
                        }
                    }
                );
            });

            DeleteConfigCommand = new DelegateCommand(delegate
            {
                if (ConfigModels.Any())
                {
                    dialogService.ShowDialog("AlertControlDialog", new DialogParameters
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
                    ShowAlertMessageDialog(AlertType.Error, "没有配置，无法删除");
                }
            });

            EditConfigCommand = new DelegateCommand(delegate
            {
                if (ConfigModel == null)
                {
                    ShowAlertMessageDialog(AlertType.Error, "没有配置，无法编辑");
                }
                else
                {
                    dialogService.ShowDialog("ConfigDialog", new DialogParameters
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

                                if (ConfigModel.MsgType == "文本")
                                {
                                    IsTextChecked = true;
                                }
                                else
                                {
                                    IsHexChecked = true;
                                }
                            
                                if (_timer.IsEnabled)
                                {
                                    _timer.Stop();
                                }

                                if (ConfigModel.TimePeriod == null)
                                {
                                    return;
                                }

                                _timer.Interval = TimeSpan.FromMilliseconds(double.Parse(ConfigModel.TimePeriod));
                                _timer.Start();
                            }
                        }
                    );
                }
            });

            StartListenCommand = new DelegateCommand(delegate
            {
                if (ConfigModel == null)
                {
                    ShowAlertMessageDialog(AlertType.Error, "无配置项，无法启动监听");
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
                        ShowAlertMessageDialog(AlertType.Error, e.Message);
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
                    ShowAlertMessageDialog(AlertType.Error, "不能发送空消息");
                    return;
                }

                if (_selectedClientModel != null)
                {
                    try
                    {
                        if (_isTextChecked)
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
                                ShowAlertMessageDialog(AlertType.Error, "数据格式错误，无法发送");
                            }
                        }
                    }
                    catch (NotConnectedException e)
                    {
                        ShowAlertMessageDialog(AlertType.Error, e.Message);
                    }
                }
                else
                {
                    ShowAlertMessageDialog(AlertType.Error, "请指定接收消息的客户端");
                }
            });
        }
        
        /// <summary>
        /// 显示普通提示对话框
        /// </summary>
        private void ShowAlertMessageDialog(AlertType type, string message)
        {
            _dialogService.ShowDialog("AlertMessageDialog", new DialogParameters
                {
                    { "AlertType", type }, { "Message", message }
                },
                delegate { }
            );
        }
    }
}