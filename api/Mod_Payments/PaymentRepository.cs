using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using ClothesShopping.Api.Main;

namespace ClothesShopping.Api.PaymentsModule
{
    public class PaymentRepository : BaseRepository, IPaymentRepository
    {
        public PaymentRepository(MyCon dbConnection) : base(dbConnection) { }

        private Payment MapRow(DbDataReader reader)
        {
            try
            {
                return new Payment
                {
                    PaymentId     = ReadValue<int>(reader, "payment_id", 0),
                    OrderId       = ReadValue<int>(reader, "order_id", 0),
                    PaymentDate   = ReadValue<string>(reader, "payment_date", string.Empty),
                    Amount        = ReadValue<double>(reader, "amount", 0),
                    PaymentMethod = ReadValue<string>(reader, "payment_method", string.Empty),
                    PaymentStatus = ReadValue<string>(reader, "payment_status", "pending"),
                    CouponId      = ReadValue<int>(reader, "coupon_id", 0),
                };
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to map database row to Payment.", ex);
            }
        }

        public async Task<IEnumerable<Payment>> GetAllAsync()
            => await ExecuteReaderToListAsync("SELECT * FROM \"payments\"", MapRow);

        public async Task<Payment?> GetByIdAsync(int paymentId)
        {
            var results = await ExecuteReaderToListAsync(
                "SELECT * FROM \"payments\" WHERE \"payment_id\" = @paymentId",
                MapRow, new[] { CreateParameter("paymentId", paymentId) });
            return results.FirstOrDefault();
        }

        public async Task<IEnumerable<Payment>> GetByOrderIdAsync(int orderId)
            => await ExecuteReaderToListAsync(
                "SELECT * FROM \"payments\" WHERE \"order_id\" = @orderId",
                MapRow, new[] { CreateParameter("orderId", orderId) });

        public async Task<PaginationModel<Payment>> GetPaginatedAsync(int pageNumber, int pageSize)
        {
            if (pageNumber <= 0) pageNumber = 1;
            if (pageSize <= 0)
            {
                var all = await ExecuteReaderToListAsync("SELECT * FROM \"payments\"", MapRow);
                return new PaginationModel<Payment> { Items = all, TotalCount = all.Count, PageSize = all.Count, CurrentPage = 1 };
            }

            var items = await ExecuteReaderToListAsync(
                "SELECT * FROM \"payments\" ORDER BY \"payment_id\" LIMIT @pageSize OFFSET @offset",
                MapRow,
                new[] { CreateParameter("offset", (pageNumber - 1) * pageSize), CreateParameter("pageSize", pageSize) });

            var total = await ExecuteScalarAsync<int>("SELECT COUNT(*) FROM \"payments\"");
            return new PaginationModel<Payment> { Items = items, TotalCount = total, PageSize = pageSize, CurrentPage = pageNumber };
        }

        public async Task<int> AddAsync(Payment entity)
        {
            const string sql = "INSERT INTO \"payments\" (\"order_id\", \"amount\", \"payment_method\", \"payment_status\", \"coupon_id\") " +
                               "VALUES (@orderId, @amount, @paymentMethod, @paymentStatus, @couponId); SELECT last_insert_rowid();";
            var id = await ExecuteScalarAsync<long>(sql, new[]
            {
                CreateParameter("orderId",       entity.OrderId),
                CreateParameter("amount",        entity.Amount),
                CreateParameter("paymentMethod", entity.PaymentMethod),
                CreateParameter("paymentStatus", entity.PaymentStatus),
                CreateParameter("couponId",      entity.CouponId),
            });
            return (int)id;
        }

        public async Task UpdateStatusAsync(int paymentId, string status)
            => await ExecuteNonQueryAsync(
                "UPDATE \"payments\" SET \"payment_status\" = @status WHERE \"payment_id\" = @paymentId",
                new[] { CreateParameter("paymentId", paymentId), CreateParameter("status", status) });

        public async Task DeleteAsync(int paymentId)
            => await ExecuteNonQueryAsync("DELETE FROM \"payments\" WHERE \"payment_id\" = @paymentId",
                new[] { CreateParameter("paymentId", paymentId) });
    }
}
