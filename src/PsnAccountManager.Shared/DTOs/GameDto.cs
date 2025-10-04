namespace PsnAccountManager.Shared.DTOs;

public class GameDto
{
    public int Id { get; set; }
    public string SonyCode { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Region { get; set; }
    public string? PosterUrl { get; set; }

    /// <summary>
    /// Game description for display purposes
    /// </summary>
    public string? Description { get; set; }
}