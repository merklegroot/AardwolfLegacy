using System;

namespace balance_lib
{
    public class GetHoldingsForExchangeServiceModel
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public bool ForceRefresh { get; set; }
    }
}
