using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PsnAccountManager.Domain.Entities;
using PsnAccountManager.Domain.Interfaces;
using PsnAccountManager.Infrastructure.Data;

namespace PsnAccountManager.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for LearningData entity
/// NEW: Supports machine learning training data management
/// </summary>
public class LearningDataRepository : GenericRepository<LearningData, int>, ILearningDataRepository
{
    private readonly ILogger<LearningDataRepository> _logger;

    public LearningDataRepository(
        PsnAccountManagerDbContext context,
        ILogger<LearningDataRepository> logger) : base(context)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    // ==================== BASIC QUERIES ====================

    public async Task<IEnumerable<LearningData>> GetByChannelIdAsync(int channelId)
    {
        try
        {
            return await DbSet
                .Include(ld => ld.Channel)
                .Where(ld => ld.ChannelId == channelId)
                .OrderByDescending(ld => ld.CreatedAt)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting learning data for channel {ChannelId}", channelId);
            throw;
        }
    }

    public async Task<IEnumerable<LearningData>> GetByEntityTypeAsync(string entityType)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(entityType))
            {
                _logger.LogWarning("Entity type is null or empty");
                return Enumerable.Empty<LearningData>();
            }

            return await DbSet
                .Include(ld => ld.Channel)
                .Where(ld => ld.EntityType == entityType)
                .OrderByDescending(ld => ld.CreatedAt)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting learning data for entity type {EntityType}", entityType);
            throw;
        }
    }

    public async Task<IEnumerable<LearningData>> GetByConfidenceLevelAsync(int minConfidence, int maxConfidence)
    {
        try
        {
            return await DbSet
                .Include(ld => ld.Channel)
                .Where(ld => ld.ConfidenceLevel >= minConfidence && ld.ConfidenceLevel <= maxConfidence)
                .OrderByDescending(ld => ld.ConfidenceLevel)
                .ThenByDescending(ld => ld.CreatedAt)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting learning data by confidence level {Min}-{Max}",
                minConfidence, maxConfidence);
            throw;
        }
    }

    // ==================== MANUAL CORRECTIONS ====================

    public async Task<IEnumerable<LearningData>> GetManualCorrectionsAsync()
    {
        try
        {
            return await DbSet
                .Include(ld => ld.Channel)
                .Where(ld => ld.IsManualCorrection)
                .OrderByDescending(ld => ld.CreatedAt)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting manual corrections");
            throw;
        }
    }

    public async Task<IEnumerable<LearningData>> GetManualCorrectionsByChannelAsync(int channelId)
    {
        try
        {
            return await DbSet
                .Include(ld => ld.Channel)
                .Where(ld => ld.ChannelId == channelId && ld.IsManualCorrection)
                .OrderByDescending(ld => ld.CreatedAt)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting manual corrections for channel {ChannelId}", channelId);
            throw;
        }
    }

    // ==================== TRAINING DATA ====================

    public async Task<IEnumerable<LearningData>> GetUnusedForTrainingAsync()
    {
        try
        {
            return await DbSet
                .Include(ld => ld.Channel)
                .Where(ld => !ld.IsUsedInTraining)
                .OrderByDescending(ld => ld.ConfidenceLevel)
                .ThenByDescending(ld => ld.CreatedAt)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting unused training data");
            throw;
        }
    }

    public async Task<IEnumerable<LearningData>> GetTrainingDataAsync(string entityType, int maxSamples = 1000)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(entityType))
            {
                _logger.LogWarning("Entity type is null or empty");
                return Enumerable.Empty<LearningData>();
            }

            var query = DbSet
                .Include(ld => ld.Channel)
                .Where(ld => ld.EntityType == entityType)
                .OrderByDescending(ld => ld.ConfidenceLevel)
                .ThenByDescending(ld => ld.CreatedAt);

            if (maxSamples > 0)
            {
                query = (IOrderedQueryable<LearningData>)query.Take(maxSamples);
            }

            return await query.ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting training data for entity type {EntityType}", entityType);
            throw;
        }
    }

    public async Task MarkAsUsedInTrainingAsync(IEnumerable<int> ids)
    {
        try
        {
            var idList = ids.ToList();
            if (!idList.Any())
            {
                _logger.LogWarning("No IDs provided to mark as used in training");
                return;
            }

            var learningDataList = await DbSet
                .Where(ld => idList.Contains(ld.Id))
                .ToListAsync();

            foreach (var learningData in learningDataList)
            {
                learningData.IsUsedInTraining = true;
            }

            await Context.SaveChangesAsync();

            _logger.LogInformation("Marked {Count} learning data entries as used in training", learningDataList.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking learning data as used in training");
            throw;
        }
    }

    // ==================== STATISTICS ====================

    public async Task<LearningDataStatistics> GetChannelStatisticsAsync(int channelId)
    {
        try
        {
            var channelData = await DbSet
                .Where(ld => ld.ChannelId == channelId)
                .ToListAsync();

            if (!channelData.Any())
            {
                return new LearningDataStatistics
                {
                    TotalCount = 0,
                    CountByEntityType = new Dictionary<string, int>(),
                    CountByConfidenceLevel = new Dictionary<int, int>()
                };
            }

            var statistics = new LearningDataStatistics
            {
                TotalCount = channelData.Count,
                ManualCorrections = channelData.Count(ld => ld.IsManualCorrection),
                AutomaticExtractions = channelData.Count(ld => !ld.IsManualCorrection),
                UsedInTraining = channelData.Count(ld => ld.IsUsedInTraining),
                UnusedInTraining = channelData.Count(ld => !ld.IsUsedInTraining),
                AverageConfidenceLevel = channelData.Average(ld => ld.ConfidenceLevel),
                OldestEntryDate = channelData.Min(ld => ld.CreatedAt),
                NewestEntryDate = channelData.Max(ld => ld.CreatedAt),
                CountByEntityType = channelData
                    .GroupBy(ld => ld.EntityType)
                    .ToDictionary(g => g.Key, g => g.Count()),
                CountByConfidenceLevel = channelData
                    .GroupBy(ld => ld.ConfidenceLevel)
                    .ToDictionary(g => g.Key, g => g.Count())
            };

            return statistics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting channel statistics for channel {ChannelId}", channelId);
            throw;
        }
    }

    public async Task<int> GetCountByChannelAsync(int channelId)
    {
        try
        {
            return await DbSet
                .CountAsync(ld => ld.ChannelId == channelId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting learning data count for channel {ChannelId}", channelId);
            throw;
        }
    }

    // ==================== UTILITY ====================

    public async Task<bool> ExistsForMessageAsync(int rawMessageId, string entityType)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(entityType))
            {
                return false;
            }

            return await DbSet
                .AnyAsync(ld => ld.RawMessageId == rawMessageId && ld.EntityType == entityType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if learning data exists for message {MessageId} and entity type {EntityType}",
                rawMessageId, entityType);
            throw;
        }
    }

    public async Task<IEnumerable<LearningData>> GetRecentAsync(int count = 100)
    {
        try
        {
            return await DbSet
                .Include(ld => ld.Channel)
                .OrderByDescending(ld => ld.CreatedAt)
                .Take(count)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting recent learning data");
            throw;
        }
    }
}
