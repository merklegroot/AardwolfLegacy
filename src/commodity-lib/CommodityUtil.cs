namespace commodity_lib
{
    public static class CommodityUtil
    {
        public enum IsEthTokenResult
        {
            DontKnow = 0,
            Yes = 1,
            No = 2
        }

        public static IsEthTokenResult IsEthToken(string symbol)
        {
            return IsEthTokenResult.DontKnow;
        }

        public static string GetEthContractAddress(string symbol)
        {
            return null;
        }

        public static string GetSymbolForEthContractAddress(string contractAddress)
        {
            return null;
        }
    }
}
