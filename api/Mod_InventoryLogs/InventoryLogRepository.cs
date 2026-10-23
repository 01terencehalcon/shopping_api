using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using ClothesShopping.Api.Main;

namespace ClothesShopping.Api.InventoryLogsModule
{
    public class InventoryLogRepository : BaseRepository, IInventoryLogRepository
    {
        public InventoryLogRepository(MyCon dbConnection) : base(dbConnection) { }

        private InventoryLog MapRow(DbDataReader reader)
        {
            try
            {
                return new InventoryLog
                {
                    LogId          = ReadValue<int>(reader, "log_id", 0),
                    ProductId      = ReadValue<int>(reader, "product_id", 0),
                    ChangeQuantity = ReadValue<int>(reader, "change_quantity", 0),
                    Action         = ReadValue<string>(reader, "action", string.Empty),
                    LogDate        = ReadValue<string>(reader, "log_date", string.Empty),
                };
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to map database row to InventoryLog.", ex);
            }
        }

        public async Task<IEnumerable<InventoryLog>> GetAllAsync()
            => await ExecuteReaderToListAsync("SELECT * FROM \"inventory_logs\"", MapRow);

        public async Task<InventoryLog?> GetByIdAsync(int logId)
        {
            var results = await ExecuteReaderToListAsync(
                "SELECT * FROM \"inventory_logs\" WHERE \"log_id\" = @logId",
                MapRow, new[] { CreateParameter("logId", logId) });
            return results.FirstOrDefault();
        }

        public async Task<IEnumerable<InventoryLog>> GetByProductIdAsync(int productId)
            => await ExecuteReaderToListAsync(
                "SELECT * FROM \"inventory_logs\" WHERE \"product_id\" = @productId ORDER BY \"log_date\" DESC",
                MapRow, new[] { CreateParameter("productId", productId) });

        public async Task<PaginationModel<InventoryLog>> GetPaginatedAsync(int pageNumber, int pageSize)
        {
            if (pageNumber <= 0) pageNumber = 1;
            pageSize = Math.Clamp(pageSize, 1, 100);

            var items = await ExecuteReaderToListAsync(
                "SELECT * FROM \"inventory_logs\" ORDER BY \"log_id\" DESC LIMIT @pageSize OFFSET @offset",
                MapRow,
                new[] { CreateParameter("offset", (pageNumber - 1) * pageSize), CreateParameter("pageSize", pageSize) });

            var total = await ExecuteScalarAsync<int>("SELECT COUNT(*) FROM \"inventory_logs\"");
            return new PaginationModel<InventoryLog> { Items = items, TotalCount = total, PageSize = pageSize, CurrentPage = pageNumber };
        }

        public async Task<int> AddAsync(InventoryLog entity)
        {
            const string sql = "INSERT INTO \"inventory_logs\" (\"product_id\", \"change_quantity\", \"action\") " +
                               "VALUES (@productId, @changeQuantity, @action); SELECT last_insert_rowid();";
            var id = await ExecuteScalarAsync<long>(sql, new[]
            {
                CreateParameter("productId",      entity.ProductId),
                CreateParameter("changeQuantity", entity.ChangeQuantity),
                CreateParameter("action",         entity.Action as object ?? DBNull.Value),
            });
            return (int)id;
        }

        public async Task DeleteAsync(int logId)
            => await ExecuteNonQueryAsync("DELETE FROM \"inventory_logs\" WHERE \"log_id\" = @logId",
                new[] { CreateParameter("logId", logId) });
    }
}
