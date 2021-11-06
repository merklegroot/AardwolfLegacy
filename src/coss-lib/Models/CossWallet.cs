using Newtonsoft.Json;
using System;

namespace coss_lib.Models
{
    public class CossWallet
    {
        [JsonProperty("guid")]
        public Guid Id { get; set; }

        [JsonProperty("user_guid")]
        public Guid UserId { get; set; }

        [JsonProperty("reference")]
        public string WalletAddress { get; set; }

        [JsonProperty("cold_wallet_balance")]
        public decimal ColdWalletBalance { get; set; }

        [JsonProperty("transaction_id")]
        public string TransactionId { get; set; }

        [JsonProperty("orders_balance")]
        public decimal OrdersBalance { get; set; }

        [JsonProperty("last_transaction_id")]
        public string LastTransactionId { get; set; }

        [JsonProperty("last_block_number")]
        public long? LastBlockNumber { get; set; }

        [JsonProperty("has_pending_deposit_transactions")]
        public bool HasPendingDepositTransactions { get; set; }

        //"currencyGuid":"98e5c9ff-6460-46a08f0f-3c9d0e31637d",
        [JsonProperty("currencyGuid")]
        public string CurrencyId { get; set; }

        //"currencyType": "CRYPTO",
        public string CurrencyType { get; set; }

        //"currencyName":"BPL",
        public string CurrencyName { get; set; }

        //"currencyCode":"BPL",
        public string CurrencyCode { get; set; }

        //"currencyPrecision":8,
        public int CurrencyPrecision { get; set; }

        //"currencyDisplayLabel":"Blockpool",
        public string CurrencyDisplayLabel { get; set; }

        //"currencyIsErc20Token":false,
        public bool CurrencyIsErc20Token { get; set; }

        //"currencyWithdrawalFee":"0.20000000",
        public decimal CurrencyWithdrawalFee { get; set; }

        //"currencyMinWithdrawalAmount":"20.00000000",
        public decimal CurrencyMinWithdrawalAmount { get; set; }

        //"currencyMinDepositAmount":"1.00000000",
        public decimal CurrencyMinDepositAmount { get; set; }

        //"currencyIsWithdrawalLocked":false,
        public bool CurrencyIsWithdrawalLocked { get; set; }

        //"currencyIsDepositLocked":false
        public bool CurrencyIsDepositLocked { get; set; }
    }
}
