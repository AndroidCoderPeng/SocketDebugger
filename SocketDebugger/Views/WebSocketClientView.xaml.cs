using System.Windows.Controls;
using Prism.Services.Dialogs;
using SocketDebugger.Services;
using SocketDebugger.ViewModels;

namespace SocketDebugger.Views
{
    public partial class WebSocketClientView : UserControl
    {
        public WebSocketClientView(IApplicationDataService dataService, IDialogService dialogService)
        {
            InitializeComponent();
            
            DataContext = new WebSocketClientViewModel(dataService, dialogService);
        }
    }
}