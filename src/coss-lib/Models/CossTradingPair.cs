namespace coss_model
{
    public class CossTradingPair
    {
        public string Id { get; set; }

        public string Pair { get; set; }

        public string First { get; set; }

        public string Second { get; set; }

        public int FirstPrecision { get; set; }

        public int SecondPrecision { get; set; }

        public decimal Volume { get; set; }

        public decimal VolumeUsd { get; set; }

        public decimal Price { get; set; }

        public string PriceDirection { get; set; }

        public decimal Change { get; set; }

        public decimal Start24h { get; set; }

        public decimal High { get; set; }

        public decimal Low { get; set; }
    }
}
