using System.Linq.Expressions;
using PsnAccountManager.Domain.Entities;

namespace PsnAccountManager.Domain.Interfaces;

/// <summary>
/// Repository interface for Game entity operations
/// </summary>
public interface IGameRepository : IGenericRepository<Game, int>
{
    // ==================== EXISTING METHODS ====================

    /// <summary>
    /// Finds a game by its unique identifier from Sony
    /// </summary>
    Task<Game?> GetBySonyCodeAsync(string sonyCode);

    /// <summary>
    /// Searches for games based on their title (partial match)
    /// </summary>
    Task<IEnumerable<Game>> SearchByTitleAsync(string titleQuery);

    /// <summary>
    /// Finds a single game by its exact title (case-insensitive)
    /// Used by the ProcessingService to check for existing games
    /// </summary>
    Task<Game?> FindByTitleAsync(string title);


    // ==================== NEW METHODS ====================

    /// <summary>
    /// Gets a game by its exact title
    /// CRITICAL: Used by ProcessingService and LearningData
    /// </summary>
    Task<Game?> GetByTitleAsync(string title);

    /// <summary>
    /// Gets multiple games by their titles (bulk operation)
    /// </summary>
    Task<IEnumerable<Game>> GetByTitlesAsync(IEnumerable<string> titles);

    /// <summary>
    /// Gets total game count
    /// </summary>
    Task<int> GetTotalCountAsync();

    /// <summary>
    /// Gets most popular games based on account associations
    /// </summary>
    Task<IEnumerable<Game>> GetMostPopularAsync(int count = 10);
}