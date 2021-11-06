using dump_lib;
using idex_client_lib;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using trade_res;
using web_util;

namespace idex_client_lib_tests
{
    [TestClass]
    public class IdexClientTests
    {
        public IdexClient _client;

        [TestInitialize]
        public void Setup()
        {
            var webUtil = new WebUtil();
            _client = new IdexClient(webUtil);
        }

        [TestMethod]
        public void Idex_client__place_limit_order__agrello()
        {
            const string EmptyContract = "0x0000000000000000000000000000000000000000";

            // DLT = Agrello
            var commodity = CommodityRes.Agrello;
            commodity.Dump();

            var decimalFactor = Math.Pow(commodity.Decimals.Value, 10);

            const double QuantityToBuy = 100.0;
            const double Price = 0.000248959155;

            var adjustedQuantityToBuy = QuantityToBuy * decimalFactor;

            // _client.PlaceLimitOrder(commodity.ContractId, adjustedQuantityToBuy, EmptyContract, )
        }
    }
}
