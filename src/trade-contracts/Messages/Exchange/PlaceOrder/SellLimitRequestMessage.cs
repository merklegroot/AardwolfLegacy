using System;
using trade_contracts.Payloads;

namespace trade_contracts.Messages.Exchange
{
    public class SellLimitRequestMessage : RequestMessage
    {
        // For backward compatibility.
        [Obsolete]
        public string Exchange { get { return Payload.Exchange; } set { Payload.Exchange = value; } }
        [Obsolete]
        public string Symbol { get { return Payload.Symbol; } set { Payload.Symbol = value; } }
        [Obsolete]
        public string BaseSymbol { get { return Payload.BaseSymbol; } set { Payload.BaseSymbol = value; } }
        [Obsolete]
        public decimal Quantity { get { return Payload.Quantity; } set { Payload.Quantity = value; } }
        [Obsolete]
        public decimal Price { get { return Payload.Price; } set { Payload.Price = value; } }

        private LimitRequestPayload _payload;
        private object PayloadLocker = new object();
        public LimitRequestPayload Payload
        {
            get
            {
                lock (PayloadLocker)
                {
                    return _payload ?? (_payload = new LimitRequestPayload());
                }
            }
            set
            {
                lock (PayloadLocker)
                {
                    _payload = value;
                }
            }
        }
    }
}
