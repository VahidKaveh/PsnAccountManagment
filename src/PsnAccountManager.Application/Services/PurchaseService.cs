using Microsoft.Extensions.Logging;
using PsnAccountManager.Application.Interfaces;
using PsnAccountManager.Domain.Interfaces;
using PsnAccountManager.Shared.DTOs;
using PsnAccountManager.Shared.Enums;

namespace PsnAccountManager.Application.Services;

public class PurchaseService : IPurchaseService
{
    private readonly IPurchaseRepository _purchaseRepository;
    private readonly IAccountRepository _accountRepository;
    private readonly ILogger<PurchaseService> _logger;

    public PurchaseService(
        IPurchaseRepository purchaseRepository,
        IAccountRepository accountRepository,
        ILogger<PurchaseService> logger)
    {
        _purchaseRepository = purchaseRepository;
        _accountRepository = accountRepository;
        _logger = logger;
    }

    // متد CreatePurchaseAsync که قبلاً پیاده‌سازی شد
    public async Task<PurchaseDto> CreatePurchaseAsync(CreatePurchaseDto dto)
    {
        var account = await _accountRepository.GetByIdAsync(dto.AccountId);
        if (account == null || account.IsDeleted || account.StockStatus != StockStatus.InStock)
            throw new InvalidOperationException("Account is not available for purchase.");
        account.StockStatus = StockStatus.Reserved;
        _accountRepository.Update(account);
        var purchasePrice = account.PricePs5 ?? account.PricePs4 ?? 0;
        if (purchasePrice <= 0) throw new InvalidOperationException("Cannot purchase an account with no price.");

        var purchase = new Domain.Entities.Purchase
        {
            BuyerUserId = dto.BuyerUserId,
            AccountId = dto.AccountId,
            SellerChannelId = account.ChannelId,
            TotalAmount = purchasePrice,
            Status = PurchaseStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };
        await _purchaseRepository.AddAsync(purchase);
        await _purchaseRepository.SaveChangesAsync();
        _logger.LogInformation("Purchase created with ID {Id} for Account {AccountId}", purchase.Id, account.Id);

        // This mapping needs the full entity, which we don't have here.
        // It's better to fetch it again or manually map.
        var purchaseDetails = await GetPurchaseDetailsAsync(purchase.Id);
        return purchaseDetails!;
    }

    public async Task<PurchaseDto?> GetPurchaseDetailsAsync(int purchaseId)
    {
        _logger.LogInformation("Fetching details for Purchase ID: {PurchaseId}", purchaseId);

        // 1. فراخوانی ریپازیتوری برای گرفتن خرید به همراه جزئیات (موجودیت‌های مرتبط)
        // این متد باید در ریپازیتوری با Include() پیاده‌سازی شده باشد.
        var purchaseEntity = await _purchaseRepository.GetPurchaseWithDetailsAsync(purchaseId);

        // 2. بررسی اینکه آیا خرید پیدا شده است یا خیر
        if (purchaseEntity == null)
        {
            _logger.LogWarning("Purchase with ID: {PurchaseId} not found.", purchaseId);
            return null; // بازگرداندن null اگر خرید وجود نداشته باشد
        }

        // 3. نگاشت (Map) کردن Entity به DTO
        // در یک پروژه واقعی از AutoMapper استفاده می‌شود.
        var purchaseDto = new PurchaseDto
        {
            Id = purchaseEntity.Id,
            BuyerUserId = purchaseEntity.BuyerUserId,
            // استفاده از Null-conditional operator (?.) برای جلوگیری از خطا اگر Buyer لود نشده باشد
            BuyerUsername = purchaseEntity.Buyer?.Username ?? "N/A",
            AccountId = purchaseEntity.AccountId,
            AccountTitle = purchaseEntity.Account?.Title ?? "N/A",
            TotalAmount = purchaseEntity.TotalAmount,
            Status = purchaseEntity.Status.ToString(), // تبدیل enum به string برای نمایش
            CreatedAt = purchaseEntity.CreatedAt
        };

        // 4. بازگرداندن DTO
        return purchaseDto;
    }
}