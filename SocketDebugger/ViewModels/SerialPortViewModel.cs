using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO.Ports;
using System.Linq;
using System.Windows.Threading;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Services.Dialogs;
using SocketDebugger.Model;
using SocketDebugger.Services;
using SocketDebugger.Utils;
using TouchSocket.Core;

namespace SocketDebugger.ViewModels
{
    public class SerialPortViewModel : BindableBase
    {
        #region VM

        private string[] _portNameArray;

        public string[] PortNameArray
        {
            get => _portNameArray;
            set
            {
                _portNameArray = value;
                RaisePropertyChanged();
            }
        }

        private List<int> _baudRateArray;

        public List<int> BaudRateArray
        {
            get => _baudRateArray;
            set
            {
                _baudRateArray = value;
                RaisePropertyChanged();
            }
        }

        private List<int> _dataBitArray;

        public List<int> DataBitArray
        {
            get => _dataBitArray;
            set
            {
                _dataBitArray = value;
                RaisePropertyChanged();
            }
        }

        private List<Parity> _parityArray;

        public List<Parity> ParityArray
        {
            get => _parityArray;
            set
            {
                _parityArray = value;
                RaisePropertyChanged();
            }
        }

        private List<StopBits> _stopBitArray;

        public List<StopBits> StopBitArray
        {
            get => _stopBitArray;
            set
            {
                _stopBitArray = value;
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

        private string _serialPortState = "打开串口";

        public string SerialPortState
        {
            get => _serialPortState;
            private set
            {
                _serialPortState = value;
                RaisePropertyChanged();
            }
        }

        private bool _isPortOpen = false;

        public bool IsPortOpen
        {
            get => _isPortOpen;
            private set
            {
                _isPortOpen = value;
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

        #endregion

        #region DelegateCommand

        public DelegateCommand<string> PortItemSelectedCommand { get; set; }
        public DelegateCommand<object> BaudRateItemSelectedCommand { get; set; }
        public DelegateCommand<object> DataBitItemSelectedCommand { get; set; }
        public DelegateCommand<object> ParityItemSelectedCommand { get; set; }
        public DelegateCommand<object> StopBitItemSelectedCommand { get; set; }
        public DelegateCommand OpenSerialPortCommand { get; set; }
        public DelegateCommand ClearSerialPortCommand { get; set; }
        public DelegateCommand SendMessageCommand { get; set; }
        public DelegateCommand CycleCheckedCommand { get; set; }
        public DelegateCommand CycleUncheckedCommand { get; set; }

        #endregion

        private readonly IApplicationDataService _dataService;
        private readonly IDialogService _dialogService;
        private readonly SerialPort _serialPort = new SerialPort();
        private readonly DispatcherTimer _timer = new DispatcherTimer();
        private string _portName;
        private int _baudRate;
        private int _dataBit;
        private Parity _parityValue;
        private StopBits _stopBit;

        public SerialPortViewModel(IApplicationDataService dataService, IDialogService dialogService)
        {
            _dataService = dataService;
            _dialogService = dialogService;

            InitMessageType();

            PortItemSelectedCommand = new DelegateCommand<string>(
                delegate(string value) { _portName = value; }
            );

            BaudRateItemSelectedCommand = new DelegateCommand<object>(
                delegate(object value) { _baudRate = (int)value; }
            );

            DataBitItemSelectedCommand = new DelegateCommand<object>(
                delegate(object value) { _dataBit = (int)value; }
            );

            ParityItemSelectedCommand = new DelegateCommand<object>(
                delegate(object value) { _parityValue = (Parity)value; }
            );

            StopBitItemSelectedCommand = new DelegateCommand<object>(
                delegate(object value) { _stopBit = (StopBits)value; }
            );

            OpenSerialPortCommand = new DelegateCommand(delegate
            {
                if (!_serialPort.IsOpen)
                {
                    _serialPort.PortName = _portName;
                    _serialPort.BaudRate = _baudRate;
                    _serialPort.Parity = _parityValue;
                    _serialPort.DataBits = _dataBit;
                    _serialPort.StopBits = _stopBit;

                    _serialPort.Open();
                    SerialPortState = "关闭串口";
                    ConnectColorBrush = "LimeGreen";
                    IsPortOpen = true;
                    //注册事件
                    _serialPort.DataReceived += SerialPort_DataReceived;
                }
                else
                {
                    //解注册事件
                    _serialPort.DataReceived -= SerialPort_DataReceived;
                    _serialPort.Close();
                    SerialPortState = "打开串口";
                    ConnectColorBrush = "DarkGray";
                    IsPortOpen = false;
                }
            });

            ClearSerialPortCommand = new DelegateCommand(delegate { ChatMessages.Clear(); });

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
            PortNameArray = _dataService.GetSerialPorts();
            BaudRateArray = _dataService.GetBaudRateArray();
            DataBitArray = _dataService.GetDataBitArray();
            ParityArray = _dataService.GetParityArray();
            StopBitArray = _dataService.GetStopBitArray();

            //默认值
            _portName = PortNameArray.First();
            _baudRate = BaudRateArray.First();
            _dataBit = DataBitArray.First();
            _parityValue = ParityArray.First();
            _stopBit = StopBitArray.First();
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

            if (_isTextChecked)
            {
                // if (ConnectState == "未连接")
                // {
                //     "未连接成功，无法发送消息".ShowAlertMessageDialog(_dialogService, AlertType.Error);
                //     return;
                // }
                //
                // _tcpClient.Send(_userInputText);

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
                    // if (ConnectState == "未连接")
                    // {
                    //     "未连接成功，无法发送消息".ShowAlertMessageDialog(_dialogService, AlertType.Warning);
                    //     return;
                    // }

                    if (_userInputText.Contains(" "))
                    {
                        _userInputText = _userInputText.Replace(" ", "");
                    }
                    else if (_userInputText.Contains("-"))
                    {
                        _userInputText = _userInputText.Replace("-", "");
                    }

                    //以UTF-8的编码同步发送字符串
                    // var bytes = Encoding.UTF8.GetBytes(_userInputText);
                    // _tcpClient.Send(bytes);

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

        /// <summary>
        /// 数据接收
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
        }
    }
}