using System.Collections.Generic;

namespace cryptopia_lib.Models
{
    public class CryptopiaMarketOrdersPayload
    {
        public List<CryptopiaMarketOrder> Buy { get; set; }
        public List<CryptopiaMarketOrder> Sell { get; set; }
    }
}
