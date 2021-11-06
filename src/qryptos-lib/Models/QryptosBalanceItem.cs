using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace qryptos_lib.Models
{
    public class QryptosBalanceItem
    {
        public decimal? Free { get; set; }
        public decimal? Used { get; set; }
        public decimal? Total { get; set; }
    }
}
