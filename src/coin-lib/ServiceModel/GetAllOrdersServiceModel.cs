using System.Collections.Generic;

namespace coin_lib.ServiceModel
{
    public class GetAllOrdersServiceModel
    {
        public List<string> FilteredOutExchanges { get; set; }
        public List<string> ExchangesToInclude { get; set; }
        public bool ForceRefresh { get; set; }
    }
}
