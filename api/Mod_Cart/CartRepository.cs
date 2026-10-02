using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using ClothesShopping.Api.Main;

namespace ClothesShopping.Api.CartModule
{
    public class CartRepository : BaseRepository, ICartRepository
    {
        public CartRepository(MyCon dbConnection) : base(dbConnection) { }

        private Cart MapRow(DbDataReader reader)
        {
            try
            {
                return new Cart
                {
                    CartId    = ReadValue<int>(reader, "cart_id", 0),
                    UserId    = ReadValue<int>(reader, "user_id", 0),
                    CreatedAt = ReadValue<string>(reader, "created_at", string.Empty),
                };
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to map database row to Cart.", ex);
            }
        }

        public async Task<Cart?> GetByIdAsync(int cartId)
        {
            var results = await ExecuteReaderToListAsync(
                "SELECT * FROM \"cart\" WHERE \"cart_id\" = @cartId",
                MapRow, new[] { CreateParameter("cartId", cartId) });
            return results.FirstOrDefault();
        }

        public async Task<Cart?> GetByUserIdAsync(int userId)
        {
            var results = await ExecuteReaderToListAsync(
                "SELECT * FROM \"cart\" WHERE \"user_id\" = @userId",
                MapRow, new[] { CreateParameter("userId", userId) });
            return results.FirstOrDefault();
        }

        public async Task<int> AddAsync(Cart entity)
        {
            const string sql = "INSERT INTO \"cart\" (\"user_id\") VALUES (@userId); SELECT last_insert_rowid();";
            var id = await ExecuteScalarAsync<long>(sql,
                new[] { CreateParameter("userId", entity.UserId) });
            return (int)id;
        }

        public async Task DeleteAsync(int cartId)
            => await ExecuteNonQueryAsync("DELETE FROM \"cart\" WHERE \"cart_id\" = @cartId",
                new[] { CreateParameter("cartId", cartId) });

        public async Task DeleteByUserIdAsync(int userId)
            => await ExecuteNonQueryAsync("DELETE FROM \"cart\" WHERE \"user_id\" = @userId",
                new[] { CreateParameter("userId", userId) });
    }
}
