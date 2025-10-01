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
public class ScraperService : IScraperService // This class implements the interface
{
    private readonly IAccountRepository _accountRepository;
    private readonly ILogger<ScraperService> _logger;

    public ScraperService(IAccountRepository accountRepository, ILogger<ScraperService> logger)
    {
        _accountRepository = accountRepository;
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
            _logger.LogDebug("Found existing account with ExternalId {ExternalId}. Processing update.", parsedData.ExternalId);

            existingAccount.Title = parsedData.Title;
            existingAccount.PricePs4 = parsedData.PricePs4;
            existingAccount.PricePs5 = parsedData.PricePs5;
            existingAccount.Region = parsedData.Region;
            existingAccount.LastScrapedAt = DateTime.UtcNow;

            if (parsedData.IsSold)
            {
                if (!existingAccount.IsDeleted)
                {
                    _logger.LogInformation("Marking account as sold/deleted: ExternalId {ExternalId} in Channel {ChannelId}", parsedData.ExternalId, channelId);
                }
                existingAccount.IsDeleted = true;
                existingAccount.StockStatus = StockStatus.OutOfStock;
            }
            else
            {
                if (existingAccount.IsDeleted)
                {
                    _logger.LogInformation("Re-listing a previously sold account: ExternalId {ExternalId} in Channel {ChannelId}", parsedData.ExternalId, channelId);
                }
                existingAccount.IsDeleted = false;
                existingAccount.StockStatus = StockStatus.InStock;
            }
            _accountRepository.Update(existingAccount);

        }
        else if (!parsedData.IsSold)
        {
            // --- INSERT Logic ---
            _logger.LogDebug("No existing account found. Creating new account for ExternalId {ExternalId}.", parsedData.ExternalId);
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
                Capacity = AccountCapacity.OfflineOnly,
            };
            await _accountRepository.AddAsync(newAccount);
            _logger.LogInformation("New account created from scrape: ExternalId {ExternalId} in Channel {ChannelId}", parsedData.ExternalId, channelId);
        }
        else
        {
            _logger.LogDebug("Skipping creation of a new account that is already marked as deleted: ExternalId {ExternalId}", parsedData.ExternalId);
        }
    }
}