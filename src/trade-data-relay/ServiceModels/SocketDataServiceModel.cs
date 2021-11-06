using System;

namespace trade_data_relay.ServiceModels
{
    public class SocketDataServiceModel
    {
        public DateTime ClientTimeStampLocal { get; set; }
        public string ClientMachineName { get; set; }
        public string FrameContents { get; set; }
    }
}