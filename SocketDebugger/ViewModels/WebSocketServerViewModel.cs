using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;
using System.Windows;
using System.Windows.Threading;
using DotNetty.Buffers;
using DotNetty.Codecs.Http;
using DotNetty.Codecs.Http.WebSockets;
using DotNetty.Handlers.Logging;
using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using Prism.Services.Dialogs;
using SocketDebugger.Events;
using SocketDebugger.Model;
using SocketDebugger.Services;
using SocketDebugger.Utils;
using TouchSocket.Sockets;

namespace SocketDebugger.ViewModels
{
    public class WebSocketServerViewModel : BindableBase
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
        private IChannel _channelTask;
        private bool _isListened;
        private ConnectedClientModel _selectedClient;

        public WebSocketServerViewModel(IApplicationDataService dataService, IDialogService dialogService,
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
            ConnectionConfigCollection = _dataService.GetConnectionCollection("WebSocket服务端");
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

            if (configModel.ConnectionType.Equals("WebSocket服务端"))
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

        private async void StartListenPort()
        {
            if (!_isListened)
            {
                var port = Convert.ToInt32(_selectedConfig.ConnectionPort);
                var webSocketPath = string.IsNullOrWhiteSpace(_selectedConfig.WebSocketPath)
                    ? "/"
                    : $"/{_selectedConfig.WebSocketPath}";

                var bootstrap = new ServerBootstrap();
                bootstrap.Group(new MultithreadEventLoopGroup(1), new MultithreadEventLoopGroup())
                    .Channel<TcpServerSocketChannel>()
                    .Option(ChannelOption.SoBacklog, 100)
                    .Option(ChannelOption.RcvbufAllocator, new AdaptiveRecvByteBufAllocator(64, 1024, 65536))
                    .ChildHandler(new ActionChannelInitializer<ISocketChannel>(channel =>
                    {
                        channel.Pipeline
                            .AddLast(new LoggingHandler(LogLevel.INFO))
                            .AddLast(new HttpServerCodec())
                            .AddLast(new HttpObjectAggregator(8192))
                            .AddLast(new WebSocketServerProtocolHandler(webSocketPath))
                            .AddLast(
                                new WebSocketServerDataFrameHandler(WebSocketStateObserver, WebSocketMessageObserver)
                            );
                    }));
                try
                {
                    _channelTask = await bootstrap.BindAsync(port);

                    ConnectColorBrush = "LimeGreen";
                    ConnectState = "监听中";
                    ConnectButtonState = "停止监听";

                    _isListened = true;

                    await _channelTask.CloseCompletion;
                }
                catch (SocketException ex)
                {
                    Console.WriteLine($@"WebSocket client error: {ex}");
                }
            }
            else
            {
                await _channelTask.CloseAsync();

                ConnectColorBrush = "DarkGray";
                ConnectState = "未在监听";
                ConnectButtonState = "开始监听";

                if (_timer.IsEnabled)
                {
                    _timer.Stop();
                }

                _isListened = false;
            }
        }

        private void WebSocketStateObserver(IChannelHandlerContext context, WebSocketState state)
        {
            var clientChannel = context.Channel;
            var id = clientChannel.Id.AsShortText();
            var remoteAddress = clientChannel.RemoteAddress;
            Console.WriteLine($@"{clientChannel.RemoteAddress.Serialize()}");
            switch (state)
            {
                case WebSocketState.Open:
                    Application.Current.Dispatcher.Invoke(delegate
                    {
                        if (remoteAddress is IPEndPoint endPoint)
                        {
                            ConnectedClients.Add(new ConnectedClientModel
                            {
                                ClientId = id,
                                ClientConnectColorBrush = "LimeGreen",
                                ClientHostAddress = $"{endPoint.Address}:{endPoint.Port}"
                            });

                            ClientIndex = ConnectedClients.Count - 1;
                        }
                    });
                    break;
                case WebSocketState.Closed:
                case WebSocketState.Aborted:
                {
                    Application.Current.Dispatcher.Invoke(delegate
                    {
                        //有客户端断开后，找到断开的那个客户端
                        var clientModel = ConnectedClients.First(x => x.ClientId == $"{id}");
                        //改变连接状态颜色
                        clientModel.ClientConnectColorBrush = "DarkGray";
                    });
                    break;
                }
                case WebSocketState.None:
                    break;
                case WebSocketState.Connecting:
                    break;
                case WebSocketState.CloseSent:
                    break;
                case WebSocketState.CloseReceived:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(state), state, null);
            }
        }

        private void WebSocketMessageObserver(WebSocketFrame dataFrame)
        {
            if (dataFrame is TextWebSocketFrame textFrame)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    ChatMessages.Add(new ChatMessageModel
                    {
                        MessageTime = DateTime.Now.ToString("HH:mm:ss"),
                        Message = textFrame.Text(),
                        IsSend = false
                    });
                });
            }
            else if (dataFrame is CloseWebSocketFrame closeFrame)
            {
            }
            else if (dataFrame is BinaryWebSocketFrame binaryFrame)
            {
            }
        }

        private void ClearMessage()
        {
            ChatMessages.Clear();
        }

        private void ClientItemSelected(ConnectedClientModel clientModel)
        {
            _selectedClient = clientModel;
        }

        /// <summary>
        /// 发送消息
        /// </summary>
        private async void SendMessage()
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
                            var byteArray = Encoding.UTF8.GetBytes(_userInputText);
                            var byteBuffer = Unpooled.WrappedBuffer(byteArray);
                            await _channelTask.WriteAndFlushAsync(new BinaryWebSocketFrame(byteBuffer));
                            ChatMessages.Add(new ChatMessageModel
                            {
                                MessageTime = DateTime.Now.ToString("HH:mm:ss"),
                                Message = _userInputText,
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
                        await _channelTask.WriteAndFlushAsync(new TextWebSocketFrame(_userInputText));
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
            if (string.IsNullOrWhiteSpace(_messageCycleTime))
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