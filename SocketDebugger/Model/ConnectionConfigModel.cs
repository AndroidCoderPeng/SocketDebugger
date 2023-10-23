using SQLite;

namespace SocketDebugger.Model
{
    [Table("ConnectionTable")]
    public class ConnectionConfigModel
    {
        [PrimaryKey, Unique, NotNull] public string Uuid { get; set; }

        public string Comment { get; set; }

        public string ConnType { get; set; }

        public string ConnHost { get; set; }

        public string ConnPort { get; set; }

        public string MsgType { get; set; }

        public string Message { get; set; }

        public string TimePeriod { get; set; }
    }
}