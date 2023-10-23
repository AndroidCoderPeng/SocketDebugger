using System.Windows.Controls;
using SocketDebugger.ViewModels;

namespace SocketDebugger.Dialogs
{
    public partial class ConfigDialog : UserControl
    {
        public ConfigDialog()
        {
            InitializeComponent();

            DataContext = new ConfigDialogViewModel();
        }
    }
}