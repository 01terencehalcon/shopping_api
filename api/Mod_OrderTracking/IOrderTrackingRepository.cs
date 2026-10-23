using System.Collections.Generic;
using System.Threading.Tasks;

namespace ClothesShopping.Api.OrderTrackingModule
{
    public interface IOrderTrackingRepository
    {
        Task<IEnumerable<OrderTracking>> GetByOrderIdAsync(int orderId);
        Task<int> AddAsync(OrderTracking entity);
        Task EnsureDatabaseMigratedAsync();
    }
}
