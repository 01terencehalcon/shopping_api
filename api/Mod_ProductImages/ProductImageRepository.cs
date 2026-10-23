using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using ClothesShopping.Api.Main;

namespace ClothesShopping.Api.ProductImagesModule
{
    public class ProductImageRepository : BaseRepository, IProductImageRepository
    {
        public ProductImageRepository(MyCon dbConnection) : base(dbConnection) { }

        private ProductImage MapRow(DbDataReader reader)
        {
            try
            {
                return new ProductImage
                {
                    ImageId   = ReadValue<int>(reader, "image_id", 0),
                    ProductId = ReadValue<int>(reader, "product_id", 0),
                    ImageUrl  = ReadValue<string>(reader, "image_url", string.Empty),
                };
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to map database row to ProductImage.", ex);
            }
        }

        public async Task<IEnumerable<ProductImage>> GetByProductIdAsync(int productId)
            => await ExecuteReaderToListAsync(
                "SELECT * FROM \"product_images\" WHERE \"product_id\" = @productId ORDER BY \"image_id\" ASC",
                MapRow, new[] { CreateParameter("productId", productId) });

        public async Task<ProductImage?> GetByIdAsync(int imageId)
        {
            var results = await ExecuteReaderToListAsync(
                "SELECT * FROM \"product_images\" WHERE \"image_id\" = @imageId",
                MapRow, new[] { CreateParameter("imageId", imageId) });
            return results.FirstOrDefault();
        }

        public async Task<int> AddAsync(ProductImage entity)
        {
            const string sql = "INSERT INTO \"product_images\" (\"product_id\", \"image_url\") " +
                               "VALUES (@productId, @imageUrl); SELECT last_insert_rowid();";
            var id = await ExecuteScalarAsync<long>(sql, new[]
            {
                CreateParameter("productId", entity.ProductId),
                CreateParameter("imageUrl",  entity.ImageUrl as object ?? DBNull.Value),
            });
            return (int)id;
        }

        public async Task DeleteAsync(int imageId)
            => await ExecuteNonQueryAsync("DELETE FROM \"product_images\" WHERE \"image_id\" = @imageId",
                new[] { CreateParameter("imageId", imageId) });

        public async Task DeleteByProductIdAsync(int productId)
            => await ExecuteNonQueryAsync("DELETE FROM \"product_images\" WHERE \"product_id\" = @productId",
                new[] { CreateParameter("productId", productId) });
    }
}
