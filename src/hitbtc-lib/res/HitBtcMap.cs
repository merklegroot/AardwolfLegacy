using commodity_map;
using System.Reflection;

namespace hitbtc_lib.res
{
    public class HitBtcMap : CommodityMap
    {
        protected override string MapFilePiece => "hitbtc-map";
        protected override string MapFilePath => @"C:\repo\trade-ex\hitbtc-lib\res\";
        protected override Assembly IntegrationAssembly => typeof(HitBtcIntegration).Assembly;
    }
}
