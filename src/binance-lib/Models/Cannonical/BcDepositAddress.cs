using Binance.Net.Objects;

namespace binance_lib.Models.Canonical
{
    public class BcDepositAddress
    {
        public string Address { get; set; }
        public bool Success { get; set; }
        public string AddressTag { get; set; }
        public string Asset { get; set; }

        public static BcDepositAddress FromModel(BinanceDepositAddress model)
        {
            if (model == null) { return null; }

            return new BcDepositAddress
            {
                Address = model.Address,
                Success = model.Success,
                AddressTag = model.AddressTag,
                Asset = model.Asset
            };
        }
    }
}
