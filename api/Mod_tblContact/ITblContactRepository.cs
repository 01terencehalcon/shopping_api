using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ClothesShopping.Api.Main;

namespace ClothesShopping.Api.TblContactModule
{
    public interface ITblContactRepository
    {
        Task<IEnumerable<TblContact>> GetAllAsync();
        Task<TblContact?> GetByIdAsync(int id);
        Task DeleteAsync(int id);
        Task AddAsync(TblContact entity);
        Task UpdateAsync(TblContact entity);
        Task DeleteAllAsync();
        Task<IEnumerable<TblContact>> GetFilteredExactAsync(IReadOnlyDictionary<string, object?> filters);
        Task<IEnumerable<TblContact>> GetFilteredLikeAsync(IReadOnlyDictionary<string, object?> filters);
        Task<IEnumerable<TblContact>> SearchAsync(string query);
        Task<IEnumerable<TblContact>> SearchAsyncAll(string query);
        Task<PaginationModel<TblContact>> GetPaginatedAsync(int pageNumber, int pageSize);
        Task BulkUploadAsync(List<TblContact> dataList);
    }
}