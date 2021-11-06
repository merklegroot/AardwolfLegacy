namespace trade_contracts
{
    public interface IResultContract
    {
        bool WasSuccessful { get; set; }
        string FailureReason { get; set; }
    }
}
