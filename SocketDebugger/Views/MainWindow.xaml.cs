using System.Windows;
using System.Windows.Input;
using Prism.Ioc;
using Prism.Regions;
using SocketDebugger.Utils;

namespace SocketDebugger.Views
{
    /// <summary>
    /// Interaction logic for xaml
    /// </summary>
    public partial class MainWindow
    {
        private readonly IRegionManager _regionManager;
        private readonly IContainerProvider _container;

        public MainWindow(IRegionManager regionManager, IContainerProvider container)
        {
            InitializeComponent();

            _regionManager = regionManager;
            _container = container;

            //显示默认View
            MemoryCacheManager.SelectedMainMenu = "TCP客户端";
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);
            DragMove();
        }

        private void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
        {
            //显示默认View
            _regionManager.AddToRegion("ContentRegion", _container.Resolve<TcpClientView>());
        }
    }
}