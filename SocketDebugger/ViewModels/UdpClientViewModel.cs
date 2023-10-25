using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Services.Dialogs;
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
        public DelegateCommand ClearMessageCommand { get; }
        public DelegateCommand SendMessageCommand { get; }

        #endregion

        private readonly IDialogService _dialogService;
        private readonly UdpSession _udpSession = new UdpSession();
        private readonly DispatcherTimer _timer = new DispatcherTimer();

        public UdpClientViewModel(IApplicationDataService dataService, IDialogService dialogService)
        {
            _dialogService = dialogService;
            
            ConfigModels = dataService.GetConfigModels();
            if (ConfigModels.Any())
            {
                ConfigModel = ConfigModels[0];
                if (ConfigModel.MsgType == "文本")
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

            AddConfigCommand = new DelegateCommand(delegate
            {
                var configModel = new ConnectionConfigModel
                {
                    Comment = "",
                    ConnType = "UDP客户端",
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

                            if (ConfigModel.MsgType == "文本")
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

                            if (ConfigModel.TimePeriod == null)
                            {
                                return;
                            }

                            _timer.Interval = TimeSpan.FromMilliseconds(double.Parse(ConfigModel.TimePeriod));
                            _timer.Start();
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
                    ShowAlertMessageDialog(AlertType.Error, "无配置项，无法编辑");
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

                                if (ConfigModel.MsgType == "文本")
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

                                if (ConfigModel.TimePeriod == null)
                                {
                                    return;
                                }

                                _timer.Interval = TimeSpan.FromMilliseconds(double.Parse(ConfigModel.TimePeriod));
                                _timer.Start();
                            }
                        }
                    );
                }
            });

            ClearMessageCommand = new DelegateCommand(() => { ChatMessages.Clear(); });

            SendMessageCommand = new DelegateCommand(delegate
            {
                if (string.IsNullOrEmpty(_userInputText))
                {
                    ShowAlertMessageDialog(AlertType.Error, "不能发送空消息");
                    return;
                }

                if (ConfigModel == null)
                {
                    ShowAlertMessageDialog(AlertType.Error, "未选择UDP目的服务器，无法发送数据");
                    return;
                }

                var config = new TouchSocketConfig();
                var host = new IPHost(ConfigModel.ConnHost + ":" + ConfigModel.ConnPort);
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
                            var buffer = Encoding.UTF8.GetBytes(_userInputText);
                            //以UTF-8的编码同步发送字符串
                            _udpSession.Send(endPoint, buffer);

                            ChatMessages.Add(new ChatMessageModel
                            {
                                MessageTime = DateTime.Now.ToString("HH:mm:ss"),
                                Message = _userInputText,
                                IsSend = true
                            });
                        }
                        else
                        {
                            ShowAlertMessageDialog(AlertType.Error, "数据格式错误，无法发送");
                        }
                    }
                }
                catch (NotConnectedException e)
                {
                    ShowAlertMessageDialog(AlertType.Error, e.Message);
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