namespace balance_lib
{
    public class HoldingWithValueViewModel
    {
        public string Asset { get; set; }
        public string AccountType { get; set; }
        public string DispayName
        {
            get
            {
                var accountTypeSection = !string.IsNullOrWhiteSpace(AccountType)
                    ? $"({AccountType})"
                    : string.Empty;
                return $"{Asset ?? string.Empty} {accountTypeSection}".Trim();
            }
        }
        public decimal Available { get; set; }
        public decimal InOrders { get; set; }
        public decimal Total { get; set; }
        public decimal? Value { get; set; }
    }
}
