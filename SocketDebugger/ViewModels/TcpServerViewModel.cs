using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Threading;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using Prism.Services.Dialogs;
using SocketDebugger.Events;
using SocketDebugger.Model;
using SocketDebugger.Utils;

namespace SocketDebugger.ViewModels
{
    public class TcpServerViewModel : BindableBase
    {
        #region VM

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

        public DelegateCommand EditConfigCommand { get; set; }
        public DelegateCommand StartListenCommand { get; set; }
        public DelegateCommand ClearMessageCommand { get; set; }
        public DelegateCommand<ConnectedClientModel> ClientItemSelectedCommand { get; set; }
        public DelegateCommand SendMessageCommand { get; set; }
        public DelegateCommand CycleCheckedCommand { get; set; }
        public DelegateCommand CycleUncheckedCommand { get; set; }

        #endregion

        private readonly IDialogService _dialogService;
        // private readonly TcpService _tcpService = new TcpService();
        private readonly DispatcherTimer _timer = new DispatcherTimer();
        private ConnectedClientModel _selectedClientModel;

        public TcpServerViewModel(IDialogService dialogService, IEventAggregator eventAggregator)
        {
            _dialogService = dialogService;

            InitDelegate();

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
            
            eventAggregator.GetEvent<UpdateConnectionDetailEvent>().Subscribe(UpdateDetailView);
        }

        private void UpdateDetailView(ConnectionConfigModel configModel)
        {
            SelectedConfigModel = configModel;
            if (configModel.MessageType == "文本")
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
        
        private void InitDelegate()
        {
            // _tcpService.Connected = delegate(SocketClient client, TouchSocketEventArgs args)
            // {
            //     Application.Current.Dispatcher.Invoke(delegate
            //     {
            //         ConnectedClients.Add(new ConnectedClientModel
            //         {
            //             ClientId = client.ID,
            //             ClientConnectColorBrush = "LimeGreen",
            //             ClientHostAddress = client.IP + ":" + client.Port
            //         });
            //
            //         ClientIndex = ConnectedClients.Count - 1;
            //     });
            // };
            //
            // _tcpService.Disconnected = delegate(SocketClient client, DisconnectEventArgs args)
            // {
            //     Application.Current.Dispatcher.Invoke(delegate
            //     {
            //         var clientModel = ConnectedClients.FirstOrDefault(x => x.ClientId == $"{client.ID}");
            //
            //         if (clientModel != null)
            //         {
            //             //改变连接颜色
            //             clientModel.ClientConnectColorBrush = "DarkGray";
            //
            //             //没有update函数，只能先删除再添加
            //             ConnectedClients.Remove(clientModel);
            //             ConnectedClients.Add(clientModel);
            //         }
            //     });
            // };
            //
            // _tcpService.Received = delegate(SocketClient client, ByteBlock block, IRequestInfo info)
            // {
            //     var message = _isTextChecked
            //         ? Encoding.UTF8.GetString(block.Buffer, 0, block.Len)
            //         : BitConverter.ToString(block.Buffer, 0, block.Len).Replace("-", " ");
            //
            //     Application.Current.Dispatcher.Invoke(() =>
            //     {
            //         ChatMessages.Add(new ChatMessageModel
            //         {
            //             MessageTime = DateTime.Now.ToString("HH:mm:ss"),
            //             Message = message,
            //             IsSend = false
            //         });
            //     });
            // };
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
                        UpdateDetailView(
                            result.Parameters.GetValue<ConnectionConfigModel>("ConnectionConfigModel")
                        );
                    }
                }
            );
        }

        private void StartListenPort()
        {
            // var config = new TouchSocketConfig();
            // config.SetListenIPHosts(new[]
            // {
            //     new IPHost(_selectedConfigModel.ConnectionHost + ":" + _selectedConfigModel.ConnectionPort)
            // });
            //
            // //载入配置
            // _tcpService.Setup(config);
            // try
            // {
            //     if (_connectButtonState == "开始监听")
            //     {
            //         _tcpService.Start();
            //
            //         ConnectColorBrush = "LimeGreen";
            //         ConnectState = "监听中";
            //         ConnectButtonState = "停止监听";
            //     }
            //     else
            //     {
            //         _timer.Stop();
            //         _tcpService.Stop();
            //
            //         ConnectColorBrush = "DarkGray";
            //         ConnectState = "未在监听";
            //         ConnectButtonState = "开始监听";
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

        private void ClientItemSelected(ConnectedClientModel clientModel)
        {
            _selectedClientModel = clientModel;
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

            if (_selectedClientModel != null)
            {
                // try
                // {
                //     if (_isTextChecked)
                //     {
                //         _tcpService.Send(_selectedClientModel.ClientId, _userInputText);
                //
                //         ChatMessages.Add(new ChatMessageModel
                //         {
                //             MessageTime = DateTime.Now.ToString("HH:mm:ss"),
                //             Message = _userInputText,
                //             IsSend = true
                //         });
                //     }
                //     else
                //     {
                //         if (_userInputText.IsHex())
                //         {
                //             //以UTF-8的编码同步发送字符串
                //             var result = _userInputText.GetBytesWithUtf8();
                //             _tcpService.Send(_selectedClientModel.ClientId, result.Item2);
                //
                //             ChatMessages.Add(new ChatMessageModel
                //             {
                //                 MessageTime = DateTime.Now.ToString("HH:mm:ss"),
                //                 Message = result.Item1.FormatHexString(),
                //                 IsSend = true
                //             });
                //         }
                //         else
                //         {
                //             MessageBox.Show("数据格式错误，无法发送", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                //         }
                //     }
                // }
                // catch (ClientNotFindException e)
                // {
                //     MessageBox.Show(e.Message, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                // }
            }
            else

            {
                MessageBox.Show("请指定接收消息的客户端", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
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
    }
}