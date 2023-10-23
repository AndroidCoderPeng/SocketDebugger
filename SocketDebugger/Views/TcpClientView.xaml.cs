using System.Windows.Controls;
using Prism.Services.Dialogs;
using SocketDebugger.Services;
using SocketDebugger.ViewModels;

namespace SocketDebugger.Views
{
    /// <summary>
    /// TcpClientView.xaml 的交互逻辑
    /// </summary>
    public partial class TcpClientView : UserControl
    {
        public TcpClientView(IApplicationDataService dataService, IDialogService dialogService)
        {
            InitializeComponent();

            DataContext = new TcpClientViewModel(dataService, dialogService);
        }
    }
}