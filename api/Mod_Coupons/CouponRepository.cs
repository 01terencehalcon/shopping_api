using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using ClothesShopping.Api.Main;

namespace ClothesShopping.Api.CouponsModule
{
    public class CouponRepository : BaseRepository, ICouponRepository
    {
        public CouponRepository(MyCon dbConnection) : base(dbConnection) { }

        private Coupon MapRow(DbDataReader reader)
        {
            try
            {
                return new Coupon
                {
                    CouponId       = ReadValue<int>(reader, "coupon_id", 0),
                    Code           = ReadValue<string>(reader, "code", string.Empty),
                    Discount       = ReadValue<double>(reader, "discount", 0),
                    ExpirationDate = ReadValue<string>(reader, "expiration_date", string.Empty),
                    Status         = ReadValue<string>(reader, "status", string.Empty),
                };
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to map database row to Coupon.", ex);
            }
        }

        public async Task<IEnumerable<Coupon>> GetAllAsync()
            => await ExecuteReaderToListAsync("SELECT * FROM \"coupons\"", MapRow);

        public async Task<Coupon?> GetByIdAsync(int couponId)
        {
            var results = await ExecuteReaderToListAsync(
                "SELECT * FROM \"coupons\" WHERE \"coupon_id\" = @couponId",
                MapRow, new[] { CreateParameter("couponId", couponId) });
            return results.FirstOrDefault();
        }

        public async Task<Coupon?> GetByCodeAsync(string code)
        {
            var results = await ExecuteReaderToListAsync(
                "SELECT * FROM \"coupons\" WHERE \"code\" = @code",
                MapRow, new[] { CreateParameter("code", code) });
            return results.FirstOrDefault();
        }

        public async Task<PaginationModel<Coupon>> GetPaginatedAsync(int pageNumber, int pageSize)
        {
            if (pageNumber <= 0) pageNumber = 1;
            if (pageSize <= 0)
            {
                var all = await ExecuteReaderToListAsync("SELECT * FROM \"coupons\"", MapRow);
                return new PaginationModel<Coupon> { Items = all, TotalCount = all.Count, PageSize = all.Count, CurrentPage = 1 };
            }

            var items = await ExecuteReaderToListAsync(
                "SELECT * FROM \"coupons\" ORDER BY \"coupon_id\" LIMIT @pageSize OFFSET @offset",
                MapRow,
                new[] { CreateParameter("offset", (pageNumber - 1) * pageSize), CreateParameter("pageSize", pageSize) });

            var total = await ExecuteScalarAsync<int>("SELECT COUNT(*) FROM \"coupons\"");
            return new PaginationModel<Coupon> { Items = items, TotalCount = total, PageSize = pageSize, CurrentPage = pageNumber };
        }

        public async Task<int> AddAsync(Coupon entity)
        {
            const string sql = "INSERT INTO \"coupons\" (\"code\", \"discount\", \"expiration_date\", \"status\") " +
                               "VALUES (@code, @discount, @expirationDate, @status); SELECT last_insert_rowid();";
            var id = await ExecuteScalarAsync<long>(sql, new[]
            {
                CreateParameter("code",           entity.Code           as object ?? DBNull.Value),
                CreateParameter("discount",       entity.Discount),
                CreateParameter("expirationDate", entity.ExpirationDate as object ?? DBNull.Value),
                CreateParameter("status",         entity.Status         as object ?? DBNull.Value),
            });
            return (int)id;
        }

        public async Task UpdateAsync(Coupon entity)
        {
            const string sql = "UPDATE \"coupons\" SET \"code\"=@code, \"discount\"=@discount, " +
                               "\"expiration_date\"=@expirationDate, \"status\"=@status WHERE \"coupon_id\"=@couponId";
            await ExecuteNonQueryAsync(sql, new[]
            {
                CreateParameter("couponId",       entity.CouponId),
                CreateParameter("code",           entity.Code           as object ?? DBNull.Value),
                CreateParameter("discount",       entity.Discount),
                CreateParameter("expirationDate", entity.ExpirationDate as object ?? DBNull.Value),
                CreateParameter("status",         entity.Status         as object ?? DBNull.Value),
            });
        }

        public async Task DeleteAsync(int couponId)
            => await ExecuteNonQueryAsync("DELETE FROM \"coupons\" WHERE \"coupon_id\" = @couponId",
                new[] { CreateParameter("couponId", couponId) });
    }
}
