using rabbit_lib;

namespace trade_api
{
    public class RabbitWorkflow
    {
        public void Initialize(IRabbitConnection conn)
        {
            //conn.Listen("valuation", HandleValuation);
        }

        private void HandleValuation(string message)
        {

        }
    }
}