using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading.Tasks;
using ClothesShopping.Api.Main;

namespace ClothesShopping.Api.OrderTrackingModule
{
    public class OrderTrackingRepository : BaseRepository, IOrderTrackingRepository
    {
        public OrderTrackingRepository(MyCon dbConnection) : base(dbConnection) { }

        private OrderTracking MapRow(DbDataReader reader)
        {
            try
            {
                return new OrderTracking
                {
                    Id        = ReadValue<int>(reader, "id", 0),
                    OrderId   = ReadValue<int>(reader, "order_id", 0),
                    Status    = ReadValue<string>(reader, "status", string.Empty),
                    Latitude  = ReadValue<double>(reader, "latitude", 0.0),
                    Longitude = ReadValue<double>(reader, "longitude", 0.0),
                    Note      = ReadValue<string>(reader, "note", string.Empty),
                    CreatedAt = ReadValue<string>(reader, "created_at", string.Empty),
                };
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to map database row to OrderTracking.", ex);
            }
        }

        public async Task<IEnumerable<OrderTracking>> GetByOrderIdAsync(int orderId)
            => await ExecuteReaderToListAsync(
                "SELECT * FROM \"order_tracking\" WHERE \"order_id\" = @orderId ORDER BY \"id\" ASC",
                MapRow,
                new[] { CreateParameter("orderId", orderId) });

        public async Task<int> AddAsync(OrderTracking entity)
        {
            const string sql = "INSERT INTO \"order_tracking\" (\"order_id\", \"status\", \"latitude\", \"longitude\", \"note\", \"created_at\") " +
                               "VALUES (@orderId, @status, @latitude, @longitude, @note, @createdAt); SELECT last_insert_rowid();";

            var createdAt = string.IsNullOrWhiteSpace(entity.CreatedAt)
                ? DateTime.UtcNow.ToString("o")
                : entity.CreatedAt;

            var id = await ExecuteScalarAsync<long>(sql, new[]
            {
                CreateParameter("orderId",   entity.OrderId),
                CreateParameter("status",    entity.Status),
                CreateParameter("latitude",  entity.Latitude),
                CreateParameter("longitude", entity.Longitude),
                CreateParameter("note",      entity.Note as object ?? DBNull.Value),
                CreateParameter("createdAt", createdAt),
            });

            return (int)id;
        }

        public async Task EnsureDatabaseMigratedAsync()
        {
            await using var conn = _db.GetConnection();
            await conn.OpenAsync();

            // 1. Check columns in "address" table
            bool hasLatitude = false;
            bool hasLongitude = false;

            // 1. Check if "address" table exists
            bool addressTableExists = false;
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='address';";
                var count = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                addressTableExists = count > 0;
            }

            if (!addressTableExists)
            {
                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                    CREATE TABLE IF NOT EXISTS ""address"" (
                        ""address_id"" INTEGER PRIMARY KEY AUTOINCREMENT,
                        ""user_id"" INTEGER NOT NULL,
                        ""reciepient_name"" TEXT,
                        ""phone"" TEXT,
                        ""street"" TEXT,
                        ""city"" TEXT,
                        ""province"" TEXT,
                        ""postal_code"" TEXT,
                        ""latitude"" REAL,
                        ""longitude"" REAL
                    );
                ";
                await cmd.ExecuteNonQueryAsync();
            }
            else
            {
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "PRAGMA table_info(\"address\");";
                    using var reader = await cmd.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        var colName = reader["name"]?.ToString() ?? string.Empty;
                        if (string.Equals(colName, "latitude", StringComparison.OrdinalIgnoreCase))
                            hasLatitude = true;
                        if (string.Equals(colName, "longitude", StringComparison.OrdinalIgnoreCase))
                            hasLongitude = true;
                    }
                }

                if (!hasLatitude)
                {
                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = "ALTER TABLE \"address\" ADD COLUMN \"latitude\" REAL;";
                    await cmd.ExecuteNonQueryAsync();
                }

                if (!hasLongitude)
                {
                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = "ALTER TABLE \"address\" ADD COLUMN \"longitude\" REAL;";
                    await cmd.ExecuteNonQueryAsync();
                }
            }

            // 2. Create "order_tracking" table if it doesn't exist
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
                    CREATE TABLE IF NOT EXISTS ""order_tracking"" (
                        ""id"" INTEGER PRIMARY KEY AUTOINCREMENT,
                        ""order_id"" INTEGER NOT NULL,
                        ""status"" TEXT NOT NULL,
                        ""latitude"" REAL NOT NULL,
                        ""longitude"" REAL NOT NULL,
                        ""note"" TEXT,
                        ""created_at"" TEXT NOT NULL
                    );
                    CREATE INDEX IF NOT EXISTS ""idx_order_tracking_order_id"" ON ""order_tracking"" (""order_id"");
                ";
                await cmd.ExecuteNonQueryAsync();
            }
        }
    }
}
