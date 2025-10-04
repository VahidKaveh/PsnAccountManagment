namespace PsnAccountManager.Shared.ViewModels;

/// <summary>
/// Represents a single detected change.
/// </summary>
public class ChangeInfo
{
    public string FieldName { get; set; }
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
}

/// <summary>
/// The result object returned by the processing service.
/// </summary>
public class ProcessingResult
{
    public bool Success { get; set; }
    public int AccountId { get; set; }
    public string AccountTitle { get; set; }
    public List<ChangeInfo> DetectedChanges { get; set; } = new();
    public bool IsNewAccount { get; set; }
    public string? ErrorMessage { get; set; }
    public bool IsChange { get; set; }
}
/// <summary>
/// Statistics class for processing operations
/// </summary>
public class ProcessingStats
{
    public int TotalMessages { get; set; }
    public int PendingMessages { get; set; }
    public int ProcessedMessages { get; set; }
    public int FailedMessages { get; set; }
    public int ChangesDetected { get; set; }
    public DateTime? LastProcessingRun { get; set; }
}