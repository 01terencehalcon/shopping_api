using System.Collections.Generic;
using System.Threading.Tasks;

namespace ClothesShopping.Api.ProductImagesModule
{
    public interface IProductImageRepository
    {
        Task<IEnumerable<ProductImage>> GetByProductIdAsync(int productId);
        Task<ProductImage?> GetByIdAsync(int imageId);
        Task<int> AddAsync(ProductImage entity);
        Task DeleteAsync(int imageId);
        Task DeleteByProductIdAsync(int productId);
    }
}
