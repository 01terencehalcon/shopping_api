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
                    Street        = ReadValue<string>(reader, "street", string.Empty),
                    City          = ReadValue<string>(reader, "city", string.Empty),
                    Province      = ReadValue<string>(reader, "province", string.Empty),
                    PostalCode    = ReadValue<string>(reader, "postal_code", string.Empty),
                };
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to map database row to Address.", ex);
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
            const string sql = "INSERT INTO \"address\" (\"user_id\", \"reciepient_name\", \"phone\", \"street\", \"city\", \"province\", \"postal_code\") " +
                               "VALUES (@userId, @recipientName, @phone, @street, @city, @province, @postalCode); SELECT last_insert_rowid();";
            var id = await ExecuteScalarAsync<long>(sql, new[]
            {
                CreateParameter("userId",        entity.UserId),
                CreateParameter("recipientName", entity.RecipientName as object ?? DBNull.Value),
                CreateParameter("phone",         entity.Phone         as object ?? DBNull.Value),
                CreateParameter("street",        entity.Street        as object ?? DBNull.Value),
                CreateParameter("city",          entity.City          as object ?? DBNull.Value),
                CreateParameter("province",      entity.Province      as object ?? DBNull.Value),
                CreateParameter("postalCode",    entity.PostalCode    as object ?? DBNull.Value),
            });
            return (int)id;
        }

        public async Task UpdateAsync(Address entity)
        {
            const string sql = "UPDATE \"address\" SET \"reciepient_name\"=@recipientName, \"phone\"=@phone, " +
                               "\"street\"=@street, \"city\"=@city, \"province\"=@province, \"postal_code\"=@postalCode " +
                               "WHERE \"address_id\"=@addressId";
            await ExecuteNonQueryAsync(sql, new[]
            {
                CreateParameter("addressId",     entity.AddressId),
                CreateParameter("recipientName", entity.RecipientName as object ?? DBNull.Value),
                CreateParameter("phone",         entity.Phone         as object ?? DBNull.Value),
                CreateParameter("street",        entity.Street        as object ?? DBNull.Value),
                CreateParameter("city",          entity.City          as object ?? DBNull.Value),
                CreateParameter("province",      entity.Province      as object ?? DBNull.Value),
                CreateParameter("postalCode",    entity.PostalCode    as object ?? DBNull.Value),
            });
        }

        public async Task DeleteAsync(int addressId)
            => await ExecuteNonQueryAsync("DELETE FROM \"address\" WHERE \"address_id\" = @addressId",
                new[] { CreateParameter("addressId", addressId) });

        public async Task DeleteByUserIdAsync(int userId)
            => await ExecuteNonQueryAsync("DELETE FROM \"address\" WHERE \"user_id\" = @userId",
                new[] { CreateParameter("userId", userId) });
    }
}
