using Microsoft.Extensions.Logging;
using PsnAccountManager.Application.Interfaces;
using PsnAccountManager.Domain.Entities;
using PsnAccountManager.Domain.Interfaces;
using PsnAccountManager.Shared.DTOs;
using PsnAccountManager.Shared.Enums;

namespace PsnAccountManager.Application.Services;

/// <summary>
/// Implements the business logic for processing data scraped from external sources.
/// This service decides whether to insert, update, or ignore scraped account information.
/// </summary>
public class ScraperService : IScraperService
{
    private readonly IAccountRepository _accountRepository;
    private readonly IRawMessageRepository _rawMessageRepository;
    private readonly ILogger<ScraperService> _logger;

    public ScraperService(
        IAccountRepository accountRepository,
        IRawMessageRepository rawMessageRepository,
        ILogger<ScraperService> logger)
    {
        _accountRepository = accountRepository;
        _rawMessageRepository = rawMessageRepository;
        _logger = logger;
    }

    /// <summary>
    /// Processes a single piece of parsed data. This method's signature
    /// EXACTLY matches the one defined in IScraperService.
    /// </summary>
    public async Task ProcessScrapedDataAsync(int channelId, ParsedAccountDto? parsedData)
    {
        // Step 1: Validate input.
        if (parsedData == null || string.IsNullOrWhiteSpace(parsedData.ExternalId))
        {
            _logger.LogTrace("Received null or invalid parsedData. Skipping processing.");
            return;
        }

        // Step 2: Check for existing account.
        var existingAccount = await _accountRepository.GetByExternalIdAsync(channelId, parsedData.ExternalId);

        if (existingAccount != null)
        {
            // --- UPDATE or DELETE Logic ---
            _logger.LogDebug("Found existing account with ExternalId {ExternalId}. Processing update.",
                parsedData.ExternalId);

            existingAccount.Title = parsedData.Title;
            existingAccount.PricePs4 = parsedData.PricePs4;
            existingAccount.PricePs5 = parsedData.PricePs5;
            existingAccount.Region = parsedData.Region;
            existingAccount.LastScrapedAt = DateTime.UtcNow;

            if (parsedData.IsSold)
            {
                if (!existingAccount.IsDeleted)
                    _logger.LogInformation(
                        "Marking account as sold/deleted: ExternalId {ExternalId} in Channel {ChannelId}",
                        parsedData.ExternalId, channelId);

                existingAccount.IsDeleted = true;
                existingAccount.StockStatus = StockStatus.OutOfStock;
            }
            else
            {
                if (existingAccount.IsDeleted)
                    _logger.LogInformation(
                        "Re-listing a previously sold account: ExternalId {ExternalId} in Channel {ChannelId}",
                        parsedData.ExternalId, channelId);

                existingAccount.IsDeleted = false;
                existingAccount.StockStatus = StockStatus.InStock;
            }

            _accountRepository.Update(existingAccount);
        }
        else if (!parsedData.IsSold)
        {
            // --- INSERT Logic ---
            _logger.LogDebug("No existing account found. Creating new account for ExternalId {ExternalId}.",
                parsedData.ExternalId);

            var newAccount = new Account
            {
                ChannelId = channelId,
                ExternalId = parsedData.ExternalId,
                Title = parsedData.Title,
                PricePs4 = parsedData.PricePs4,
                PricePs5 = parsedData.PricePs5,
                Region = parsedData.Region,
                LastScrapedAt = DateTime.UtcNow,
                IsDeleted = false,
                StockStatus = StockStatus.InStock,
                Capacity = AccountCapacity.Z1
            };

            await _accountRepository.AddAsync(newAccount);
            _logger.LogInformation("New account created from scrape: ExternalId {ExternalId} in Channel {ChannelId}",
                parsedData.ExternalId, channelId);
        }
        else
        {
            _logger.LogDebug(
                "Skipping creation of a new account that is already marked as deleted: ExternalId {ExternalId}",
                parsedData.ExternalId);
        }
    }

    /// <summary>
    /// Handles accounts that were not scraped in the current run (removed from channel).
    /// Sets IsDeleted=true, StockStatus=OutOfStock, RawMessageId=null for the account,
    /// and updates the related RawMessage status to Deleted with AccountId=null.
    /// </summary>
    public async Task HandleRemovedAccountsAsync(int channelId, IEnumerable<string> scrapedExternalIds)
    {
        try
        {
            _logger.LogInformation("Checking for removed accounts in channel {ChannelId}", channelId);

            // Get all non-deleted accounts for this channel
            var allChannelAccounts = await _accountRepository.GetByChannelIdAsync(channelId);
            var activeAccounts = allChannelAccounts.Where(a => !a.IsDeleted).ToList();

            var scrapedIdSet = new HashSet<string>(scrapedExternalIds, StringComparer.OrdinalIgnoreCase);
            var removedAccounts = activeAccounts.Where(a => !scrapedIdSet.Contains(a.ExternalId)).ToList();

            if (!removedAccounts.Any())
            {
                _logger.LogInformation("No removed accounts found in channel {ChannelId}", channelId);
                return;
            }

            _logger.LogInformation("Found {Count} removed accounts in channel {ChannelId}", 
                removedAccounts.Count, channelId);

            foreach (var account in removedAccounts)
            {
                _logger.LogInformation(
                    "Marking account as removed: ID {AccountId}, ExternalId {ExternalId}",
                    account.Id, account.ExternalId);

                // Update account status
                account.IsDeleted = true;
                account.StockStatus = StockStatus.OutOfStock;
                var oldRawMessageId = account.RawMessageId;
                account.RawMessageId = null;
                account.UpdatedAt = DateTime.UtcNow;

                _accountRepository.Update(account);

                // Update the related RawMessage if it exists
                if (oldRawMessageId.HasValue)
                {
                    var rawMessage = await _rawMessageRepository.GetByIdAsync(oldRawMessageId.Value);
                    if (rawMessage != null)
                    {
                        rawMessage.Status = RawMessageStatus.Deleted;
                        rawMessage.AccountId = null;
                        rawMessage.UpdatedAt = DateTime.UtcNow;
                        rawMessage.UpdatedBy = "ScraperService";
                        rawMessage.ErrorMessage = "Account no longer available in channel";

                        _rawMessageRepository.Update(rawMessage);

                        _logger.LogInformation(
                            "Updated RawMessage {MessageId} status to Deleted for removed account {AccountId}",
                            rawMessage.Id, account.Id);
                    }
                }
            }

            // Save all changes
            await _accountRepository.SaveChangesAsync();
            await _rawMessageRepository.SaveChangesAsync();

            _logger.LogInformation(
                "Successfully processed {Count} removed accounts in channel {ChannelId}",
                removedAccounts.Count, channelId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling removed accounts for channel {ChannelId}", channelId);
            throw;
        }
    }
}
