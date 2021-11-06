namespace bit_z_lib.Models
{
    public class BitzHolding
    {
        public string Symbol { get; set; }
        public decimal? Quantity { get; set; }
        public decimal? OverQuantity { get; set; }
        public decimal? LockQuantity { get; set; }
    }
}
