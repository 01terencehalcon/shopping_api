using System.Collections.Generic;
using System.Threading.Tasks;
using ClothesShopping.Api.Main;

namespace ClothesShopping.Api.ReviewsModule
{
    public interface IReviewRepository
    {
        Task<IEnumerable<Review>> GetAllAsync();
        Task<Review?> GetByIdAsync(int reviewId);
        Task<IEnumerable<Review>> GetByProductIdAsync(int productId);
        Task<IEnumerable<Review>> GetByUserIdAsync(int userId);
        Task<PaginationModel<Review>> GetPaginatedAsync(int pageNumber, int pageSize);
        Task<int> AddAsync(Review entity);
        Task UpdateAsync(Review entity);
        Task DeleteAsync(int reviewId);
    }
}
