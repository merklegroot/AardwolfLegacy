namespace tidex_integration_library.Models
{
    public class TidexCurrency
    {
        public long Id { get; set; }
        public string Symbol { get; set; }
        public long Type { get; set; }
        public long Order { get; set; }
        public decimal AmountPoint { get; set; }
        public bool DepositEnable { get; set; }
        public decimal DepositMinAmount { get; set; }
        public bool WithdrawEnable { get; set; }
        public decimal WithdrawFee { get; set; }

        /*
			"id": 2,
			"symbol": "BTC",
			"type": 2,
			"name": "Bitcoin",
			"order": 1,
			"amountPoint": 8,
			"depositEnable": true,
			"depositMinAmount": 0.0005,
			"withdrawEnable": true,
			"withdrawFee": 0.001,
			"withdrawMinAmout": 0.001,
			"settings": {
				"Blockchain": "https://blockchain.info/",
				"TxUrl": "https://blockchain.info/tx/{0}",
				"AddrUrl": null,
				"ConfirmationCount": 3,
				"NeedMemo": false
			},
			"visible": true
        */
    }
}
