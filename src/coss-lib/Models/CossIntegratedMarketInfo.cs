using System;
using System.Collections.Generic;
using System.Linq;

namespace coss_model
{
    public class CossIntegratedMarketInfo
    {
        public DateTime? AsOf { get; set; }

        private List<List<List<decimal>>> _data;
        public void SetData(List<List<List<decimal>>> data)
        {
            // clone
            _data = data != null ? data.Select(item => item).ToList() : new List<List<List<decimal>>>();
            if (_data != null && _data.Count() >= 1)
            {
                _bids = new List<Order>();
                foreach (var item in _data[0])
                {
                    var order = Order.FromModel(item);

                    _bids.Add(order);
                }
            }

            if (_data != null && _data.Count() >= 2)
            {
                _asks = new List<Order>();
                foreach (var item in _data[1])
                {
                    var order = Order.FromModel(item);

                    _asks.Add(order);
                }
            }
            else
            {
                _asks = new List<Order>();
            }
        }
        
        public string BaseSymbol { get; set; }

        public string Symbol { get; set; }       

        public class Order
        {
            public decimal? Price { get; set; }
            public decimal? Quantity { get; set; }

            public static Order FromModel(List<decimal> model)
            {
                return model != null
                    ? new Order
                    {
                        Price = model != null && model.Count() >= 1 ? model[0]: default(decimal),
                        Quantity = model != null && model.Count() >= 2 ? model[1] : default(decimal)
                    }
                    : null;
            }
        }

        private List<Order> _bids = null;
        public List<Order> Bids { get { return _bids; } }

        private List<Order> _asks = null;
        public List<Order> Asks { get { return _asks; } }
    }
}
