using commodity_map;
using System.Reflection;
using yobit_lib;

namespace kucoin_lib.Res
{
    public class YobitMap : CommodityMap
    {
        protected override string MapFilePiece => "yobit-map.json";
        protected override string MapFilePath => @"C:\repo\trade-ex\yobit-lib\res\";
        protected override Assembly IntegrationAssembly => typeof(YobitIntegration).Assembly;
    }
}
