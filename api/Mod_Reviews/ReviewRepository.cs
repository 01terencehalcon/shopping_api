using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using ClothesShopping.Api.Main;

namespace ClothesShopping.Api.ReviewsModule
{
    public class ReviewRepository : BaseRepository, IReviewRepository
    {
        public ReviewRepository(MyCon dbConnection) : base(dbConnection) { }

        private Review MapRow(DbDataReader reader)
        {
            try
            {
                return new Review
                {
                    ReviewId   = ReadValue<int>(reader, "review_id", 0),
                    UserId     = ReadValue<int>(reader, "user_id", 0),
                    ProductId  = ReadValue<int>(reader, "product_id", 0),
                    Rating     = ReadValue<int>(reader, "rating", 1),
                    Comment    = ReadValue<string>(reader, "comment", string.Empty),
                    ReviewDate = ReadValue<string>(reader, "review_date", string.Empty),
                };
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to map database row to Review.", ex);
            }
        }

        public async Task<IEnumerable<Review>> GetAllAsync()
            => await ExecuteReaderToListAsync("SELECT * FROM \"reviews\"", MapRow);

        public async Task<Review?> GetByIdAsync(int reviewId)
        {
            var results = await ExecuteReaderToListAsync(
                "SELECT * FROM \"reviews\" WHERE \"review_id\" = @reviewId",
                MapRow, new[] { CreateParameter("reviewId", reviewId) });
            return results.FirstOrDefault();
        }

        public async Task<IEnumerable<Review>> GetByProductIdAsync(int productId)
            => await ExecuteReaderToListAsync(
                "SELECT * FROM \"reviews\" WHERE \"product_id\" = @productId ORDER BY \"review_date\" DESC",
                MapRow, new[] { CreateParameter("productId", productId) });

        public async Task<IEnumerable<Review>> GetByUserIdAsync(int userId)
            => await ExecuteReaderToListAsync(
                "SELECT * FROM \"reviews\" WHERE \"user_id\" = @userId ORDER BY \"review_date\" DESC",
                MapRow, new[] { CreateParameter("userId", userId) });

        public async Task<PaginationModel<Review>> GetPaginatedAsync(int pageNumber, int pageSize)
        {
            if (pageNumber <= 0) pageNumber = 1;
            if (pageSize <= 0)
            {
                var all = await ExecuteReaderToListAsync("SELECT * FROM \"reviews\"", MapRow);
                return new PaginationModel<Review> { Items = all, TotalCount = all.Count, PageSize = all.Count, CurrentPage = 1 };
            }

            var items = await ExecuteReaderToListAsync(
                "SELECT * FROM \"reviews\" ORDER BY \"review_id\" LIMIT @pageSize OFFSET @offset",
                MapRow,
                new[] { CreateParameter("offset", (pageNumber - 1) * pageSize), CreateParameter("pageSize", pageSize) });

            var total = await ExecuteScalarAsync<int>("SELECT COUNT(*) FROM \"reviews\"");
            return new PaginationModel<Review> { Items = items, TotalCount = total, PageSize = pageSize, CurrentPage = pageNumber };
        }

        public async Task<int> AddAsync(Review entity)
        {
            const string sql = "INSERT INTO \"reviews\" (\"user_id\", \"product_id\", \"rating\", \"comment\") " +
                               "VALUES (@userId, @productId, @rating, @comment); SELECT last_insert_rowid();";
            var id = await ExecuteScalarAsync<long>(sql, new[]
            {
                CreateParameter("userId",    entity.UserId),
                CreateParameter("productId", entity.ProductId),
                CreateParameter("rating",    entity.Rating),
                CreateParameter("comment",   entity.Comment as object ?? DBNull.Value),
            });
            return (int)id;
        }

        public async Task UpdateAsync(Review entity)
        {
            const string sql = "UPDATE \"reviews\" SET \"rating\"=@rating, \"comment\"=@comment WHERE \"review_id\"=@reviewId";
            await ExecuteNonQueryAsync(sql, new[]
            {
                CreateParameter("reviewId", entity.ReviewId),
                CreateParameter("rating",   entity.Rating),
                CreateParameter("comment",  entity.Comment as object ?? DBNull.Value),
            });
        }

        public async Task DeleteAsync(int reviewId)
            => await ExecuteNonQueryAsync("DELETE FROM \"reviews\" WHERE \"review_id\" = @reviewId",
                new[] { CreateParameter("reviewId", reviewId) });
    }
}
