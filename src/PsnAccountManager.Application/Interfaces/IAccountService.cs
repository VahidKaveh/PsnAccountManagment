using PsnAccountManager.Shared.DTOs;

namespace PsnAccountManager.Application.Interfaces;

public interface IAccountService
{
    Task<AccountDto?> GetAccountWithGamesAsync(int id);
    Task<IEnumerable<AccountDto>> GetAvailableAccountsAsync();
    Task<AccountDto> CreateAccountAsync(CreateAccountDto createDto);
    Task<bool> UpdateAccountAsync(int id, UpdateAccountDto updateDto);
    Task<bool> DeleteAccountAsync(int id); 
}