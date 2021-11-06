using commodity_map;
using System.Reflection;

namespace cryptopia_lib.Res
{
    public class CryptopiaMap : CommodityMap
    {
        protected override string MapFilePiece => "cryptopia-map.json";
        protected override string MapFilePath => @"C:\repo\trade-ex\cryptopia-lib\res\";
        protected override Assembly IntegrationAssembly => typeof(CryptopiaIntegration).Assembly;
    }
}
