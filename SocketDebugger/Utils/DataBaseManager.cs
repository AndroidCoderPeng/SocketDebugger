using System;
using SocketDebugger.Model;
using SQLite;

namespace SocketDebugger.Utils
{
    public class DataBaseManager : SQLiteConnection
    {
        public DataBaseManager() : base(AppDomain.CurrentDomain.BaseDirectory + @"SocketConnection.db")
        {
            CreateTable<ConnectionConfigModel>();
        }
    }
}