using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net;
using SocketDebugger.Converts;
using SocketDebugger.Model;
using SocketDebugger.Utils;

namespace SocketDebugger.Services
{
    public class ApplicationDataServiceImpl : IApplicationDataService
    {
        public List<MainMenuModel> GetMainMenu()
        {
            return new List<MainMenuModel>
            {
                new MainMenuModel { MainMenuIcon = "\ue6a2", MainMenuName = "TCP客户端" },
                new MainMenuModel { MainMenuIcon = "\ue6a2", MainMenuName = "TCP服务端" },
                new MainMenuModel { MainMenuIcon = "\ue6a1", MainMenuName = "UDP客户端" },
                new MainMenuModel { MainMenuIcon = "\ue6a1", MainMenuName = "UDP服务端" },
                new MainMenuModel { MainMenuIcon = "\ue6a0", MainMenuName = "WebSocket客户端" },
                new MainMenuModel { MainMenuIcon = "\ue6a0", MainMenuName = "WebSocket服务端" },
                new MainMenuModel { MainMenuIcon = "\ue6a0", MainMenuName = "Http服务端" }
            };
        }

        public ObservableCollection<ConnectionConfigModel> GetConfigModels()
        {
            using (var manager = new DataBaseManager())
            {
                var queryResult = manager
                    .Table<ConnectionConfigModel>()
                    .Where(config => config.ConnType == MemoryCacheManager.SelectedMainMenu)
                    .ToList();
                return ListConvert<ConnectionConfigModel>.ToObservableCollection(queryResult);
            }
        }

        public string GetHostAddress()
        {
            var host = "";
            var hostName = Dns.GetHostName();
            var addresses = Dns.GetHostAddresses(hostName);
            foreach (var ip in addresses)
            {
                if (ip.AddressFamily.ToString() != "InterNetwork") continue;
                //只找一个IPV4地址
                host = ip.ToString();
            }

            return host;
        }
    }
}