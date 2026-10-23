using System.Collections.Generic;
using System.Threading.Tasks;

namespace ClothesShopping.Api.CartItemsModule
{
    public interface ICartItemRepository
    {
        Task<IEnumerable<CartItem>> GetByCartIdAsync(int cartId);
        Task<CartItem?> GetByIdAsync(int cartItemId);
        Task<int> AddAsync(CartItem entity);
        Task UpdateQuantityAsync(int cartItemId, int quantity);
        Task DeleteAsync(int cartItemId);
        Task DeleteByCartIdAsync(int cartId);
        Task EnsureDatabaseMigratedAsync();
    }
}
