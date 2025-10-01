using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PsnAccountManager.Domain.Entities;
using PsnAccountManager.Domain.Interfaces;
using PsnAccountManager.Infrastructure.Data;

namespace PsnAccountManager.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for Game entity
/// Updated with critical GetByTitleAsync method for ProcessingService
/// </summary>
public class GameRepository : GenericRepository<Game, int>, IGameRepository
{
    private readonly ILogger<GameRepository> _logger;

    public GameRepository(
        PsnAccountManagerDbContext context,
        ILogger<GameRepository> logger) : base(context)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    // ==================== EXISTING METHODS ====================

    public async Task<Game?> GetBySonyCodeAsync(string sonyCode)
    {
        try
        {
            return await DbSet.FirstOrDefaultAsync(g => g.SonyCode == sonyCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting game by Sony code {SonyCode}", sonyCode);
            throw;
        }
    }

    public async Task<IEnumerable<Game>> SearchByTitleAsync(string titleQuery)
    {
        try
        {
            return await DbSet
                .Where(g => g.Title.Contains(titleQuery))
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching games by title {Title}", titleQuery);
            throw;
        }
    }

    public async Task<Game?> FindByTitleAsync(string title)
    {
        try
        {
            var trimmedTitle = title.Trim();
            return await DbSet
                .FirstOrDefaultAsync(g => g.Title.ToLower() == trimmedTitle.ToLower());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error finding game by title {Title}", title);
            throw;
        }
    }

    // ==================== NEW METHODS ====================

    /// <summary>
    /// CRITICAL METHOD: Used by ProcessingService and LearningData
    /// Gets a game by exact title (case-insensitive)
    /// </summary>
    public async Task<Game?> GetByTitleAsync(string title)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(title))
            {
                _logger.LogWarning("Title is null or empty");
                return null;
            }

            var trimmedTitle = title.Trim();

            return await DbSet
                .AsNoTracking()
                .FirstOrDefaultAsync(g => g.Title.ToLower() == trimmedTitle.ToLower());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting game by title {Title}", title);
            throw;
        }
    }

    /// <summary>
    /// Gets multiple games by their titles in a single query (bulk operation)
    /// Optimized for performance when checking multiple games
    /// </summary>
    public async Task<IEnumerable<Game>> GetByTitlesAsync(IEnumerable<string> titles)
    {
        try
        {
            if (titles == null || !titles.Any())
            {
                _logger.LogWarning("Titles collection is null or empty");
                return Enumerable.Empty<Game>();
            }

            var trimmedTitles = titles
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .Select(t => t.Trim().ToLower())
                .ToList();

            if (!trimmedTitles.Any())
            {
                return Enumerable.Empty<Game>();
            }

            return await DbSet
                .AsNoTracking()
                .Where(g => trimmedTitles.Contains(g.Title.ToLower()))
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting games by titles");
            throw;
        }
    }

    /// <summary>
    /// Gets total count of games in database
    /// </summary>
    public async Task<int> GetTotalCountAsync()
    {
        try
        {
            return await DbSet.CountAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting total game count");
            throw;
        }
    }

    /// <summary>
    /// Gets most popular games based on number of account associations
    /// Useful for analytics and recommendations
    /// </summary>
    public async Task<IEnumerable<Game>> GetMostPopularAsync(int count = 10)
    {
        try
        {
            return await DbSet
                .Include(g => g.AccountGames)
                .OrderByDescending(g => g.AccountGames.Count)
                .Take(count)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting most popular games");
            throw;
        }
    }
}
