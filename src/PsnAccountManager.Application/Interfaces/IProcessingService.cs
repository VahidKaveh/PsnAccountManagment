using System.Threading.Tasks;
using PsnAccountManager.Shared.DTOs;
using PsnAccountManager.Shared.ViewModels;

namespace PsnAccountManager.Application.Interfaces;

public interface IProcessingService
{
    /// <summary>
    /// Parses a raw message using its channel's parsing profile.
    /// Does not save anything to the database.
    /// </summary>
    Task<ParsedAccountDto?> GetParsedDataPreviewAsync(int rawMessageId);

    /// <summary>
    /// Processes and saves an account from the submitted view model data.
    /// </summary>
    Task<ProcessingResult> ProcessAndSaveAccountAsync(ProcessMessageViewModel viewModel);

    /// <summary>
    /// Processes a raw message with change detection capabilities.
    /// </summary>
    Task<ProcessingResult> ProcessRawMessageAsync(int rawMessageId);

    /// <summary>
    /// Processes a single message by ID with enhanced error handling and change detection.
    /// </summary>
    Task ProcessMessageAsync(int messageId);

    /// <summary>
    /// Process all pending messages in batch.
    /// </summary>
    Task ProcessAllPendingMessagesAsync();

    /// <summary>
    /// Get processing statistics using repository methods.
    /// </summary>
    Task<ProcessingStats> GetProcessingStatsAsync();
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