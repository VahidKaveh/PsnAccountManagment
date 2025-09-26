using Microsoft.EntityFrameworkCore;
using PsnAccountManager.Domain.Entities;
using PsnAccountManager.Domain.Interfaces;
using PsnAccountManager.Infrastructure.Data;

namespace PsnAccountManager.Infrastructure.Repositories;

/// <summary>
/// Implements IAdminNotificationRepository for data access using EF Core.
/// </summary>
public class AdminNotificationRepository : GenericRepository<AdminNotification, int>, IAdminNotificationRepository
{
    public AdminNotificationRepository(PsnAccountManagerDbContext context) : base(context) { }

    public async Task<IEnumerable<AdminNotification>> GetUnreadNotificationsAsync(int take = 10)
    {
        return await _dbSet
            .Where(n => !n.IsRead)
            .OrderByDescending(n => n.CreatedAt)
            .Take(take)
            .ToListAsync();
    }

    public async Task MarkAllAsReadAsync()
    {
        // This is an efficient way to update multiple records in the database
        // without loading them into memory first.
        await _dbSet
            .Where(n => !n.IsRead)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true));
    }

    public async Task MarkAsReadAsync(int notificationId)
    {
        var notification = await _dbSet.FindAsync(notificationId);
        if (notification != null && !notification.IsRead)
        {
            notification.IsRead = true;
            _dbSet.Update(notification);
            await _context.SaveChangesAsync();
        }
    }
}