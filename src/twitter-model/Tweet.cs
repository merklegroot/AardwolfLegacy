using System;

namespace twitter_model
{
    public class Tweet
    {
        public long Id { get; set; }
        public DateTime TimeStampUtc { get; set; }       
        public string Text { get; set; }
        public bool Truncated { get; set; }
        public bool IsQuoteStatus { get; set; }
        public long RetweetCount { get; set; }
        public long FavoriteCount { get; set; }
    }
}
