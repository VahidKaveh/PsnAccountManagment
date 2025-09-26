using System.Collections.Generic;
using System.Threading.Tasks;
using TL;

namespace PsnAccountManager.Application.Interfaces;

public interface ITelegramClient
{
    Task LoginAsync();

    /// <summary>
    /// Fetches a batch of messages from a specific channel using flexible parameters.
    /// </summary>
    /// <param name="channelUsername">The username of the channel.</param>
    /// <param name="minMessageId">Fetch messages with IDs greater than this.</param>
    /// <param name="limit">The maximum number of messages to fetch.</param>
    /// <param name="offsetDate">Fetch messages published before this date.</param>
    /// <returns>A collection of Message objects.</returns>
    Task<IEnumerable<Message>> GetMessagesAsync(string channelUsername, int minMessageId = 0, int limit = 100, DateTime? offsetDate = null);
}