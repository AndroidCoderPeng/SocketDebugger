using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Sockets;
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
using SuperSocket.SocketBase;
using SuperSocket.WebSocket;
using TouchSocket.Core;
using TouchSocket.Sockets;

namespace SocketDebugger.ViewModels
{
    public class WebSocketServerViewModel : BindableBase
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

        private int _clientIndex;

        public int ClientIndex
        {
            get => _clientIndex;
            set
            {
                _clientIndex = value;
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
        public DelegateCommand StartListenCommand { get; set; }
        public DelegateCommand ClearMessageCommand { get; set; }
        public DelegateCommand<ConnectedClientModel> ClientItemSelectedCommand { get; set; }
        public DelegateCommand SendMessageCommand { get; set; }
        public DelegateCommand CycleCheckedCommand { get; set; }
        public DelegateCommand CycleUncheckedCommand { get; set; }

        #endregion

        private readonly IApplicationDataService _dataService;
        private readonly IDialogService _dialogService;
        private readonly DispatcherTimer _timer = new DispatcherTimer();
        private WebSocketServer _webSocketService;
        private ConnectedClientModel _selectedClientModel;

        public WebSocketServerViewModel(IApplicationDataService dataService, IDialogService dialogService,
            IEventAggregator eventAggregator)
        {
            _dataService = dataService;
            _dialogService = dialogService;

            InitMessageType();

            eventAggregator.GetEvent<MainMenuSelectedEvent>().Subscribe(InitMessageType);

            ConfigItemSelectedCommand = new DelegateCommand<ConnectionConfigModel>(
                delegate(ConnectionConfigModel configModel) { SelectedConfigModel = configModel; });

            AddConfigCommand = new DelegateCommand(delegate
            {
                var configModel = new ConnectionConfigModel
                {
                    ConnectionTitle = "",
                    ConnectionType = "WebSocket服务端",
                    ConnectionHost = SystemHelper.GetHostAddress(),
                    ConnectionPort = "8080"
                };

                dialogService.ShowDialog("ConfigDialog", new DialogParameters
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
                                    manager.Delete(ConfigModels[_index]);
                                }

                                ConfigModels = dataService.GetConfigModels();
                                if (ConfigModels.Any())
                                {
                                    SelectedConfigModel = ConfigModels.First();
                                    //选中第一条
                                    Index = 0;
                                }
                            }
                        }
                    );
                }
                else
                {
                    "没有配置，无法删除".ShowAlertMessageDialog(_dialogService, AlertType.Error);
                }
            });

            EditConfigCommand = new DelegateCommand(delegate
            {
                var tempIndex = _index;
                dialogService.ShowDialog("ConfigDialog", new DialogParameters
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

            StartListenCommand = new DelegateCommand(() =>
            {
                if (_webSocketService == null)
                {
                    _webSocketService = new WebSocketServer();
                    _webSocketService.Setup(_selectedConfigModel.ConnectionHost,
                        Convert.ToInt32(_selectedConfigModel.ConnectionPort));
                }

                _webSocketService.NewSessionConnected += SessionConnectedEvent;
                _webSocketService.NewMessageReceived += MessageReceivedEvent;
                _webSocketService.SessionClosed += SessionClosedEvent;

                try
                {
                    if (_connectButtonState == "开始监听")
                    {
                        _webSocketService.Start();

                        ConnectColorBrush = "LimeGreen";
                        ConnectState = "监听中";
                        ConnectButtonState = "停止监听";
                    }
                    else
                    {
                        _timer.Stop();
                        _webSocketService.Stop();

                        ConnectColorBrush = "DarkGray";
                        ConnectState = "未在监听";
                        ConnectButtonState = "开始监听";

                        _webSocketService.NewSessionConnected -= SessionConnectedEvent;
                        _webSocketService.NewMessageReceived -= MessageReceivedEvent;
                        _webSocketService.SessionClosed -= SessionClosedEvent;

                        _webSocketService = null;
                    }
                }
                catch (SocketException e)
                {
                    e.Message.ShowAlertMessageDialog(_dialogService, AlertType.Error);
                }
            });

            ClearMessageCommand = new DelegateCommand(delegate { ChatMessages.Clear(); });

            ClientItemSelectedCommand = new DelegateCommand<ConnectedClientModel>(
                delegate(ConnectedClientModel clientModel) { _selectedClientModel = clientModel; });

            SendMessageCommand = new DelegateCommand(SendMessage);

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
            _timer.Tick += delegate { SendMessage(); };
        }

        private void InitMessageType()
        {
            ConfigModels = _dataService.GetConfigModels();
            if (ConfigModels.Any())
            {
                //选中第一条
                Index = 0;

                SelectedConfigModel = ConfigModels.First();
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

        /// <summary>
        /// 发送消息
        /// </summary>
        private void SendMessage()
        {
            if (string.IsNullOrEmpty(_userInputText))
            {
                "不能发送空消息".ShowAlertMessageDialog(_dialogService, AlertType.Error);
                return;
            }

            if (_selectedClientModel != null)
            {
                try
                {
                    var socketSession = _webSocketService.GetSessionByID(_selectedClientModel.ClientId);
                    if (_isTextChecked)
                    {
                        socketSession.Send(_userInputText);

                        ChatMessages.Add(new ChatMessageModel
                        {
                            MessageTime = DateTime.Now.ToString("HH:mm:ss"),
                            Message = _userInputText,
                            IsSend = true
                        });
                    }
                    else
                    {
                        if (_userInputText.IsHex())
                        {
                            //以UTF-8的编码同步发送字符串
                            var result = _userInputText.GetBytesWithUtf8();
                            socketSession.Send(result.Item2, 0, result.Item2.Length);

                            ChatMessages.Add(new ChatMessageModel
                            {
                                MessageTime = DateTime.Now.ToString("HH:mm:ss"),
                                Message = result.Item1.FormatHexString(),
                                IsSend = true
                            });
                        }
                    }
                }
                catch (ClientNotFindException e)
                {
                    e.Message.ShowAlertMessageDialog(_dialogService, AlertType.Error);
                }
            }
            else
            {
                "请指定接收消息的客户端".ShowAlertMessageDialog(_dialogService, AlertType.Error);
            }
        }

        private void MessageReceivedEvent(WebSocketSession session, string value)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                ChatMessages.Add(new ChatMessageModel
                {
                    MessageTime = DateTime.Now.ToString("HH:mm:ss"),
                    Message = value,
                    IsSend = false
                });
            });
        }

        private void SessionConnectedEvent(WebSocketSession session)
        {
            Application.Current.Dispatcher.Invoke(delegate
            {
                ConnectedClients.Add(new ConnectedClientModel
                {
                    ClientId = session.SessionID,
                    ClientConnectColorBrush = "LimeGreen",
                    ClientHostAddress = session.Config.Ip + ":" + session.Config.Port
                });
            });
        }

        private void SessionClosedEvent(WebSocketSession session, CloseReason value)
        {
            Application.Current.Dispatcher.Invoke(delegate
            {
                var clientModel = ConnectedClients.FirstOrDefault(x => x.ClientId == $"{session.SessionID}");

                if (clientModel != null)
                {
                    //改变连接颜色
                    clientModel.ClientConnectColorBrush = "DarkGray";

                    //没有update函数，只能先删除再添加
                    ConnectedClients.Remove(clientModel);
                    ConnectedClients.Add(clientModel);
                }
            });
        }
    }
}