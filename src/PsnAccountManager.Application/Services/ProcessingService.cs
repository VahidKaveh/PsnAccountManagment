using Microsoft.Extensions.Logging;
using PsnAccountManager.Application.Interfaces;
using PsnAccountManager.Domain.Entities;
using PsnAccountManager.Domain.Interfaces;
using PsnAccountManager.Shared.DTOs;
using PsnAccountManager.Shared.Enums;
using PsnAccountManager.Shared.ViewModels;
using System.Text.RegularExpressions;

namespace PsnAccountManager.Application.Services;

public class ProcessingService : IProcessingService
{
    private readonly IRawMessageRepository _rawMessageRepo;
    private readonly IMessageParser _messageParser;
    private readonly IChannelRepository _channelRepo;
    private readonly IAccountRepository _accountRepository;
    private readonly IGameRepository _gameRepository;
    private readonly ILogger<ProcessingService> _logger;
    private readonly IAdminNotificationRepository _notificationRepository;


    public ProcessingService(
        IRawMessageRepository rawMessageRepo,
        IMessageParser messageParser,
        IChannelRepository channelRepo,
        IAccountRepository accountRepository,
        IGameRepository gameRepository,
        ILogger<ProcessingService> logger)
    {
        _rawMessageRepo = rawMessageRepo;
        _messageParser = messageParser;
        _channelRepo = channelRepo;
        _accountRepository = accountRepository;
        _gameRepository = gameRepository;
        _logger = logger;
    }

    public async Task<ParsedAccountDto?> GetParsedDataPreviewAsync(int rawMessageId)
    {
        var rawMessage = await _rawMessageRepo.GetByIdAsync(rawMessageId);
        if (rawMessage == null) return null;

        var channel = await _channelRepo.GetChannelWithProfileAsync(rawMessage.ChannelId);
        if (channel?.ParsingProfile?.Rules == null || !channel.ParsingProfile.Rules.Any()) return null;

        var parsedDto = _messageParser.Parse(rawMessage.MessageText, rawMessage.ExternalMessageId.ToString(),
            channel.ParsingProfile.Rules);
        if (parsedDto == null) return null;

        // --- New, robust logic to extract the games block ---
        string textToProcess = rawMessage.MessageText;
        var rules = channel.ParsingProfile.Rules;

        // 1. Find the end of the games block first
        var endPattern = rules.FirstOrDefault(r => r.FieldType == ParsedFieldType.GamesBlockEnd)?.RegexPattern;
        if (!string.IsNullOrEmpty(endPattern))
        {
            var endMatch = Regex.Match(textToProcess, endPattern, RegexOptions.Multiline);
            if (endMatch.Success)
            {
                textToProcess = textToProcess.Substring(0, endMatch.Index);
            }
        }

        // 2. Find the start of the games block
        var startPattern = rules.FirstOrDefault(r => r.FieldType == ParsedFieldType.GamesBlockStart)?.RegexPattern;
        if (!string.IsNullOrEmpty(startPattern))
        {
            var startMatch = Regex.Match(textToProcess, startPattern, RegexOptions.Multiline);
            if (startMatch.Success)
            {
                textToProcess = textToProcess.Substring(startMatch.Index + startMatch.Length);
            }
        }

        var gameTitlesFromBlock = textToProcess
            .Split(new[] { '\r', '\n' }, System.StringSplitOptions.RemoveEmptyEntries)
            .Select(line => CleanGameTitle(line.Trim()))
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .ToList();

        _logger.LogInformation("Found {Count} game titles after processing the block.", gameTitlesFromBlock.Count);

        var finalGamesList = new List<ParsedGameViewModel>();
        foreach (var title in gameTitlesFromBlock)
        {
            var existingGame = await _gameRepository.FindByTitleAsync(title);
            finalGamesList.Add(new ParsedGameViewModel { Title = title, ExistsInDb = existingGame != null });
        }

        parsedDto.Games = finalGamesList.OrderBy(g => g.ExistsInDb).ToList();

        if (finalGamesList.Any())
        {
            parsedDto.Title = $"{channel.Name} #{rawMessage.ExternalMessageId} ({finalGamesList.Count} Games)";
        }
        else
        {
            parsedDto.Title = $"Account from {channel.Name} #{rawMessage.ExternalMessageId}";
        }

        return parsedDto;
    }

    public async Task<ProcessingResult> ProcessAndSaveAccountAsync(ProcessMessageViewModel viewModel)
    {
        var rawMessage = await _rawMessageRepo.GetByIdAsync(viewModel.RawMessageId);
        if (rawMessage == null)
        {
            _logger.LogError("Cannot save account. RawMessage with ID {MessageId} not found.", viewModel.RawMessageId);
            return new ProcessingResult { Success = false };
        }

        var channel = await _channelRepo.GetChannelWithProfileAsync(rawMessage.ChannelId);
        if (channel?.ParsingProfile == null)
        {
            _logger.LogError("Cannot save account. No valid parsing profile for Channel ID {ChannelId}.",
                rawMessage.ChannelId);
            return new ProcessingResult { Success = false };
        }

        // --- Step 1: Re-parse the raw text to get all details, not just what's in the ViewModel ---
        var parsedData = _messageParser.Parse(rawMessage.MessageText, rawMessage.ExternalMessageId.ToString(),
            channel.ParsingProfile.Rules);
        if (parsedData == null) return new ProcessingResult { Success = false };

        var existingAccount = await _accountRepository.GetByExternalIdAsync(rawMessage.ChannelId, rawMessage.ExternalMessageId.ToString());

        ProcessingResult result;
        if (existingAccount != null)
        {
            result = await UpdateExistingAccount(existingAccount, viewModel, parsedData, rawMessage);
        }
        else
        {
            result = await CreateNewAccount(viewModel, parsedData, rawMessage, channel);
        }

        if (result.Success)
        {
            rawMessage.Status = RawMessageStatus.Processed;
            _rawMessageRepo.Update(rawMessage);
            await _rawMessageRepo.SaveChangesAsync();
        }

        return result;
    }

    private AccountCapacity MapCapacityInfoToEnum(string? capacityInfo)
    {
        if (string.IsNullOrWhiteSpace(capacityInfo))
        {
            return AccountCapacity.Unknown;
        }

        var lowerCaseInfo = capacityInfo.ToLower();

        if (lowerCaseInfo.Contains("z1") || lowerCaseInfo.Contains("offline"))
        {
            return AccountCapacity.OfflineOnly;
        }

        if (lowerCaseInfo.Contains("z2") || lowerCaseInfo.Contains("capacity 2"))
        {
            return AccountCapacity.Capacity2;
        }

        if (lowerCaseInfo.Contains("z3") || lowerCaseInfo.Contains("capacity 3") || lowerCaseInfo.Contains("full"))
        {
            return AccountCapacity.Capacity3; // Capacity 3 is often considered Full Access
        }

        // Add more rules here as you discover new formats

        return AccountCapacity.Unknown; // Default fallback
    }

    private string CleanGameTitle(string title)
    {
        var platformSuffixRegex = new Regex(@"\s+(PS[45]|P[45])(\s*&\s*(PS[45]|P[45]))?$", RegexOptions.IgnoreCase);
        return platformSuffixRegex.Replace(title, "").Trim();
    }

    private async Task<ProcessingResult> CreateNewAccount(ProcessMessageViewModel viewModel, ParsedAccountDto parsedData,
        RawMessage rawMessage, Channel channel)
    {
        _logger.LogInformation("Creating new account from RawMessage ID: {RawMessageId}", rawMessage.Id);
        var gameEntities = await GetOrCreateGames(viewModel.GameTitles);

        var newAccount = new Account
        {
            ChannelId = rawMessage.ChannelId,
            ExternalId = rawMessage.ExternalMessageId.ToString(),
            Title = viewModel.Title,
            Description = rawMessage.MessageText,
            PricePs4 = viewModel.PricePs4,
            PricePs5 = viewModel.PricePs5,
            Region = viewModel.Region,
            AdditionalInfo = parsedData.AdditionalInfo,
            HasOriginalMail = parsedData.HasOriginalMail,
            GuaranteeMinutes = parsedData.GuaranteeMinutes,
            SellerInfo = parsedData.SellerInfo,
            Capacity = MapCapacityInfoToEnum(parsedData.CapacityInfo),
            LastScrapedAt = rawMessage.ReceivedAt,
            StockStatus = parsedData.IsSold ? StockStatus.OutOfStock : StockStatus.InStock,
            IsDeleted = false
        };

        foreach (var game in gameEntities)
        {
            newAccount.AccountGames.Add(new AccountGame { GameId = game.Id });
        }

        await _accountRepository.AddAsync(newAccount);
        // SaveChanges will be called by the parent method

        return new ProcessingResult
        {
            Success = true,
            IsNewAccount = true,
            AccountId = newAccount.Id,
            AccountTitle = newAccount.Title
        };
    }

    private async Task<ProcessingResult> UpdateExistingAccount(Account existingAccount, ProcessMessageViewModel viewModel,
        ParsedAccountDto parsedData, RawMessage rawMessage)
    {
        _logger.LogInformation("Updating existing Account ID: {AccountId}", existingAccount.Id);
        var changes = new List<ChangeInfo>();

        // Check for changes and update properties
        CheckForChange(changes, existingAccount, "PricePs4", viewModel.PricePs4);
        CheckForChange(changes, existingAccount, "PricePs5", viewModel.PricePs5);
        CheckForChange(changes, existingAccount, "Title", viewModel.Title);
        CheckForChange(changes, existingAccount, "Region", viewModel.Region);
        var newStockStatus = parsedData.IsSold ? StockStatus.OutOfStock : StockStatus.InStock;
        CheckForChange(changes, existingAccount, "StockStatus", newStockStatus);

        if (changes.Any())
        {
            existingAccount.RecentChanges = string.Join(", ", changes.Select(c => c.FieldName));
            foreach (var change in changes)
            {
                existingAccount.History.Add(new AccountHistory
                {
                    FieldName = change.FieldName,
                    OldValue = change.OldValue,
                    NewValue = change.NewValue,
                    ChangedBy = "Scraper"
                });
            }
        }

        // Sync games list
        var submittedGameEntities = await GetOrCreateGames(viewModel.GameTitles);
        var existingGameIds = existingAccount.AccountGames.Select(ag => ag.GameId).ToHashSet();
        var submittedGameIds = submittedGameEntities.Select(g => g.Id).ToHashSet();

        // Remove games that are no longer in the list
        var gamesToRemove = existingAccount.AccountGames.Where(ag => !submittedGameIds.Contains(ag.GameId)).ToList();
        foreach (var gameToRemove in gamesToRemove) existingAccount.AccountGames.Remove(gameToRemove);

        // Add new games
        var gameIdsToAdd = submittedGameIds.Where(id => !existingGameIds.Contains(id));
        foreach (var gameId in gameIdsToAdd) existingAccount.AccountGames.Add(new AccountGame { GameId = gameId });

        _accountRepository.Update(existingAccount);
        return new ProcessingResult
        {
            Success = true,
            IsNewAccount = false,
            AccountId = existingAccount.Id,
            AccountTitle = existingAccount.Title,
            DetectedChanges = changes
        };
    }

    private async Task<List<Game>> GetOrCreateGames(IEnumerable<string> titles)
    {
        var gameEntities = new List<Game>();
        var distinctTitles = titles.Where(t => !string.IsNullOrWhiteSpace(t)).Select(t => t.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase);

        foreach (var title in distinctTitles)
        {
            var game = await _gameRepository.FindByTitleAsync(title);
            if (game == null)
            {
                game = new Game { Title = title };
                await _gameRepository.AddAsync(game);
                await _gameRepository.SaveChangesAsync(); // Save immediately to get an ID
            }

            gameEntities.Add(game);
        }

        return gameEntities;
    }

    private void CheckForChange<T>(List<ChangeInfo> changes, Account account, string fieldName, T newValue)
    {
        var prop = typeof(Account).GetProperty(fieldName);
        var oldValue = (T)prop.GetValue(account);

        if (!EqualityComparer<T>.Default.Equals(oldValue, newValue))
        {
            changes.Add(new ChangeInfo
            {
                FieldName = fieldName,
                OldValue = oldValue?.ToString(),
                NewValue = newValue?.ToString()
            });
            prop.SetValue(account, newValue);
        }
    }
}
