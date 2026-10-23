using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using ClothesShopping.Api.Main;

namespace ClothesShopping.Api.CartItemsModule
{
    public class CartItemRepository : BaseRepository, ICartItemRepository
    {
        public CartItemRepository(MyCon dbConnection) : base(dbConnection) { }

        private CartItem MapRow(DbDataReader reader)
        {
            try
            {
                return new CartItem
                {
                    CartItemId = ReadValue<int>(reader, "cart_item_id", 0),
                    CartId     = ReadValue<int>(reader, "cart_id", 0),
                    ProductId  = ReadValue<int>(reader, "product_id", 0),
                    Quantity   = ReadValue<int>(reader, "quantity", 1),
                };
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to map database row to CartItem.", ex);
            }
        }

        public async Task<IEnumerable<CartItem>> GetByCartIdAsync(int cartId)
            => await ExecuteReaderToListAsync(
                "SELECT * FROM \"cart_items\" WHERE \"cart_id\" = @cartId",
                MapRow, new[] { CreateParameter("cartId", cartId) });

        public async Task<CartItem?> GetByIdAsync(int cartItemId)
        {
            var results = await ExecuteReaderToListAsync(
                "SELECT * FROM \"cart_items\" WHERE \"cart_item_id\" = @cartItemId",
                MapRow, new[] { CreateParameter("cartItemId", cartItemId) });
            return results.FirstOrDefault();
        }

        public async Task<int> AddAsync(CartItem entity)
        {
            const string sql = "INSERT INTO \"cart_items\" (\"cart_id\", \"product_id\", \"quantity\") " +
                               "VALUES (@cartId, @productId, @quantity); SELECT last_insert_rowid();";
            var id = await ExecuteScalarAsync<long>(sql, new[]
            {
                CreateParameter("cartId",    entity.CartId),
                CreateParameter("productId", entity.ProductId),
                CreateParameter("quantity",  entity.Quantity),
            });
            return (int)id;
        }

        public async Task UpdateQuantityAsync(int cartItemId, int quantity)
            => await ExecuteNonQueryAsync(
                "UPDATE \"cart_items\" SET \"quantity\" = @quantity WHERE \"cart_item_id\" = @cartItemId",
                new[] { CreateParameter("cartItemId", cartItemId), CreateParameter("quantity", quantity) });

        public async Task DeleteAsync(int cartItemId)
            => await ExecuteNonQueryAsync("DELETE FROM \"cart_items\" WHERE \"cart_item_id\" = @cartItemId",
                new[] { CreateParameter("cartItemId", cartItemId) });

        public async Task DeleteByCartIdAsync(int cartId)
            => await ExecuteNonQueryAsync("DELETE FROM \"cart_items\" WHERE \"cart_id\" = @cartId",
                new[] { CreateParameter("cartId", cartId) });

        /// <summary>
        /// Fixes the cart_items FK mismatch: the original DDL referenced cart(id) but cart's PK is cart_id.
        /// Recreates the table with the correct FK if needed.
        /// </summary>
        public async Task EnsureDatabaseMigratedAsync()
        {
            await using var conn = _db.GetConnection();
            await conn.OpenAsync();

            // Check if cart_items table exists
            string? tableExists;
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT sql FROM sqlite_master WHERE type='table' AND name='cart_items';";
                tableExists = (await cmd.ExecuteScalarAsync())?.ToString();
            }
            if (tableExists == null) return;

            // If the DDL already references cart(cart_id) it's already fixed
            if (tableExists.Contains("cart(cart_id)", StringComparison.OrdinalIgnoreCase))
                return;

            // Recreate with correct FK using the rename-copy-drop pattern
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
BEGIN;
ALTER TABLE ""cart_items"" RENAME TO ""cart_items_old"";
CREATE TABLE ""cart_items"" (
    cart_item_id INTEGER PRIMARY KEY AUTOINCREMENT,
    cart_id      INTEGER NOT NULL,
    product_id   INTEGER NOT NULL,
    quantity     INTEGER NOT NULL DEFAULT 1,
    FOREIGN KEY (cart_id)    REFERENCES cart(cart_id),
    FOREIGN KEY (product_id) REFERENCES products(id)
);
INSERT INTO ""cart_items"" (cart_item_id, cart_id, product_id, quantity)
    SELECT cart_item_id, cart_id, product_id, quantity FROM ""cart_items_old"";
DROP TABLE ""cart_items_old"";
COMMIT;";
                await cmd.ExecuteNonQueryAsync();
            }
        }
    }
}
