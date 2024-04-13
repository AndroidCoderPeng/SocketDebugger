using SQLite;

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
        /// 连接端口
        /// </summary>
        public string ConnectionPort { get; set; }

        /// <summary>
        /// 消息类型，文本、Hex
        /// </summary>
        public string MessageType { get; set; }

        /// <summary>
        /// 消息内容
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// 连发消息间隔
        /// </summary>
        public string TimePeriod { get; set; }
    }
}