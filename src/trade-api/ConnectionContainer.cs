using rabbit_lib;

namespace trade_api
{
    public class ConnectionContainer
    {
        public static IRabbitConnection Connection { get; set; }
    }
}