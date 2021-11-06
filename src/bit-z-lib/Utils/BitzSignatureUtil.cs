using binary_lib;
using crypt_lib;
using System.Collections.Generic;
using System.Linq;

namespace bit_z_lib.Utils
{
    public static class BitzSignatureUtil
    {
        public static string BuildSignature(string privateKey, Dictionary<string, string> queryDictionary)
        {
            var linkString = CreateLinkString(queryDictionary);
            var fullString = linkString + privateKey;
            
            return fullString != null
                ? BinaryUtil.ByteArrayToHexString(Md5Util.GetMd5Hash(fullString))
                : null;
        }

        private static string CreateLinkString(Dictionary<string, string> queryDictionary)
        {
            var sortedKeys = queryDictionary.Keys.OrderBy(queryKey => queryKey).ToList();
            return string.Join("&", sortedKeys.Select(queryParam => $"{queryParam}={queryDictionary[queryParam]}"));
        }
    }
}
