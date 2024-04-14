using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Windows;
using System.Windows.Threading;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using Prism.Services.Dialogs;
using SocketDebugger.Events;
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

        private ConnectionConfigModel _selectedConfigModel;

        public ConnectionConfigModel SelectedConfigModel
        {
            get => _selectedConfigModel;
            private set
            {
                _selectedConfigModel = value;
                RaisePropertyChanged();
            }
        }

        private int _index;

        public int Index
        {
            get => _index;
            set
            {
                _index = value;
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

        private string _messageCycleTime = string.Empty;

        public string MessageCycleTime
        {
            get => _messageCycleTime;
            set
            {
                _messageCycleTime = value;
                RaisePropertyChanged();
            }
        }

        private bool _isCycleChecked;

        public bool IsCycleChecked
        {
            get => _isCycleChecked;
            set
            {
                _isCycleChecked = value;
                RaisePropertyChanged();
            }
        }
        #endregion

        #region DelegateCommand

        public DelegateCommand<ConnectionConfigModel> ConfigItemSelectedCommand { get; set; }
        public DelegateCommand AddConfigCommand { get; set; }
        public DelegateCommand DeleteConfigCommand { get; set; }
        public DelegateCommand EditConfigCommand { get; set; }
        public DelegateCommand ConnectServerCommand { get; set; }
        public DelegateCommand ClearMessageCommand { get; set; }
        public DelegateCommand SendMessageCommand { get; set; }
        public DelegateCommand CycleCheckedCommand { get; set; }
        public DelegateCommand CycleUncheckedCommand { get; set; }

        #endregion

        private readonly IApplicationDataService _dataService;
        private readonly IDialogService _dialogService;
        private readonly IEventAggregator _eventAggregator;
        private readonly TcpClient _tcpClient = new TcpClient();
        private readonly DispatcherTimer _timer = new DispatcherTimer();

        public TcpClientViewModel(IApplicationDataService dataService, IDialogService dialogService,
            IEventAggregator eventAggregator)
        {
            _dataService = dataService;
            _dialogService = dialogService;
            _eventAggregator = eventAggregator;

            InitMessageType(0);

            InitDelegate();

            ConfigItemSelectedCommand = new DelegateCommand<ConnectionConfigModel>(
                delegate(ConnectionConfigModel configModel) { SelectedConfigModel = configModel; });

            AddConfigCommand = new DelegateCommand(delegate
            {
                var configModel = new ConnectionConfigModel
                {
                    ConnectionTitle = "",
                    ConnectionType = "TCP客户端",
                    ConnectionHost = dataService.GetHostAddress(),
                    ConnectionPort = "8080"
                };

                _dialogService.ShowDialog("ConfigDialog", new DialogParameters
                    {
                        { "Title", "添加配置" }, { "SelectedConfigModel", configModel }
                    },
                    delegate(IDialogResult result)
                    {
                        if (result.Result == ButtonResult.OK)
                        {
                            //更新列表
                            ConfigModels = dataService.GetConfigModels();

                            //选中最新添加的数据
                            Index = ConfigModels.Count - 1;

                            //更新最右侧面板
                            SelectedConfigModel =
                                result.Parameters.GetValue<ConnectionConfigModel>("SelectedConfigModel");
                            if (SelectedConfigModel.MessageType == "文本")
                            {
                                IsTextChecked = true;
                            }
                            else
                            {
                                IsHexChecked = true;
                            }
                        }
                    }
                );
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
                                    manager.Delete(ConfigModels[_index]);
                                }

                                ConfigModels = dataService.GetConfigModels();
                                if (ConfigModels.Any())
                                {
                                    SelectedConfigModel = ConfigModels[0];
                                    //选中第一条
                                    Index = 0;
                                }
                            }
                        }
                    );
                }
                else
                {
                    "无配置项，无法删除".ShowAlertMessageDialog(_dialogService, AlertType.Error);
                }
            });

            EditConfigCommand = new DelegateCommand(delegate
            {
                var tempIndex = _index;
                _dialogService.ShowDialog("ConfigDialog", new DialogParameters
                    {
                        { "Title", "编辑配置" }, { "SelectedConfigModel", _selectedConfigModel }
                    },
                    delegate(IDialogResult result)
                    {
                        if (result.Result == ButtonResult.OK)
                        {
                            //更新列表
                            ConfigModels = dataService.GetConfigModels();

                            //Index保持不变
                            Index = tempIndex;

                            SelectedConfigModel =
                                result.Parameters.GetValue<ConnectionConfigModel>("SelectedConfigModel");
                            if (SelectedConfigModel.MessageType == "文本")
                            {
                                IsTextChecked = true;
                            }
                            else
                            {
                                IsHexChecked = true;
                            }
                        }
                    }
                );
            });

            ConnectServerCommand = new DelegateCommand(delegate
            {
                //声明配置
                var config = new TouchSocketConfig();
                config.SetRemoteIPHost(new IPHost(
                    _selectedConfigModel.ConnectionHost + ":" + _selectedConfigModel.ConnectionPort)
                ).UsePlugin().ConfigurePlugins(
                    manager => { manager.UseReconnection(5, true, 3000); }
                );

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
                    e.Message.ShowAlertMessageDialog(_dialogService, AlertType.Error);
                }
            });

            ClearMessageCommand = new DelegateCommand(delegate { ChatMessages.Clear(); });

            SendMessageCommand = new DelegateCommand(delegate { SendMessage(_userInputText); });

            //周期发送CheckBox选中、取消选中事件
            CycleCheckedCommand = new DelegateCommand(delegate
            {
                //判断周期时间是否为空
                if (_messageCycleTime.IsNullOrWhiteSpace())
                {
                    "请先设置周期发送的时间间隔".ShowAlertMessageDialog(_dialogService, AlertType.Error);
                    IsCycleChecked = false;
                    return;
                }

                //判断周期时间是否是数字
                if (!_messageCycleTime.IsNumber())
                {
                    "时间间隔只能是数字".ShowAlertMessageDialog(_dialogService, AlertType.Error);
                    IsCycleChecked = false;
                    return;
                }

                _timer.Interval = TimeSpan.FromMilliseconds(double.Parse(_messageCycleTime));
                _timer.Start();
            });
            CycleUncheckedCommand = new DelegateCommand(delegate
            {
                //停止timer
                if (_timer.IsEnabled)
                {
                    _timer.Stop();
                }
            });
            //自动发消息
            _timer.Tick += delegate { SendMessage(_userInputText); };
            
            //TODO Test
            // ChatMessages.Add(new ChatMessageModel
            // {
            //     MessageTime = DateTime.Now.ToString("HH:mm:ss"),
            //     Message = "发送的消息发送的消息发送的消息发送的消息发送的消息发送的消息发送的消息发送的消息发送的消息发送的消息发送的消息",
            //     IsSend = true
            // });
            //
            // ChatMessages.Add(new ChatMessageModel
            // {
            //     MessageTime = DateTime.Now.ToString("HH:mm:ss"),
            //     Message = "接收的消息接收的消息接收的消息接收的消息接收的消息接收的消息接收的消息接收的消息接收的消息接收的消息接收的消息",
            //     IsSend = false
            // });
        }

        private void InitMessageType(int index)
        {
            ConfigModels = _dataService.GetConfigModels();
            if (ConfigModels.Any())
            {
                //选中第一条
                Index = 0;

                SelectedConfigModel = ConfigModels[index];
                if (SelectedConfigModel.MessageType == "文本")
                {
                    IsTextChecked = true;
                    IsHexChecked = false;
                }
                else
                {
                    IsTextChecked = false;
                    IsHexChecked = true;
                }
            }
        }

        private void InitDelegate()
        {
            _eventAggregator.GetEvent<MainMenuSelectedEvent>().Subscribe(InitMessageType);

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
                var message = _isTextChecked
                    ? Encoding.UTF8.GetString(block.Buffer, 0, block.Len)
                    : BitConverter.ToString(block.Buffer, 0, block.Len).Replace("-", " ");

                Application.Current.Dispatcher.Invoke(delegate
                {
                    ChatMessages.Add(new ChatMessageModel
                    {
                        MessageTime = DateTime.Now.ToString("HH:mm:ss"),
                        Message = message,
                        IsSend = false
                    });
                });
            };
        }

        /// <summary>
        /// 发送消息
        /// </summary>
        private void SendMessage(string message)
        {
            if (string.IsNullOrEmpty(message))
            {
                "不能发送空消息".ShowAlertMessageDialog(_dialogService, AlertType.Error);
                return;
            }

            if (_isTextChecked)
            {
                if (ConnectState == "未连接")
                {
                    "未连接成功，无法发送消息".ShowAlertMessageDialog(_dialogService, AlertType.Error);
                    return;
                }

                _tcpClient.Send(message);

                ChatMessages.Add(new ChatMessageModel
                {
                    MessageTime = DateTime.Now.ToString("HH:mm:ss"),
                    Message = message,
                    IsSend = true
                });
            }
            else
            {
                if (message.IsHex())
                {
                    if (ConnectState == "未连接")
                    {
                        "未连接成功，无法发送消息".ShowAlertMessageDialog(_dialogService, AlertType.Warning);
                        return;
                    }

                    var buffer = Encoding.UTF8.GetBytes(message);
                    //以UTF-8的编码同步发送字符串
                    _tcpClient.Send(buffer);

                    ChatMessages.Add(new ChatMessageModel
                    {
                        MessageTime = DateTime.Now.ToString("HH:mm:ss"),
                        Message = message,
                        IsSend = true
                    });
                }
                else
                {
                    "数据格式错误，无法发送".ShowAlertMessageDialog(_dialogService, AlertType.Error);
                }
            }
        }
    }
}