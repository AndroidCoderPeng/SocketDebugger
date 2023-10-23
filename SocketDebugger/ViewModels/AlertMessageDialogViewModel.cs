using System;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Services.Dialogs;
using SocketDebugger.Utils;

namespace SocketDebugger.ViewModels
{
    public class AlertMessageDialogViewModel : BindableBase, IDialogAware
    {
        public string Title { get; private set; }
        public event Action<IDialogResult> RequestClose;

        #region VM

        private string _alertMessage;

        public string AlertMessage
        {
            get => _alertMessage;
            private set
            {
                _alertMessage = value;
                RaisePropertyChanged();
            }
        }

        private string _alertIcon;

        public string AlertIcon
        {
            get => _alertIcon;
            private set
            {
                _alertIcon = value;
                RaisePropertyChanged();
            }
        }

        private string _alertIconColor;

        public string AlertIconColor
        {
            get => _alertIconColor;
            private set
            {
                _alertIconColor = value;
                RaisePropertyChanged();
            }
        }

        #endregion

        #region DelegateCommand

        public DelegateCommand CloseAlertCommand { set; get; }

        #endregion

        public AlertMessageDialogViewModel()
        {
            CloseAlertCommand = new DelegateCommand(delegate { RequestClose?.Invoke(new DialogResult()); });
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
            var alertType = parameters.GetValue<AlertType>("AlertType");
            switch (alertType)
            {
                case AlertType.Warning:
                    AlertIcon = "\ue701";
                    Title = "温馨提示";
                    AlertIconColor = "DarkOrange";
                    break;
                case AlertType.Error:
                    AlertIcon = "\ue667";
                    Title = "错误";
                    AlertIconColor = "Red";
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            AlertMessage = parameters.GetValue<string>("Message");
        }
    }
}