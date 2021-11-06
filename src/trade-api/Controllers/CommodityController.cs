using cache_lib.Models;
using integration_workflow_lib;
using client_lib;
using log_lib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using trade_api.Utils;
using trade_api.ViewModels;
using trade_contracts;
using trade_model;
using trade_res;
using exchange_client_lib;

namespace trade_api.Controllers
{
    public class CommodityController : ApiController
    {
        private readonly IExchangeClient _exchangeClient;

        private readonly ILogRepo _log;

        public CommodityController(
            IExchangeClient exchangeClient,
            ILogRepo log)
        {
             _exchangeClient = exchangeClient;

            _log = log;
        }

        public class CommodityViewModel
        {
            public Guid? Id { get; set; }
            public string Symbol { get; set; }
            public string Name { get; set; }
            public int? Decimals { get; set; }
            public string Contract { get; set; }
            public List<string> Exchanges { get; set; }
        }

        public class GetCommoditiesServiceModel
        {
            public bool ForceRefresh { get; set; }
            public string CachePolicy { get; set; }
        }

        [HttpPost]
        [Route("api/get-commodities")]
        public HttpResponseMessage GetCommodities(GetCommoditiesServiceModel serviceModel)
        {
            try
            {
                var cachePolicy = CachePolicyParser.ParseCachePolicy(
                    serviceModel?.CachePolicy,
                    serviceModel?.ForceRefresh ?? false,
                    CachePolicy.OnlyUseCacheUnlessEmpty);

                var commodities = _exchangeClient.GetCommodities(cachePolicy);
                return Request.CreateResponse(HttpStatusCode.OK, commodities);
            }
            catch (Exception exception)
            {
                var cachePolicyText = serviceModel?.CachePolicy != null ? serviceModel.CachePolicy.ToString() : "(null)";
                _log.Error($"Failed to get aggregate commodities with cache policy {cachePolicyText}, force refresh: {serviceModel?.ForceRefresh}.");
                _log.Error(exception);
                throw;
            }
        }

        [HttpPost]
        [Route("api/get-commodity-details")]
        public HttpResponseMessage GetCommodityDetails(GetCommodityDetailsServiceModel serviceModel)
        {
            try
            {
                if (serviceModel == null) { throw new ArgumentNullException(nameof(serviceModel)); }
                if (string.IsNullOrWhiteSpace(serviceModel.Symbol)) { throw new ArgumentNullException(nameof(serviceModel.Symbol)); }

                var cachePolicy = CachePolicyParser.ParseCachePolicy(serviceModel.CachePolicy, serviceModel.ForceRefresh, CachePolicy.AllowCache);
                var vm = _exchangeClient.GetCommodityDetails(serviceModel.Symbol, cachePolicy);

                return Request.CreateResponse(HttpStatusCode.OK, vm);
            }
            catch (Exception exception)
            {
                _log.Error(exception);
                throw;
            }
        }

        public class GetCommoditiesForExchangeServiceModel
        {
            public string Exchange { get; set; }
            public bool ForceRefresh { get; set; }
        }

        public class GetCommodityForExchangeServiceModel
        {
            public string Exchange { get; set; }
            public string Symbol { get; set; }
            public string NativeSymbol { get; set; }
            public bool ForceRefresh { get; set; }
        }

        [HttpPost]
        [Route("api/get-commodities-for-exchange")]
        public HttpResponseMessage GetCommoditiesForExchange(GetCommoditiesForExchangeServiceModel serviceModel)
        {
            if (serviceModel == null) { throw new ArgumentNullException(nameof(serviceModel)); }
            if (string.IsNullOrWhiteSpace(serviceModel.Exchange)) { throw new ArgumentNullException(nameof(serviceModel.Exchange)); }

            var cachePolicy = serviceModel.ForceRefresh ? CachePolicy.ForceRefresh : CachePolicy.OnlyUseCacheUnlessEmpty;
            var commoditiesContract = _exchangeClient.GetCommoditiesForExchange(serviceModel.Exchange, cachePolicy);

            var commodities = (commoditiesContract ?? new List<CommodityForExchange>())
                .OrderBy(item => item.Symbol)
                .ToList();

            var vms = new List<ExchangeCommodityViewModel>();
            foreach(var commodity in commodities)
            {
                var vm = ExchangeCommodityViewModel.FromModel(commodity);
                if (vm.CanonicalId.HasValue && vm.CanonicalId != default(Guid))
                {
                    var canon = CommodityRes.ById(vm.CanonicalId.Value);
                    if (canon != null)
                    {
                        if (!string.IsNullOrWhiteSpace(canon.ContractId))
                        {
                            vm.ContractAddress = canon.ContractId;
                        }
                    }

                    vm.IsEth = canon.IsEth;
                    vm.IsEthToken = canon.IsEthToken;
                }

                vms.Add(vm);
            }

            return Request.CreateResponse(HttpStatusCode.OK, vms);
        }

        [HttpPost]
        [Route("api/get-commodity-for-exchange")]
        public HttpResponseMessage GetCommodityForExchange(GetCommodityForExchangeServiceModel serviceModel)
        {
            try
            {
                if (serviceModel == null) { throw new ArgumentNullException(nameof(serviceModel)); }
                if (string.IsNullOrWhiteSpace(serviceModel.Exchange)) { throw new ArgumentNullException(nameof(serviceModel.Exchange)); }
                if (string.IsNullOrWhiteSpace(serviceModel.NativeSymbol)
                    && string.IsNullOrWhiteSpace(serviceModel.Symbol)) { throw new ArgumentException($"Either {nameof(serviceModel.NativeSymbol)} or {nameof(serviceModel.Symbol)} must not be null or whitespace."); }

                var cachePolicy = serviceModel.ForceRefresh ? CachePolicy.ForceRefresh : CachePolicy.OnlyUseCacheUnlessEmpty;

                var commodity = _exchangeClient.GetCommoditiyForExchange(serviceModel.Exchange, serviceModel.Symbol, serviceModel.NativeSymbol, cachePolicy);

                return Request.CreateResponse(HttpStatusCode.OK, commodity);
            }
            catch (Exception exception)
            {
                try
                {
                    var errorText = $"CommodityController.GetCommodityForExchange() failed for Exchange {serviceModel?.Exchange}, Symbol {serviceModel?.Symbol}, NativeSymbol {serviceModel?.NativeSymbol}, ForceRefresh {serviceModel?.ForceRefresh}";
                    _log.Error(errorText);
                } catch { }
                _log.Error(exception);
                throw;
            }
        }

        public class GetDepositAddressServiceModel
        {
            public string Exchange { get; set; }
            public string Symbol { get; set; }
            public bool ForceRefresh { get; set; }
        }

        [HttpPost]
        [Route("api/get-deposit-address")]
        public HttpResponseMessage GetDepositAddress(GetDepositAddressServiceModel serviceModel)
        {
            if (serviceModel == null) { throw new ArgumentNullException(nameof(serviceModel)); }
            if (string.IsNullOrWhiteSpace(serviceModel.Exchange)) { throw new ArgumentNullException(nameof(serviceModel.Exchange)); }
            if (string.IsNullOrWhiteSpace(serviceModel.Symbol)) { throw new ArgumentNullException(nameof(serviceModel.Symbol)); }

            var symbol = serviceModel.Symbol.Trim();

            var cachePolicy = serviceModel.ForceRefresh ? CachePolicy.ForceRefresh : CachePolicy.AllowCache;
            var depositAddress = _exchangeClient.GetDepositAddress(serviceModel.Exchange, serviceModel.Symbol, cachePolicy);

            var viewModel = new
            {
                DepositAddress = depositAddress?.Address,
                DepositMemo = depositAddress?.Memo,
                Symbol = symbol
            };

            return Request.CreateResponse(HttpStatusCode.OK, viewModel);
        }

        public class GetExchangesForCommodityServiceModel
        {
            public string Symbol { get; set; }
            public bool ForceRefresh { get; set; }
        }

        [HttpPost]
        [Route("api/get-exchanges-for-commodity")]
        public HttpResponseMessage GetExchangesForCommodity(GetExchangesForCommodityServiceModel serviceModel)
        {
            try
            {
                if (serviceModel == null) { throw new ArgumentNullException(nameof(serviceModel)); }
                if (string.IsNullOrWhiteSpace(serviceModel.Symbol)) { throw new ArgumentNullException(nameof(serviceModel.Symbol)); }

                var cachePolicy = serviceModel.ForceRefresh ? CachePolicyContract.ForceRefresh : CachePolicyContract.OnlyUseCacheUnlessEmpty;

                var results = _exchangeClient.GetExchangesForCommodity(serviceModel.Symbol, cachePolicy);

                return Request.CreateResponse(HttpStatusCode.OK, results);
            }
            catch (Exception exception)
            {
                _log.Error(exception);
                throw;
            }
        }
    }
}
