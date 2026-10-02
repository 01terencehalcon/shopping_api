using System.Collections.Generic;
using System.Threading.Tasks;
using ClothesShopping.Api.Main;

namespace ClothesShopping.Api.InventoryLogsModule
{
    public interface IInventoryLogRepository
    {
        Task<IEnumerable<InventoryLog>> GetAllAsync();
        Task<InventoryLog?> GetByIdAsync(int logId);
        Task<IEnumerable<InventoryLog>> GetByProductIdAsync(int productId);
        Task<PaginationModel<InventoryLog>> GetPaginatedAsync(int pageNumber, int pageSize);
        Task<int> AddAsync(InventoryLog entity);
        Task DeleteAsync(int logId);
    }
}
