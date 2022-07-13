namespace OCPP.Core.Database;

public enum ChargeTagStatus
{
    Accepted,
    Blocked,
    Expired,
    Invalid,
    ConcurrentTx
    
}

public enum CpTagAccsessStatus
{
    Accepted,
    Blocked,
    Expired,
    Invalid,
    ConcurrentTx
}

public enum TransactionStatus
{
    Started,
    Terminated,
    Completed,
}

public enum SendRequestStatus
{
    Queued,
    Sent,
    Failed,
    Cancelled,
    Completed,
}
