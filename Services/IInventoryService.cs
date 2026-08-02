using WH_Logistic.Models;

namespace WH_Logistic.Services
{
    public interface IInventoryService
    {
        Task<(bool Success, string Message)> PutAwayAsync(int productId, int locationId, int quantity, int userId);
        Task<(bool Success, string Message)> TransferStockAsync(int productId, int fromLocationId, int toLocationId, int quantity, int userId);
        Task<List<InventoryBalance>> GetLowStockItemsAsync();
        Task<(bool Success, string Message)> AdjustStockAsync(int productId, int locationId, int newQty, int userId, string reason);
        Task<List<Location>> GetSuggestedLocationsAsync(int productId);
    }
}
