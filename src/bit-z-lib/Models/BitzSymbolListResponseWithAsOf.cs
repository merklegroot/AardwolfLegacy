using System;

namespace bit_z_lib.Models
{
    public class BitzSymbolListResponseWithAsOf
    {
        public BitzSymbolListResponse SymbolListResponse { get; set; }
        public DateTime? AsOfUtc { get; set; }
    }
}
