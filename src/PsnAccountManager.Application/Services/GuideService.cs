using Microsoft.Extensions.Logging;
using PsnAccountManager.Application.Interfaces;
using PsnAccountManager.Domain.Interfaces;
using PsnAccountManager.Shared.DTOs;

namespace PsnAccountManager.Application.Services;

public class GuideService : IGuideService
{
    private readonly IGuideRepository _guideRepository;
    private readonly ILogger<GuideService> _logger;

    public GuideService(IGuideRepository guideRepository, ILogger<GuideService> logger)
    {
        _guideRepository = guideRepository;
        _logger = logger;
    }

    public async Task<IEnumerable<GuideSummaryDto>> GetActiveGuidesAsync()
    {
        _logger.LogInformation("Fetching active guides from repository.");
        var activeGuides = await _guideRepository.GetActiveGuidesAsync();

        // Map the collection of entities to a collection of DTOs
        return activeGuides.Select(guide => new GuideSummaryDto
        {
            Id = guide.Id,
            Title = guide.Title
        });
    }

    public async Task<GuideDetailsDto?> GetGuideByIdAsync(int id)
    {
        _logger.LogInformation("Fetching guide with ID: {GuideId}", id);
        var guide = await _guideRepository.GetByIdAsync(id);

        if (guide == null)
        {
            _logger.LogWarning("Guide with ID: {GuideId} not found.", id);
            return null;
        }

        // Map the single entity to a detailed DTO
        return new GuideDetailsDto
        {
            Id = guide.Id,
            Title = guide.Title,
            Content = guide.Content,
            MediaUrl = guide.MediaUrl
        };
    }
}