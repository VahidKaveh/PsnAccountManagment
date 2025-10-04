namespace PsnAccountManager.Shared.DTOs;

/// <summary>
/// Represents a game with its database existence status for preview purposes.
/// </summary>
public class GamePreviewDto
{
    public string Title { get; set; } = string.Empty;
    public bool ExistsInDb { get; set; }
}