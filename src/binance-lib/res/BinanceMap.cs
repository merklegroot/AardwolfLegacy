using commodity_map;
using System.Reflection;

namespace binance_lib.res
{
    public class BinanceMap : CommodityMap
    {
        protected override string MapFilePiece => "binance-map";
        protected override string MapFilePath => @"C:\repo\trade-ex\binance-lib\res\";
        protected override Assembly IntegrationAssembly => typeof(BinanceIntegration).Assembly;
    }
}
