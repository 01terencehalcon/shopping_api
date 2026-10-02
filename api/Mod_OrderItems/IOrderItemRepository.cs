using System.Collections.Generic;
using System.Threading.Tasks;

namespace ClothesShopping.Api.OrderItemsModule
{
    public interface IOrderItemRepository
    {
        Task<IEnumerable<OrderItem>> GetAllAsync();
        Task<OrderItem?> GetByIdAsync(int id);
        Task<IEnumerable<OrderItem>> GetByOrderIdAsync(int orderId);
        Task AddAsync(OrderItem entity);
        Task UpdateAsync(OrderItem entity);
        Task DeleteAsync(int id);
        Task DeleteByOrderIdAsync(int orderId);
    }
}
