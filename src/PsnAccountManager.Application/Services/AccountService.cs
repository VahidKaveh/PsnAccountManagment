using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using PsnAccountManager.Application.Interfaces;
using PsnAccountManager.Domain.Entities;
using PsnAccountManager.Domain.Interfaces;
using PsnAccountManager.Shared.DTOs;
using PsnAccountManager.Shared.Enums;

namespace PsnAccountManager.Application.Services;

public class AccountService(
    IAccountRepository accountRepository,
    IGameRepository gameRepository,
    IAccountHistoryRepository historyRepository,
    IChangeTrackerService changeTracker) : IAccountService
{
    public async Task<Account?> GetAccountByIdAsync(int id)
    {
        return await accountRepository.GetAccountWithGamesAsync(id);
    }

    public async Task UpdateAccountAsync(Account updatedAccount, List<string> gameTitles, string updatedBy)
    {
        var originalAccount = await accountRepository.GetAccountWithGamesAsync(updatedAccount.Id);
        if (originalAccount == null) throw new KeyNotFoundException($"Account with ID {updatedAccount.Id} not found.");

        var snapshot = new Account
        {
            Id = originalAccount.Id,
            Title = originalAccount.Title,
            PricePs4 = originalAccount.PricePs4,
            PricePs5 = originalAccount.PricePs5,
            Region = originalAccount.Region,
            Capacity = originalAccount.Capacity,
            StockStatus = originalAccount.StockStatus,
            HasOriginalMail = originalAccount.HasOriginalMail,
            SellerInfo = originalAccount.SellerInfo,
            AdditionalInfo = originalAccount.AdditionalInfo,
            AccountGames = originalAccount.AccountGames
                .Select(ag => new AccountGame { Game = new Game { Title = ag.Game.Title } }).ToList()
        };

        originalAccount.Title = updatedAccount.Title;
        originalAccount.PricePs4 = updatedAccount.PricePs4;
        originalAccount.PricePs5 = updatedAccount.PricePs5;
        originalAccount.Region = updatedAccount.Region;
        originalAccount.Capacity = updatedAccount.Capacity;
        originalAccount.StockStatus = updatedAccount.StockStatus;
        originalAccount.HasOriginalMail = updatedAccount.HasOriginalMail;
        originalAccount.AdditionalInfo = updatedAccount.AdditionalInfo;
        originalAccount.SellerInfo = updatedAccount.SellerInfo;
        originalAccount.UpdatedAt = DateTime.UtcNow;

        var gameEntities = await GetOrCreateGamesAsync(gameTitles);
        originalAccount.AccountGames.Clear();
        foreach (var game in gameEntities) originalAccount.AccountGames.Add(new AccountGame { Game = game });

        var changes = changeTracker.GetChanges(snapshot, originalAccount, updatedBy);
        if (changes.Any())
            foreach (var change in changes)
                await historyRepository.AddAsync(change);

        await accountRepository.SaveChangesAsync();
    }

    public async Task<AccountDto> CreateAccountAsync(CreateAccountDto createDto)
    {
        var gameEntities = await GetOrCreateGamesAsync(createDto.GameTitles);

        var newAccount = new Account
        {
            ChannelId = createDto.ChannelId,
            ExternalId = createDto.ExternalId,
            Title = createDto.Title,
            Description = createDto.Description,
            PricePs4 = createDto.PricePs4,
            PricePs5 = createDto.PricePs5,
            Region = createDto.Region,
            Capacity = createDto.Capacity,
            StockStatus = StockStatus.InStock,
            HasOriginalMail = createDto.HasOriginalMail,
            SellerInfo = createDto.SellerInfo,
            AdditionalInfo = createDto.AdditionalInfo,
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow
        };

        foreach (var game in gameEntities) newAccount.AccountGames.Add(new AccountGame { Game = game });

        await accountRepository.AddAsync(newAccount);
        await accountRepository.SaveChangesAsync();

        return MapToAccountDto(newAccount);
    }

    public async Task<bool> DeleteAccountAsync(int id)
    {
        var account = await accountRepository.GetByIdAsync(id);
        if (account == null) return false;

        account.IsDeleted = true;
        account.StockStatus = StockStatus.OutOfStock; // Or another appropriate status
        accountRepository.Update(account);
        await accountRepository.SaveChangesAsync();
        return true;
    }

    private async Task<List<Game>> GetOrCreateGamesAsync(List<string> titles)
    {
        var gameEntities = new List<Game>();
        if (titles == null || !titles.Any()) return gameEntities;

        var distinctTitles =
            titles.Where(t => !string.IsNullOrWhiteSpace(t)).Distinct(StringComparer.OrdinalIgnoreCase);

        foreach (var title in distinctTitles)
        {
            var game = await gameRepository.FindByTitleAsync(title);
            game ??= new Game { Title = title };
            gameEntities.Add(game);
        }

        return gameEntities;
    }

    private AccountDto MapToAccountDto(Account account)
    {
        return new AccountDto
        {
            Id = account.Id,
            ChannelId = account.ChannelId,
            Title = account.Title,
            Description = account.Description,
            PricePs4 = account.PricePs4,
            PricePs5 = account.PricePs5,
            Region = account.Region,
            Capacity = account.Capacity.ToString(),
            StockStatus = account.StockStatus.ToString(),
            HasOriginalMail = account.HasOriginalMail,
            SellerInfo = account.SellerInfo,
            AdditionalInfo = account.AdditionalInfo,
            IsDeleted = account.IsDeleted,
            Games = account.AccountGames?.Select(ag => new GameDto
            {
                Id = ag.Game.Id,
                Title = ag.Game.Title,
                PosterUrl = ag.Game.PosterUrl
            }).ToList() ?? new List<GameDto>()
        };
    }
}