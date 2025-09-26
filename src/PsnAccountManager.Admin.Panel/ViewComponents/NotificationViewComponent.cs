using Microsoft.AspNetCore.Mvc;
using PsnAccountManager.Domain.Interfaces;
namespace PsnAccountManager.Admin.Panel.ViewComponents;

public class NotificationViewComponent : ViewComponent
{
    private readonly IAdminNotificationRepository _notificationRepo;

    public NotificationViewComponent(IAdminNotificationRepository notificationRepo)
    {
        _notificationRepo = notificationRepo;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var notifications = await _notificationRepo.GetUnreadNotificationsAsync(5); // Get latest 5 unread
        ViewData["UnreadCount"] = notifications.Count();
        return View(notifications);
    }
}