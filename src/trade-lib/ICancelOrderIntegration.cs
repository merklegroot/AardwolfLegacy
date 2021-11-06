namespace trade_lib
{
    public interface ICancelOrderIntegration
    {
        void CancelOrder(string orderId);
    }
}
