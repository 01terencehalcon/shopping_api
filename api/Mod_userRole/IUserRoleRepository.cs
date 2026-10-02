using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ClothesShopping.Api.Main;

namespace ClothesShopping.Api.UserroleModule
{
    public interface IUserroleRepository
    {
        Task<IEnumerable<Userrole>> GetAllAsync();
        Task<Userrole?> GetByIdAsync(int userRoleId);
        Task DeleteAsync(int userRoleId);
        Task AddAsync(Userrole entity);
        Task UpdateAsync(Userrole entity);
        Task DeleteAllAsync();
        Task<IEnumerable<Userrole>> GetFilteredExactAsync(IReadOnlyDictionary<string, object?> filters);
        Task<IEnumerable<Userrole>> GetFilteredLikeAsync(IReadOnlyDictionary<string, object?> filters);
        Task<IEnumerable<Userrole>> SearchAsync(string query);
        Task<IEnumerable<Userrole>> SearchAsyncAll(string query);
        Task<PaginationModel<Userrole>> GetPaginatedAsync(int pageNumber, int pageSize);
        Task BulkUploadAsync(List<Userrole> dataList);
    }
}