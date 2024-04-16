using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
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

namespace SocketDebugger.ViewModels
{
    public class UdpClientViewModel : BindableBase
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
        public DelegateCommand ClearMessageCommand { get; set; }
        public DelegateCommand SendMessageCommand { get; set; }
        public DelegateCommand CycleCheckedCommand { get; set; }
        public DelegateCommand CycleUncheckedCommand { get; set; }

        #endregion

        private readonly IApplicationDataService _dataService;
        private readonly IDialogService _dialogService;
        private readonly IEventAggregator _eventAggregator;
        private readonly UdpSession _udpSession = new UdpSession();
        private readonly DispatcherTimer _timer = new DispatcherTimer();

        public UdpClientViewModel(IApplicationDataService dataService, IDialogService dialogService,
            IEventAggregator eventAggregator)
        {
            _dataService = dataService;
            _dialogService = dialogService;
            _eventAggregator = eventAggregator;

            InitMessageType();

            InitDelegate();

            ConfigItemSelectedCommand = new DelegateCommand<ConnectionConfigModel>(
                delegate(ConnectionConfigModel configModel) { SelectedConfigModel = configModel; });

            AddConfigCommand = new DelegateCommand(delegate
            {
                var configModel = new ConnectionConfigModel
                {
                    ConnectionTitle = "",
                    ConnectionType = "UDP客户端",
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

            ClearMessageCommand = new DelegateCommand(() => { ChatMessages.Clear(); });

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
        
        private void InitDelegate()
        {
            _eventAggregator.GetEvent<MainMenuSelectedEvent>().Subscribe(InitMessageType);

            _udpSession.Received += delegate(EndPoint endpoint, ByteBlock block, IRequestInfo info)
            {
                var message = _isTextChecked
                    ? Encoding.UTF8.GetString(block.Buffer, 0, block.Len)
                    : BitConverter.ToString(block.Buffer, 0, block.Len).Replace("-", " ");

                Application.Current.Dispatcher.Invoke(() =>
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
        private void SendMessage()
        {
            if (string.IsNullOrEmpty(_userInputText))
            {
                "不能发送空消息".ShowAlertMessageDialog(_dialogService, AlertType.Error);
                return;
            }

            if (_selectedConfigModel == null)
            {
                "未选择UDP目的服务器，无法发送数据".ShowAlertMessageDialog(_dialogService, AlertType.Error);
                return;
            }

            var config = new TouchSocketConfig();
            var host = new IPHost(_selectedConfigModel.ConnectionHost + ":" + _selectedConfigModel.ConnectionPort);
            config.SetBindIPHost(0).SetRemoteIPHost(host);
            _udpSession.Setup(config).Start();
            var endPoint = host.EndPoint;

            try
            {
                if (_isTextChecked)
                {
                    _udpSession.Send(endPoint, _userInputText);

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
                        if (_userInputText.Contains(" "))
                        {
                            _userInputText = _userInputText.Replace(" ", "");
                        }
                        else if (_userInputText.Contains("-"))
                        {
                            _userInputText = _userInputText.Replace("-", "");
                        }

                        //以UTF-8的编码同步发送字符串
                        var bytes = Encoding.UTF8.GetBytes(_userInputText);
                        _udpSession.Send(endPoint, bytes);

                        ChatMessages.Add(new ChatMessageModel
                        {
                            MessageTime = DateTime.Now.ToString("HH:mm:ss"),
                            Message = _userInputText,
                            IsSend = true
                        });
                    }
                    else
                    {
                        "数据格式错误，无法发送".ShowAlertMessageDialog(_dialogService, AlertType.Error);
                    }
                }
            }
            catch (NotConnectedException e)
            {
                e.Message.ShowAlertMessageDialog(_dialogService, AlertType.Error);
            }
        }
    }
}