using System.ComponentModel;

namespace SocketDebugger.Model
{
    public class ConnectedClientModel : INotifyPropertyChanged
    {
        public string ClientId { get; set; }
        
        private string _clientConnectColorBrush;

        public string ClientConnectColorBrush
        {
            get => _clientConnectColorBrush;
            set
            {
                _clientConnectColorBrush = value;
                OnPropertyChanged(nameof(ClientConnectColorBrush));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        
        public string ClientHostAddress { get; set; }
    }
}