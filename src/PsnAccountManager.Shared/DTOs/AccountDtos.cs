using System;
using System.Collections.Generic;

namespace PsnAccountManager.Shared.DTOs;

/// <summary>
/// Complete account data transfer object for display in admin panel
/// Enhanced with all entity fields including new processing tracking fields
/// </summary>
public class AccountDto
{
    public int Id { get; set; }

    // Channel Information
    public int ChannelId { get; set; }
    public string ChannelName { get; set; } = string.Empty;

    // Basic Account Info
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ExternalId { get; set; } = string.Empty;

    // Pricing
    public decimal? PricePs4 { get; set; }
    public decimal? PricePs5 { get; set; }
    public string? Region { get; set; }

    // Account Features
    public bool HasOriginalMail { get; set; }
    public int? GuaranteeMinutes { get; set; }
    public string? SellerInfo { get; set; }

    // Status & Capacity (stored as strings from enums)
    public string Capacity { get; set; } = string.Empty; // e.g., "Primary", "Secondary"
    public string StockStatus { get; set; } = string.Empty; // e.g., "Available", "Sold"
    public bool IsDeleted { get; set; }

    // Timestamps
    public DateTime LastScrapedAt { get; set; }

    // ========== NEW FIELDS (Processing Tracking) ==========
    /// <summary>
    /// When this account was processed from a RawMessage
    /// </summary>
    public DateTime? ProcessedAt { get; set; }

    /// <summary>
    /// Result of processing (success message or error details)
    /// </summary>
    public string? ProcessingResult { get; set; }

    // Games Collection
    public List<GameDto> Games { get; set; } = new();
}