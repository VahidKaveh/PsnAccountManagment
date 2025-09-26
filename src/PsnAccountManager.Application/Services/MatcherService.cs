using Microsoft.Extensions.Logging;
using PsnAccountManager.Shared.DTOs;
using PsnAccountManager.Domain.Entities;
using PsnAccountManager.Domain.Interfaces;
using PsnAccountManager.Application.Interfaces;
namespace PsnAccountManager.Application.Services;

public class MatcherService : IMatcherService
{
    private readonly IAccountRepository _accountRepository;
    private readonly ISettingRepository _settingRepository;
    private readonly ILogger<MatcherService> _logger;

    public MatcherService(
        IAccountRepository accountRepository,
        ISettingRepository settingRepository,
        ILogger<MatcherService> logger)
    {
        _accountRepository = accountRepository;
        _settingRepository = settingRepository;
        _logger = logger;
    }

    public async Task<MatchResultDto> FindMatchesAsync(MatchRequestDto request)
    {
        _logger.LogInformation("Matcher service started for a request with {GameCount} games.", request.RequestedGameIds.Count);

        // --- Step 1: Read Configuration from Settings ---
        var minMatchedGames = await _settingRepository.GetValueAsync("Matcher.MinMatchedGames", 1);
        var maxSuggestions = await _settingRepository.GetValueAsync("Matcher.MaxSuggestions", 5);
        var sortOrder = await _settingRepository.GetValueAsync("Matcher.SuggestionSortOrder", SuggestionSortOrder.ByMatchedGames);

        var requestedGameIds = new HashSet<int>(request.RequestedGameIds);
        var result = new MatchResultDto { RequestedGamesCount = requestedGameIds.Count };

        if (!requestedGameIds.Any()) return result;

        // --- Step 2: Fetch All Necessary Data in a Single, Efficient Query ---
        var availableAccounts = await _accountRepository.GetActiveAccountsWithGamesAsync();
        if (!availableAccounts.Any())
        {
            _logger.LogWarning("No active accounts found in the database to match against.");
            return result;
        }

        // --- Step 3: Find the Primary Match (the account with the most matched games) ---
        Account? bestMatchAccount = null;
        HashSet<int> bestMatchGameIds = new();

        foreach (var account in availableAccounts)
        {
            var accountGameIds = account.AccountGames.Select(ag => ag.GameId).ToHashSet();
            var matchedIds = new HashSet<int>(requestedGameIds.Intersect(accountGameIds));

            if (matchedIds.Count > bestMatchGameIds.Count)
            {
                bestMatchGameIds = matchedIds;
                bestMatchAccount = account;
            }
        }

        // --- Step 4: Validate Primary Match and Add to Results ---
        if (bestMatchAccount == null || bestMatchGameIds.Count < minMatchedGames)
        {
            _logger.LogInformation("No primary match found that meets the minimum requirement of {MinGames} games.", minMatchedGames);
            return result;
        }

        result.Suggestions.Add(MapToMatchedAccountDto(bestMatchAccount, bestMatchGameIds, isPrimary: true));
        _logger.LogDebug("Primary match found: AccountId {AccountId} with {MatchCount} games.", bestMatchAccount.Id, bestMatchGameIds.Count);

        // --- Step 5: Determine Remaining Games and Prepare for Secondary Search ---
        var remainingGameIds = requestedGameIds.Except(bestMatchGameIds).ToHashSet();
        if (!remainingGameIds.Any() || result.Suggestions.Count >= maxSuggestions)
        {
            _logger.LogInformation("All games found or max suggestions reached. Finalizing results.");
            return result;
        }

        var otherAccounts = availableAccounts.Where(a => a.Id != bestMatchAccount.Id).ToList();

        // --- Step 6: Find and Rank Secondary Matches ---
        var secondaryMatches = new List<MatchedAccountDto>();
        foreach (var account in otherAccounts)
        {
            var accountGameIds = account.AccountGames.Select(ag => ag.GameId).ToHashSet();
            var matchedIds = new HashSet<int>(remainingGameIds.Intersect(accountGameIds));

            if (matchedIds.Any())
            {
                secondaryMatches.Add(MapToMatchedAccountDto(account, matchedIds, isPrimary: false));
            }
        }

        // --- Step 7: Sort Secondary Matches Based on Settings ---
        IOrderedEnumerable<MatchedAccountDto> sortedSecondaryMatches;
        switch (sortOrder)
        {
            case SuggestionSortOrder.ByPrice:
                sortedSecondaryMatches = secondaryMatches.OrderBy(a => a.Price);
                _logger.LogDebug("Sorting secondary matches by Price.");
                break;
            case SuggestionSortOrder.ByMatchedGames:
            default:
                sortedSecondaryMatches = secondaryMatches.OrderByDescending(a => a.MatchedGamesCount);
                _logger.LogDebug("Sorting secondary matches by MatchedGames count.");
                break;
        }

        // --- Step 8: Add Secondary Suggestions to Final Result ---
        result.Suggestions.AddRange(sortedSecondaryMatches.Take(maxSuggestions - result.Suggestions.Count));

        _logger.LogInformation("Matcher service finished. Found {TotalSuggestions} suggestions.", result.TotalSuggestions);
        return result;
    }

    // --- Private Helper Method for Mapping ---
    private MatchedAccountDto MapToMatchedAccountDto(Account account, ICollection<int> matchedGameIds, bool isPrimary)
    {
        decimal displayPrice = account.PricePs5 ?? account.PricePs4 ?? 0;
        return new MatchedAccountDto
        {
            AccountId = account.Id,
            Title = account.Title,
            Price = displayPrice,
            MatchedGamesCount = matchedGameIds.Count,
            IsPrimarySuggestion = isPrimary,
            MatchedGames = account.AccountGames
                .Where(ag => matchedGameIds.Contains(ag.GameId))
                .Select(ag => new GameDto
                {
                    Id = ag.Game.Id,
                    Title = ag.Game.Title,
                    PosterUrl = ag.Game.PosterUrl
                }).ToList()
        };
    }
}