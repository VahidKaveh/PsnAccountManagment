namespace PsnAccountManager.Shared.Enums;

public enum RawMessageStatus
{
    Pending,
    Processed,
    Processing,
    Ignored,
    Failed,
    Deleted,
    PendingChange = 4
}