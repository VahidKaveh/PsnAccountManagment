using PsnAccountManager.Domain.Entities;
using System.Linq.Expressions;

namespace PsnAccountManager.Domain.Interfaces;

/// <summary>
/// Repository interface for LearningData entity operations
/// NEW: Supports machine learning training data management
/// </summary>
public interface ILearningDataRepository : IGenericRepository<LearningData, int>
{
    // ==================== BASIC QUERIES ====================

    /// <summary>
    /// Gets all learning data for a specific channel
    /// </summary>
    Task<IEnumerable<LearningData>> GetByChannelIdAsync(int channelId);

    /// <summary>
    /// Gets learning data by entity type (game, price, region, etc.)
    /// </summary>
    Task<IEnumerable<LearningData>> GetByEntityTypeAsync(string entityType);

    /// <summary>
    /// Gets learning data within a confidence level range
    /// </summary>
    Task<IEnumerable<LearningData>> GetByConfidenceLevelAsync(int minConfidence, int maxConfidence);


    // ==================== MANUAL CORRECTIONS ====================

    /// <summary>
    /// Gets all manual corrections made by admins
    /// </summary>
    Task<IEnumerable<LearningData>> GetManualCorrectionsAsync();

    /// <summary>
    /// Gets manual corrections for a specific channel
    /// </summary>
    Task<IEnumerable<LearningData>> GetManualCorrectionsByChannelAsync(int channelId);


    // ==================== TRAINING DATA ====================

    /// <summary>
    /// Gets learning data that hasn't been used in training yet
    /// </summary>
    Task<IEnumerable<LearningData>> GetUnusedForTrainingAsync();

    /// <summary>
    /// Gets training data for a specific entity type with sample limit
    /// </summary>
    Task<IEnumerable<LearningData>> GetTrainingDataAsync(string entityType, int maxSamples = 1000);

    /// <summary>
    /// Marks learning data entries as used in training
    /// </summary>
    Task MarkAsUsedInTrainingAsync(IEnumerable<int> ids);


    // ==================== STATISTICS ====================

    /// <summary>
    /// Gets comprehensive statistics for a channel's learning data
    /// </summary>
    Task<LearningDataStatistics> GetChannelStatisticsAsync(int channelId);

    /// <summary>
    /// Gets learning data count for a channel
    /// </summary>
    Task<int> GetCountByChannelAsync(int channelId);


    // ==================== UTILITY ====================

    /// <summary>
    /// Checks if learning data exists for a message and entity type
    /// </summary>
    Task<bool> ExistsForMessageAsync(int rawMessageId, string entityType);

    /// <summary>
    /// Gets most recent learning data entries
    /// </summary>
    Task<IEnumerable<LearningData>> GetRecentAsync(int count = 100);
}

/// <summary>
/// Statistics model for learning data analysis
/// </summary>
public class LearningDataStatistics
{
    public int TotalCount { get; set; }
    public int ManualCorrections { get; set; }
    public int AutomaticExtractions { get; set; }
    public int UsedInTraining { get; set; }
    public int UnusedInTraining { get; set; }
    public Dictionary<string, int> CountByEntityType { get; set; } = new();
    public Dictionary<int, int> CountByConfidenceLevel { get; set; } = new();
    public double AverageConfidenceLevel { get; set; }
    public DateTime? OldestEntryDate { get; set; }
    public DateTime? NewestEntryDate { get; set; }
}
