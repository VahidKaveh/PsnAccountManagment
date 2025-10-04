using PsnAccountManager.Domain.Entities;

namespace PsnAccountManager.Application.Interfaces;

public interface IChangeTrackerService
{
    List<AccountHistory> GetChanges(Account original, Account updated, string updatedBy);
}