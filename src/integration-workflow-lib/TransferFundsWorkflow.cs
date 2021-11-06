using System;
using System.Collections.Generic;
using System.Linq;
using trade_lib;
using cache_lib.Models;
using trade_model;
using trade_res;
using exchange_client_lib;
using trade_constants;

namespace integration_workflow_lib
{
    public class TransferFundsWorkflow : ITransferFundsWorkflow
    {
        private readonly IExchangeClient _exchangeClient;
        private readonly IDepositAddressValidator _depositAddressValidator;

        private static List<string> _permittedSources = new List<string> { IntegrationNameRes.Coss, IntegrationNameRes.Binance, IntegrationNameRes.KuCoin, IntegrationNameRes.Mew, IntegrationNameRes.HitBtc };

        public TransferFundsWorkflow(
            IExchangeClient exchangeClient,
            IDepositAddressValidator depositAddressValidator)
        {
            _exchangeClient = exchangeClient;
            _depositAddressValidator = depositAddressValidator;
        }

        public void Transfer(
            string source,
            string destination,
            Commodity commodity,
            bool shouldTransferAll,
            decimal? quantity = null)
        {
            if (string.IsNullOrWhiteSpace(source)) { throw new ArgumentNullException(nameof(source)); }
            if (string.IsNullOrWhiteSpace(destination)) { throw new ArgumentNullException(nameof(source)); }
            if (shouldTransferAll && quantity.HasValue) { throw new ArgumentException($"When {nameof(shouldTransferAll)} is true, {nameof(quantity)} must be null."); }

            var sourceExchange = _exchangeClient.GetExchange(source);
            if (sourceExchange == null) { throw new ApplicationException($"Failed to retrieve exchange by name \"{source}\"."); }

            var destExchange = _exchangeClient.GetExchange(destination);
            if (destExchange == null) { throw new ApplicationException($"Failed to retrieve exchange by name \"{destination}\"."); }

            if (!sourceExchange.IsWithdrawable) { throw new ApplicationException($"Exchange {source} is not setup for withdrawals."); }
            if (commodity == null) { throw new ArgumentNullException(nameof(commodity)); }

            if (string.Equals(sourceExchange.Name, destExchange.Name, StringComparison.InvariantCultureIgnoreCase))
            {
                throw new ApplicationException($"The source and the destination must be different exchanges. They are both {sourceExchange.Name}.");
            }

            if (!_permittedSources.Any(queryName => string.Equals(queryName, sourceExchange.Name, StringComparison.InvariantCultureIgnoreCase)))
            {
                throw new NotImplementedException($"So far, the only implemented sources are: {string.Join(", ", _permittedSources)}.");
            }

            var sourceHolding = _exchangeClient.GetBalance(source, commodity.Symbol, CachePolicy.ForceRefresh);
            if (sourceHolding == null || sourceHolding.Available <= 0)
            {
                throw new ApplicationException($"There is no available {commodity} to transfer from {sourceExchange.Name} to {destExchange.Name}.");
            }

            if (quantity.HasValue && sourceHolding.Available < quantity.Value)
            {
                throw new ApplicationException($"Requested quantity was {quantity.Value}, but there is only {sourceHolding.Available} available.");
            }

            var depositAddress = _exchangeClient.GetDepositAddress(destination, commodity.Symbol, CachePolicy.ForceRefresh);
            if (depositAddress == null || string.IsNullOrWhiteSpace(depositAddress.Address))
            {
                throw new ApplicationException($"{destExchange.Name} did not return a deposit address for \"{commodity}\".");
            }

            if (!IsDepositSymbolPermittedForDestination(destExchange, commodity))
            {
                throw new ApplicationException($"\"{commodity.Symbol}\" deposits are not yet implemented for {destExchange.Name}");
            }

            ValidateDepositAddress(commodity, depositAddress);

            var effectiveQuantity = shouldTransferAll ? sourceHolding.Available : quantity.Value;
            var transferResult = _exchangeClient.Withdraw(sourceExchange.Name, commodity.Symbol, effectiveQuantity, depositAddress);

            if (!transferResult)
            {
                throw new ApplicationException($"The transfer of {sourceHolding.Available} {commodity.Name} ({commodity.Symbol}) from {sourceExchange.Name} to {destExchange.Name} has failed.");
            }
        }

        private bool IsDepositSymbolPermittedForDestination(Exchange destination, Commodity commodity)
        {
            return true;

            //if (commodity == null) { throw new ArgumentNullException(nameof(commodity)); }

            //if (string.Equals(destination.Name, "Coss", StringComparison.InvariantCultureIgnoreCase)) { return true; }
            //if (string.Equals(destination.Name, "Mew", StringComparison.InvariantCultureIgnoreCase))
            //{
            //    return commodity.IsEth || (commodity.IsEthToken.HasValue && commodity.IsEthToken.Value);
            //}

            //if (destination is IBinanceIntegration)
            //{
            //    var depositableSymbols = new List<Commodity>
            //    {
            //        CommodityRes.Ambrosous,
            //        CommodityRes.Poe
            //    };

            //    return depositableSymbols.Any(queryDepositableSymbol => queryDepositableSymbol.Id == commodity.Id);
            //}

            //if (destination is IHitBtcIntegration)
            //{
            //    var depositableSymbols = new List<Commodity> { CommodityRes.Fortuna, CommodityRes.Poe };
            //    return depositableSymbols.Any(queryDepositableSymbol => queryDepositableSymbol.Id == commodity.Id);
            //}

            //if (string.Equals(destination.Name, "qryptos", StringComparison.InvariantCultureIgnoreCase))
            //{
            //    var depositableSymbols = new List<Commodity>
            //    {
            //        CommodityRes.CanYaCoin
            //    };

            //    return depositableSymbols.Any(queryDepositableSymbol => queryDepositableSymbol.Id == commodity.Id);
            //}

            //return false;
        }

        private void ValidateDepositAddress(Commodity commodity, DepositAddress address)
        {
            _depositAddressValidator.Validate(commodity, address);
        }
    }
}
