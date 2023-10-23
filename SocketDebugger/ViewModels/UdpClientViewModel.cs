using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Services.Dialogs;
using SocketDebugger.Model;
using SocketDebugger.Services;
using SocketDebugger.Utils;
using TouchSocket.Core;
using TouchSocket.Sockets;
using MessageBox = HandyControl.Controls.MessageBox;

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

        #endregion

        #region DelegateCommand

        public DelegateCommand<ListView> ConfigItemSelectedCommand { get; }
        public DelegateCommand AddConfigCommand { get; }
        public DelegateCommand DeleteConfigCommand { get; }
        public DelegateCommand EditConfigCommand { get; }
        public DelegateCommand ClearMessageCommand { get; }
        public DelegateCommand SendMessageCommand { get; }

        #endregion

        private readonly UdpSession _udpSession = new UdpSession();

        public UdpClientViewModel(IApplicationDataService dataService, IDialogService dialogService)
        {
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

            _udpSession.Received += delegate(EndPoint endpoint, ByteBlock byteBlock, IRequestInfo info)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    // var message = _viewPage.TextRadioButton.IsChecked == true
                    //     ? Encoding.UTF8.GetString(byteBlock.Buffer, 0, byteBlock.Len)
                    //     : BitConverter.ToString(byteBlock.Buffer, 0, byteBlock.Len).Replace("-", " ");
                    //
                    // ChatMessages.Add(new ChatMessageModel
                    // {
                    //     MessageTime = DateTime.Now.ToString("yyyy年MM月dd HH时mm分ss秒"),
                    //     Message = message,
                    //     IsSend = false
                    // });
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
                // var dialog = new ConfigDialog(configModel) { Owner = Window.GetWindow(_viewPage) };
                // dialog.AddConfigEventHandler += AddConfigResult;
                // dialog.ShowDialog();
            });

            DeleteConfigCommand = new DelegateCommand(delegate
            {
                if (ConfigModels.Any())
                {
                    var result = MessageBox.Show("是否删除当前配置？", "温馨提示", MessageBoxButton.OKCancel,
                        MessageBoxImage.Warning);
                    if (result != MessageBoxResult.OK) return;
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
                else
                {
                    MessageBox.Show("没有配置，无法删除", "温馨提示", MessageBoxButton.OKCancel, MessageBoxImage.Warning);
                }
            });

            EditConfigCommand = new DelegateCommand(delegate
            {
                if (ConfigModel == null)
                {
                    MessageBox.Show("无配置项，无法编辑", "温馨提示", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                else
                {
                    // var dialog = new ConfigDialog(ConfigModel) { Owner = Window.GetWindow(_viewPage) };
                    // dialog.AddConfigEventHandler += AddConfigResult;
                    // dialog.ShowDialog();
                }
            });

            ClearMessageCommand = new DelegateCommand(() => { ChatMessages.Clear(); });

            SendMessageCommand = new DelegateCommand(delegate
            {
                if (string.IsNullOrEmpty(_userInputText))
                {
                    MessageBox.Show("不能发送空消息", "温馨提示", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (ConfigModel == null)
                {
                    MessageBox.Show("未选择UDP目的服务器，无法发送数据", "温馨提示", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var config = new TouchSocketConfig();
                var host = new IPHost(ConfigModel.ConnHost + ":" + ConfigModel.ConnPort);
                config.SetBindIPHost(0).SetRemoteIPHost(host);
                _udpSession.Setup(config).Start();
                var endPoint = host.EndPoint;

                try
                {
                    // if (_viewPage.TextRadioButton.IsChecked == true)
                    // {
                    //     _udpSession.Send(endPoint, _userInputText);
                    //
                    //     ChatMessages.Add(new ChatMessageModel
                    //     {
                    //         MessageTime = DateTime.Now.ToString("yyyy年MM月dd HH时mm分ss秒"),
                    //         Message = _userInputText,
                    //         IsSend = true
                    //     });
                    // }
                    // else
                    // {
                    //     if (_userInputText.IsHex())
                    //     {
                    //         var buffer = Encoding.UTF8.GetBytes(_userInputText);
                    //         //以UTF-8的编码同步发送字符串
                    //         _udpSession.Send(endPoint, buffer);
                    //
                    //         ChatMessages.Add(new ChatMessageModel
                    //         {
                    //             MessageTime = DateTime.Now.ToString("yyyy年MM月dd HH时mm分ss秒"),
                    //             Message = _userInputText,
                    //             IsSend = true
                    //         });
                    //     }
                    //     else
                    //     {
                    //         MessageBox.Show("数据格式错误，无法发送", "温馨提示", MessageBoxButton.OK, MessageBoxImage.Error);
                    //     }
                    // }
                }
                catch (NotConnectedException e)
                {
                    MessageBox.Show(e.Message, "温馨提示", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            });
        }
    }
}