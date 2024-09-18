using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

namespace SocketDebugger.ViewModels
{
    public class TcpServerViewModel : BindableBase
    {
        #region VM

        private ObservableCollection<ConnectionConfigModel> _connectionConfigCollection;

        public ObservableCollection<ConnectionConfigModel> ConnectionConfigCollection
        {
            get => _connectionConfigCollection;
            set
            {
                _connectionConfigCollection = value;
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

        private ConnectionConfigModel _selectedConfig;

        public ConnectionConfigModel SelectedConfig
        {
            get => _selectedConfig;
            set
            {
                _selectedConfig = value;
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
            set
            {
                _connectColorBrush = value;
                RaisePropertyChanged();
            }
        }

        private string _connectState = "未在监听";

        public string ConnectState
        {
            get => _connectState;
            set
            {
                _connectState = value;
                RaisePropertyChanged();
            }
        }

        private string _connectButtonState = "开始监听";

        public string ConnectButtonState
        {
            get => _connectButtonState;
            set
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

        private ObservableCollection<ConnectedClientModel> _connectedClients = new ObservableCollection<ConnectedClientModel>();

        public ObservableCollection<ConnectedClientModel> ConnectedClients
        {
            get => _connectedClients;
            set
            {
                _connectedClients = value;
                RaisePropertyChanged();
            }
        }

        private bool _isHexChecked;

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

        public DelegateCommand<ConnectionConfigModel> ConnectionItemSelectedCommand { set; get; }
        public DelegateCommand<ConnectionConfigModel> DeleteConnectionConfigCommand { set; get; }
        public DelegateCommand<string> AddConnectionConfigCommand { set; get; }
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
        private TcpService _tcpService;
        private bool _isListened;
        private ConnectedClientModel _selectedClient;

        public TcpServerViewModel(IApplicationDataService dataService, IDialogService dialogService,
            IEventAggregator eventAggregator)
        {
            _dataService = dataService;
            _dialogService = dialogService;

            InitDefaultConfig();

            eventAggregator.GetEvent<ChangeViewByMainMenuEvent>().Subscribe(ChangeViewByMainMenu);

            ConnectionItemSelectedCommand = new DelegateCommand<ConnectionConfigModel>(ConnectionItemSelected);
            DeleteConnectionConfigCommand = new DelegateCommand<ConnectionConfigModel>(DeleteConnectionConfig);
            AddConnectionConfigCommand = new DelegateCommand<string>(AddConnectionConfig);
            EditConfigCommand = new DelegateCommand(EditConnectionConfig);
            StartListenCommand = new DelegateCommand(StartListenPort);
            ClearMessageCommand = new DelegateCommand(ClearMessage);
            ClientItemSelectedCommand = new DelegateCommand<ConnectedClientModel>(ClientItemSelected);
            SendMessageCommand = new DelegateCommand(SendMessage);

            //周期发送CheckBox选中、取消选中事件
            CycleCheckedCommand = new DelegateCommand(CycleSendMessage);
            CycleUncheckedCommand = new DelegateCommand(StopCycleSendMessage);

            //自动发消息
            _timer.Tick += delegate { SendMessage(); };
        }

        private void InitDefaultConfig()
        {
            ConnectionConfigCollection = _dataService.GetConnectionCollection("TCP服务端");
            if (_connectionConfigCollection.Any())
            {
                SelectedConfig = _connectionConfigCollection.First();
                CurrentIndex = 0;
            }
        }

        private void ChangeViewByMainMenu(string type)
        {
            ConnectionConfigCollection = _dataService.GetConnectionCollection(type);
            if (_connectionConfigCollection.Any())
            {
                SelectedConfig = _connectionConfigCollection.First();
                CurrentIndex = 0;
            }
        }

        private void ConnectionItemSelected(ConnectionConfigModel configModel)
        {
            if (configModel == null)
            {
                return;
            }

            if (configModel.ConnectionType.Equals("TCP服务端"))
            {
                SelectedConfig = configModel;
            }
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
                //更新列表和面板
                ConnectionConfigCollection = _dataService.GetConnectionCollection(configModel.ConnectionType);
                if (_connectionConfigCollection.Any())
                {
                    SelectedConfig = _connectionConfigCollection.First();
                    CurrentIndex = 0;
                }
            }
        }

        private void AddConnectionConfig(string type)
        {
            if (type == null)
            {
                return;
            }

            var configModel = new ConnectionConfigModel
            {
                ConnectionTitle = "",
                ConnectionType = type,
                ConnectionHost = _dataService.GetHostAddress(),
                ConnectionPort = "8080"
            };

            var dialogParameters = new DialogParameters
            {
                { "Title", "添加配置" }, { "ConnectionConfigModel", configModel }
            };
            _dialogService.ShowDialog("ConfigDialog", dialogParameters, delegate(IDialogResult result)
                {
                    if (result.Result == ButtonResult.OK)
                    {
                        //更新列表和面板
                        ConnectionConfigCollection = _dataService.GetConnectionCollection(configModel.ConnectionType);
                        SelectedConfig = _connectionConfigCollection.Last();
                        CurrentIndex = _connectionConfigCollection.Count - 1;
                    }
                }
            );
        }

        private void EditConnectionConfig()
        {
            var dialogParameters = new DialogParameters
            {
                { "Title", "编辑配置" }, { "ConnectionConfigModel", _selectedConfig }
            };
            _dialogService.ShowDialog("ConfigDialog", dialogParameters, delegate(IDialogResult result)
                {
                    if (result.Result == ButtonResult.OK)
                    {
                        SelectedConfig = result.Parameters.GetValue<ConnectionConfigModel>("ConnectionConfigModel");
                    }
                }
            );
        }

        private void StartListenPort()
        {
            if (!_isListened)
            {
                _tcpService = new TcpService();
                var socketConfig = new TouchSocketConfig();
                socketConfig.SetListenIPHosts(
                    new IPHost($"{_selectedConfig.ConnectionHost}:{_selectedConfig.ConnectionPort}")
                );
                _tcpService.Setup(socketConfig);
                _tcpService.Start();
                ConnectColorBrush = "LimeGreen";
                ConnectState = "监听中";
                ConnectButtonState = "停止监听";

                _isListened = true;

                _tcpService.Connected += Client_Connected;
                _tcpService.Closing += Server_DisConnected;
                _tcpService.Closed += Client_DisConnected;
                _tcpService.Received += Message_Received;
            }
            else
            {
                _tcpService.Stop();
                ConnectColorBrush = "DarkGray";
                ConnectState = "未在监听";
                ConnectButtonState = "开始监听";

                _isListened = false;

                _tcpService.Connected -= Client_Connected;
                _tcpService.Closing -= Server_DisConnected;
                _tcpService.Closed -= Client_DisConnected;
                _tcpService.Received -= Message_Received;

                _timer.Stop();
            }
        }

        private Task Client_Connected(TcpSessionClient client, ConnectedEventArgs e)
        {
            Application.Current.Dispatcher.Invoke(delegate
            {
                ConnectedClients.Add(new ConnectedClientModel
                {
                    ClientId = client.Id,
                    ClientConnectColorBrush = "LimeGreen",
                    ClientHostAddress = $"{client.IP}:{client.Port}"
                });

                ClientIndex = ConnectedClients.Count - 1;
            });
            return EasyTask.CompletedTask;
        }

        private Task Server_DisConnected(TcpSessionClient client, ClosingEventArgs e)
        {
            Console.WriteLine(@"服务器主动断开");
            Application.Current.Dispatcher.Invoke(delegate
            {
                foreach (var model in ConnectedClients)
                {
                    //改变连接状态颜色
                    model.ClientConnectColorBrush = "DarkGray";
                }
            });
            return EasyTask.CompletedTask;
        }
        
        private Task Client_DisConnected(TcpSessionClient client, ClosedEventArgs e)
        {
            Console.WriteLine(@"客户端主动断开");
            Application.Current.Dispatcher.Invoke(delegate
            {
                //有客户端断开后，找到断开的那个客户端
                var clientModel = ConnectedClients.First(x => x.ClientId == $"{client.Id}");
                //改变连接状态颜色
                clientModel.ClientConnectColorBrush = "DarkGray";
            });
            return EasyTask.CompletedTask;
        }

        private Task Message_Received(TcpSessionClient client, ReceivedDataEventArgs e)
        {
            var bytes = e.ByteBlock.ToArray();

            var message = _isHexChecked
                ? BitConverter.ToString(bytes).Replace("-", " ")
                : Encoding.UTF8.GetString(bytes);

            Application.Current.Dispatcher.Invoke(() =>
            {
                ChatMessages.Add(new ChatMessageModel
                {
                    MessageTime = DateTime.Now.ToString("HH:mm:ss"),
                    Message = message,
                    IsSend = false
                });
            });
            return EasyTask.CompletedTask;
        }

        private void ClearMessage()
        {
            ChatMessages.Clear();
        }

        private void ClientItemSelected(ConnectedClientModel client)
        {
            if (client == null)
            {
                return;
            }
            
            _selectedClient = client;
        }

        /// <summary>
        /// 发送消息
        /// </summary>
        private void SendMessage()
        {
            if (string.IsNullOrEmpty(_userInputText))
            {
                MessageBox.Show("不能发送空消息", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (_selectedClient != null)
            {
                try
                {
                    if (_isHexChecked)
                    {
                        if (_userInputText.IsHex())
                        {
                            var result = _userInputText.GetBytesWithUtf8();
                            _tcpService.Send(_selectedClient.ClientId, result.Item2);
                    
                            ChatMessages.Add(new ChatMessageModel
                            {
                                MessageTime = DateTime.Now.ToString("HH:mm:ss"),
                                Message = result.Item1.FormatHexString(),
                                IsSend = true
                            });
                        }
                        else
                        {
                            MessageBox.Show("数据格式错误，无法发送", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                    else
                    {
                        _tcpService.Send(_selectedClient.ClientId, _userInputText);
                        ChatMessages.Add(new ChatMessageModel
                        {
                            MessageTime = DateTime.Now.ToString("HH:mm:ss"),
                            Message = _userInputText,
                            IsSend = true
                        });
                    }
                }
                catch (ClientNotFindException e)
                {
                    MessageBox.Show(e.Message, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else

            {
                MessageBox.Show("请指定接收消息的客户端", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CycleSendMessage()
        {
            //判断周期时间是否为空
            if (_messageCycleTime.IsNullOrWhiteSpace())
            {
                MessageBox.Show("请先设置周期发送的时间间隔", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                IsCycleChecked = false;
                return;
            }

            //判断周期时间是否是数字
            if (!_messageCycleTime.IsNumber())
            {
                MessageBox.Show("时间间隔只能是数字", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                IsCycleChecked = false;
                return;
            }

            _timer.Interval = TimeSpan.FromMilliseconds(double.Parse(_messageCycleTime));
            _timer.Start();
        }

        private void StopCycleSendMessage()
        {
            //停止timer
            if (_timer.IsEnabled)
            {
                _timer.Stop();
            }
        }
    }
}