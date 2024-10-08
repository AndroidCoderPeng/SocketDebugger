﻿using SQLite;

namespace SocketDebugger.Model
{
    [Table("ConnectionTable")]
    public class ConnectionConfigModel
    {
        [PrimaryKey, Unique, NotNull] public string Uuid { get; set; }

        /// <summary>
        /// 连接配置描述
        /// </summary>
        public string ConnectionTitle { get; set; }

        /// <summary>
        /// 连接类型，Tcp、Udp、WebSocket
        /// </summary>
        public string ConnectionType { get; set; }

        /// <summary>
        /// 连接IP
        /// </summary>
        public string ConnectionHost { get; set; }
        
        /// <summary>
        /// WebSocket路径，如：ws://192.168.92.146:8080/websocket/1。其中WebSocketPath => websocket
        /// </summary>
        public string WebSocketPath { get; set; }

        /// <summary>
        /// 连接端口
        /// </summary>
        public string ConnectionPort { get; set; }
    }
}