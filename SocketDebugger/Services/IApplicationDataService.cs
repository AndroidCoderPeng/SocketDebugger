using System.Collections.Generic;
using System.Collections.ObjectModel;
using SocketDebugger.Model;

namespace SocketDebugger.Services
{
    public interface IApplicationDataService
    {
        List<MainMenuModel> GetMainMenu();
        
        ObservableCollection<ConnectionConfigModel> GetConfigModels();

        List<string> GetDataType();
        
        string GetHostAddress();
    }
}