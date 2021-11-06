using commodity_map;
using System.Reflection;

namespace qryptos_lib.Res
{
    public class QryptosMap : CommodityMap
    {
        protected override string MapFilePiece => "qryptos-map";
        protected override string MapFilePath => @"C:\repo\trade-ex\qryptos-lib\res\";
        protected override Assembly IntegrationAssembly => typeof(QryptosIntegration).Assembly;
    }
}
