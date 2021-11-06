namespace trade_email_lib
{
    public interface ITradeEmailUtil
    {
        string GetCossWithdrawalLink(string symbol, decimal quantity);
        string GetWithdrawalLink(string integrationName, string symbol, decimal quantity);
    }
}
