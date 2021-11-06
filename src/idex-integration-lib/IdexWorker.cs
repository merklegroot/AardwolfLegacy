using cache_lib.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using trade_lib;
using trade_model;

namespace idex_integration_lib
{
    public class IdexWorker : IDisposable
    {
        private readonly ITradeIntegration _integration;
        private readonly Timer _timer;

        private static object Locker = new object();
        private static List<string> _instances = new List<string>();
        private readonly string _instanceId;

        public IdexWorker(IdexIntegration integation)
        {
            _integration = integation;
            _instanceId = DateTime.UtcNow.Ticks.ToString() + "_" + new Random().NextDouble().ToString();

            lock (Locker)
            {
                if (_instances.Any()) { return; }
                _instances.Add(_instanceId);
            }

            _timer = new Timer(state => Tick(), null, 0, (int)TimeSpan.FromSeconds(30).TotalMilliseconds);
        }

        public void Dispose()
        {
            if (_timer != null)
            {
                _timer.Dispose();
            }

            lock (Locker)
            {
                _instances = _instances.Where(item => !string.Equals(item, _instanceId)).ToList();
            }
        }

        private List<TradingPair> _tradingPairs;

        private static object TickLock = new object();

        private bool _isProcessing = false;

        private void Tick()
        {
            if (_isProcessing) { return; }

            lock (TickLock)
            {
                _isProcessing = true;

                try
                {
                    if (_tradingPairs == null || !_tradingPairs.Any())
                    {
                        _tradingPairs = _integration.GetTradingPairs(CachePolicy.AllowCache);
                    }

                    if (_tradingPairs == null || !_tradingPairs.Any()) { return; }

                    var tradingPair = _tradingPairs.First();
                    _tradingPairs.RemoveAt(0);

                    _integration.GetOrderBook(tradingPair, true);
                }
                finally
                {
                    _isProcessing = false;
                }
            }
        }
    }
}
