using PsnAccountManager.Shared.DTOs;
using System.Threading.Tasks;

namespace PsnAccountManager.Application.Interfaces;

/// <summary>
/// Defines the logic for the intelligent account matching algorithm.
/// </summary>
public interface IMatcherService
{
    /// <summary>
    /// Finds the best combination of available accounts to satisfy a user's game request.
    /// The algorithm's behavior is configured via application settings.
    /// </summary>
    /// <param name="request">A DTO containing the list of requested game IDs.</param>
    /// <returns>A DTO containing a ranked list of suggested accounts.</returns>
    Task<MatchResultDto> FindMatchesAsync(MatchRequestDto request);
}