﻿using System;
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
using SuperSocket.SocketBase;
using SuperSocket.WebSocket;
using TouchSocket.Sockets;

namespace SocketDebugger.ViewModels
{
    public class WebSocketServerViewModel : BindableBase
    {
        #region DelegateCommand

        public DelegateCommand<ListView> ConfigItemSelectedCommand { get; }
        public DelegateCommand AddConfigCommand { get; }
        public DelegateCommand DeleteConfigCommand { get; }
        public DelegateCommand EditConfigCommand { get; }
        public DelegateCommand StartListenCommand { get; }
        public DelegateCommand ClearMessageCommand { get; }
        public DelegateCommand<ListView> ClientItemSelectedCommand { get; }
        public DelegateCommand SendMessageCommand { get; }

        #endregion

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
        
        #endregion

        private readonly IDialogService _dialogService;
        private ConnectedClientModel _selectedClientModel;
        private WebSocketServer _webSocketService;
        private readonly DispatcherTimer _timer = new DispatcherTimer();

        public WebSocketServerViewModel(IApplicationDataService dataService, IDialogService dialogService)
        {
            _dialogService = dialogService;
            
            ConfigModels = dataService.GetConfigModels();
            if (ConfigModels.Any())
            {
                ConfigModel = ConfigModels[0];
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
                    Comment = "",
                    ConnType = "WebSocket服务端",
                    ConnHost = SystemHelper.GetHostAddress(),
                    ConnPort = "8080"
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

                            if (string.IsNullOrEmpty(ConfigModel.Message))
                            {
                                //停止循环
                                _timer.Stop();
                            }
                            else
                            {
                                _timer.Interval = TimeSpan.FromMilliseconds(double.Parse(ConfigModel.TimePeriod));
                                _timer.Start();
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
                    ShowAlertMessageDialog(AlertType.Error, "没有配置，无法删除");
                }
            });

            EditConfigCommand = new DelegateCommand(delegate
            {
                if (ConfigModel == null)
                {
                    ShowAlertMessageDialog(AlertType.Error, "没有配置，无法编辑");
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

                                if (string.IsNullOrEmpty(ConfigModel.Message))
                                {
                                    //停止循环
                                    _timer.Stop();
                                }
                                else
                                {
                                    _timer.Interval = TimeSpan.FromMilliseconds(double.Parse(ConfigModel.TimePeriod));
                                    _timer.Start();
                                }
                            }
                        }
                    );
                }
            });

            StartListenCommand = new DelegateCommand(() =>
            {
                if (ConfigModel == null)
                {
                    ShowAlertMessageDialog(AlertType.Error, "无配置项，无法启动监听");
                }
                else
                {
                    if (_webSocketService == null)
                    {
                        _webSocketService = new WebSocketServer();
                        _webSocketService.Setup(ConfigModel.ConnHost, Convert.ToInt32(ConfigModel.ConnPort));
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
                        ShowAlertMessageDialog(AlertType.Error, e.Message);
                    }
                }
            });

            ClearMessageCommand = new DelegateCommand(() => { ChatMessages.Clear(); });

            ClientItemSelectedCommand = new DelegateCommand<ListView>(it =>
            {
                if (it.SelectedIndex == -1)
                {
                    return;
                }

                _selectedClientModel = (ConnectedClientModel)it.SelectedItem;
            });

            SendMessageCommand = new DelegateCommand(delegate
            {
                if (string.IsNullOrEmpty(_userInputText))
                {
                    ShowAlertMessageDialog(AlertType.Error, "不能发送空消息");
                    return;
                }

                if (_selectedClientModel != null)
                {
                    try
                    {
                        _webSocketService.GetSessionByID(_selectedClientModel.ClientId).Send(_userInputText);

                        ChatMessages.Add(new ChatMessageModel
                        {
                            MessageTime = DateTime.Now.ToString("yyyy年MM月dd HH时mm分ss秒"),
                            Message = _userInputText,
                            IsSend = true
                        });
                    }
                    catch (ClientNotFindException e)
                    {
                        ShowAlertMessageDialog(AlertType.Error, e.Message);
                    }
                }
                else
                {
                    ShowAlertMessageDialog(AlertType.Error, "请指定接收消息的客户端");
                }
            });
        }

        private void MessageReceivedEvent(WebSocketSession session, string value)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                ChatMessages.Add(new ChatMessageModel
                {
                    MessageTime = DateTime.Now.ToString("yyyy年MM月dd HH时mm分ss秒"),
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
        
        /// <summary>
        /// 显示普通提示对话框
        /// </summary>
        private void ShowAlertMessageDialog(AlertType type, string message)
        {
            _dialogService.ShowDialog("AlertMessageDialog", new DialogParameters
                {
                    { "AlertType", type }, { "Message", message }
                },
                delegate { }
            );
        }
    }
}