using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using ClothesShopping.Api.Main;

namespace ClothesShopping.Api.AddressModule
{
    public class AddressRepository : BaseRepository, IAddressRepository
    {
        public AddressRepository(MyCon dbConnection) : base(dbConnection) { }

        private Address MapRow(DbDataReader reader)
        {
            try
            {
                return new Address
                {
                    AddressId     = ReadValue<int>(reader, "address_id", 0),
                    UserId        = ReadValue<int>(reader, "user_id", 0),
                    RecipientName = ReadValue<string>(reader, "reciepient_name", string.Empty),
                    Phone         = ReadValue<string>(reader, "phone", string.Empty),
                    Region        = ReadValue<string>(reader, "region", string.Empty),
                    Province      = ReadValue<string>(reader, "province", string.Empty),
                    City          = ReadValue<string>(reader, "city", string.Empty),
                    Barangay      = ReadValue<string>(reader, "barangay", string.Empty),
                    Street        = ReadValue<string>(reader, "street", string.Empty),
                    PostalCode    = ReadValue<string>(reader, "postal_code", string.Empty),
                    Latitude      = ReadNullableDouble(reader, "latitude"),
                    Longitude     = ReadNullableDouble(reader, "longitude"),
                };
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to map database row to Address.", ex);
            }
        }

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

        public async Task<IEnumerable<Address>> GetByUserIdAsync(int userId)
            => await ExecuteReaderToListAsync(
                "SELECT * FROM \"address\" WHERE \"user_id\" = @userId",
                MapRow, new[] { CreateParameter("userId", userId) });

        public async Task<Address?> GetByIdAsync(int addressId)
        {
            var results = await ExecuteReaderToListAsync(
                "SELECT * FROM \"address\" WHERE \"address_id\" = @addressId",
                MapRow, new[] { CreateParameter("addressId", addressId) });
            return results.FirstOrDefault();
        }

        public async Task<int> AddAsync(Address entity)
        {
            const string sql = "INSERT INTO \"address\" (\"user_id\", \"reciepient_name\", \"phone\", \"region\", \"province\", \"city\", \"barangay\", \"street\", \"postal_code\", \"latitude\", \"longitude\") " +
                               "VALUES (@userId, @recipientName, @phone, @region, @province, @city, @barangay, @street, @postalCode, @lat, @lng); SELECT last_insert_rowid();";
            var id = await ExecuteScalarAsync<long>(sql, new[]
            {
                CreateParameter("userId",        entity.UserId),
                CreateParameter("recipientName", entity.RecipientName as object ?? DBNull.Value),
                CreateParameter("phone",         entity.Phone         as object ?? DBNull.Value),
                CreateParameter("region",        entity.Region        as object ?? DBNull.Value),
                CreateParameter("province",      entity.Province      as object ?? DBNull.Value),
                CreateParameter("city",          entity.City          as object ?? DBNull.Value),
                CreateParameter("barangay",      entity.Barangay      as object ?? DBNull.Value),
                CreateParameter("street",        entity.Street        as object ?? DBNull.Value),
                CreateParameter("postalCode",    entity.PostalCode    as object ?? DBNull.Value),
                CreateParameter("lat",           entity.Latitude      as object ?? DBNull.Value),
                CreateParameter("lng",           entity.Longitude     as object ?? DBNull.Value),
            });
            return (int)id;
        }

        public async Task UpdateAsync(Address entity)
        {
            const string sql = "UPDATE \"address\" SET \"reciepient_name\"=@recipientName, \"phone\"=@phone, " +
                               "\"region\"=@region, \"province\"=@province, \"city\"=@city, \"barangay\"=@barangay, \"street\"=@street, \"postal_code\"=@postalCode, " +
                               "\"latitude\"=@lat, \"longitude\"=@lng " +
                               "WHERE \"address_id\"=@addressId";
            await ExecuteNonQueryAsync(sql, new[]
            {
                CreateParameter("addressId",     entity.AddressId),
                CreateParameter("recipientName", entity.RecipientName as object ?? DBNull.Value),
                CreateParameter("phone",         entity.Phone         as object ?? DBNull.Value),
                CreateParameter("region",        entity.Region        as object ?? DBNull.Value),
                CreateParameter("province",      entity.Province      as object ?? DBNull.Value),
                CreateParameter("city",          entity.City          as object ?? DBNull.Value),
                CreateParameter("barangay",      entity.Barangay      as object ?? DBNull.Value),
                CreateParameter("street",        entity.Street        as object ?? DBNull.Value),
                CreateParameter("postalCode",    entity.PostalCode    as object ?? DBNull.Value),
                CreateParameter("lat",           entity.Latitude      as object ?? DBNull.Value),
                CreateParameter("lng",           entity.Longitude     as object ?? DBNull.Value),
            });
        }

        public async Task DeleteAsync(int addressId)
            => await ExecuteNonQueryAsync("DELETE FROM \"address\" WHERE \"address_id\" = @addressId",
                new[] { CreateParameter("addressId", addressId) });

        public async Task DeleteByUserIdAsync(int userId)
            => await ExecuteNonQueryAsync("DELETE FROM \"address\" WHERE \"user_id\" = @userId",
                new[] { CreateParameter("userId", userId) });

        public async Task EnsureDatabaseMigratedAsync()
        {
            await using var conn = _db.GetConnection();
            await conn.OpenAsync();

            bool tableExists = false;
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT 1 FROM sqlite_master WHERE type='table' AND name='address';";
                var res = await cmd.ExecuteScalarAsync();
                tableExists = res != null;
            }
            if (!tableExists) return;

            bool hasLatitude = false;
            bool hasLongitude = false;

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
    }
}
