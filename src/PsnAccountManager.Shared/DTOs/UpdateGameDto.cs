using System.ComponentModel.DataAnnotations;

namespace PsnAccountManager.Shared.DTOs;

public class UpdateGameDto
{
    [Required(ErrorMessage = "Title is required.")]
    [StringLength(255, ErrorMessage = "Title cannot be longer than 255 characters.")]
    public string Title { get; set; }

    [StringLength(50)] public string? Region { get; set; }

    [Url] [StringLength(500)] public string? PosterUrl { get; set; }
}