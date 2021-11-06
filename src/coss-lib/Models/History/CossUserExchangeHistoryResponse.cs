using System.Collections.Generic;

namespace coss_data_model
{
    public class CossUserExchangeHistoryResponse
    {
        public bool successful { get; set; }
        public Payload payload { get; set; }

        public class Actions
        {
            public int totalCount { get; set; }
            public List<CossExchangeHistoryItem> items { get; set; }
        }

        public class Payload
        {
            public Actions actions { get; set; }
        }
    }
}
