//using Newtonsoft.Json;

//namespace iridium_lib
//{
//    public class ValuationClient : IValuationClient
//    {
//        private readonly IServiceInvoker _serviceInvoker;

//        public ValuationClient(IServiceInvoker serviceInvoker)
//        {
//            _serviceInvoker = serviceInvoker;
//        }

//        public decimal? GetUsdValue(string symbol, bool forceRefresh)
//        {
//            //GetValueServiceModel
//            var serviceModel = new
//            {
//                Symbol = symbol,
//                ForceRefresh = forceRefresh
//            };

//            var response = _serviceInvoker.CallApi<string>("get-usdvalue", serviceModel);

//            return JsonConvert.DeserializeObject<decimal?>(response);
//        }
//    }
//}
