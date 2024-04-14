using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Sockets;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Services.Dialogs;
using SocketDebugger.Model;
using SocketDebugger.Services;
using SocketDebugger.Utils;
using WebSocket4Net;

namespace SocketDebugger.ViewModels
{
    public class WebSocketClientViewModel : BindableBase
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

        private ConnectionConfigModel _configModel;

        public ConnectionConfigModel ConfigModel
        {
            get => _configModel;
            private set
            {
                _configModel = value;
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
        
        #endregion

        #region DelegateCommand

        public DelegateCommand<ListView> ConfigItemSelectedCommand { get; }
        public DelegateCommand AddConfigCommand { get; }
        public DelegateCommand DeleteConfigCommand { get; }
        public DelegateCommand EditConfigCommand { get; }
        public DelegateCommand ConnectServerCommand { get; }
        public DelegateCommand ClearMessageCommand { get; }
        public DelegateCommand SendMessageCommand { get; }

        #endregion

        private readonly IDialogService _dialogService;
        private WebSocket _webSocketClient;
        private readonly DispatcherTimer _timer = new DispatcherTimer();

        public WebSocketClientViewModel(IApplicationDataService dataService, IDialogService dialogService)
        {
            _dialogService = dialogService;
            
            ConfigModels = dataService.GetConfigModels();
            if (ConfigModels.Any())
            {
                ConfigModel = ConfigModels[0];
                if (ConfigModel.MessageType == "文本")
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

            ConfigItemSelectedCommand = new DelegateCommand<ListView>(it =>
            {
                if (it.SelectedIndex == -1)
                {
                    return;
                }

                ConfigModel = (ConnectionConfigModel)it.SelectedItem;
            });

            AddConfigCommand = new DelegateCommand(delegate
            {
                var configModel = new ConnectionConfigModel
                {
                    ConnectionTitle = "",
                    ConnectionType = "WebSocket客户端",
                    ConnectionHost = SystemHelper.GetHostAddress(),
                    ConnectionPort = "8080"
                };
                
                dialogService.ShowDialog("ConfigDialog", new DialogParameters
                    {
                        { "Title", "添加配置" }, { "ConfigModel", configModel }
                    },
                    delegate(IDialogResult result)
                    {
                        if (result.Result == ButtonResult.OK)
                        {
                            //更新列表
                            ConfigModels = dataService.GetConfigModels();

                            ConfigModel = result.Parameters.GetValue<ConnectionConfigModel>("ConfigModel");

                            if (ConfigModel.MessageType == "文本")
                            {
                                IsTextChecked = true;
                            }
                            else
                            {
                                IsHexChecked = true;
                            }
                            
                            if (_timer.IsEnabled)
                            {
                                _timer.Stop();
                            }

                            // if (ConfigModel.TimePeriod == null)
                            // {
                            //     return;
                            // }
                            //
                            // _timer.Interval = TimeSpan.FromMilliseconds(double.Parse(ConfigModel.TimePeriod));
                            // _timer.Start();
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
                                    manager.Delete(ConfigModel);
                                }

                                ConfigModels = dataService.GetConfigModels();
                                if (ConfigModels.Any())
                                {
                                    ConfigModel = ConfigModels[0];
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
                if (ConfigModel == null)
                {
                    "无配置项，无法编辑".ShowAlertMessageDialog(_dialogService, AlertType.Error);
                }
                else
                {
                    dialogService.ShowDialog("ConfigDialog", new DialogParameters
                        {
                            { "Title", "编辑配置" }, { "ConfigModel", _configModel }
                        },
                        delegate(IDialogResult result)
                        {
                            if (result.Result == ButtonResult.OK)
                            {
                                //更新列表
                                ConfigModels = dataService.GetConfigModels();

                                ConfigModel = result.Parameters.GetValue<ConnectionConfigModel>("ConfigModel");

                                if (ConfigModel.MessageType == "文本")
                                {
                                    IsTextChecked = true;
                                }
                                else
                                {
                                    IsHexChecked = true;
                                }
                            
                                if (_timer.IsEnabled)
                                {
                                    _timer.Stop();
                                }

                                // if (ConfigModel.TimePeriod == null)
                                // {
                                //     return;
                                // }
                                //
                                // _timer.Interval = TimeSpan.FromMilliseconds(double.Parse(ConfigModel.TimePeriod));
                                // _timer.Start();
                            }
                        }
                    );
                }
            });

            ConnectServerCommand = new DelegateCommand(delegate
            {
                if (_webSocketClient == null)
                {
                    _webSocketClient = new WebSocket("ws://" + ConfigModel.ConnectionHost + ":" + ConfigModel.ConnectionPort);
                }

                _webSocketClient.Opened += WebSocketOpened;
                _webSocketClient.MessageReceived += WebSocketMessageReceived;
                _webSocketClient.Closed += WebSocketClosed;

                try
                {
                    if (_connectButtonState == "连接")
                    {
                        _webSocketClient.Open();
                    }
                    else
                    {
                        _webSocketClient.Opened -= WebSocketOpened;
                        _webSocketClient.MessageReceived -= WebSocketMessageReceived;
                        _webSocketClient.Closed -= WebSocketClosed;

                        _webSocketClient.Close();
                    }
                }
                catch (SocketException e)
                {
                    e.Message.ShowAlertMessageDialog(_dialogService, AlertType.Error);
                }
            });

            ClearMessageCommand = new DelegateCommand(() => { ChatMessages.Clear(); });

            SendMessageCommand = new DelegateCommand(delegate { SendMessage(_userInputText); });

            //自动发消息
            // _timer.Tick += delegate { SendMessage(ConfigModel.Message); };
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

            if (ConnectState == "未连接")
            {
                "未连接成功，无法发送消息".ShowAlertMessageDialog(_dialogService, AlertType.Error);
                return;
            }

            _webSocketClient?.Send(message);

            ChatMessages.Add(new ChatMessageModel
            {
                MessageTime = DateTime.Now.ToString("HH:mm:ss"),
                Message = message,
                IsSend = true
            });
        }
        
        private void WebSocketOpened(object sender, EventArgs e)
        {
            ConnectColorBrush = "LimeGreen";
            ConnectState = "已连接";
            ConnectButtonState = "断开";
        }

        private void WebSocketClosed(object sender, EventArgs e)
        {
            ConnectColorBrush = "DarkGray";
            ConnectState = "未连接";
            ConnectButtonState = "连接";
        }

        private void WebSocketMessageReceived(object sender, MessageReceivedEventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                ChatMessages.Add(new ChatMessageModel
                {
                    MessageTime = DateTime.Now.ToString("HH:mm:ss"),
                    Message = e.Message,
                    IsSend = false
                });
            });
        }
    }
}