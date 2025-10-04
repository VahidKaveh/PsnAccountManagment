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

    Task<ProcessingResult> ProcessAndSaveAccountAsync(ProcessMessageViewModel viewModel);
}