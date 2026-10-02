using System.Collections.Generic;
using System.Threading.Tasks;
using ClothesShopping.Api.Main;

namespace ClothesShopping.Api.PaymentsModule
{
    public interface IPaymentRepository
    {
        Task<IEnumerable<Payment>> GetAllAsync();
        Task<Payment?> GetByIdAsync(int paymentId);
        Task<IEnumerable<Payment>> GetByOrderIdAsync(int orderId);
        Task<PaginationModel<Payment>> GetPaginatedAsync(int pageNumber, int pageSize);
        Task<int> AddAsync(Payment entity);
        Task UpdateStatusAsync(int paymentId, string status);
        Task DeleteAsync(int paymentId);
    }
}
