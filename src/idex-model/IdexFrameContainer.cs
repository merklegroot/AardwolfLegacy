using MongoDB.Bson;
using System;

namespace idex_model
{
    public class IdexFrameContainer
    {
        public ObjectId Id { get; set; }
        public DateTime ClientTimeStampLocal { get; set; }
        public DateTime RelayServerTimeStampUtc { get; set; }
        public string ClientMachine { get; set; }
        public string RelayServiceMachineName { get; set; }
        public string FrameContents { get; set; }
    }
}
