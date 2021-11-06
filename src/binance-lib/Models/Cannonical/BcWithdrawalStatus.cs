namespace binance_lib.Models.Canonical
{
    public enum BcWithdrawalStatus
    {
        EmailSend = 0,
        Canceled = 1,
        AwaitingApproval = 2,
        Rejected = 3,
        Proccessing = 4,
        Failure = 5,
        Completed = 6
    }
}
