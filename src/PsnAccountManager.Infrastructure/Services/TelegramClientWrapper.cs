using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PsnAccountManager.Application.Interfaces;
using PsnAccountManager.Shared.DTOs;
using PsnAccountManager.Shared.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TL;
using WTelegram;

namespace PsnAccountManager.Infrastructure.Services;

/// <summary>
/// Wrapper for WTelegram.Client
/// Handles Telegram authentication and message fetching
/// Enhanced with better error handling and session management
/// </summary>
public class TelegramClientWrapper : ITelegramClient, IDisposable
{
    private readonly Client _client;
    private readonly IConfiguration _config;
    private readonly ILogger<TelegramClientWrapper> _logger;
    private User? _user;
    private bool _isAuthenticated;

    public TelegramClientWrapper(
        IConfiguration config,
        ILogger<TelegramClientWrapper> logger)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _client = new Client(Config);
        _isAuthenticated = false;
    }

    /// <summary>
    /// Configuration callback for WTelegram
    /// </summary>
    private string? Config(string key)
    {
        return key switch
        {
            "api_id" => _config["TelegramSettings:ApiId"],
            "api_hash" => _config["TelegramSettings:ApiHash"],
            "phone_number" => _config["TelegramSettings:PhoneNumber"],
            "session_pathname" => _config["TelegramSettings:SessionPath"] ?? "telegram.session",
            "verification_code" => GetVerificationCode(),
            "password" => _config["TelegramSettings:Password"], // 2FA password if needed
            _ => null
        };
    }

    /// <summary>
    /// Gets verification code from configuration or throws exception
    /// For production, verification code should be pre-configured or handled via admin panel
    /// </summary>
    private string? GetVerificationCode()
    {
        var code = _config["TelegramSettings:VerificationCode"];

        if (string.IsNullOrEmpty(code))
        {
            _logger.LogWarning(
                "Verification code not found in configuration. " +
                "Please set TelegramSettings:VerificationCode in appsettings.json or environment variables.");

            // In production, throw exception instead of Console.ReadLine()
            throw new InvalidOperationException(
                "Telegram verification code is required but not configured. " +
                "Please add 'TelegramSettings:VerificationCode' to your configuration.");
        }

        return code;
    }

    /// <summary>
    /// Authenticates with Telegram if not already authenticated
    /// </summary>
    public async Task LoginUserIfNeededAsync()
    {
        if (_isAuthenticated)
        {
            _logger.LogDebug("Already authenticated with Telegram");
            return;
        }

        try
        {
            _logger.LogInformation("Authenticating with Telegram...");

            _user = await _client.LoginUserIfNeeded();
            _isAuthenticated = true;

            _logger.LogInformation("Successfully authenticated as {Username}", _user.MainUsername ?? "Unknown");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to authenticate with Telegram");
            _isAuthenticated = false;
            throw new InvalidOperationException("Telegram authentication failed. Check your credentials.", ex);
        }
    }

    /// <summary>
    /// Fetches messages from a Telegram channel
    /// </summary>
    public async Task<List<TelegramMessageDto>> FetchMessagesAsync(
        string channelIdentifier,
        TelegramFetchMode mode,
        int parameter)
    {
        if (!_isAuthenticated)
        {
            throw new InvalidOperationException("Not authenticated with Telegram. Call LoginUserIfNeededAsync() first.");
        }

        try
        {
            _logger.LogDebug(
                "Fetching messages from channel {Channel} with mode {Mode} and parameter {Parameter}",
                channelIdentifier, mode, parameter);

            // Resolve channel
            var resolvedChannel = await _client.Contacts_ResolveUsername(channelIdentifier);
            if (resolvedChannel?.Channel == null)
            {
                _logger.LogWarning("Could not resolve channel: {Channel}", channelIdentifier);
                return new List<TelegramMessageDto>();
            }

            var channel = resolvedChannel.Channel;
            var inputPeer = new InputPeerChannel(channel.ID, channel.access_hash);

            // Fetch messages based on mode
            var messages = mode switch
            {
                TelegramFetchMode.LastXMessages => await FetchLastXMessagesAsync(inputPeer, parameter),
                TelegramFetchMode.SinceXHoursAgo => await FetchSinceXHoursAgoAsync(inputPeer, parameter),
                TelegramFetchMode.SinceLastMessage => await FetchSinceLastMessageAsync(inputPeer, parameter),
                _ => new List<TelegramMessageDto>()
            };

            _logger.LogInformation(
                "Successfully fetched {Count} messages from channel {Channel}",
                messages.Count, channelIdentifier);

            return messages;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error fetching messages from channel {Channel} with mode {Mode}",
                channelIdentifier, mode);

            // Check if it's a rate limit error
            if (ex.Message.Contains("FLOOD_WAIT"))
            {
                _logger.LogWarning("Rate limit hit for channel {Channel}. Consider increasing delays.",
                    channelIdentifier);
            }

            throw;
        }
    }

    /// <summary>
    /// Fetches the last X messages from a channel
    /// </summary>
    private async Task<List<TelegramMessageDto>> FetchLastXMessagesAsync(
        InputPeerChannel inputPeer,
        int count)
    {
        var history = await _client.Messages_GetHistory(inputPeer, limit: count);
        return ConvertMessagesToDto(history.Messages);
    }

    /// <summary>
    /// Fetches messages from the last X hours
    /// </summary>
    private async Task<List<TelegramMessageDto>> FetchSinceXHoursAgoAsync(
        InputPeerChannel inputPeer,
        int hours)
    {
        var cutoffDate = DateTime.UtcNow.AddHours(-hours);
        var allMessages = new List<TelegramMessageDto>();
        int offsetId = 0;
        const int batchSize = 100;

        // Keep fetching until we reach the cutoff date
        while (true)
        {
            var history = await _client.Messages_GetHistory(
                inputPeer,
                offset_id: offsetId,
                limit: batchSize);

            if (!history.Messages.Any())
                break;

            var batch = ConvertMessagesToDto(history.Messages);
            var recentMessages = batch.Where(m => m.ReceivedAt >= cutoffDate).ToList();

            allMessages.AddRange(recentMessages);

            // If we found messages older than cutoff, stop
            if (recentMessages.Count < batch.Count)
                break;

            // Update offset for next batch
            offsetId = history.Messages.Last().ID;

            // Safety limit: max 1000 messages
            if (allMessages.Count >= 1000)
            {
                _logger.LogWarning("Reached safety limit of 1000 messages for SinceXHoursAgo mode");
                break;
            }
        }

        return allMessages;
    }

    /// <summary>
    /// Fetches messages since a specific message ID
    /// </summary>
    private async Task<List<TelegramMessageDto>> FetchSinceLastMessageAsync(
        InputPeerChannel inputPeer,
        int lastMessageId)
    {
        var allMessages = new List<TelegramMessageDto>();
        int offsetId = 0;
        const int batchSize = 100;

        // Keep fetching until we reach the last known message
        while (true)
        {
            var history = await _client.Messages_GetHistory(
                inputPeer,
                offset_id: offsetId,
                limit: batchSize);

            if (!history.Messages.Any())
                break;

            var batch = ConvertMessagesToDto(history.Messages);
            var newMessages = batch.Where(m => m.ExternalMessageId > lastMessageId).ToList();

            allMessages.AddRange(newMessages);

            // If we found the last known message, stop
            if (newMessages.Count < batch.Count)
                break;

            offsetId = history.Messages.Last().ID;

            // Safety limit
            if (allMessages.Count >= 1000)
            {
                _logger.LogWarning("Reached safety limit of 1000 messages for SinceLastMessage mode");
                break;
            }
        }

        return allMessages;
    }

    /// <summary>
    /// Converts Telegram Message objects to DTOs
    /// </summary>
    private List<TelegramMessageDto> ConvertMessagesToDto(MessageBase[] messages)
    {
        var result = new List<TelegramMessageDto>();

        foreach (var messageBase in messages)
        {
            if (messageBase is not Message message)
                continue;

            // Skip empty messages
            if (string.IsNullOrWhiteSpace(message.message))
                continue;

            result.Add(new TelegramMessageDto
            {
                ExternalMessageId = message.ID,
                MessageText = message.message,
                ReceivedAt = message.Date
            });
        }

        return result;
    }

    /// <summary>
    /// Checks if the client is currently authenticated
    /// </summary>
    public bool IsAuthenticated => _isAuthenticated;

    /// <summary>
    /// Gets the current authenticated user
    /// </summary>
    public User? CurrentUser => _user;

    /// <summary>
    /// Disposes the Telegram client
    /// </summary>
    public void Dispose()
    {
        _logger.LogInformation("Disposing TelegramClientWrapper");
        _client?.Dispose();
        GC.SuppressFinalize(this);
    }
}
