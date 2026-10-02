using System.Collections.Generic;
using System.Threading.Tasks;

namespace ClothesShopping.Api.CartModule
{
    public interface ICartRepository
    {
        Task<Cart?> GetByIdAsync(int cartId);
        Task<Cart?> GetByUserIdAsync(int userId);
        Task<int> AddAsync(Cart entity);
        Task DeleteAsync(int cartId);
        Task DeleteByUserIdAsync(int userId);
    }
}
