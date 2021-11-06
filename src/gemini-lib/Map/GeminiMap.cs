using commodity_map;
using gemini_lib.res;
using System.Reflection;

namespace gemini_lib.Map
{
    public class GeminiMap : CommodityMap
    {
        protected override string MapFilePiece => "gemini-map";
        protected override string MapFilePath => @"C:\repo\trade-ex\gemini-lib\res\";
        protected override Assembly IntegrationAssembly => typeof(GeminiResDummy).Assembly;
    }
}
