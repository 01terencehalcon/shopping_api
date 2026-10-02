using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using ClothesShopping.Api.Main;

namespace ClothesShopping.Api.OrderItemsModule
{
    public class OrderItemRepository : BaseRepository, IOrderItemRepository
    {
        public OrderItemRepository(MyCon dbConnection) : base(dbConnection) { }

        private OrderItem MapRow(DbDataReader reader)
        {
            try
            {
                return new OrderItem
                {
                    Id        = ReadValue<int>(reader, "id", 0),
                    OrderId   = ReadValue<int>(reader, "order_id", 0),
                    ProductId = ReadValue<int>(reader, "product_id", 0),
                    Quantity  = ReadValue<int>(reader, "quantity", 0),
                    Price     = ReadValue<double>(reader, "price", 0),
                };
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to map database row to OrderItem.", ex);
            }
        }

        public async Task<IEnumerable<OrderItem>> GetAllAsync()
            => await ExecuteReaderToListAsync("SELECT * FROM \"order_items\"", MapRow);

        public async Task<OrderItem?> GetByIdAsync(int id)
        {
            var results = await ExecuteReaderToListAsync(
                "SELECT * FROM \"order_items\" WHERE \"id\" = @id",
                MapRow, new[] { CreateParameter("id", id) });
            return results.FirstOrDefault();
        }

        public async Task<IEnumerable<OrderItem>> GetByOrderIdAsync(int orderId)
            => await ExecuteReaderToListAsync(
                "SELECT * FROM \"order_items\" WHERE \"order_id\" = @orderId",
                MapRow, new[] { CreateParameter("orderId", orderId) });

        public async Task AddAsync(OrderItem entity)
        {
            const string sql = "INSERT INTO \"order_items\" (\"order_id\", \"product_id\", \"quantity\", \"price\") " +
                               "VALUES (@orderId, @productId, @quantity, @price)";
            await ExecuteNonQueryAsync(sql, new[]
            {
                CreateParameter("orderId",   entity.OrderId),
                CreateParameter("productId", entity.ProductId),
                CreateParameter("quantity",  entity.Quantity),
                CreateParameter("price",     entity.Price),
            });
        }

        public async Task UpdateAsync(OrderItem entity)
        {
            const string sql = "UPDATE \"order_items\" SET \"order_id\"=@orderId, \"product_id\"=@productId, " +
                               "\"quantity\"=@quantity, \"price\"=@price WHERE \"id\"=@id";
            await ExecuteNonQueryAsync(sql, new[]
            {
                CreateParameter("id",        entity.Id),
                CreateParameter("orderId",   entity.OrderId),
                CreateParameter("productId", entity.ProductId),
                CreateParameter("quantity",  entity.Quantity),
                CreateParameter("price",     entity.Price),
            });
        }

        public async Task DeleteAsync(int id)
            => await ExecuteNonQueryAsync("DELETE FROM \"order_items\" WHERE \"id\" = @id",
                new[] { CreateParameter("id", id) });

        public async Task DeleteByOrderIdAsync(int orderId)
            => await ExecuteNonQueryAsync("DELETE FROM \"order_items\" WHERE \"order_id\" = @orderId",
                new[] { CreateParameter("orderId", orderId) });
    }
}
