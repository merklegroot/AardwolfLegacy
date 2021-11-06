using System;

namespace idex_integration_lib.Models
{
    public class IdexExtendedOrder
    {
        public decimal Price { get; set; }
        public decimal Quantity { get; set; }

        public string NativeSymbol { get; set; }
        public string ContractAddress { get; set; }
        public string NativeBaseSymbol { get; set; }
        public string BaseContractAddress { get; set; }
        public long Id { get; set; }
        public string User { get; set; }
        public string Hash { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
