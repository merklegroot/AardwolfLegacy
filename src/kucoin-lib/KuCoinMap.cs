using commodity_map;
using System.Reflection;

namespace kucoin_lib
{
    public class KuCoinMap : CommodityMap
    {
        protected override string MapFilePiece => "kucoin-map";
        protected override string MapFilePath => @"C:\repo\trade-ex\kucoin-lib\res\";
        protected override Assembly IntegrationAssembly => typeof(KucoinIntegration).Assembly;
    }
}
