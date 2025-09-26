using Microsoft.Extensions.Logging;
using PsnAccountManager.Shared.DTOs;
using PsnAccountManager.Domain.Entities;
using PsnAccountManager.Domain.Interfaces;
using PsnAccountManager.Shared.Enums;
using PsnAccountManager.Application.Interfaces;

namespace PsnAccountManager.Application.Services;

public class AccountService : IAccountService
{
    private readonly IAccountRepository _accountRepository;
    private readonly IGameRepository _gameRepository; // Dependency for validating Game IDs
    private readonly ILogger<AccountService> _logger;

    public AccountService(
        IAccountRepository accountRepository,
        IGameRepository gameRepository,
        ILogger<AccountService> logger)
    {
        _accountRepository = accountRepository;
        _gameRepository = gameRepository;
        _logger = logger;
    }

    public async Task<AccountDto?> GetAccountWithGamesAsync(int id)
    {
        var account = await _accountRepository.GetAccountWithGamesAsync(id); // Assumes this method exists in repo
        if (account == null)
        {
            _logger.LogWarning("Account with ID {AccountId} not found.", id);
            return null;
        }
        return MapToAccountDto(account);
    }

    public async Task<IEnumerable<AccountDto>> GetAvailableAccountsAsync()
    {
        var accounts = await _accountRepository.GetActiveAccountsWithGamesAsync(); // Assumes this method exists
        return accounts.Select(MapToAccountDto);
    }

    public async Task<AccountDto> CreateAccountAsync(CreateAccountDto createDto)
    {
        // --- Business Logic: Check for duplicates ---
        var existing = await _accountRepository.GetByExternalIdAsync(createDto.ChannelId, createDto.ExternalId);
        if (existing != null)
        {
            throw new InvalidOperationException($"Account with ExternalId '{createDto.ExternalId}' already exists in this channel.");
        }

        // --- Business Logic: Validate Game IDs ---
        var validGames = (await _gameRepository.FindAsync(g => createDto.GameIds.Contains(g.Id))).ToList();
        if (validGames.Count != createDto.GameIds.Count)
        {
            throw new KeyNotFoundException("One or more provided Game IDs are invalid.");
        }

        var newAccount = new Account
        {
            ChannelId = createDto.ChannelId,
            Title = createDto.Title,
            PricePs4 = createDto.PricePs4,
            PricePs5 = createDto.PricePs5,
            Region = createDto.Region,
            Capacity = createDto.Capacity,
            ExternalId = createDto.ExternalId,
            StockStatus = StockStatus.InStock,
            IsDeleted = false,
            LastScrapedAt = DateTime.UtcNow // Or set as needed
        };

        // Add relationships to the join table
        foreach (var game in validGames)
        {
            newAccount.AccountGames.Add(new AccountGame { GameId = game.Id });
        }

        await _accountRepository.AddAsync(newAccount);
        await _accountRepository.SaveChangesAsync();

        _logger.LogInformation("New account created with ID {AccountId}", newAccount.Id);

        return MapToAccountDto(newAccount);
    }

    public async Task<bool> UpdateAccountAsync(int id, UpdateAccountDto updateDto)
    {
        var existingAccount = await _accountRepository.GetAccountWithGamesAsync(id); // Fetch with games
        if (existingAccount == null)
        {
            _logger.LogWarning("Update failed: Account with ID {AccountId} not found.", id);
            return false;
        }

        // --- Business Logic: Validate new Game IDs ---
        var validGames = (await _gameRepository.FindAsync(g => updateDto.GameIds.Contains(g.Id))).ToList();
        if (validGames.Count != updateDto.GameIds.Count)
        {
            throw new KeyNotFoundException("One or more provided Game IDs are invalid for update.");
        }

        // Update simple properties
        existingAccount.Title = updateDto.Title;
        existingAccount.PricePs4 = updateDto.PricePs4; 
        existingAccount.PricePs5 = updateDto.PricePs5;
        existingAccount.Region = updateDto.Region;
        existingAccount.Capacity = updateDto.Capacity;
        existingAccount.StockStatus = updateDto.StockStatus;

        // Sync the games collection
        existingAccount.AccountGames.Clear(); // Easiest way to sync
        foreach (var game in validGames)
        {
            existingAccount.AccountGames.Add(new AccountGame { AccountId = existingAccount.Id, GameId = game.Id });
        }

        _accountRepository.Update(existingAccount);
        await _accountRepository.SaveChangesAsync();

        _logger.LogInformation("Account with ID {AccountId} was updated.", id);
        return true;
    }

    public async Task<bool> DeleteAccountAsync(int id)
    {
        var accountToDelete = await _accountRepository.GetByIdAsync(id);
        if (accountToDelete == null)
        {
            _logger.LogWarning("Delete failed: Account with ID {AccountId} not found.", id);
            return false;
        }

        // Perform a soft delete
        accountToDelete.IsDeleted = true;
        accountToDelete.StockStatus = StockStatus.OutOfStock;

        _accountRepository.Update(accountToDelete);
        await _accountRepository.SaveChangesAsync();

        _logger.LogInformation("Account with ID {AccountId} was soft-deleted.", id);
        return true;
    }

    // --- Private Helper Method for Mapping ---
    private AccountDto MapToAccountDto(Account account)
    {
        return new AccountDto
        {
            Id = account.Id,
            ChannelId = account.ChannelId,
            Title = account.Title,
            PricePs4 = account.PricePs4,
            PricePs5 = account.PricePs5, 
            Region = account.Region,
            Capacity = account.Capacity.ToString(),
            StockStatus = account.StockStatus.ToString(),
            IsDeleted = account.IsDeleted,
            Games = account.AccountGames?.Select(ag => new GameDto
            {
                Id = ag.Game.Id,
                Title = ag.Game.Title,
                PosterUrl = ag.Game.PosterUrl,
                Region = ag.Game.Region,
                // Note: SonyCode is not mapped here, but can be if needed
            }).ToList() ?? new List<GameDto>()
        };
    }
}