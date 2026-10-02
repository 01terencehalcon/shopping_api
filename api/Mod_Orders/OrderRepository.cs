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
            if (pageSize <= 0)
            {
                var all = await ExecuteReaderToListAsync("SELECT * FROM \"orders\"", MapRow);
                return new PaginationModel<Order> { Items = all, TotalCount = all.Count, PageSize = all.Count, CurrentPage = 1 };
            }

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
    }
}
