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

namespace SocketDebugger.ViewModels
{
    public class WebSocketClientViewModel : BindableBase
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

        private string _connectState = "未连接";

        public string ConnectState
        {
            get => _connectState;
            set
            {
                _connectState = value;
                RaisePropertyChanged();
            }
        }

        private string _connectButtonState = "连接";

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
        public DelegateCommand ConnectServerCommand { get; set; }
        public DelegateCommand ClearMessageCommand { get; set; }
        public DelegateCommand SendMessageCommand { get; set; }
        public DelegateCommand CycleCheckedCommand { get; set; }
        public DelegateCommand CycleUncheckedCommand { get; set; }

        #endregion

        private readonly IApplicationDataService _dataService;
        private readonly IDialogService _dialogService;
        private readonly DispatcherTimer _timer = new DispatcherTimer();
        private IChannel _channelTask;
        private bool _isConnected;

        public WebSocketClientViewModel(IApplicationDataService dataService, IDialogService dialogService,
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
            ConnectServerCommand = new DelegateCommand(ConnectWebsocketServer);
            ClearMessageCommand = new DelegateCommand(ClearMessage);
            SendMessageCommand = new DelegateCommand(SendMessage);

            //周期发送CheckBox选中、取消选中事件
            CycleCheckedCommand = new DelegateCommand(CycleSendMessage);
            CycleUncheckedCommand = new DelegateCommand(StopCycleSendMessage);

            //自动发消息
            _timer.Tick += delegate { SendMessage(); };
        }

        private void InitDefaultConfig()
        {
            ConnectionConfigCollection = _dataService.GetConnectionCollection("WebSocket客户端");
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

            if (configModel.ConnectionType.Equals("WebSocket客户端"))
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

        private async void ConnectWebsocketServer()
        {
            if (!_isConnected)
            {
                var host = _selectedConfig.ConnectionHost;
                var port = Convert.ToInt32(_selectedConfig.ConnectionPort);
                var webSocketPath = _selectedConfig.WebSocketPath;
                var userUuid = Guid.NewGuid().ToString("N");
                var remote = string.IsNullOrWhiteSpace(webSocketPath)
                    ? $"ws://{host}:{port}/{userUuid}"
                    : $"ws://{host}:{port}/{webSocketPath}/{userUuid}";

                var bootstrap = new Bootstrap();
                var eventLoopGroup = new MultithreadEventLoopGroup();
                bootstrap.Group(eventLoopGroup)
                    .Channel<TcpSocketChannel>()
                    .Option(ChannelOption.TcpNodelay, true)
                    .Option(ChannelOption.SoKeepalive, true)
                    .Option(ChannelOption.RcvbufAllocator, new AdaptiveRecvByteBufAllocator(64, 1024, 65536))
                    .Option(ChannelOption.ConnectTimeout, TimeSpan.FromSeconds(10))
                    .Handler(new ActionChannelInitializer<ISocketChannel>(channel =>
                    {
                        channel.Pipeline
                            .AddLast(new LoggingHandler(LogLevel.INFO))
                            .AddLast(new HttpClientCodec())
                            .AddLast(new HttpObjectAggregator(8192))
                            .AddLast(new WebSocketClientProtocolHandler(
                                new Uri(remote), WebSocketVersion.V13, null, false, new DefaultHttpHeaders(), 8192)
                            )
                            .AddLast(
                                new WebSocketClientDataFrameHandler(WebSocketStateObserver, WebSocketMessageObserver)
                            );
                    }));
                try
                {
                    _channelTask = await bootstrap.ConnectAsync(new IPEndPoint(IPAddress.Loopback, port));
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
            }
        }

        private void WebSocketStateObserver(WebSocketState state)
        {
            switch (state)
            {
                case WebSocketState.Open:
                    _isConnected = true;
                    ConnectColorBrush = "LimeGreen";
                    ConnectState = "已连接";
                    ConnectButtonState = "断开";
                    break;
                case WebSocketState.Closed:
                case WebSocketState.Aborted:
                {
                    ConnectColorBrush = "DarkGray";
                    ConnectState = "未连接";
                    ConnectButtonState = "连接";

                    if (_timer.IsEnabled)
                    {
                        _timer.Stop();
                    }

                    _isConnected = false;
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

            if (ConnectState == "未连接")
            {
                MessageBox.Show("未连接成功，无法发送消息", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (_isHexChecked)
            {
                if (_userInputText.IsHex())
                {
                    var byteArray = Encoding.UTF8.GetBytes(_userInputText);
                    var byteBuffer = Unpooled.WrappedBuffer(byteArray);
                    await _channelTask.WriteAndFlushAsync(new BinaryWebSocketFrame(byteBuffer));
                }
                else
                {
                    MessageBox.Show("数据格式错误，无法发送", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                await _channelTask.WriteAndFlushAsync(new TextWebSocketFrame(_userInputText));
            }

            ChatMessages.Add(new ChatMessageModel
            {
                MessageTime = DateTime.Now.ToString("HH:mm:ss"),
                Message = _userInputText,
                IsSend = true
            });
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