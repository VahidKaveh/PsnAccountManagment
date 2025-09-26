namespace PsnAccountManager.Shared.Enums;

public enum ParsedFieldType
{
    // Pricing
    PricePs4,
    PricePs5,

    // Attributes
    Region,
    OriginalMail,
    Guarantee,
    SellerInfo,
    Capacity,

    // Status
    SoldStatus,

    // Delimiters for Games Block
    GamesBlockStart,
    GamesBlockEnd,

    AdditionalInfo
}