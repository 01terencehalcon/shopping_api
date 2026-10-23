using System.Collections.Generic;
using System.Threading.Tasks;
using ClothesShopping.Api.Main;

namespace ClothesShopping.Api.OrdersModule
{
    public interface IOrderRepository
    {
        Task<IEnumerable<Order>> GetAllAsync();
        Task<Order?> GetByIdAsync(int id);
        Task<IEnumerable<Order>> GetByUserIdAsync(int userId);
        Task<PaginationModel<Order>> GetPaginatedAsync(int pageNumber, int pageSize);
        Task<int> AddAsync(Order entity);
        Task UpdateAsync(Order entity);
        Task DeleteAsync(int id);
        Task<int> CreateOrderTransactionalAsync(
            int userId,
            double total,
            IReadOnlyList<(int ProductId, int Quantity, double Price)> items,
            int cartId);
    }
}
