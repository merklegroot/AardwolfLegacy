using System.Linq;
using trade_model;

namespace idex_model
{
    public static class IdexOpenOrderExtensions
    {
        public static TradingPair TradingPair(this IdexOpenOrder model)
        {
            if (model == null || string.IsNullOrWhiteSpace(model.Market)) { return null; }
            var pieces = model.Market.Split('/').ToList();
            if (pieces == null || pieces.Count != 2) { return null; }

            return new TradingPair(pieces[0].Trim(), pieces[1].Trim());
        }
    }
}
