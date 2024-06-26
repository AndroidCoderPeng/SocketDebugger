using System;
using System.Collections.Generic;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Services.Dialogs;
using SocketDebugger.Model;
using SocketDebugger.Services;
using SocketDebugger.Utils;

namespace SocketDebugger.ViewModels
{
    public class ConfigDialogViewModel : BindableBase, IDialogAware
    {
        public string Title { get; private set; }
        public event Action<IDialogResult> RequestClose;

        #region VM

        private ConnectionConfigModel _configModel;

        public ConnectionConfigModel SelectedConfigModel
        {
            get => _configModel;
            set
            {
                _configModel = value;
                RaisePropertyChanged();
            }
        }

        public List<string> DataTypeArray { get; }

        #endregion

        #region DelegateCommand

        public DelegateCommand<string> DataTypeSelectedCommand { set; get; }
        public DelegateCommand SaveConfigCommand { get; set; }

        #endregion

        public ConfigDialogViewModel(IApplicationDataService dataService)
        {
            DataTypeArray = dataService.GetDataType();

            DataTypeSelectedCommand = new DelegateCommand<string>(delegate(string dataType)
            {
                SelectedConfigModel.MessageType = dataType;
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
                            ConnectionTitle = _configModel.ConnectionTitle.Trim(),
                            ConnectionType = _configModel.ConnectionType,
                            ConnectionHost = _configModel.ConnectionHost,
                            ConnectionPort = _configModel.ConnectionPort,
                            MessageType = _configModel.MessageType
                        };

                        manager.Insert(configModel);
                    }
                    else
                    {
                        manager.Update(_configModel);
                    }
                }

                RequestClose?.Invoke(new DialogResult(ButtonResult.OK, new DialogParameters
                    {
                        { "SelectedConfigModel", _configModel }
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
            SelectedConfigModel = parameters.GetValue<ConnectionConfigModel>("SelectedConfigModel");
        }
    }
}