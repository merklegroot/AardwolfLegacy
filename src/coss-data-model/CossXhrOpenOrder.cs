namespace coss_data_model
{
    public class CossXhrOpenOrder
    {
        //"amount": "17.23187207",
        public decimal? amount { get; set; }

        //"created_at": 1535462218678,
        public decimal created_at { get; set; }

        //"order_guid": "4d99d8d1-fcc0-4332-a1e4-bb853411f04c",
        public string order_guid { get; set; }

        //"pair_id": "omg-btc",
        public string pair_id { get; set; }

        //"price": "0.00058032",
        public decimal? price { get; set; }

        //"total": "0.01000000",
        public decimal? total { get; set; }

        //"type": "buy",
        public string type { get; set; }

        //"tradeType": "limit-order"
        public string tradeType { get; set; }
    }
}
