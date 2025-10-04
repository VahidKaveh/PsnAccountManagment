using PsnAccountManager.Shared.Enums;
using System.ComponentModel.DataAnnotations;

namespace PsnAccountManager.Domain.Entities
{
    /// <summary>
    /// Represents an admin notification for important system events
    /// </summary>
    public class AdminNotification : BaseEntity<int>
    {
        /// <summary>
        /// Type of the notification
        /// </summary>
        public AdminNotificationType Type { get; set; }

        /// <summary>
        /// Short title/subject of the notification
        /// </summary>
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Detailed message content
        /// </summary>
        [MaxLength(2000)]
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Priority level of the notification
        /// </summary>
        public NotificationPriority Priority { get; set; } = NotificationPriority.Normal;

        /// <summary>
        /// Whether the notification has been read by an admin
        /// </summary>
        public bool IsRead { get; set; } = false;

        /// <summary>
        /// When the notification was read (if applicable)
        /// </summary>
        public DateTime? ReadAt { get; set; }

        /// <summary>
        /// ID of related entity (optional)
        /// </summary>
        public int? RelatedEntityId { get; set; }

        /// <summary>
        /// Type of related entity (e.g., "Account", "RawMessage", etc.)
        /// </summary>
        [MaxLength(50)]
        public string? RelatedEntityType { get; set; }

        /// <summary>
        /// Additional metadata as JSON (optional)
        /// </summary>
        [MaxLength(1000)]
        public string? Metadata { get; set; }

        /// <summary>
        /// When the notification expires (optional)
        /// </summary>
        public DateTime? ExpiresAt { get; set; }
    }
}