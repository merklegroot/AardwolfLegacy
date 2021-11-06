namespace trade_strategy_lib
{
    public class AutoArb
    {
        // start off pretending that both shides have a single set price.
        public bool Compare(decimal sourceBook, decimal destBook)
        {
            return sourceBook < destBook;
        }
    }
}
