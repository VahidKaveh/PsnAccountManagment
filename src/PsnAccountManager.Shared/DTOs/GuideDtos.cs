namespace PsnAccountManager.Shared.DTOs;

/// <summary>
/// A lightweight DTO for displaying a list of guides.
/// </summary>
public class GuideSummaryDto
{
    public int Id { get; set; }
    public string Title { get; set; }
}

/// <summary>
/// A detailed DTO for displaying the full content of a single guide.
/// </summary>
public class GuideDetailsDto
{
    public int Id { get; set; }
    public string Title { get; set; }
    public string Content { get; set; }
    public string? MediaUrl { get; set; }
}