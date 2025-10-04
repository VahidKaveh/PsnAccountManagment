using PsnAccountManager.Domain.Entities;
using PsnAccountManager.Shared.DTOs;

namespace PsnAccountManager.Application.Interfaces;

public interface IAccountService
{
    /// <summary>
    /// Gets a single account by its ID, including associated games.
    /// </summary>
    Task<Account?> GetAccountByIdAsync(int id);

    /// <summary>
    /// Updates an existing account's properties and logs all changes.
    /// </summary>
    Task UpdateAccountAsync(Account updatedAccount, List<string> gameTitles, string updatedBy);

    /// <summary>
    /// Creates a new account in the database.
    /// </summary>
    Task<AccountDto> CreateAccountAsync(CreateAccountDto createDto);

    /// <summary>
    /// Performs a soft delete on an account.
    /// </summary>
    Task<bool> DeleteAccountAsync(int id);
}