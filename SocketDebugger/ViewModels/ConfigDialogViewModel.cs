using System;
using System.Collections.Generic;
using System.Windows.Controls;
using Newtonsoft.Json;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Services.Dialogs;
using SocketDebugger.Model;
using SocketDebugger.Services;
using SocketDebugger.Utils;
using TouchSocket.Core;

namespace SocketDebugger.ViewModels
{
    public class ConfigDialogViewModel : BindableBase, IDialogAware
    {
        public string Title { get; private set; }
        public event Action<IDialogResult> RequestClose;

        #region VM

        private ConnectionConfigModel _configModel;

        public ConnectionConfigModel ConfigModel
        {
            get => _configModel;
            set
            {
                _configModel = value;
                RaisePropertyChanged();
            }
        }

        public List<string> DataTypeArray { get; }

        private string _dataType = "文本";

        public string DataType
        {
            get => _dataType;
            set
            {
                _dataType = value;
                RaisePropertyChanged();
            }
        }

        private bool _isRepeatBoxChecked;

        public bool IsRepeatBoxChecked
        {
            get => _isRepeatBoxChecked;
            set
            {
                _isRepeatBoxChecked = value;
                RaisePropertyChanged();
            }
        }

        #endregion

        #region DelegateCommand

        public DelegateCommand CloseWindowCommand { get; set; }
        public DelegateCommand<ComboBox> DataTypeSelectedCommand { set; get; }
        public DelegateCommand SaveConfigCommand { get; set; }

        #endregion

        private readonly IDialogService _dialogService;

        public ConfigDialogViewModel(IApplicationDataService dataService, IDialogService dialogService)
        {
            DataTypeArray = dataService.GetDataType();
            _dialogService = dialogService;

            CloseWindowCommand = new DelegateCommand(delegate
            {
                RequestClose?.Invoke(new DialogResult(ButtonResult.Cancel, new DialogParameters()));
            });

            DataTypeSelectedCommand = new DelegateCommand<ComboBox>(delegate(ComboBox box)
            {
                ConfigModel.MsgType = box.SelectedItem.ToString();
            });

            SaveConfigCommand = new DelegateCommand(delegate
            {
                if (_configModel == null)
                {
                    return;
                }

                //保存到库
                using (var manager = new DataBaseManager())
                {
                    if (string.IsNullOrEmpty(_configModel.Uuid))
                    {
                        var configModel = new ConnectionConfigModel
                        {
                            Uuid = Guid.NewGuid().ToString("N"),
                            Comment = _configModel.Comment,
                            ConnType = _configModel.ConnType,
                            ConnHost = _configModel.ConnHost,
                            ConnPort = _configModel.ConnPort,
                            MsgType = _configModel.MsgType
                        };
                        if (IsRepeatBoxChecked)
                        {
                            if (_configModel.Message.IsNullOrEmpty() || _configModel.TimePeriod.IsNullOrEmpty())
                            {
                                ShowAlertMessageDialog(AlertType.Warning, "请完善需要连续发送的信息");
                                return;
                            }

                            configModel.Message = _configModel.Message;
                            configModel.TimePeriod = _configModel.TimePeriod;
                        }

                        manager.Insert(configModel);
                    }
                    else
                    {
                        if (!IsRepeatBoxChecked)
                        {
                            _configModel.Message = null;
                            _configModel.TimePeriod = null;
                        }

                        manager.Update(_configModel);
                    }
                }

                RequestClose?.Invoke(new DialogResult(ButtonResult.OK, new DialogParameters
                    {
                        { "ConfigModel", _configModel }
                    }
                ));
            });
        }

        public bool CanCloseDialog()
        {
            return true;
        }

        public void OnDialogClosed()
        {
        }

        public void OnDialogOpened(IDialogParameters parameters)
        {
            Title = parameters.GetValue<string>("Title");
            ConfigModel = parameters.GetValue<ConnectionConfigModel>("ConfigModel");
            if (ConfigModel.Message != null || ConfigModel.TimePeriod != null)
            {
                IsRepeatBoxChecked = true;
            }
            else
            {
                IsRepeatBoxChecked = false;
            }
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