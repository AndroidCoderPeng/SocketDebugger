using System;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Services.Dialogs;
using SocketDebugger.Model;
using SocketDebugger.Utils;

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

        #endregion

        #region DelegateCommand

        public DelegateCommand CloseWindowCommand { get; set; }
        public DelegateCommand SaveConfigCommand { get; set; }

        #endregion

        public ConfigDialogViewModel()
        {
            CloseWindowCommand = new DelegateCommand(delegate
            {
                RequestClose?.Invoke(new DialogResult(ButtonResult.Cancel, new DialogParameters()));
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
                        manager.Insert(new ConnectionConfigModel
                        {
                            Uuid = Guid.NewGuid().ToString("N"),
                            Comment = _configModel.Comment,
                            ConnType = _configModel.ConnType,
                            ConnHost = _configModel.ConnHost,
                            ConnPort = _configModel.ConnPort,
                            MsgType = _configModel.MsgType,
                            Message = _configModel.Message,
                            TimePeriod = _configModel.TimePeriod
                        });
                    }
                    else
                    {
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
        }
    }
}