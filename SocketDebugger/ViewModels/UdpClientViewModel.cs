using System;
using System.Collections.ObjectModel;
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
using SocketDebugger.Utils;
using TouchSocket.Core;
using TouchSocket.Sockets;

namespace SocketDebugger.ViewModels
{
    public class UdpClientViewModel : BindableBase
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

        public DelegateCommand EditConfigCommand { get; set; }
        public DelegateCommand ClearMessageCommand { get; set; }
        public DelegateCommand SendMessageCommand { get; set; }
        public DelegateCommand CycleCheckedCommand { get; set; }
        public DelegateCommand CycleUncheckedCommand { get; set; }

        #endregion

        private readonly IDialogService _dialogService;
        private readonly UdpSession _udpSession = new UdpSession();
        private readonly DispatcherTimer _timer = new DispatcherTimer();

        public UdpClientViewModel(IDialogService dialogService, IEventAggregator eventAggregator)
        {
            _dialogService = dialogService;

            UpdateDetailView(MemoryCacheManager.SelectedConfigModel);

            InitDelegate();

            EditConfigCommand = new DelegateCommand(EditConnectionConfig);
            ClearMessageCommand = new DelegateCommand(ClearMessage);
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

            if (_selectedConfigModel == null)
            {
                MessageBox.Show("未选择UDP目的服务器，无法发送数据", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var config = new TouchSocketConfig();
            var host = new IPHost(_selectedConfigModel.ConnectionHost + ":" + _selectedConfigModel.ConnectionPort);
            config.SetBindIPHost(0).SetRemoteIPHost(host);
            _udpSession.Setup(config).Start();
            var endPoint = host.EndPoint;

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
                    //以UTF-8的编码同步发送字符串
                    var result = _userInputText.GetBytesWithUtf8();
                    _udpSession.Send(endPoint, result.Item2);

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