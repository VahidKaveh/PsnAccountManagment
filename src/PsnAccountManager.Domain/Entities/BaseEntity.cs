namespace PsnAccountManager.Domain.Entities;

/// <summary>
/// Base entity for all domain entities with audit fields
/// </summary>
public abstract class BaseEntity<T>
{
    public T Id { get; set; }

    // Audit fields
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public string? UpdatedBy { get; set; }
}