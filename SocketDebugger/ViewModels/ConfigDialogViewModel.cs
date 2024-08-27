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

        private ConnectionConfigModel _connectionConfigModel;

        public ConnectionConfigModel ConnectionConfigModel
        {
            get => _connectionConfigModel;
            set
            {
                _connectionConfigModel = value;
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
                ConnectionConfigModel.MessageType = dataType;
            });

            SaveConfigCommand = new DelegateCommand(delegate
            {
                if (_connectionConfigModel == null)
                {
                    return;
                }

                //保存到库
                using (var manager = new DataBaseManager())
                {
                    if (string.IsNullOrEmpty(_connectionConfigModel.Uuid))
                    {
                        var configModel = new ConnectionConfigModel
                        {
                            Uuid = Guid.NewGuid().ToString("N"),
                            ConnectionTitle = _connectionConfigModel.ConnectionTitle.Trim(),
                            ConnectionType = _connectionConfigModel.ConnectionType,
                            ConnectionHost = _connectionConfigModel.ConnectionHost,
                            ConnectionPort = _connectionConfigModel.ConnectionPort,
                            MessageType = _connectionConfigModel.MessageType
                        };

                        manager.Insert(configModel);
                    }
                    else
                    {
                        manager.Update(_connectionConfigModel);
                    }
                }

                RequestClose?.Invoke(new DialogResult(ButtonResult.OK, new DialogParameters
                    {
                        { "ConnectionConfigModel", _connectionConfigModel }
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
            ConnectionConfigModel = parameters.GetValue<ConnectionConfigModel>("ConnectionConfigModel");
        }
    }
}