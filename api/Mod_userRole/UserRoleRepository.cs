using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Data.Common;
using ClothesShopping.Api.Main;

namespace ClothesShopping.Api.UserroleModule
{
    public class UserroleRepository : BaseRepository, IUserroleRepository
    {
        public UserroleRepository(MyCon dbConnection) : base(dbConnection) { }


        private Userrole MapReaderToUserrole(DbDataReader reader)
        {
            try
            {
                return new Userrole()
                {
                    UserRoleId = ReadValue<int>(reader, "user_role_id", 0),
                    UserRole = ReadValue<string>(reader, "user_role", string.Empty),
                };
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    "Failed to map database row to Userrole. Check schema/type alignment for generated columns.",
                    ex);
            }
        }

        /// <summary>
        /// Retrieves all entities from the database
        /// </summary>
        public async Task<IEnumerable<Userrole>> GetAllAsync()
        {
            return await ExecuteReaderToListAsync("SELECT * FROM \"UserRole\"", MapReaderToUserrole);
        }

        /// <summary>
        /// Retrieves an entity by UserRoleId
        /// </summary>
        public async Task<Userrole?> GetByIdAsync(int userRoleId)
        {
            var results = await ExecuteReaderToListAsync(
                "SELECT * FROM \"UserRole\" WHERE \"user_role_id\" = @userRoleId", 
                MapReaderToUserrole, 
                new[] { CreateParameter("userRoleId", userRoleId) });
            return results.FirstOrDefault();
        }


        /// <summary>
        /// Adds a new entity to the database
        /// </summary>
        public async Task AddAsync(Userrole entity)
        {
            var sql = "INSERT INTO \"UserRole\" (\"user_role\") VALUES (@userRole)";
            var dbParameters = new List<DbParameter>();
            dbParameters.Add(CreateParameter("userRole", entity.UserRole));
            await ExecuteNonQueryAsync(sql, dbParameters.ToArray());
        }

        /// <summary>
        /// Updates an existing entity in the database
        /// </summary>
        public async Task UpdateAsync(Userrole entity)
        {
            var sql = "UPDATE \"UserRole\" SET \"user_role\" = @userRole WHERE \"user_role_id\" = @userRoleId";
            var dbParameters = new List<DbParameter>();
            dbParameters.Add(CreateParameter("userRoleId", entity.UserRoleId));
            dbParameters.Add(CreateParameter("userRole", entity.UserRole));
            await ExecuteNonQueryAsync(sql, dbParameters.ToArray());
        }

        /// <summary>
        /// Deletes an entity from the database by its UserRoleId
        /// </summary>
        public async Task DeleteAsync(int userRoleId)
        {
            await ExecuteNonQueryAsync("DELETE FROM \"UserRole\" WHERE \"user_role_id\" = @userRoleId", new[] { CreateParameter("userRoleId", userRoleId) });
        }

        /// <summary>
        /// Deletes all entities from the database
        /// </summary>
        public async Task DeleteAllAsync()
        {
            await ExecuteNonQueryAsync("DELETE FROM \"UserRole\"");
        }

        private static int? GetFilterInt(IReadOnlyDictionary<string, object?> filters, string key)
        {
            if (!filters.TryGetValue(key, out object? rawValue) || rawValue == null)
            {
                return null;
            }

            if (rawValue is int intValue)
            {
                return intValue;
            }

            string text = Convert.ToString(rawValue, CultureInfo.InvariantCulture) ?? string.Empty;
            return int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed)
                ? parsed
                : null;
        }

        private static string? GetFilterText(IReadOnlyDictionary<string, object?> filters, string key)
        {
            if (!filters.TryGetValue(key, out object? rawValue) || rawValue == null)
            {
                return null;
            }

            string text = (Convert.ToString(rawValue, CultureInfo.InvariantCulture) ?? string.Empty).Trim();
            return string.IsNullOrWhiteSpace(text) ? null : text;
        }

        private static bool TryParseTimeToken(string value, out TimeSpan parsedTime, out bool hasSeconds)
        {
            parsedTime = default;
            hasSeconds = false;

            string normalized = value.Trim();
            if (TimeSpan.TryParseExact(normalized, "hh\\:mm\\:ss", CultureInfo.InvariantCulture, out parsedTime))
            {
                hasSeconds = true;
                return true;
            }

            return TimeSpan.TryParseExact(normalized, "hh\\:mm", CultureInfo.InvariantCulture, out parsedTime);
        }


        public async Task<IEnumerable<Userrole>> GetFilteredExactAsync(IReadOnlyDictionary<string, object?> filters)
        {
            if (filters == null || filters.Count == 0)
            {
                return await GetAllAsync();
            }

            var sqlFilters = new List<string>();
            var dbParameters = new List<DbParameter>();
            if (filters.TryGetValue("userRoleId", out object? userRoleIdFilter) && userRoleIdFilter != null)
            {
                sqlFilters.Add("\"user_role_id\" = @userRoleId");
                dbParameters.Add(CreateParameter("userRoleId", userRoleIdFilter));
            }

            IEnumerable<Userrole> queryable = sqlFilters.Count == 0
                ? await GetAllAsync()
                : await ExecuteReaderToListAsync(
                    $"SELECT * FROM \"UserRole\" WHERE {string.Join(" AND ", sqlFilters)}",
                    MapReaderToUserrole,
                    dbParameters.ToArray());
            return queryable.ToList();
        }

        public async Task<IEnumerable<Userrole>> GetFilteredLikeAsync(IReadOnlyDictionary<string, object?> filters)
        {
            if (filters == null || filters.Count == 0)
            {
                return await GetAllAsync();
            }

            var sqlFilters = new List<string>();
            var dbParameters = new List<DbParameter>();
            string? userRoleIdFilter = GetFilterText(filters, "userRoleId");
            if (!string.IsNullOrWhiteSpace(userRoleIdFilter))
            {
                string userRoleIdPattern = "%" + userRoleIdFilter + "%";
                sqlFilters.Add("CAST(\"user_role_id\" AS TEXT) LIKE @userRoleIdPattern");
                dbParameters.Add(CreateParameter("userRoleIdPattern", userRoleIdPattern));
            }

            IEnumerable<Userrole> queryable = sqlFilters.Count == 0
                ? await GetAllAsync()
                : await ExecuteReaderToListAsync(
                    $"SELECT * FROM \"UserRole\" WHERE {string.Join(" AND ", sqlFilters)}",
                    MapReaderToUserrole,
                    dbParameters.ToArray());
            return queryable.ToList();
        }

        /// <summary>
        /// Searches for entities by query string
        /// </summary>
        public async Task<IEnumerable<Userrole>> SearchAsync(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return await GetAllAsync();
            }
            return await SearchAsyncAll(query);
        }

        /// <summary>
        /// Searches for entities by all column
        /// </summary>
        public async Task<IEnumerable<Userrole>> SearchAsyncAll(string query)
        {
            var filters = new List<string>();
            filters.Add("CAST(\"user_role_id\" AS TEXT) LIKE @query");
            if (filters.Count == 0)
            {
                return await GetAllAsync();
            }
            string sql = $"SELECT * FROM \"UserRole\" WHERE {string.Join(" OR ", filters)}";
            return await ExecuteReaderToListAsync(sql, MapReaderToUserrole, new[] { CreateParameter("query", "%" + query + "%") });
        }

        /// <summary>
        /// Retrieves a paginated list of entities
        /// </summary>
        public async Task<PaginationModel<Userrole>> GetPaginatedAsync(int pageNumber, int pageSize)
        {
            if (pageNumber <= 0)
            {
                pageNumber = 1;
            }

            if (pageSize < 0)
            {
                pageSize = 0;
            }

            if (pageSize <= 0)
            {
                var allItems = await ExecuteReaderToListAsync("SELECT * FROM \"UserRole\"", MapReaderToUserrole);
                var totalRecords = allItems.Count;
                return new PaginationModel<Userrole>()
                {
                    Items = allItems,
                    TotalCount = totalRecords,
                    PageSize = totalRecords,
                    CurrentPage = 1
                };
            }

            string sql = "SELECT * FROM \"UserRole\" ORDER BY \"user_role_id\" LIMIT @pageSize OFFSET @offset";
            var items = await ExecuteReaderToListAsync(sql, MapReaderToUserrole, new[]
            {
                CreateParameter("offset", (pageNumber - 1) * pageSize),
                CreateParameter("pageSize", pageSize)
            });

            var totalRecordsCount = await ExecuteScalarAsync<int>("SELECT COUNT(*) FROM \"UserRole\"");
            return new PaginationModel<Userrole>()
            {
                Items = items,
                TotalCount = totalRecordsCount,
                PageSize = pageSize,
                CurrentPage = pageNumber
            };
        }

        /// <summary>
        /// Uploads data from a list to the database.
        /// </summary>
        public async Task BulkUploadAsync(List<Userrole> dataList)
        {
            if (dataList == null || dataList.Count == 0) return;

            foreach (var item in dataList)
            {
                await AddAsync(item);
            }
        }
    }
}