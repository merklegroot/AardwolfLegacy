using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace trade_res
{
    public static class CommodityListExtensions
    {
        public static Commodity ById(this List<Commodity> allCanon, Guid id)
        {
            if(id == default(Guid)) { throw new ArgumentNullException(nameof(id)); }
            if (allCanon == null) { return null; }
            return allCanon.SingleOrDefault(queryCanon => queryCanon.Id == id);
        }
    }
}
