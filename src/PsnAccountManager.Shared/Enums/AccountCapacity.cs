namespace PsnAccountManager.Shared.Enums;

public enum AccountCapacity
{
    Unknown, // A default value if parsing fails
    Capacity1,
    Capacity2,
    Capacity3,
    OfflineOnly, // For "Z1 Offline" or similar
    FullAccess // If it has original mail and all capacities
}