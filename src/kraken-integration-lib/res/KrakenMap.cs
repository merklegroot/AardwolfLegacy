using commodity_map;
using kraken_integration_lib;
using System.Reflection;

namespace kraken_lib.res
{
    public class KrakenMap : CommodityMap
    {
        protected override string MapFilePiece => "kraken-map";
        protected override string MapFilePath => @"C:\repo\trade-ex\kraken-lib\res\";
        protected override Assembly IntegrationAssembly => typeof(KrakenIntegration).Assembly;
    }
}
