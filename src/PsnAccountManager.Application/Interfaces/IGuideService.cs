using PsnAccountManager.Shared.DTOs;
namespace PsnAccountManager.Application.Interfaces;

public interface IGuideService
{
    /// <summary>
    /// Gets a list of summaries for all active guides.
    /// </summary>
    Task<IEnumerable<GuideSummaryDto>> GetActiveGuidesAsync();

    /// <summary>
    /// Gets the full details of a single guide by its ID.
    /// </summary>
    Task<GuideDetailsDto?> GetGuideByIdAsync(int id);
}