using System;

namespace trade_res
{
    public class Commodity
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Symbol { get; set; }
        public bool IsEth { get; set; }
        public bool? IsEthToken { get; set; }
        public string ContractId { get; set; }
        public int? Decimals { get; set; }
        public bool IsDominant { get; set; }
        public string Website { get; set; }
        public string Telegram { get; set; }

        public Commodity Clone()
        {
            return (Commodity)MemberwiseClone();
        }

        public override string ToString()
        {
            if (!string.IsNullOrWhiteSpace(Name) && !string.IsNullOrWhiteSpace(Symbol))
            {
                return $"{Name} ({Symbol})";
            }

            if (!string.IsNullOrWhiteSpace(Name)) { return Name; }
            if (!string.IsNullOrWhiteSpace(Symbol)) { return Symbol; }
            return "No Name.";
        }
    }
}
