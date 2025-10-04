using System.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PsnAccountManager.Application.Interfaces;
using PsnAccountManager.Domain.Entities;
using PsnAccountManager.Domain.Interfaces;
using PsnAccountManager.Shared.Enums;

namespace PsnAccountManager.Infrastructure.BackgroundWorkers;

/// <summary>
/// Background worker that periodically scrapes Telegram channels for new messages
/// Enhanced with account removal detection and better error handling
/// </summary>
public class ScraperWorker : BackgroundService
{
    private readonly ILogger<ScraperWorker> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _configuration;
    private readonly ITelegramClient _telegramClient;
    private readonly IWorkerStateService _workerState;

    // Configuration constants
    private const int DEFAULT_SCRAPE_INTERVAL_MINUTES = 15;
    private const int DEFAULT_DELAY_BETWEEN_CHANNELS_MS = 2000;
    private const int MAX_RETRY_ATTEMPTS = 3;
    private const int RETRY_DELAY_SECONDS = 5;

    // Account removal constants
    private const int DEFAULT_ACCOUNT_REMOVAL_CHECK_INTERVAL_HOURS = 6;
    private const int DEFAULT_STALE_ACCOUNT_THRESHOLD_DAYS = 7;

    public ScraperWorker(
        ILogger<ScraperWorker> logger,
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration,
        ITelegramClient telegramClient,
        IWorkerStateService workerState)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _telegramClient = telegramClient ?? throw new ArgumentNullException(nameof(telegramClient));
        _workerState = workerState ?? throw new ArgumentNullException(nameof(workerState));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ScraperWorker starting at: {Time}", DateTimeOffset.Now);

        _workerState.UpdateStatus(WorkerActivity.Initializing, "Worker is starting up...");

        // Wait for application to fully start
        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

        // Authenticate with Telegram
        try
        {
            _workerState.UpdateStatus(WorkerActivity.Authenticating, "Attempting to log into Telegram...");
            _logger.LogInformation("Authenticating with Telegram...");

            await _telegramClient.LoginUserIfNeededAsync();

            _logger.LogInformation("Successfully authenticated with Telegram");
            _workerState.UpdateStatus(WorkerActivity.Idle, "Successfully logged in, ready to scrape...");
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Failed to authenticate with Telegram. Worker cannot continue.");
            _workerState.UpdateStatus(WorkerActivity.Error, $"Failed to authenticate: {ex.Message}");
            return;
        }

        // Main scraping loop
        while (!stoppingToken.IsCancellationRequested)
        {
            // Always check IsEnabled() at the start of each cycle
            if (!_workerState.IsEnabled())
            {
                _workerState.UpdateStatus(WorkerActivity.Stopped, "Worker is disabled by admin.");

                while (!_workerState.IsEnabled() && !stoppingToken.IsCancellationRequested)
                {
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                }

                // If re-enabled, update status and continue
                if (_workerState.IsEnabled())
                {
                    _workerState.UpdateStatus(WorkerActivity.Idle, "Worker re-enabled, ready to scrape...");
                }
                continue;
            }

            var stopwatch = Stopwatch.StartNew();
            var totalMessages = 0;

            try
            {
                _workerState.UpdateStatus(WorkerActivity.Scraping, "Starting scraping cycle...");
                _logger.LogInformation("Starting scraping cycle at {Time}", DateTimeOffset.Now);

                totalMessages = await ScrapeAllChannelsAsync(stoppingToken);

                stopwatch.Stop();
                _workerState.ReportCycleCompletion(stopwatch.Elapsed, totalMessages);

                _workerState.UpdateStatus(WorkerActivity.WaitingForNextCycle,
                    $"Cycle completed. Found {totalMessages} messages in {stopwatch.Elapsed.TotalSeconds:F1}s. Waiting for next cycle...");

                _logger.LogInformation(
                    "Scraping cycle completed successfully in {ElapsedMs}ms. Found {MessageCount} messages. Waiting {Minutes} minutes until next cycle.",
                    stopwatch.ElapsedMilliseconds, totalMessages, GetScrapeIntervalMinutes());
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Critical error in scraping cycle");

                _workerState.ReportCycleCompletion(stopwatch.Elapsed, totalMessages);
                _workerState.UpdateStatus(WorkerActivity.Error, $"Error in scraping cycle: {ex.Message}");
            }

            // Wait for configured interval
            var intervalMinutes = GetScrapeIntervalMinutes();
            await Task.Delay(TimeSpan.FromMinutes(intervalMinutes), stoppingToken);
        }

        _workerState.UpdateStatus(WorkerActivity.Stopped, "Worker has been stopped.");
        _logger.LogInformation("ScraperWorker stopping at: {Time}", DateTimeOffset.Now);
    }

    /// <summary>
    /// Scrapes all active channels and returns total messages found
    /// </summary>
    private async Task<int> ScrapeAllChannelsAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var channelRepo = scope.ServiceProvider.GetRequiredService<IChannelRepository>();
        var rawMessageRepo = scope.ServiceProvider.GetRequiredService<IRawMessageRepository>();

        try
        {
            // Get all active channels
            var channels = await channelRepo.GetActiveChannelsAsync();
            var channelList = channels.ToList();

            if (!channelList.Any())
            {
                _logger.LogWarning("No active channels found to scrape");
                _workerState.UpdateStatus(WorkerActivity.Idle, "No active channels to scrape");
                return 0;
            }

            _logger.LogInformation("Starting to scrape {Count} active channels", channelList.Count);
            _workerState.UpdateStatus(WorkerActivity.Scraping, $"Scraping {channelList.Count} channels...");

            var successCount = 0;
            var failCount = 0;
            var totalMessages = 0;

            foreach (var channel in channelList)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                try
                {
                    _workerState.UpdateStatus(WorkerActivity.Scraping, $"Scraping channel: {channel.Name}");

                    var messages = await ScrapeChannelWithRetryAsync(
                        channel,
                        rawMessageRepo,
                        MAX_RETRY_ATTEMPTS,
                        cancellationToken);

                    if (messages > 0)
                    {
                        successCount++;
                        totalMessages += messages;

                        _logger.LogInformation(
                            "Successfully scraped {MessageCount} new messages from channel: {ChannelName}",
                            messages, channel.Name);
                    }
                    else
                    {
                        successCount++;
                        _logger.LogDebug("No new messages in channel: {ChannelName}", channel.Name);
                    }
                }
                catch (Exception ex)
                {
                    failCount++;
                    _logger.LogError(ex,
                        "Failed to scrape channel: {ChannelName} after {MaxRetries} retries",
                        channel.Name, MAX_RETRY_ATTEMPTS);
                }

                // Update progress
                var progress = $"Scraped {successCount + failCount}/{channelList.Count} channels. Found {totalMessages} messages so far.";
                _workerState.UpdateStatus(WorkerActivity.Scraping, progress);

                // Delay between channels to avoid rate limiting
                if (channel != channelList.Last())
                    await Task.Delay(GetDelayBetweenChannels(), cancellationToken);
            }

            _logger.LogInformation(
                "Scraping summary: {Total} channels, {Success} successful, {Failed} failed, {TotalMessages} total messages",
                channelList.Count, successCount, failCount, totalMessages);

            return totalMessages;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in ScrapeAllChannelsAsync");
            throw;
        }
    }

    private async Task<int> ScrapeChannelWithRetryAsync(
        Channel channel,
        IRawMessageRepository rawMessageRepo,
        int maxRetries,
        CancellationToken cancellationToken)
    {
        var attempt = 0;
        Exception? lastException = null;

        while (attempt < maxRetries)
        {
            attempt++;

            try
            {
                return await ScrapeChannelAsync(channel, rawMessageRepo, cancellationToken);
            }
            catch (Exception ex)
            {
                lastException = ex;

                if (attempt < maxRetries)
                {
                    _logger.LogWarning(ex,
                        "Attempt {Attempt}/{MaxRetries} failed for channel {ChannelName}. Retrying in {Seconds}s...",
                        attempt, maxRetries, channel.Name, RETRY_DELAY_SECONDS);

                    await Task.Delay(TimeSpan.FromSeconds(RETRY_DELAY_SECONDS * attempt), cancellationToken);
                }
            }
        }

        throw lastException ?? new Exception($"Failed to scrape channel {channel.Name} after {maxRetries} attempts");
    }

    /// <summary>
    /// Scrapes a single channel with account removal detection
    /// </summary>
    private async Task<int> ScrapeChannelAsync(
        Channel channel,
        IRawMessageRepository rawMessageRepo,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Scraping channel: {ChannelName} (External ID: {ExternalId})",
            channel.Name, channel.ExternalId);

        // Determine fetch mode and parameters
        var fetchMode = GetFetchMode(channel);
        var fetchParameter = GetFetchParameter(channel, fetchMode);

        // Fetch messages from Telegram
        var messages = await _telegramClient.FetchMessagesAsync(
            channel.ExternalId,
            fetchMode,
            fetchParameter);

        // Collect external IDs for removal detection
        var currentExternalIds = new HashSet<string>();
        var newMessageCount = 0;

        if (messages.Any())
        {
            _logger.LogDebug("Retrieved {Count} messages from Telegram for channel: {ChannelName}",
                messages.Count, channel.Name);

            // Process messages and collect external IDs
            foreach (var message in messages)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                // Add to current external IDs set (convert long to string)
                currentExternalIds.Add(ConvertToExternalId(message.ExternalMessageId));

                // Check if message already exists
                var existingMessage = await rawMessageRepo.GetByExternalIdAsync(
                    channel.Id,
                    ConvertToExternalId(message.ExternalMessageId));

                if (existingMessage != null)
                {
                    _logger.LogTrace("Message {ExternalId} already exists, skipping",
                        message.ExternalMessageId);
                    continue;
                }

                // Store new message
                var rawMessage = new RawMessage
                {
                    ChannelId = channel.Id,
                    ExternalMessageId = message.ExternalMessageId, // long type is correct here
                    MessageText = message.MessageText,
                    ReceivedAt = message.ReceivedAt,
                    Status = RawMessageStatus.Pending,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = "ScraperWorker"
                };

                await rawMessageRepo.AddAsync(rawMessage);
                newMessageCount++;
            }

            // Save all new messages
            if (newMessageCount > 0)
            {
                await rawMessageRepo.SaveChangesAsync();
            }
        }
        else
        {
            _logger.LogDebug("No messages retrieved from channel: {ChannelName}", channel.Name);
        }

        // Handle account removal detection
        await HandleAccountRemovalAsync(channel, currentExternalIds, rawMessageRepo);

        // Update channel's last scraped info
        channel.LastScrapedAt = DateTime.UtcNow;
        if (messages.Any())
        {
            // Convert max ExternalMessageId to string for storage
            var maxMessageId = messages.Max(m => m.ExternalMessageId);
            channel.LastScrapedMessageId = maxMessageId.ToString();
        }

        await rawMessageRepo.SaveChangesAsync();

        return newMessageCount;
    }

    /// <summary>
    /// Handles detection and marking of accounts that may have been removed from the channel
    /// </summary>
    private async Task HandleAccountRemovalAsync(
        Channel channel,
        IEnumerable<string> currentExternalIds,
        IRawMessageRepository rawMessageRepo)
    {
        try
        {
            // Check if we should perform account removal check for this channel
            if (!ShouldPerformAccountRemovalCheck(channel))
            {
                _logger.LogDebug("Skipping account removal check for channel: {ChannelName}", channel.Name);
                return;
            }

            using var scope = _scopeFactory.CreateScope();
            var accountRepo = scope.ServiceProvider.GetRequiredService<IAccountRepository>();

            // Strategy 1: Mark accounts not found in current scrape
            var externalIdsList = currentExternalIds.ToList();
            if (externalIdsList.Any())
            {
                await accountRepo.MarkAccountsAsRemovedAsync(channel.Id, externalIdsList);
                _logger.LogDebug("Performed account removal check for channel: {ChannelName} with {Count} current IDs",
                    channel.Name, externalIdsList.Count);
            }

            // Strategy 2: Mark very old accounts as stale
            var staleThresholdDays = GetStaleAccountThresholdDays();
            var staleThreshold = DateTime.UtcNow.AddDays(-staleThresholdDays);

            var staleAccounts = await accountRepo.GetStaleAccountsForChannelAsync(channel.Id, staleThreshold);
            var staleAccountsList = staleAccounts.ToList();

            if (staleAccountsList.Any())
            {
                foreach (var staleAccount in staleAccountsList)
                {
                    staleAccount.IsDeleted = true;
                    staleAccount.StockStatus = StockStatus.OutOfStock;
                    staleAccount.LastScrapedAt = DateTime.UtcNow;
                    staleAccount.Notes = $"Auto-removed: Stale account (last seen {staleAccount.LastScrapedAt:yyyy-MM-dd})";
                    accountRepo.Update(staleAccount);
                }

                await accountRepo.SaveChangesAsync();
                _logger.LogInformation("Marked {Count} stale accounts as removed for channel: {ChannelName}",
                    staleAccountsList.Count, channel.Name);
            }

            // Strategy 3: Create admin notification if significant removals
            var totalRemoved = staleAccountsList.Count;
            if (totalRemoved > 5) // Threshold for notification
            {
                await CreateAccountRemovalNotificationAsync(channel, totalRemoved, scope);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling account removal for channel: {ChannelName}", channel.Name);
            // Don't throw - this shouldn't break the main scraping process
        }
    }

    /// <summary>
    /// Determines if we should perform account removal check based on scraping strategy and timing
    /// </summary>
    private bool ShouldPerformAccountRemovalCheck(Channel channel)
    {
        try
        {
            // Check if auto removal is enabled
            var autoRemovalEnabled = _configuration.GetSection("AccountRemovalSettings:EnableAutoRemoval").Value == "true";
            if (!autoRemovalEnabled)
            {
                return false;
            }

            // Only perform removal check for comprehensive scrapes
            var fetchMode = GetFetchMode(channel);
            var isComprehensiveScrape = fetchMode == TelegramFetchMode.LastXMessages ||
                                       fetchMode == TelegramFetchMode.SinceXHoursAgo;

            if (!isComprehensiveScrape)
            {
                return false;
            }

            // Check if enough time has passed since last removal check
            var checkIntervalHours = GetAccountRemovalCheckInterval();
            var lastCheckThreshold = DateTime.UtcNow.AddHours(-checkIntervalHours);

            return channel.LastScrapedAt == null || channel.LastScrapedAt < lastCheckThreshold;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error determining if account removal check should be performed for channel: {ChannelName}",
                channel.Name);
            return false; // Safe default
        }
    }

    /// <summary>
    /// Creates admin notification for significant account removals
    /// </summary>
    private async Task CreateAccountRemovalNotificationAsync(Channel channel, int removedCount, IServiceScope scope)
    {
        try
        {
            var notificationRepo = scope.ServiceProvider.GetRequiredService<IAdminNotificationRepository>();

            var notification = new AdminNotification
            {
                Type = AdminNotificationType.AccountChanged,
                Title = $"Bulk Account Removal in {channel.Name}",
                Message = $"Automatically removed {removedCount} accounts from channel '{channel.Name}' " +
                         $"due to absence in recent scraping or staleness. " +
                         $"This may indicate channel cleanup or significant changes.",
                Priority = removedCount > 20 ? NotificationPriority.High : NotificationPriority.Normal,
                RelatedEntityId = channel.Id,
                RelatedEntityType = "Channel",
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            await notificationRepo.AddAsync(notification);
            await notificationRepo.SaveChangesAsync();

            _logger.LogInformation("Created admin notification for bulk account removal in channel: {ChannelName}",
                channel.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating account removal notification for channel: {ChannelName}",
                channel.Name);
            // Don't throw - notification failure shouldn't break main process
        }
    }

    /// <summary>
    /// Converts long to string safely for external ID comparison
    /// </summary>
    private string ConvertToExternalId(long messageId)
    {
        return messageId.ToString();
    }

    /// <summary>
    /// Gets the interval for account removal checks from configuration
    /// </summary>
    private int GetAccountRemovalCheckInterval()
    {
        try
        {
            var value = _configuration["AccountRemovalSettings:CheckIntervalHours"];
            return int.TryParse(value, out var result) ? result : DEFAULT_ACCOUNT_REMOVAL_CHECK_INTERVAL_HOURS;
        }
        catch
        {
            return DEFAULT_ACCOUNT_REMOVAL_CHECK_INTERVAL_HOURS;
        }
    }

    /// <summary>
    /// Gets the stale account threshold from configuration
    /// </summary>
    private int GetStaleAccountThresholdDays()
    {
        try
        {
            var value = _configuration["AccountRemovalSettings:StaleAccountThresholdDays"];
            return int.TryParse(value, out var result) ? result : DEFAULT_STALE_ACCOUNT_THRESHOLD_DAYS;
        }
        catch
        {
            return DEFAULT_STALE_ACCOUNT_THRESHOLD_DAYS;
        }
    }

    private TelegramFetchMode GetFetchMode(Channel channel)
    {
        if (channel.LastScrapedAt == null) return TelegramFetchMode.LastXMessages;
        if (channel.LastScrapedAt.Value > DateTime.UtcNow.AddHours(-1)) return TelegramFetchMode.SinceLastMessage;
        return TelegramFetchMode.SinceXHoursAgo;
    }

    private int GetFetchParameter(Channel channel, TelegramFetchMode mode)
    {
        return mode switch
        {
            TelegramFetchMode.LastXMessages => 50,
            TelegramFetchMode.SinceXHoursAgo => 24,
            // **FIX: Safe conversion from string to int**
            TelegramFetchMode.SinceLastMessage => !string.IsNullOrEmpty(channel.LastScrapedMessageId) &&
                                                  long.TryParse(channel.LastScrapedMessageId, out var messageId)
                                                  ? (int)messageId
                                                  : 0,
            _ => 50
        };
    }

    private int GetScrapeIntervalMinutes()
    {
        try
        {
            var value = _configuration["ScraperWorker:ScrapeIntervalMinutes"];
            return int.TryParse(value, out var result) ? result : DEFAULT_SCRAPE_INTERVAL_MINUTES;
        }
        catch
        {
            return DEFAULT_SCRAPE_INTERVAL_MINUTES;
        }
    }

    private int GetDelayBetweenChannels()
    {
        try
        {
            var value = _configuration["ScraperWorker:DelayBetweenChannelsMs"];
            return int.TryParse(value, out var result) ? result : DEFAULT_DELAY_BETWEEN_CHANNELS_MS;
        }
        catch
        {
            return DEFAULT_DELAY_BETWEEN_CHANNELS_MS;
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("ScraperWorker is stopping");
        _workerState.UpdateStatus(WorkerActivity.Stopped, "Worker is shutting down...");
        await base.StopAsync(cancellationToken);
    }
}
