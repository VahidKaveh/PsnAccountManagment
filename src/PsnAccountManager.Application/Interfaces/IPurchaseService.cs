using PsnAccountManager.Shared.DTOs;

namespace PsnAccountManager.Application.Interfaces;

public interface IPurchaseService
{
    Task<PurchaseDto> CreatePurchaseAsync(CreatePurchaseDto createPurchaseDto);
    Task<PurchaseDto?> GetPurchaseDetailsAsync(int purchaseId);
}