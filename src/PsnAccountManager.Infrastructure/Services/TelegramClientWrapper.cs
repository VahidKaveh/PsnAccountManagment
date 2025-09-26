using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PsnAccountManager.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TL;
using WTelegram;

namespace PsnAccountManager.Infrastructure.Services;

public class TelegramClientWrapper : ITelegramClient, IDisposable
{
    private readonly Client _client;
    private readonly IConfiguration _config;
    private readonly ILogger<TelegramClientWrapper> _logger;
    private User? _user;

    public TelegramClientWrapper(IConfiguration config, ILogger<TelegramClientWrapper> logger)
    {
        _config = config;
        _logger = logger;
        _client = new Client(Config);
    }

    private string? Config(string key)
    {
        return key switch
        {
            "api_id" => _config["TelegramSettings:ApiId"],
            "api_hash" => _config["TelegramSettings:ApiHash"],
            "phone_number" => _config["TelegramSettings:PhoneNumber"],
            "session_pathname" => _config["TelegramSettings:SessionPath"] ?? "telegram.session",
            "verification_code" => AskForCode("Enter verification code: "),
            "password" => AskForCode("Enter 2FA password: "),
            _ => null
        };
    }

    private string AskForCode(string prompt)
    {
        Console.Write(prompt);
        return Console.ReadLine() ?? string.Empty;
    }

    public async Task LoginAsync()
    {
        _logger.LogInformation("Attempting to log into Telegram...");
        _user = await _client.LoginUserIfNeeded();
        if (_user == null)
        {
            throw new Exception("Telegram login failed. User object is null.");
        }
        _logger.LogInformation("Logged in as: {FirstName} {LastName} (@{Username})", _user.first_name, _user.last_name, _user.username);
    }

    public async Task<IEnumerable<Message>> GetMessagesAsync(string channelUsername, int minMessageId = 0, int limit = 100, DateTime? offsetDate = null)
    {
        try
        {
            _logger.LogDebug("Resolving username: {Username}", channelUsername);
            var resolvedPeer = await _client.Contacts_ResolveUsername(channelUsername);
            if (resolvedPeer?.peer is null)
            {
                throw new InvalidOperationException($"The username '{channelUsername}' could not be resolved.");
            }

            if (!resolvedPeer.chats.TryGetValue(resolvedPeer.peer.ID, out var chat) || chat is not Channel channel)
            {
                throw new InvalidOperationException($"The username '{channelUsername}' resolved to a peer that is not a channel.");
            }

            var inputPeerChannel = new InputPeerChannel(channel.id, channel.access_hash);

            _logger.LogDebug("Fetching history for channel {ChannelId} with min_id {MinId}, limit {Limit}, offset_date {OffsetDate}",
                inputPeerChannel.channel_id, minMessageId, limit, offsetDate);

            var messagesBase = await _client.Messages_GetHistory(
                peer: inputPeerChannel,
                offset_id: 0,
                offset_date: offsetDate ?? default,
                add_offset: 0,
                limit: limit,
                max_id: 0,
                min_id: minMessageId,
                hash: 0);

            return messagesBase.Messages.OfType<Message>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get messages for channel {ChannelUsername}", channelUsername);
            return Enumerable.Empty<Message>();
        }
    }

    public void Dispose()
    {
        _client?.Dispose();
    }
}