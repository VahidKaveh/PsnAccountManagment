using PsnAccountManager.Domain.Entities;
namespace PsnAccountManager.Domain.Interfaces;

/// <summary>
/// Defines the contract for data access operations related to AdminNotification entities.
/// </summary>
public interface IAdminNotificationRepository : IGenericRepository<AdminNotification, int>
{
    /// <summary>
    /// Gets a list of the most recent unread notifications.
    /// </summary>
    /// <param name="take">The maximum number of notifications to return.</param>
    /// <returns>A collection of unread AdminNotification entities.</returns>
    Task<IEnumerable<AdminNotification>> GetUnreadNotificationsAsync(int take = 10);

    /// <summary>
    /// Marks a specific notification as read.
    /// </summary>
    /// <param name="notificationId">The ID of the notification to mark as read.</param>
    Task MarkAsReadAsync(int notificationId);

    /// <summary>
    /// Marks all unread notifications as read.
    /// </summary>
    Task MarkAllAsReadAsync();
}