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

        private static double? ReadNullableDouble(DbDataReader reader, string col)
        {
            try
            {
                int ord = reader.GetOrdinal(col);
                if (ord < 0 || reader.IsDBNull(ord)) return null;
                return Convert.ToDouble(reader.GetValue(ord), System.Globalization.CultureInfo.InvariantCulture);
            }
            catch
            {
                return null;
            }
        }

        private Order MapRow(DbDataReader reader)
        {
            try
            {
                return new Order
                {
                    Id             = ReadValue<int>(reader, "id", 0),
                    UserId         = ReadValue<int>(reader, "user_id", 0),
                    Total          = ReadValue<double>(reader, "total", 0),
                    OrderDate      = ReadValue<string>(reader, "order_date", string.Empty),
                    Status         = ReadValue<string>(reader, "status", string.Empty),
                    ShipRecipient  = ReadValue<string?>(reader, "ship_recipient", null),
                    ShipPhone      = ReadValue<string?>(reader, "ship_phone", null),
                    ShipStreet     = ReadValue<string?>(reader, "ship_street", null),
                    ShipCity       = ReadValue<string?>(reader, "ship_city", null),
                    ShipProvince   = ReadValue<string?>(reader, "ship_province", null),
                    ShipPostalCode = ReadValue<string?>(reader, "ship_postal_code", null),
                    ShipLatitude   = ReadNullableDouble(reader, "ship_latitude"),
                    ShipLongitude  = ReadNullableDouble(reader, "ship_longitude"),
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
            const string sql = "INSERT INTO \"orders\" (\"user_id\", \"total\", \"order_date\", \"status\", " +
                               "\"ship_recipient\", \"ship_phone\", \"ship_street\", \"ship_city\", \"ship_province\", \"ship_postal_code\", \"ship_latitude\", \"ship_longitude\") " +
                               "VALUES (@userId, @total, @orderDate, @status, @shipRecipient, @shipPhone, @shipStreet, @shipCity, @shipProvince, @shipPostalCode, @shipLat, @shipLng); SELECT last_insert_rowid();";
            var id = await ExecuteScalarAsync<long>(sql, new[]
            {
                CreateParameter("userId",         entity.UserId),
                CreateParameter("total",          entity.Total),
                CreateParameter("orderDate",      string.IsNullOrWhiteSpace(entity.OrderDate) ? DateTime.UtcNow.ToString("o") : entity.OrderDate),
                CreateParameter("status",         entity.Status),
                CreateParameter("shipRecipient",  entity.ShipRecipient  as object ?? DBNull.Value),
                CreateParameter("shipPhone",      entity.ShipPhone      as object ?? DBNull.Value),
                CreateParameter("shipStreet",     entity.ShipStreet     as object ?? DBNull.Value),
                CreateParameter("shipCity",       entity.ShipCity       as object ?? DBNull.Value),
                CreateParameter("shipProvince",   entity.ShipProvince   as object ?? DBNull.Value),
                CreateParameter("shipPostalCode", entity.ShipPostalCode as object ?? DBNull.Value),
                CreateParameter("shipLat",        entity.ShipLatitude   as object ?? DBNull.Value),
                CreateParameter("shipLng",        entity.ShipLongitude  as object ?? DBNull.Value),
            });
            return (int)id;
        }

        public async Task UpdateAsync(Order entity)
        {
            const string sql = "UPDATE \"orders\" SET \"user_id\"=@userId, \"total\"=@total, " +
                               "\"order_date\"=@orderDate, \"status\"=@status, " +
                               "\"ship_recipient\"=@shipRecipient, \"ship_phone\"=@shipPhone, " +
                               "\"ship_street\"=@shipStreet, \"ship_city\"=@shipCity, " +
                               "\"ship_province\"=@shipProvince, \"ship_postal_code\"=@shipPostalCode, " +
                               "\"ship_latitude\"=@shipLat, \"ship_longitude\"=@shipLng WHERE \"id\"=@id";
            await ExecuteNonQueryAsync(sql, new[]
            {
                CreateParameter("id",             entity.Id),
                CreateParameter("userId",         entity.UserId),
                CreateParameter("total",          entity.Total),
                CreateParameter("orderDate",      entity.OrderDate),
                CreateParameter("status",         entity.Status),
                CreateParameter("shipRecipient",  entity.ShipRecipient  as object ?? DBNull.Value),
                CreateParameter("shipPhone",      entity.ShipPhone      as object ?? DBNull.Value),
                CreateParameter("shipStreet",     entity.ShipStreet     as object ?? DBNull.Value),
                CreateParameter("shipCity",       entity.ShipCity       as object ?? DBNull.Value),
                CreateParameter("shipProvince",   entity.ShipProvince   as object ?? DBNull.Value),
                CreateParameter("shipPostalCode", entity.ShipPostalCode as object ?? DBNull.Value),
                CreateParameter("shipLat",        entity.ShipLatitude   as object ?? DBNull.Value),
                CreateParameter("shipLng",        entity.ShipLongitude  as object ?? DBNull.Value),
            });
        }

        public async Task DeleteAsync(int id)
            => await ExecuteNonQueryAsync("DELETE FROM \"orders\" WHERE \"id\" = @id",
                new[] { CreateParameter("id", id) });

        public async Task<int> CreateOrderTransactionalAsync(
            int userId,
            double total,
            IReadOnlyList<(int ProductId, int Quantity, double Price)> items,
            int cartId,
            AddressModule.Address? shippingAddress = null)
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

                // 2. Create the order with delivery snapshot
                var orderId = await ExecuteScalarAsync<long>(
                    conn, tx,
                    "INSERT INTO \"orders\" (\"user_id\", \"total\", \"order_date\", \"status\", " +
                    "\"ship_recipient\", \"ship_phone\", \"ship_street\", \"ship_city\", \"ship_province\", \"ship_postal_code\", \"ship_latitude\", \"ship_longitude\") " +
                    "VALUES (@userId, @total, @orderDate, @status, @shipRecipient, @shipPhone, @shipStreet, @shipCity, @shipProvince, @shipPostalCode, @shipLat, @shipLng); SELECT last_insert_rowid();",
                    new[]
                    {
                        CreateParameter("userId",         userId),
                        CreateParameter("total",          total),
                        CreateParameter("orderDate",      DateTime.UtcNow.ToString("o")),
                        CreateParameter("status",         "pending"),
                        CreateParameter("shipRecipient",  shippingAddress?.RecipientName as object ?? DBNull.Value),
                        CreateParameter("shipPhone",      shippingAddress?.Phone         as object ?? DBNull.Value),
                        CreateParameter("shipStreet",     shippingAddress?.Street        as object ?? DBNull.Value),
                        CreateParameter("shipCity",       shippingAddress?.City          as object ?? DBNull.Value),
                        CreateParameter("shipProvince",   shippingAddress?.Province      as object ?? DBNull.Value),
                        CreateParameter("shipPostalCode", shippingAddress?.PostalCode    as object ?? DBNull.Value),
                        CreateParameter("shipLat",        shippingAddress?.Latitude      as object ?? DBNull.Value),
                        CreateParameter("shipLng",        shippingAddress?.Longitude     as object ?? DBNull.Value),
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

        public async Task EnsureDatabaseMigratedAsync()
        {
            await using var conn = _db.GetConnection();
            await conn.OpenAsync();

            bool tableExists = false;
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT 1 FROM sqlite_master WHERE type='table' AND name='orders';";
                var res = await cmd.ExecuteScalarAsync();
                tableExists = res != null;
            }
            if (!tableExists) return;

            var existingCols = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "PRAGMA table_info(\"orders\");";
                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var colName = reader["name"]?.ToString();
                    if (!string.IsNullOrEmpty(colName)) existingCols.Add(colName);
                }
            }

            var migrations = new (string col, string type)[]
            {
                ("ship_recipient", "TEXT"),
                ("ship_phone", "TEXT"),
                ("ship_street", "TEXT"),
                ("ship_city", "TEXT"),
                ("ship_province", "TEXT"),
                ("ship_postal_code", "TEXT"),
                ("ship_latitude", "REAL"),
                ("ship_longitude", "REAL")
            };

            foreach (var (col, type) in migrations)
            {
                if (!existingCols.Contains(col))
                {
                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = $"ALTER TABLE \"orders\" ADD COLUMN \"{col}\" {type};";
                    await cmd.ExecuteNonQueryAsync();
                }
            }
        }
    }
}
