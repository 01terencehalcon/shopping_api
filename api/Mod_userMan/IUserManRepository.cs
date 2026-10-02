using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ClothesShopping.Api.Main;

namespace ClothesShopping.Api.UsermanModule
{
    public interface IUsermanRepository
    {
        Task<IEnumerable<Userman>> GetAllAsync();
        Task<Userman?> GetByIdAsync(int userId);
        Task DeleteAsync(int userId);
        Task AddAsync(Userman entity);
        Task UpdateAsync(Userman entity);
        Task<Userman?> GetByUserNameAsync(string userName);
        Task<Userman?> GetByEmailAsync(string email);
        Task DeleteAllAsync();
        Task<IEnumerable<Userman>> GetFilteredExactAsync(IReadOnlyDictionary<string, object?> filters);
        Task<IEnumerable<Userman>> GetFilteredLikeAsync(IReadOnlyDictionary<string, object?> filters);
        Task<IEnumerable<Userman>> SearchAsync(string query);
        Task<IEnumerable<Userman>> SearchAsyncAll(string query);
        Task<PaginationModel<Userman>> GetPaginatedAsync(int pageNumber, int pageSize);
        Task BulkUploadAsync(List<Userman> dataList);
    }
}