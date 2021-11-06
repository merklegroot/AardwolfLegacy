using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace idex_model
{
    // https://github.com/AuroraDAO/idex-api-docs
    /// <summary>
    /// Represents an Idex WebSocket frame
    /// </summary>
    public class IdexFrame
    {
        public enum MethodEnum
        {
            Unknown,
            NotifyOrderInserted,
            PushCancel,
            PushEthPrice,
            NotifyTradesInserted,
            PushServerBlock,
            PushCancels,
            PushRewardPoolSize
        }

        public ObjectId Id { get; set; }
        public ObjectId RawFrameId { get; set; }
        public string Raw { get; set; }

        //"method": "notifyOrderInserted",
        [JsonProperty("method")]
        [BsonElement("method")]
        public string Method { get; set; }

        [JsonIgnore]
        [BsonIgnore]
        public MethodEnum ParsedMethod
        {
            get
            {
                var methodEnumDictionary = new Dictionary<string, MethodEnum>(StringComparer.InvariantCultureIgnoreCase)
                {
                     { "notifyOrderInserted", MethodEnum.NotifyOrderInserted },
                     { "pushCancel", MethodEnum.PushCancel },
                     { "pushEthPrice", MethodEnum.PushEthPrice },
                     { "notifyTradesInserted", MethodEnum.NotifyTradesInserted },
                     { "pushServerBlock", MethodEnum.PushServerBlock },
                     { "pushCancels", MethodEnum.PushCancels },
                     { "pushRewardPoolSize", MethodEnum.PushRewardPoolSize }
                };

                return methodEnumDictionary.ContainsKey(Method)
                    ? methodEnumDictionary[Method] :
                    MethodEnum.Unknown;
            }
        }

        // "payload": {
        [JsonProperty("payload")]
        [BsonElement("payload")]
        public IdexFramePayload Payload { get; set; }

        public class IdexFramePayload
        {
            [JsonProperty("ethPrice")]
            [BsonElement("ethPrice")]
            public decimal? EthPrice { get; set; }

            //	"complete": false,
            [JsonProperty("complete")]
            [BsonElement("complete")]
            public bool Complete { get; set; }

            //	"id": 22798582,
            [JsonProperty("id")]
            [BsonElement("id")]
            public long Id { get; set; }

            //	"tokenBuy": "0xaa7a9ca87d3694b5755f213b5d04094b8d0f0a6f",
            [JsonProperty("tokenBuy")]
            [BsonElement("tokenBuy")]
            public string TokenBuy { get; set; }

            //	"amountBuy": "2644067796610169400000",
            [JsonProperty("amountBuy")]
            [BsonElement("amountBuy")]
            public double AmountBuy { get; set; }

            //	"tokenSell": "0x0000000000000000000000000000000000000000",
            [JsonProperty("tokenSell")]
            [BsonElement("tokenSell")]
            public string TokenSell { get; set; }

            //	"amountSell": "780026440677966075",
            [JsonProperty("amountSell")]
            [BsonElement("amountSell")]
            public double AmountSell { get; set; }

            //	"expires": 10000,
            [JsonProperty("expires")]
            [BsonElement("expires")]
            public long Expires { get; set; }

            //	"nonce": 19957994,
            [JsonProperty("nonce")]
            [BsonElement("nonce")]
            public long Nonce { get; set; }

            //	"user": "0xa46f905017692c832618faf5fce3a7c4fa335591",
            [JsonProperty("user")]
            [BsonElement("user")]
            public string User { get; set; }

            //	"v": 27,
            [JsonProperty("v")]
            [BsonElement("v")]
            public int V { get; set; }

            //	"r": "0x16f8831fb01fafaa24658abd725c5bdf160ab3549c66a2b223a84ffaf2e192b3",
            [JsonProperty("r")]
            [BsonElement("r")]
            public string R { get; set; }

            //	"s": "0x3354fe1bfee541242936fc294515d58084258db27f58e8662ddd2f3adccd9fe8",
            [JsonProperty("s")]
            [BsonElement("s")]
            public string S { get; set; }

            //	"hash": "0x763479c43f59d666440c8c28bcaf89021b53d096a0715502bdd3450d1539242d",
            /// <summary>
            /// Related events for an order share this hash value.
            /// </summary>
            [JsonProperty("hash")]
            [BsonElement("hash")]
            public string Hash { get; set; }

            //	"feeDiscount": "0",
            [JsonProperty("feeDiscount")]
            [BsonElement("feeDiscount")]
            public decimal FeeDiscount { get; set; }

            //	"rewardsMultiple": "100",
            [JsonProperty("rewardsMultiple")]
            [BsonElement("rewardsMultiple")]
            public decimal RewardsMultiple { get; set; }

            //	"updatedAt": "2018-06-15T18:36:55.000Z",
            [JsonProperty("updatedAt")]
            [BsonElement("updatedAt")]
            public DateTime UpdatedAt { get; set; }

            //	"createdAt": "2018-06-15T18:36:55.000Z"
            [JsonProperty("createdAt")]
            [BsonElement("createdAt")]
            public DateTime CreatedAt { get; set; }
        }
    }
}
