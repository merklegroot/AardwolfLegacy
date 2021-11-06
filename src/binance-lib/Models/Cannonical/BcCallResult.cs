using Binance.Net.Objects;
using CryptoExchange.Net;
using System;

namespace binance_lib.Models.Canonical
{
    public class BcCallResult<TCanonical>
        where TCanonical: class
    {
        public TCanonical Data { get; set; }
        public BcError Error { get; set; }
        public bool Success { get; set; }

        public static BcCallResult<TCanonical> FromModel<TNative>(CallResult<TNative> item)
            where TNative : class
        {
			if (item == null) { return null; }

            Func<TNative, TCanonical> converter;
			if (item.Data == null)
            {
                converter = new Func<TNative, TCanonical>(data => default(TCanonical));
            }
			else if (typeof(BinanceOrderBook).IsAssignableFrom(typeof(TNative)))// && typeof(BcOrderBook).IsAssignableFrom(typeof(TCanonical)))
            {
                converter = new Func<TNative, TCanonical>(data => BcOrderBook.FromModel(item.Data as BinanceOrderBook) as TCanonical);
            }
            else if (typeof(BinanceDepositAddress).IsAssignableFrom(typeof(TNative)) && typeof(BcDepositAddress).IsAssignableFrom(typeof(TCanonical)))
            {
                converter = new Func<TNative, TCanonical>(data => BcDepositAddress.FromModel(item.Data as BinanceDepositAddress) as TCanonical);
            }
            else
            {
                throw new ApplicationException($"Could not find a suitable converter for \"{ typeof(TNative).FullName }\".");
            }

            var model = new BcCallResult<TCanonical>
            {
				Data = converter(item.Data),
                Error = BcError.FromModel(item.Error),
                Success = item.Success
            };

            return model;
        }
    }
}
