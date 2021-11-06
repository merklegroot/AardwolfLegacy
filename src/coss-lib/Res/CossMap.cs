using commodity_map;
using System.Reflection;

namespace coss_lib.Res
{
    public class CossMap : CommodityMap
    {
        protected override string MapFilePiece => "coss-map";
        protected override string MapFilePath => @"C:\repo\trade-ex\coss-lib\res\";
        protected override Assembly IntegrationAssembly => typeof(CossIntegration).Assembly;
    }
}
