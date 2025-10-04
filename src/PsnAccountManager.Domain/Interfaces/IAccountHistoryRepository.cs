using PsnAccountManager.Domain.Entities;

namespace PsnAccountManager.Domain.Interfaces;

public interface IAccountHistoryRepository : IGenericRepository<AccountHistory, int>
{
    /// <summary>
    /// Gets the complete change history for a specific account, ordered by date.
    /// </summary>
    /// <param name="accountId">The ID of the account.</param>
    /// <returns>A list of history entries.</returns>
    Task<List<AccountHistory>> GetHistoryForAccountAsync(int accountId);
}