using PsnAccountManager.Application.Interfaces;
using PsnAccountManager.Domain.Entities;

namespace PsnAccountManager.Application.Services;

public class ChangeTrackerService : IChangeTrackerService
{
    public List<AccountHistory> GetChanges(Account original, Account updated, string updatedBy)
    {
        var changes = new List<AccountHistory>();

        CompareAndLog(changes, "Title", original.Title, updated.Title, updated.Id, updatedBy);
        CompareAndLog(changes, "PricePs4", original.PricePs4, updated.PricePs4, updated.Id, updatedBy);
        CompareAndLog(changes, "PricePs5", original.PricePs5, updated.PricePs5, updated.Id, updatedBy);
        CompareAndLog(changes, "Region", original.Region, updated.Region, updated.Id, updatedBy);
        CompareAndLog(changes, "Capacity", original.Capacity.ToString(), updated.Capacity.ToString(), updated.Id,
            updatedBy);
        CompareAndLog(changes, "StockStatus", original.StockStatus.ToString(), updated.StockStatus.ToString(),
            updated.Id, updatedBy);
        CompareAndLog(changes, "HasOriginalMail", original.HasOriginalMail, updated.HasOriginalMail, updated.Id,
            updatedBy);
        CompareAndLog(changes, "SellerInfo", original.SellerInfo, updated.SellerInfo, updated.Id, updatedBy);
        CompareAndLog(changes, "AdditionalInfo", original.AdditionalInfo, updated.AdditionalInfo, updated.Id,
            updatedBy);

        // Compare game lists
        var originalGames = original.AccountGames.Select(g => g.Game.Title).OrderBy(t => t).ToList();
        var updatedGames = updated.AccountGames.Select(g => g.Game.Title).OrderBy(t => t).ToList();

        if (!originalGames.SequenceEqual(updatedGames))
            CompareAndLog(changes, "Games", string.Join(", ", originalGames), string.Join(", ", updatedGames),
                updated.Id, updatedBy);

        return changes;
    }

    private void CompareAndLog<T>(List<AccountHistory> changes, string propertyName, T oldValue, T newValue,
        int accountId, string changedBy)
    {
        // Use EqualityComparer to handle nulls and different types correctly
        if (!EqualityComparer<T>.Default.Equals(oldValue, newValue))
            changes.Add(new AccountHistory
            {
                AccountId = accountId,
                FieldName = propertyName,
                OldValue = oldValue?.ToString(),
                NewValue = newValue?.ToString(),
                ChangedAt = DateTime.UtcNow,
                ChangedBy = changedBy
            });
    }
}