using CryptoExchange.Net;

namespace binance_lib.Models.Canonical
{
	public class BcError
    {
        public int Code { get; set; }
        public string Message { get; set; }

        public static BcError FromModel(Error item)
        {
            return item != null
                ? new BcError { Code = item.Code, Message = item.Message }
                : null;
        }
    }
}
