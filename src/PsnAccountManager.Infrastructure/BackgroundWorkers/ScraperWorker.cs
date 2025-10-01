using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PsnAccountManager.Application.Interfaces;
using PsnAccountManager.Domain.Entities;
using PsnAccountManager.Domain.Interfaces;
using PsnAccountManager.Shared.Enums;
using System.Diagnostics;

namespace PsnAccountManager.Infrastructure.BackgroundWorkers;

/// <summary>
/// Background worker that periodically scrapes Telegram channels for new messages
/// Enhanced with retry logic, better error handling, and metrics
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
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Failed to authenticate with Telegram. Worker cannot continue.");
            _workerState.UpdateStatus(WorkerActivity.Idle, "Successfully logged in...");
            return;
        }

        // Main scraping loop
        while (!stoppingToken.IsCancellationRequested)
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                _workerState.UpdateStatus(WorkerActivity.Scraping, "Scraping cycle has started.");

                await ScrapeAllChannelsAsync(stoppingToken);

                _workerState.UpdateStatus(WorkerActivity.Idle,"Worker id in Idle......");
                stopwatch.Stop();

                _logger.LogInformation(
                    "Scraping cycle completed in {ElapsedMs}ms. Waiting {Minutes} minutes until next cycle.",
                    stopwatch.ElapsedMilliseconds,
                    GetScrapeIntervalMinutes());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Critical error in scraping cycle");
                _workerState.UpdateStatus(WorkerActivity.Error, $"Error scraping cycle {ex.Message.ToString()}. Check logs.");
            }

            // Wait for configured interval
            var intervalMinutes = GetScrapeIntervalMinutes();
            await Task.Delay(TimeSpan.FromMinutes(intervalMinutes), stoppingToken);
        }

        _logger.LogInformation("ScraperWorker stopping at: {Time}", DateTimeOffset.Now);
    }

    /// <summary>
    /// Scrapes all active channels
    /// </summary>
    private async Task ScrapeAllChannelsAsync(CancellationToken cancellationToken)
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
                return;
            }

            _logger.LogInformation("Starting to scrape {Count} active channels", channelList.Count);

            int successCount = 0;
            int failCount = 0;
            int totalMessages = 0;

            foreach (var channel in channelList)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                try
                {
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

                // Delay between channels to avoid rate limiting
                if (channel != channelList.Last())
                {
                    await Task.Delay(GetDelayBetweenChannels(), cancellationToken);
                }
            }

            _logger.LogInformation(
                "Scraping summary: {Total} channels, {Success} successful, {Failed} failed, {TotalMessages} total messages",
                channelList.Count, successCount, failCount, totalMessages);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in ScrapeAllChannelsAsync");
            throw;
        }
    }

    /// <summary>
    /// Scrapes a single channel with retry logic
    /// </summary>
    private async Task<int> ScrapeChannelWithRetryAsync(
        Channel channel,
        IRawMessageRepository rawMessageRepo,
        int maxRetries,
        CancellationToken cancellationToken)
    {
        int attempt = 0;
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

        // All retries failed
        throw lastException ?? new Exception($"Failed to scrape channel {channel.Name} after {maxRetries} attempts");
    }

    /// <summary>
    /// Scrapes a single channel
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

        if (!messages.Any())
        {
            return 0;
        }

        _logger.LogDebug("Retrieved {Count} messages from Telegram for channel: {ChannelName}",
            messages.Count, channel.Name);

        // Filter and store new messages
        int newMessageCount = 0;

        foreach (var message in messages)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            // Check if message already exists
            var existingMessage = await rawMessageRepo.GetByExternalIdAsync(
                channel.Id,
                message.ExternalMessageId);

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
                ExternalMessageId = message.ExternalMessageId,
                MessageText = message.MessageText,
                ReceivedAt = message.ReceivedAt,
                Status = RawMessageStatus.Pending,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "ScraperWorker"
            };

            await rawMessageRepo.AddAsync(rawMessage);
            newMessageCount++;
        }

        // Save all new messages in one transaction
        if (newMessageCount > 0)
        {
            await rawMessageRepo.SaveChangesAsync();

            // Update channel's last scraped info
            channel.LastScrapedAt = DateTime.UtcNow;
            if (messages.Any())
            {
                channel.LastScrapedMessageId = messages.Max(m => m.ExternalMessageId);
            }

            await rawMessageRepo.SaveChangesAsync();
        }

        return newMessageCount;
    }

    /// <summary>
    /// Determines the fetch mode for a channel
    /// </summary>
    private TelegramFetchMode GetFetchMode(Channel channel)
    {
        // If channel has never been scraped, get recent messages
        if (channel.LastScrapedAt == null)
        {
            return TelegramFetchMode.LastXMessages;
        }

        // If last scrape was recent (within 1 hour), fetch since last message
        if (channel.LastScrapedAt.Value > DateTime.UtcNow.AddHours(-1))
        {
            return TelegramFetchMode.SinceLastMessage;
        }

        // Otherwise, fetch recent messages
        return TelegramFetchMode.SinceXHoursAgo;
    }

    /// <summary>
    /// Gets the fetch parameter based on mode
    /// </summary>
    private int GetFetchParameter(Channel channel, TelegramFetchMode mode)
    {
        return mode switch
        {
            TelegramFetchMode.LastXMessages => 50, // Get last 50 messages for new channels
            TelegramFetchMode.SinceXHoursAgo => 24, // Get messages from last 24 hours
            TelegramFetchMode.SinceLastMessage => (int)(channel.LastScrapedMessageId ?? 0),
            _ => 50
        };
    }

    /// <summary>
    /// Gets scrape interval from configuration
    /// </summary>
    private int GetScrapeIntervalMinutes()
    {
        return _configuration.GetValue<int>(
            "ScraperWorker.ScrapeIntervalMinutes",
            DEFAULT_SCRAPE_INTERVAL_MINUTES);
    }

    /// <summary>
    /// Gets delay between channels from configuration
    /// </summary>
    private int GetDelayBetweenChannels()
    {
        return _configuration.GetValue<int>(
            "ScraperWorker.DelayBetweenChannelsMs",
            DEFAULT_DELAY_BETWEEN_CHANNELS_MS);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("ScraperWorker is stopping");
        _workerState.UpdateStatus(WorkerActivity.Idle, "Worker is stop...");
        await base.StopAsync(cancellationToken);
    }
}
