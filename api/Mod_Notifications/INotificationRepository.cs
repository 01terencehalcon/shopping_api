using System.Collections.Generic;
using System.Threading.Tasks;
using ClothesShopping.Api.Main;

namespace ClothesShopping.Api.NotificationsModule
{
    public interface INotificationRepository
    {
        Task<IEnumerable<Notification>> GetAllAsync();
        Task<Notification?> GetByIdAsync(int notificationId);
        Task<IEnumerable<Notification>> GetByUserIdAsync(int userId);
        Task<PaginationModel<Notification>> GetPaginatedAsync(int pageNumber, int pageSize);
        Task<int> AddAsync(Notification entity);
        Task MarkAsReadAsync(int notificationId);
        Task DeleteAsync(int notificationId);
        Task DeleteByUserIdAsync(int userId);
    }
}
