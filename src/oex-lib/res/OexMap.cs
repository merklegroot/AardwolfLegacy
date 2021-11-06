using commodity_map;
using oex_lib.res;
using System.Reflection;

namespace qryptos_lib.Res
{
    public class OexMap : CommodityMap
    {
        protected override string MapFilePiece => "oex-map";
        protected override string MapFilePath => @"C:\repo\trade-ex\oex-lib\res\";
        protected override Assembly IntegrationAssembly => typeof(OexResDummy).Assembly;
    }
}
