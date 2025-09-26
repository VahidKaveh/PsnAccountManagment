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

public class ScraperWorker : BackgroundService
{
    private readonly ILogger<ScraperWorker> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _configuration;
    private readonly ITelegramClient _telegramClient;
    private readonly IWorkerStateService _workerState;

    public ScraperWorker(
        ILogger<ScraperWorker> logger,
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration,
        ITelegramClient telegramClient,
        IWorkerStateService workerState)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
        _configuration = configuration;
        _telegramClient = telegramClient;
        _workerState = workerState;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ScraperWorker is starting up.");
        _workerState.UpdateStatus(WorkerActivity.Initializing, "Worker is starting up...");

        try
        {
            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

            _workerState.UpdateStatus(WorkerActivity.ConnectingToTelegram, "Attempting to log into Telegram...");
            await _telegramClient.LoginAsync();
            _workerState.UpdateStatus(WorkerActivity.Idle, "Successfully logged in. Waiting for the first cycle.");
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Telegram login failed. ScraperWorker cannot start.");
            _workerState.UpdateStatus(WorkerActivity.Error, $"Telegram login failed: {ex.Message}");
            return;
        }

        var interval = TimeSpan.FromMinutes(_configuration.GetValue<int>("ScraperWorkerSettings:ScrapeIntervalMinutes", 15));

        while (!stoppingToken.IsCancellationRequested)
        {
            var stopwatch = Stopwatch.StartNew();
            int totalNewMessagesInCycle = 0;

            try
            {
                if (_workerState.IsEnabled())
                {
                    _logger.LogInformation("ScraperWorker cycle starting.");
                    _workerState.UpdateStatus(WorkerActivity.Scraping, "Scraping cycle has started.");

                    totalNewMessagesInCycle = await ScrapeAndStoreMessagesAsync(stoppingToken);

                    stopwatch.Stop();
                    _workerState.ReportCycleCompletion(stopwatch.Elapsed, totalNewMessagesInCycle);
                    _workerState.UpdateStatus(WorkerActivity.CycleFinished, $"Cycle finished. Found {totalNewMessagesInCycle} new messages.");
                    _logger.LogInformation("ScraperWorker cycle finished in {Duration}. Found {Count} new messages.", stopwatch.Elapsed, totalNewMessagesInCycle);
                }
                else
                {
                    // Update status only if it's not already stopped to avoid redundant messages
                    if (_workerState.GetStatus().CurrentActivity != WorkerActivity.Stopped)
                    {
                        _workerState.UpdateStatus(WorkerActivity.Stopped, "Worker is stopped by admin. Skipping cycle.");
                        _logger.LogInformation("ScraperWorker is stopped. Skipping cycle.");
                    }
                }
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "An unhandled exception occurred during the scraping cycle.");
                _workerState.UpdateStatus(WorkerActivity.Error, $"An error occurred: {ex.Message}");
            }

            _workerState.UpdateStatus(WorkerActivity.Idle, $"Waiting for the next cycle in {interval.TotalMinutes} minutes.");
            await Task.Delay(interval, stoppingToken);
        }
    }

    private async Task<int> ScrapeAndStoreMessagesAsync(CancellationToken stoppingToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var channelRepository = scope.ServiceProvider.GetRequiredService<IChannelRepository>();
        var rawMessageRepository = scope.ServiceProvider.GetRequiredService<IRawMessageRepository>();

        var channelsToScrape = (await channelRepository.GetActiveChannelsAsync()).ToList();
        int totalNewMessagesInCycle = 0;

        for (int i = 0; i < channelsToScrape.Count; i++)
        {
            var channel = channelsToScrape[i];
            if (stoppingToken.IsCancellationRequested) break;

            _workerState.UpdateStatus(WorkerActivity.Scraping, $"Scraping channel {i + 1}/{channelsToScrape.Count}: {channel.Name}");

            try
            {
                IEnumerable<TL.Message> messages;
                int lastKnownMessageId = channel.LastScrapedMessageId ?? 0;

                switch (channel.FetchMode)
                {
                    case FetchMode.LastXMessages:
                        int limit = channel.FetchValue ?? 100;
                        _logger.LogInformation("Channel '{Name}' (Mode: LastXMessages): Fetching last {Value} messages.", channel.Name, limit);
                        messages = await _telegramClient.GetMessagesAsync(
                            channelUsername: channel.Name,
                            limit: limit
                        );
                        break;

                    case FetchMode.SinceXHoursAgo:
                        int hours = channel.FetchValue ?? 24;
                        var dateLimit = DateTime.UtcNow.AddHours(-hours);
                        _logger.LogInformation("Channel '{Name}' (Mode: SinceXHoursAgo): Fetching messages since {DateLimit} ({Hours} hours ago).", channel.Name, dateLimit, hours);

                        // Telegram API doesn't have a direct "get since date". A common strategy is to fetch recent messages and filter.
                        // Fetching a larger batch (e.g., 200) increases the chance of finding all recent messages.
                        var recentMessages = await _telegramClient.GetMessagesAsync(channel.Name, limit: 200);
                        messages = recentMessages.Where(m => m.date.ToUniversalTime() >= dateLimit);
                        break;

                    case FetchMode.SinceLastMessage:
                    default:
                        _logger.LogInformation("Channel '{Name}' (Mode: SinceLastMessage): Fetching messages after ID {LastId}.", channel.Name, lastKnownMessageId);
                        messages = await _telegramClient.GetMessagesAsync(
                            channelUsername: channel.Name,
                            minMessageId: lastKnownMessageId,
                            limit: 100
                        );
                        break;
                }

                var messageList = messages.ToList();
                if (!messageList.Any())
                {
                    _logger.LogInformation("No new messages found for channel '{ChannelName}' with the current fetch strategy.", channel.Name);
                    channel.LastScrapedAt = DateTime.UtcNow; // Update scraped time even if no messages found
                    channelRepository.Update(channel);
                    await channelRepository.SaveChangesAsync();
                    continue;
                }

                int newMessagesFromThisChannel = 0;
                foreach (var message in messageList)
                {
                    if (string.IsNullOrWhiteSpace(message.message)) continue;

                    // IMPORTANT: Check if we have already stored this message to prevent duplicates.
                    var existingRawMessage = await rawMessageRepository.GetByExternalIdAsync(channel.Id, message.id);
                    if (existingRawMessage != null)
                    {
                        // Optionally, you could update the existing message text here if needed.
                        continue;
                    }

                    var newRawMessage = new RawMessage
                    {
                        ChannelId = channel.Id,
                        ExternalMessageId = message.id,
                        MessageText = message.message,
                        ReceivedAt = message.date.ToUniversalTime(),
                        Status = RawMessageStatus.Pending
                    };
                    await rawMessageRepository.AddAsync(newRawMessage);
                    newMessagesFromThisChannel++;
                }

                if (newMessagesFromThisChannel > 0)
                {
                    channel.LastScrapedMessageId = messageList.Max(m => m.id);
                    channel.LastScrapedAt = DateTime.UtcNow;
                    channelRepository.Update(channel);

                    await rawMessageRepository.SaveChangesAsync();
                    _logger.LogInformation("Successfully stored {Count} new messages from {ChannelName}.", newMessagesFromThisChannel, channel.Name);
                    totalNewMessagesInCycle += newMessagesFromThisChannel;
                }

                if (channel.DelayAfterScrapeMs > 0)
                {
                    _logger.LogDebug("Applying custom delay of {Delay}ms for channel {ChannelName}.", channel.DelayAfterScrapeMs, channel.Name);
                    await Task.Delay(channel.DelayAfterScrapeMs, stoppingToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to scrape channel {ChannelName}. Continuing to the next channel.", channel.Name);
                _workerState.UpdateStatus(WorkerActivity.Error, $"Error scraping channel {channel.Name}. Check logs.");
            }
        }
        return totalNewMessagesInCycle;
    }
}