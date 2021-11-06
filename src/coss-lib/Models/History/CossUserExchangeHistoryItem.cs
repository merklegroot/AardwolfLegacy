using System;

namespace coss_data_model
{
    public class CossExchangeHistoryItem
    {
        public string id { get; set; }
        public DateTime created_at { get; set; }
        public decimal amount { get; set; }
        public decimal amount_sold { get; set; }
        public decimal amount_bought { get; set; }
        public decimal price { get; set; }
        public decimal total { get; set; }
        public decimal total_sold { get; set; }
        public decimal total_bought { get; set; }
        public decimal transaction_fee_percentage { get; set; }
        public decimal transaction_fee_percentage_sold { get; set; }
        public decimal transaction_fee_percentage_bought { get; set; }
        public decimal transaction_fee_total { get; set; }
        public decimal transaction_fee_total_sold { get; set; }
        public decimal transaction_fee_total_bought { get; set; }
        public Guid user_guid { get; set; }
        public Guid pair_guid { get; set; }
        public string order_direction { get; set; }
        public string counterpart_user_guid { get; set; }
        public string from_code { get; set; }
        public string to_code { get; set; }
        public string username { get; set; }
        public string full_name { get; set; }
        public string email_address { get; set; }
        public Guid? kyc_level_guid { get; set; }
        public bool kyc_validated { get; set; }
        public string counterpart_username { get; set; }
        public string counterpart_full_name { get; set; }
        public string counterpart_email_address { get; set; }
        public string counterpart_kyc_level_guid { get; set; }
        public bool counterpart_kyc_validated { get; set; }
        public string side { get; set; }

        public CossExchangeHistoryItem Clone()
        {
            return this != null
                ? (CossExchangeHistoryItem)MemberwiseClone()
                : null;
        }
    }
}
