using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using ClothesShopping.Api.Main;

namespace ClothesShopping.Api.OrdersModule
{
    public class OrderRepository : BaseRepository, IOrderRepository
    {
        public OrderRepository(MyCon dbConnection) : base(dbConnection) { }

        private Order MapRow(DbDataReader reader)
        {
            try
            {
                return new Order
                {
                    Id        = ReadValue<int>(reader, "id", 0),
                    UserId    = ReadValue<int>(reader, "user_id", 0),
                    Total     = ReadValue<double>(reader, "total", 0),
                    OrderDate = ReadValue<string>(reader, "order_date", string.Empty),
                    Status    = ReadValue<string>(reader, "status", string.Empty),
                };
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to map database row to Order.", ex);
            }
        }

        public async Task<IEnumerable<Order>> GetAllAsync()
            => await ExecuteReaderToListAsync("SELECT * FROM \"orders\"", MapRow);

        public async Task<Order?> GetByIdAsync(int id)
        {
            var results = await ExecuteReaderToListAsync(
                "SELECT * FROM \"orders\" WHERE \"id\" = @id",
                MapRow,
                new[] { CreateParameter("id", id) });
            return results.FirstOrDefault();
        }

        public async Task<IEnumerable<Order>> GetByUserIdAsync(int userId)
            => await ExecuteReaderToListAsync(
                "SELECT * FROM \"orders\" WHERE \"user_id\" = @userId",
                MapRow,
                new[] { CreateParameter("userId", userId) });

        public async Task<PaginationModel<Order>> GetPaginatedAsync(int pageNumber, int pageSize)
        {
            if (pageNumber <= 0) pageNumber = 1;
            pageSize = Math.Clamp(pageSize, 1, 100);

            var items = await ExecuteReaderToListAsync(
                "SELECT * FROM \"orders\" ORDER BY \"id\" LIMIT @pageSize OFFSET @offset",
                MapRow,
                new[] { CreateParameter("offset", (pageNumber - 1) * pageSize), CreateParameter("pageSize", pageSize) });

            var total = await ExecuteScalarAsync<int>("SELECT COUNT(*) FROM \"orders\"");
            return new PaginationModel<Order> { Items = items, TotalCount = total, PageSize = pageSize, CurrentPage = pageNumber };
        }

        public async Task<int> AddAsync(Order entity)
        {
            const string sql = "INSERT INTO \"orders\" (\"user_id\", \"total\", \"order_date\", \"status\") " +
                               "VALUES (@userId, @total, @orderDate, @status); SELECT last_insert_rowid();";
            var id = await ExecuteScalarAsync<long>(sql, new[]
            {
                CreateParameter("userId",    entity.UserId),
                CreateParameter("total",     entity.Total),
                CreateParameter("orderDate", string.IsNullOrWhiteSpace(entity.OrderDate) ? DateTime.UtcNow.ToString("o") : entity.OrderDate),
                CreateParameter("status",    entity.Status),
            });
            return (int)id;
        }

        public async Task UpdateAsync(Order entity)
        {
            const string sql = "UPDATE \"orders\" SET \"user_id\"=@userId, \"total\"=@total, " +
                               "\"order_date\"=@orderDate, \"status\"=@status WHERE \"id\"=@id";
            await ExecuteNonQueryAsync(sql, new[]
            {
                CreateParameter("id",        entity.Id),
                CreateParameter("userId",    entity.UserId),
                CreateParameter("total",     entity.Total),
                CreateParameter("orderDate", entity.OrderDate),
                CreateParameter("status",    entity.Status),
            });
        }

        public async Task DeleteAsync(int id)
            => await ExecuteNonQueryAsync("DELETE FROM \"orders\" WHERE \"id\" = @id",
                new[] { CreateParameter("id", id) });

        public async Task<int> CreateOrderTransactionalAsync(
            int userId,
            double total,
            IReadOnlyList<(int ProductId, int Quantity, double Price)> items,
            int cartId)
        {
            return await WithTransactionAsync<int>(async (conn, tx) =>
            {
                // 1. Decrement stock for each item — fails if insufficient
                foreach (var (productId, quantity, _) in items)
                {
                    var affected = await ExecuteNonQueryAsync(
                        conn, tx,
                        "UPDATE \"products\" SET \"stock\" = \"stock\" - @qty WHERE \"id\" = @id AND \"stock\" >= @qty",
                        new[] { CreateParameter("id", productId), CreateParameter("qty", quantity) });

                    if (affected == 0)
                        throw new InvalidOperationException($"Insufficient stock for product {productId}.");
                }

                // 2. Create the order
                var orderId = await ExecuteScalarAsync<long>(
                    conn, tx,
                    "INSERT INTO \"orders\" (\"user_id\", \"total\", \"order_date\", \"status\") " +
                    "VALUES (@userId, @total, @orderDate, @status); SELECT last_insert_rowid();",
                    new[]
                    {
                        CreateParameter("userId",    userId),
                        CreateParameter("total",     total),
                        CreateParameter("orderDate", DateTime.UtcNow.ToString("o")),
                        CreateParameter("status",    "pending"),
                    });

                // 3. Create order items
                foreach (var (productId, quantity, price) in items)
                {
                    await ExecuteNonQueryAsync(
                        conn, tx,
                        "INSERT INTO \"order_items\" (\"order_id\", \"product_id\", \"quantity\", \"price\") " +
                        "VALUES (@orderId, @productId, @quantity, @price)",
                        new[]
                        {
                            CreateParameter("orderId",   (int)orderId),
                            CreateParameter("productId", productId),
                            CreateParameter("quantity",  quantity),
                            CreateParameter("price",     price),
                        });
                }

                // 4. Clear the cart
                await ExecuteNonQueryAsync(
                    conn, tx,
                    "DELETE FROM \"cart_items\" WHERE \"cart_id\" = @cartId",
                    new[] { CreateParameter("cartId", cartId) });

                return (int)orderId;
            });
        }
    }
}
