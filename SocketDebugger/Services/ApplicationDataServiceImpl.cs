using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO.Ports;
using System.Linq;
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
                new MainMenuModel { MainMenuIcon = "\ue8a6", MainMenuName = "TCP客户端" },
                new MainMenuModel { MainMenuIcon = "\ue8a6", MainMenuName = "TCP服务端" },
                new MainMenuModel { MainMenuIcon = "\ue8a9", MainMenuName = "UDP客户端" },
                new MainMenuModel { MainMenuIcon = "\ue8a9", MainMenuName = "UDP服务端" },
                new MainMenuModel { MainMenuIcon = "\ue8b2", MainMenuName = "WebSocket客户端" },
                new MainMenuModel { MainMenuIcon = "\ue8b2", MainMenuName = "WebSocket服务端" }
            };
        }

        public ObservableCollection<ConnectionConfigModel> GetConnectionCollection(string type)
        {
            using (var manager = new DataBaseManager())
            {
                var queryResult = manager
                    .Table<ConnectionConfigModel>()
                    .Where(config => config.ConnectionType == type)
                    .ToList();
                return ListConvert<ConnectionConfigModel>.ToObservableCollection(queryResult);
            }
        }

        public void DeleteConnectionById(string id)
        {
            using (var manager = new DataBaseManager())
            {
                var queryResult = manager
                    .Table<ConnectionConfigModel>()
                    .Where(config => config.Uuid == id)
                    .ToList();
                if (!queryResult.Any()) return;
                foreach (var connectionConfigModel in queryResult)
                {
                    manager.Delete(connectionConfigModel);
                }
            }
        }

        public List<string> GetDataType()
        {
            return new List<string> { "文本", "16进制" };
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

        public string[] GetSerialPorts()
        {
            return SerialPort.GetPortNames();
        }

        public List<int> GetBaudRateArray()
        {
            return new List<int> { 9600, 14400, 19200, 38400, 56000, 57600, 115200, 128000, 230400 };
        }

        public List<int> GetDataBitArray()
        {
            return new List<int> { 5, 6, 7, 8 };
        }

        public List<Parity> GetParityArray()
        {
            return new List<Parity> { Parity.None, Parity.Odd, Parity.Even, Parity.Mark, Parity.Space };
        }

        public List<StopBits> GetStopBitArray()
        {
            return new List<StopBits> { StopBits.None, StopBits.One, StopBits.Two };
        }
    }
}