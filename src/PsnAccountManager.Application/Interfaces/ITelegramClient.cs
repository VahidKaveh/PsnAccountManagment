using System.Collections.Generic;
using System.Threading.Tasks;
using PsnAccountManager.Shared.DTOs;
using PsnAccountManager.Shared.Enums;
using TL;

namespace PsnAccountManager.Application.Interfaces;

public interface ITelegramClient
{
    Task LoginUserIfNeededAsync();


    Task<List<TelegramMessageDto>> FetchMessagesAsync(
        string channelIdentifier,
        TelegramFetchMode mode,
        int parameter);
}