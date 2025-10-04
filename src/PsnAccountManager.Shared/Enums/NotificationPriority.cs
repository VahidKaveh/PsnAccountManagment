namespace PsnAccountManager.Shared.Enums
{
    /// <summary>
    /// Types of admin notifications
    /// </summary>
    public enum AdminNotificationType
    {
        /// <summary>
        /// System-related notifications
        /// </summary>
        System = 0,

        /// <summary>
        /// Account-related notifications (created, updated, deleted)
        /// </summary>
        AccountChanged = 1,

        /// <summary>
        /// Scraper-related notifications
        /// </summary>
        ScraperIssue = 2,

        /// <summary>
        /// Processing errors
        /// </summary>
        ProcessingError = 3,

        /// <summary>
        /// Channel-related notifications
        /// </summary>
        ChannelIssue = 4,

        /// <summary>
        /// Security-related notifications
        /// </summary>
        Security = 5,

        /// <summary>
        /// Performance or resource warnings
        /// </summary>
        Performance = 6,

        /// <summary>
        /// Data quality issues
        /// </summary>
        DataQuality = 7
    }

    /// <summary>
    /// Priority levels for notifications
    /// </summary>
    public enum NotificationPriority
    {
        /// <summary>
        /// Low priority - informational
        /// </summary>
        Low = 0,

        /// <summary>
        /// Normal priority - standard notifications
        /// </summary>
        Normal = 1,

        /// <summary>
        /// High priority - requires attention
        /// </summary>
        High = 2,

        /// <summary>
        /// Critical - immediate action required
        /// </summary>
        Critical = 3
    }
}