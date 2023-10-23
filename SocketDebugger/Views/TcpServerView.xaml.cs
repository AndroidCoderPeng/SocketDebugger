using System.Windows.Controls;
using Prism.Services.Dialogs;
using SocketDebugger.Services;
using SocketDebugger.ViewModels;

namespace SocketDebugger.Views
{
    public partial class TcpServerView : UserControl
    {
        public TcpServerView(IApplicationDataService dataService, IDialogService dialogService)
        {
            InitializeComponent();
            
            DataContext = new TcpServerViewModel(dataService, dialogService);
        }
    }
}