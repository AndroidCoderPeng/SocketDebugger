using System.Windows.Controls;
using Prism.Services.Dialogs;
using SocketDebugger.Services;
using SocketDebugger.ViewModels;

namespace SocketDebugger.Views
{
    public partial class UdpServerView : UserControl
    {
        public UdpServerView(IApplicationDataService dataService, IDialogService dialogService)
        {
            InitializeComponent();

            DataContext = new UdpServerViewModel(dataService, dialogService);
        }
    }
}