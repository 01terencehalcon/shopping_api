using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using ClothesShopping.Api.Main;

namespace ClothesShopping.Api.NotificationsModule
{
    public class NotificationRepository : BaseRepository, INotificationRepository
    {
        public NotificationRepository(MyCon dbConnection) : base(dbConnection) { }

        private Notification MapRow(DbDataReader reader)
        {
            try
            {
                return new Notification
                {
                    NotificationId = ReadValue<int>(reader, "notification_id", 0),
                    UserId         = ReadValue<int>(reader, "user_id", 0),
                    Message        = ReadValue<string>(reader, "message", string.Empty),
                    IsRead         = ReadValue<int>(reader, "is_read", 0),
                    CreatedAt      = ReadValue<string>(reader, "created_at", string.Empty),
                };
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to map database row to Notification.", ex);
            }
        }

        public async Task<IEnumerable<Notification>> GetAllAsync()
            => await ExecuteReaderToListAsync("SELECT * FROM \"notifications\"", MapRow);

        public async Task<Notification?> GetByIdAsync(int notificationId)
        {
            var results = await ExecuteReaderToListAsync(
                "SELECT * FROM \"notifications\" WHERE \"notification_id\" = @notificationId",
                MapRow, new[] { CreateParameter("notificationId", notificationId) });
            return results.FirstOrDefault();
        }

        public async Task<IEnumerable<Notification>> GetByUserIdAsync(int userId)
            => await ExecuteReaderToListAsync(
                "SELECT * FROM \"notifications\" WHERE \"user_id\" = @userId ORDER BY \"created_at\" DESC",
                MapRow, new[] { CreateParameter("userId", userId) });

        public async Task<PaginationModel<Notification>> GetPaginatedAsync(int pageNumber, int pageSize)
        {
            if (pageNumber <= 0) pageNumber = 1;
            if (pageSize <= 0)
            {
                var all = await ExecuteReaderToListAsync("SELECT * FROM \"notifications\"", MapRow);
                return new PaginationModel<Notification> { Items = all, TotalCount = all.Count, PageSize = all.Count, CurrentPage = 1 };
            }

            var items = await ExecuteReaderToListAsync(
                "SELECT * FROM \"notifications\" ORDER BY \"notification_id\" LIMIT @pageSize OFFSET @offset",
                MapRow,
                new[] { CreateParameter("offset", (pageNumber - 1) * pageSize), CreateParameter("pageSize", pageSize) });

            var total = await ExecuteScalarAsync<int>("SELECT COUNT(*) FROM \"notifications\"");
            return new PaginationModel<Notification> { Items = items, TotalCount = total, PageSize = pageSize, CurrentPage = pageNumber };
        }

        public async Task<int> AddAsync(Notification entity)
        {
            const string sql = "INSERT INTO \"notifications\" (\"user_id\", \"message\", \"is_read\") " +
                               "VALUES (@userId, @message, @isRead); SELECT last_insert_rowid();";
            var id = await ExecuteScalarAsync<long>(sql, new[]
            {
                CreateParameter("userId",  entity.UserId),
                CreateParameter("message", entity.Message as object ?? DBNull.Value),
                CreateParameter("isRead",  entity.IsRead),
            });
            return (int)id;
        }

        public async Task MarkAsReadAsync(int notificationId)
            => await ExecuteNonQueryAsync(
                "UPDATE \"notifications\" SET \"is_read\" = 1 WHERE \"notification_id\" = @notificationId",
                new[] { CreateParameter("notificationId", notificationId) });

        public async Task DeleteAsync(int notificationId)
            => await ExecuteNonQueryAsync("DELETE FROM \"notifications\" WHERE \"notification_id\" = @notificationId",
                new[] { CreateParameter("notificationId", notificationId) });

        public async Task DeleteByUserIdAsync(int userId)
            => await ExecuteNonQueryAsync("DELETE FROM \"notifications\" WHERE \"user_id\" = @userId",
                new[] { CreateParameter("userId", userId) });
    }
}
