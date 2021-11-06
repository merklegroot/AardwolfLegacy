using commodity_map;
using System.Reflection;

namespace bit_z_lib.res
{
    public class BitzMap : CommodityMap
    {
        protected override string MapFilePiece => "bitz-map";
        protected override string MapFilePath => @"C:\repo\trade-ex\bitz-lib\res\";
        protected override Assembly IntegrationAssembly => typeof(BitzIntegration).Assembly;
    }
}
