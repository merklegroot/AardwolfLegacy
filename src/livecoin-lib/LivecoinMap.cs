using commodity_map;
using System.Reflection;

namespace livecoin_lib
{
    public class LivecoinMap : CommodityMap
    {
        protected override string MapFilePiece => "livecoin-map";
        protected override string MapFilePath => @"C:\repo\trade-ex\livecoin-lib\res\";
        protected override Assembly IntegrationAssembly => typeof(LivecoinIntegration).Assembly;
    }
}
