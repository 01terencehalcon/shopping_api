using System.Collections.Generic;
using System.Threading.Tasks;

namespace ClothesShopping.Api.AddressModule
{
    public interface IAddressRepository
    {
        Task<IEnumerable<Address>> GetByUserIdAsync(int userId);
        Task<Address?> GetByIdAsync(int addressId);
        Task<int> AddAsync(Address entity);
        Task UpdateAsync(Address entity);
        Task DeleteAsync(int addressId);
        Task DeleteByUserIdAsync(int userId);
    }
}
