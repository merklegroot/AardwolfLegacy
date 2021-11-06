//namespace trade_model
//{
//    public class Commodity
//    {
//        public string Symbol { get; set; }

//        public override string ToString()
//        {
//            return Symbol;
//        }

//        public static implicit operator string(Commodity item)
//        {
//            return item != null ? item.Symbol : null;
//        }

//        public override bool Equals(object item)
//        {
//            if (this == null && item == null) { return true; }
//            if (this == null || item == null) { return false; }

//            return string.Equals(Symbol, ((Commodity)item).Symbol, System.StringComparison.InvariantCultureIgnoreCase);
//        }

//        public override int GetHashCode()
//        {
//            return Symbol.GetHashCode();
//        }
//    }
//}
