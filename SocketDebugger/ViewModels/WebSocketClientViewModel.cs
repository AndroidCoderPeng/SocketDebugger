using System;
using System.Collections.ObjectModel;
using System.Linq;
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

        private ConnectionConfigModel _selectedConfigModel;

        public ConnectionConfigModel SelectedConfigModel
        {
            get => _selectedConfigModel;
            set
            {
                _selectedConfigModel = value;
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
        // private WebSocket _webSocketClient;

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
                SelectedConfigModel = _connectionConfigCollection.First();
                CurrentIndex = 0;
            }
        }

        private void ChangeViewByMainMenu(string type)
        {
            ConnectionConfigCollection = _dataService.GetConnectionCollection(type);
            if (_connectionConfigCollection.Any())
            {
                SelectedConfigModel = _connectionConfigCollection.First();
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
                SelectedConfigModel = configModel;
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
                    SelectedConfigModel = _connectionConfigCollection.First();
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

            _dialogService.ShowDialog("ConfigDialog", new DialogParameters
                {
                    { "Title", "添加配置" }, { "ConnectionConfigModel", configModel }
                },
                delegate(IDialogResult result)
                {
                    if (result.Result == ButtonResult.OK)
                    {
                        //更新列表和面板
                        ConnectionConfigCollection = _dataService.GetConnectionCollection(configModel.ConnectionType);
                        SelectedConfigModel = _connectionConfigCollection.Last();
                        CurrentIndex = _connectionConfigCollection.Count - 1;
                    }
                }
            );
        }

        private void EditConnectionConfig()
        {
            _dialogService.ShowDialog("ConfigDialog", new DialogParameters
                {
                    { "Title", "编辑配置" }, { "ConnectionConfigModel", _selectedConfigModel }
                },
                delegate(IDialogResult result)
                {
                    if (result.Result == ButtonResult.OK)
                    {
                        SelectedConfigModel = result.Parameters.GetValue<ConnectionConfigModel>("ConnectionConfigModel");
                    }
                }
            );
        }

        private void ConnectWebsocketServer()
        {
            // if (_webSocketClient == null)
            // {
            //     _webSocketClient = new WebSocket(
            //         "ws://" + _selectedConfigModel.ConnectionHost + ":" + _selectedConfigModel.ConnectionPort);
            // }
            //
            // _webSocketClient.Opened += WebSocketOpened;
            // _webSocketClient.MessageReceived += WebSocketMessageReceived;
            // _webSocketClient.Closed += WebSocketClosed;
            //
            // try
            // {
            //     if (_connectButtonState == "连接")
            //     {
            //         _webSocketClient.Open();
            //     }
            //     else
            //     {
            //         _webSocketClient.Opened -= WebSocketOpened;
            //         _webSocketClient.MessageReceived -= WebSocketMessageReceived;
            //         _webSocketClient.Closed -= WebSocketClosed;
            //
            //         _webSocketClient.Close();
            //     }
            // }
            // catch (SocketException e)
            // {
            //     MessageBox.Show(e.Message, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            // }
        }

        private void ClearMessage()
        {
            ChatMessages.Clear();
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

            if (ConnectState == "未连接")
            {
                MessageBox.Show("未连接成功，无法发送消息", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // if (_selectedConfigModel.MessageType.Equals("文本"))
            // {
            //     // _webSocketClient?.Send(_userInputText);
            //
            //     ChatMessages.Add(new ChatMessageModel
            //     {
            //         MessageTime = DateTime.Now.ToString("HH:mm:ss"),
            //         Message = _userInputText,
            //         IsSend = true
            //     });
            // }
            // else
            // {
            //     if (_userInputText.IsHex())
            //     {
            //         //以UTF-8的编码同步发送字符串
            //         var result = _userInputText.GetBytesWithUtf8();
            //         // _webSocketClient?.Send(result.Item2, 0, result.Item2.Length);
            //
            //         ChatMessages.Add(new ChatMessageModel
            //         {
            //             MessageTime = DateTime.Now.ToString("HH:mm:ss"),
            //             Message = result.Item1.FormatHexString(),
            //             IsSend = true
            //         });
            //     }
            //     else
            //     {
            //         MessageBox.Show("数据格式错误，无法发送", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            //     }
            // }
        }

        private void CycleSendMessage()
        {
            //判断周期时间是否为空
            // if (_messageCycleTime.IsNullOrWhiteSpace())
            // {
            //     MessageBox.Show("请先设置周期发送的时间间隔", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            //     IsCycleChecked = false;
            //     return;
            // }

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

        // private void WebSocketOpened(object sender, EventArgs e)
        // {
        //     ConnectColorBrush = "LimeGreen";
        //     ConnectState = "已连接";
        //     ConnectButtonState = "断开";
        // }
        //
        // private void WebSocketClosed(object sender, EventArgs e)
        // {
        //     ConnectColorBrush = "DarkGray";
        //     ConnectState = "未连接";
        //     ConnectButtonState = "连接";
        // }
        //
        // private void WebSocketMessageReceived(object sender, MessageReceivedEventArgs e)
        // {
        //     Application.Current.Dispatcher.Invoke(() =>
        //     {
        //         ChatMessages.Add(new ChatMessageModel
        //         {
        //             MessageTime = DateTime.Now.ToString("HH:mm:ss"),
        //             Message = e.Message,
        //             IsSend = false
        //         });
        //     });
        // }
    }
}