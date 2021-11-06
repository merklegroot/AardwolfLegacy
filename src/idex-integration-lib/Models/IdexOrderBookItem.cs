using System;

namespace idex_integration_lib.Models
{
    public class IdexOrderBookItem
    {
        public long Id { get; set; }
        public string TokenBuy { get; set; }
        public double AmountBuy { get; set; }
        public string TokenSell { get; set; }
        public double AmountSell { get; set; }
        //public ulong Expires { get; set; }
        //public ulong Nonce { get; set; }
        public string Hash { get; set; }
        public string User { get; set; }
        public string V { get; set; }
        public string R { get; set; }
        public string S { get; set; }
        //public Ulong Filled { get; set; }
        
        // public decimal? FeeDiscount { get; set; }
        public double? FeeDiscount { get; set; }

        // public decimal? RewardsMultiple { get; set; }
        public double? RewardsMultiple { get; set; }

        public bool Complete { get; set; }
        public string Cancelled { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        /*
{ id: 2195604,
    tokenBuy: '0x0000000000000000000000000000000000000000',
    amountBuy: '3715000000000000000',
    tokenSell: '0x7c5a0ce9267ed19b22f8cae653f198e3e8daf098',
    amountSell: '500000000000000000000',
    expires: 190000,
    nonce: 173,
    hash: '0xe4f0ec8a9d4865c61f2f206a5d911770c96c7e57a6550c8d3121a9df0bc10124',
    user: '0x35cc4ea543552d735893e337f88aa804da3de439',
    v: 27,
    r: '0xf2dda40cee9c16c882025fe3f5ad65dbc3c53a2437991ce6252cb846b4429ed0',
    s: '0x465992011a05bb74ad4fffe3e8bd961a0b817b2c3fa48425485c3ee1ed5182dd',
    filled: '15898368370369999',
    feeDiscount: '0',
    rewardsMultiple: '100',
    complete: false,
    cancelled: null,
    createdAt: '2018-01-10T13:18:10.000Z',
    updatedAt: '2018-01-14T09:49:38.000Z' }
        */
    }
}
