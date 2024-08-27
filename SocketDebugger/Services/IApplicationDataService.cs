using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO.Ports;
using SocketDebugger.Model;

namespace SocketDebugger.Services
{
    public interface IApplicationDataService
    {
        List<MainMenuModel> GetMainMenu();
        
        ObservableCollection<ConnectionConfigModel> GetConnectionCollection(string type);

        void DeleteConnectionById(string id);
        
        List<string> GetDataType();
        
        string GetHostAddress();

        string[] GetSerialPorts();

        List<int> GetBaudRateArray();

        List<int> GetDataBitArray();

        List<Parity> GetParityArray();

        List<StopBits> GetStopBitArray();
    }
}