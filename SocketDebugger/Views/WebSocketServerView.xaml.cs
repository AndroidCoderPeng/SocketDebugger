using System.Windows.Controls;
using Prism.Services.Dialogs;
using SocketDebugger.Services;
using SocketDebugger.ViewModels;

namespace SocketDebugger.Views
{
    public partial class WebSocketServerView : UserControl
    {
        public WebSocketServerView(IApplicationDataService dataService, IDialogService dialogService)
        {
            InitializeComponent();

            DataContext = new WebSocketServerViewModel(dataService, dialogService);
        }
    }
}