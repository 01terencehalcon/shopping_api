using System.Collections.Generic;
using System.Threading.Tasks;
using ClothesShopping.Api.Main;
using ClothesShopping.Api.DTOs;

namespace ClothesShopping.Api.ProductsModule
{
    public interface IProductRepository
    {
        Task<IEnumerable<Product>> GetAllAsync();
        Task<Product?> GetByIdAsync(int id);
        Task<IEnumerable<Product>> GetByCategoryAsync(string category);
        Task<IEnumerable<Product>> SearchAsync(string query);
        Task<PaginationModel<Product>> GetPaginatedAsync(int pageNumber, int pageSize);
        Task<IEnumerable<Product>> GetFilteredExactAsync(IReadOnlyDictionary<string, object?> filters);
        Task<IEnumerable<Product>> GetFilteredLikeAsync(IReadOnlyDictionary<string, object?> filters);
        Task<int> AddAsync(Product entity);
        Task UpdateAsync(Product entity);
        Task DeleteAsync(int id);
        Task BulkUploadAsync(List<Product> dataList);
        Task<IEnumerable<TopProductResponse>> GetTopAsync(int count);
    }
}
