using commodity_map;
using System.Reflection;

namespace coinbase_lib.res
{
    public class CoinbaseMap : CommodityMap
    {
        protected override string MapFilePiece => "coinbase-map";
        protected override string MapFilePath => @"C:\repo\trade-ex\coinbase-lib\res\";
        protected override Assembly IntegrationAssembly => typeof(CoinbaseIntegration).Assembly;
    }
}
