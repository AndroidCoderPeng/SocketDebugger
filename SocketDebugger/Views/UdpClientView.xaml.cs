using System.Windows.Controls;
using Prism.Services.Dialogs;
using SocketDebugger.Services;
using SocketDebugger.ViewModels;

namespace SocketDebugger.Views
{
    public partial class UdpClientView : UserControl
    {
        public UdpClientView(IApplicationDataService dataService, IDialogService dialogService)
        {
            InitializeComponent();
            
            DataContext = new UdpClientViewModel(dataService, dialogService);
        }
    }
}