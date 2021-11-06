using res_util_lib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace refresher_service_lib.res
{
    public static class RefresherRes
    {
        private static List<string> _symbolsOfInterestPropertyInternal = null;
        public static List<string> SymbolsOfInterest
        {
            get
            {
                var retriever = new Func<List<string>>(() =>
                {
                    return ResUtil.Get<string>("symbols-of-interest.txt", typeof(RefresherResDummy).Assembly)
                        .Trim()
                        .Replace("\r\n", "\r")
                        .Replace("\n", "\r")
                        .Split('\r')
                        .Where(item => !string.IsNullOrWhiteSpace(item))
                        .Select(item => item.Trim().ToUpper())
                        .Distinct()
                        .OrderBy(item => item)
                        .ToList();
                });

                return _symbolsOfInterestPropertyInternal
                    ?? (_symbolsOfInterestPropertyInternal = retriever());
            }
        }
    }
}
