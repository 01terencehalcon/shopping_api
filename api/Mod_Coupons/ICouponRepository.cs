using System.Collections.Generic;
using System.Threading.Tasks;
using ClothesShopping.Api.Main;

namespace ClothesShopping.Api.CouponsModule
{
    public interface ICouponRepository
    {
        Task<IEnumerable<Coupon>> GetAllAsync();
        Task<Coupon?> GetByIdAsync(int couponId);
        Task<Coupon?> GetByCodeAsync(string code);
        Task<PaginationModel<Coupon>> GetPaginatedAsync(int pageNumber, int pageSize);
        Task<int> AddAsync(Coupon entity);
        Task UpdateAsync(Coupon entity);
        Task DeleteAsync(int couponId);
    }
}
