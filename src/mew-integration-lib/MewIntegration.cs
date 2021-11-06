using etherscan_lib;
using rabbit_lib;
using System;
using System.Collections.Generic;
using System.Linq;
using trade_constants;
using trade_contracts;
using cache_lib.Models;
using trade_model;
using trade_res;
using config_client_lib;

namespace mew_integration_lib
{
    public class MewIntegration : IMewIntegration
    {
        public string Name => "Mew";
        public Guid Id => new Guid("F2FBAB08-C9D0-4E61-A870-14F54CD948EF");

        private readonly IEtherscanHoldingRepo _etherscanHoldingRepo;
        private readonly IEtherscanHistoryRepo _etherscanHistoryRepo;
        private readonly IRabbitConnectionFactory _rabbitConnectionFactory;
        private readonly IConfigClient _configClient;

        public MewIntegration(
            IConfigClient configClient,
            IEtherscanHoldingRepo etherscanHoldingRepo,
            IEtherscanHistoryRepo etherscanHistoryRepo,
            IRabbitConnectionFactory rabbitConnectionFactory)
        {
            _configClient = configClient;
            _etherscanHoldingRepo = etherscanHoldingRepo;
            _etherscanHistoryRepo = etherscanHistoryRepo;
            _rabbitConnectionFactory = rabbitConnectionFactory;
        }

        public List<DepositAddressWithSymbol> GetDepositAddresses(CachePolicy cachePolicy)
        {
            var commodities = GetCommodities();
            return commodities.Select(item =>
            {
                var depositAddress = GetDepositAddress(item.Symbol, cachePolicy);
                return depositAddress != null
                    ? new DepositAddressWithSymbol
                    {
                        Address = depositAddress.Address,
                        Memo = depositAddress.Memo,
                        Symbol = item.Symbol
                    }
                    : null;
            })
            .ToList();
        }

        public HoldingInfo GetHoldings(CachePolicy cachePolicy)
        {
            if (cachePolicy == CachePolicy.ForceRefresh)
            {
                using (var conn = _rabbitConnectionFactory.Connect())
                {
                    var message = new UpdateFundsRequestMessage();
                    conn.PublishContract(TradeRabbitConstants.Queues.EtherscanAgentQueue, message);
                }
            }

            return _etherscanHoldingRepo.Get();
        }

        public Dictionary<string, decimal> GetWithdrawalFees(CachePolicy cachePolicy)
        {
            return new Dictionary<string, decimal>();
        }

        public decimal? GetWithdrawalFee(string symbol, CachePolicy cachePolicy)
        {
            return null;
        }

        public List<CommodityForExchange> GetCommodities(CachePolicy cachePolicy = CachePolicy.AllowCache)
        {
            var converter = new Func<Commodity, CommodityForExchange>(commodity =>
                new CommodityForExchange
                {
                    CanonicalId = commodity.Id,
                    Name = commodity.Name,
                    Symbol = commodity.Symbol,
                    CanDeposit = true,
                    CanWithdraw = true,
                    NativeSymbol = commodity.Symbol,
                    WithdrawalFee = 0.002m // rough estimate
                });

            var commodities = CommodityRes.All.Where(item => item.IsEthToken ?? false)
                .OrderBy(item => item.Symbol)
                .ToList();

            commodities.Insert(0, CommodityRes.Eth);

            return commodities.Select(item => converter(item)).ToList();            
        }

        public OrderBook GetOrderBook(TradingPair tradingPair, CachePolicy cachePolicy)
        {
            return new OrderBook
            {
                Asks = new List<Order>(),
                Bids = new List<Order>()
            };
        }

        public List<TradingPair> GetTradingPairs(CachePolicy cachePolicy = CachePolicy.AllowCache)
        {
            return new List<TradingPair> { };
        }

        public DepositAddress GetDepositAddress(string symbol, CachePolicy cachePolicy)
        {
            if (string.IsNullOrWhiteSpace(symbol)) { throw new ArgumentNullException(nameof(symbol)); }

            var ethAddress = _configClient.GetMewWalletAddress();

            return new DepositAddress
            {
                Address = ethAddress
            };
        }

        public Holding GetHolding(string symbol)
        {
            return GetHoldings(CachePolicy.AllowCache)?.Holdings?.SingleOrDefault(item => string.Equals(item.Asset, symbol));
        }

        public List<HistoricalTrade> GetUserTradeHistory(CachePolicy cachePolicy)
        {
            if (cachePolicy == CachePolicy.ForceRefresh)
            {
                using (var conn = _rabbitConnectionFactory.Connect())
                {
                    var message = new UpdateHistoryRequestMessage();
                    conn.PublishContract(TradeRabbitConstants.Queues.EtherscanAgentQueue, message);
                }
            }

            return _etherscanHistoryRepo.Get();
        }

        public bool Withdraw(Commodity commodity, decimal quantity, DepositAddress depositAddress)
        {
            // TODO: This really should be done from within a client.
            using (var conn = _rabbitConnectionFactory.Connect())
            {
                var message = new WithdrawFundsRequestMessage
                {
                    Commodity = new CommodityContract
                    {
                        ContractId = commodity.ContractId,
                        Decimals = commodity.Decimals,
                        Id = commodity.Id,
                        IsDominant = commodity.IsDominant,
                        IsEth = commodity.IsEth,
                        IsEthToken = commodity.IsEthToken,
                        Name = commodity.Name,
                        Symbol = commodity.Symbol
                    },
                    Quantity = quantity,
                    DepositAddress = depositAddress.Address
                };

                conn.PublishContract(TradeRabbitConstants.Queues.MewAgentQueue, message, TimeSpan.FromMinutes(5));
            }

            return true;
        }
    }
}
