using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Data.Common;
using ClothesShopping.Api.Main;

namespace ClothesShopping.Api.UsermanModule
{
    public class UsermanRepository : BaseRepository, IUsermanRepository
    {
        public UsermanRepository(MyCon dbConnection) : base(dbConnection) { }


        private Userman MapReaderToUserman(DbDataReader reader)
        {
            try
            {
                return new Userman()
                {
                    UserId = ReadValue<int>(reader, "id", 0),
                    UserName = ReadValue<string>(reader, "name", string.Empty),
                    Email = ReadValue<string>(reader, "email", string.Empty),
                    UserPass = ReadValue<string>(reader, "password", string.Empty),
                    UserRoleId = ReadValue<int>(reader, "role_id", 0),
                };
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    "Failed to map database row to Userman. Check schema/type alignment for generated columns.",
                    ex);
            }
        }

        /// <summary>
        /// Retrieves all entities from the database
        /// </summary>
        public async Task<IEnumerable<Userman>> GetAllAsync()
        {
            return await ExecuteReaderToListAsync("SELECT * FROM \"users\"", MapReaderToUserman);
        }

        /// <summary>
        /// Retrieves an entity by UserId
        /// </summary>
        public async Task<Userman?> GetByIdAsync(int userId)
        {
            var results = await ExecuteReaderToListAsync(
                "SELECT * FROM \"users\" WHERE \"id\" = @userId", 
                MapReaderToUserman, 
                new[] { CreateParameter("userId", userId) });
            return results.FirstOrDefault();
        }
        /// <summary>
        /// Retrieves an entity by UserName
        /// </summary>
        public async Task<Userman?> GetByUserNameAsync(string userName)
        {
            var results = await ExecuteReaderToListAsync(
                "SELECT * FROM \"users\" WHERE \"name\" = @userName",
                MapReaderToUserman,
                new[] { CreateParameter("userName", userName) });
            return results.FirstOrDefault();
        }

        /// <summary>
        /// Retrieves an entity by Email
        /// </summary>
        public async Task<Userman?> GetByEmailAsync(string email)
        {
            var results = await ExecuteReaderToListAsync(
                "SELECT * FROM \"users\" WHERE \"email\" = @email",
                MapReaderToUserman,
                new[] { CreateParameter("email", email) });
            return results.FirstOrDefault();
        }

        /// <summary>
        /// Adds a new entity to the database
        /// </summary>
        public async Task AddAsync(Userman entity)
        {
            var sql = "INSERT INTO \"users\" (\"name\", \"email\", \"password\", \"role_id\") VALUES (@userName, @email, @userPass, @userRoleId)";
            var dbParameters = new List<DbParameter>();
            dbParameters.Add(CreateParameter("userName", entity.UserName));
            dbParameters.Add(CreateParameter("email", entity.Email));
            dbParameters.Add(CreateParameter("userPass", entity.UserPass));
            dbParameters.Add(CreateParameter("userRoleId", entity.UserRoleId));
            await ExecuteNonQueryAsync(sql, dbParameters.ToArray());
        }

        /// <summary>
        /// Updates an existing entity in the database
        /// </summary>
        public async Task UpdateAsync(Userman entity)
        {
            var sql = "UPDATE \"users\" SET \"name\" = @userName, \"email\" = @email, \"password\" = @userPass, \"role_id\" = @userRoleId WHERE \"id\" = @userId";
            var dbParameters = new List<DbParameter>();
            dbParameters.Add(CreateParameter("userId", entity.UserId));
            dbParameters.Add(CreateParameter("userName", entity.UserName));
            dbParameters.Add(CreateParameter("email", entity.Email));
            dbParameters.Add(CreateParameter("userPass", entity.UserPass));
            dbParameters.Add(CreateParameter("userRoleId", entity.UserRoleId));
            await ExecuteNonQueryAsync(sql, dbParameters.ToArray());
        }

        /// <summary>
        /// Deletes an entity from the database by its UserId
        /// </summary>
        public async Task DeleteAsync(int userId)
        {
            await ExecuteNonQueryAsync("DELETE FROM \"users\" WHERE \"id\" = @userId", new[] { CreateParameter("userId", userId) });
        }

        /// <summary>
        /// Deletes all entities from the database
        /// </summary>
        public async Task DeleteAllAsync()
        {
            await ExecuteNonQueryAsync("DELETE FROM \"users\"");
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


        public async Task<IEnumerable<Userman>> GetFilteredExactAsync(IReadOnlyDictionary<string, object?> filters)
        {
            if (filters == null || filters.Count == 0)
            {
                return await GetAllAsync();
            }

            var sqlFilters = new List<string>();
            var dbParameters = new List<DbParameter>();
            if (filters.TryGetValue("userId", out object? userIdFilter) && userIdFilter != null)
            {
                sqlFilters.Add("\"id\" = @userId");
                dbParameters.Add(CreateParameter("userId", userIdFilter));
            }
            if (filters.TryGetValue("userName", out object? userNameFilter) && userNameFilter != null)
            {
                sqlFilters.Add("\"name\" = @userName");
                dbParameters.Add(CreateParameter("userName", userNameFilter));
            }
            if (filters.TryGetValue("email", out object? emailFilter) && emailFilter != null)
            {
                sqlFilters.Add("\"email\" = @email");
                dbParameters.Add(CreateParameter("email", emailFilter));
            }

            IEnumerable<Userman> queryable = sqlFilters.Count == 0
                ? await GetAllAsync()
                : await ExecuteReaderToListAsync(
                    $"SELECT * FROM \"users\" WHERE {string.Join(" AND ", sqlFilters)}",
                    MapReaderToUserman,
                    dbParameters.ToArray());
            return queryable.ToList();
        }

        public async Task<IEnumerable<Userman>> GetFilteredLikeAsync(IReadOnlyDictionary<string, object?> filters)
        {
            if (filters == null || filters.Count == 0)
            {
                return await GetAllAsync();
            }

            var sqlFilters = new List<string>();
            var dbParameters = new List<DbParameter>();
            string? userIdFilter = GetFilterText(filters, "userId");
            if (!string.IsNullOrWhiteSpace(userIdFilter))
            {
                string userIdPattern = "%" + userIdFilter + "%";
                sqlFilters.Add("CAST(\"id\" AS TEXT) LIKE @userIdPattern");
                dbParameters.Add(CreateParameter("userIdPattern", userIdPattern));
            }
            string? userNameFilter = GetFilterText(filters, "userName");
            if (!string.IsNullOrWhiteSpace(userNameFilter))
            {
                string userNamePattern = "%" + userNameFilter + "%";
                sqlFilters.Add("\"name\" LIKE @userNamePattern");
                dbParameters.Add(CreateParameter("userNamePattern", userNamePattern));
            }
            string? emailFilter = GetFilterText(filters, "email");
            if (!string.IsNullOrWhiteSpace(emailFilter))
            {
                string emailPattern = "%" + emailFilter + "%";
                sqlFilters.Add("\"email\" LIKE @emailPattern");
                dbParameters.Add(CreateParameter("emailPattern", emailPattern));
            }

            IEnumerable<Userman> queryable = sqlFilters.Count == 0
                ? await GetAllAsync()
                : await ExecuteReaderToListAsync(
                    $"SELECT * FROM \"users\" WHERE {string.Join(" AND ", sqlFilters)}",
                    MapReaderToUserman,
                    dbParameters.ToArray());
            return queryable.ToList();
        }

        /// <summary>
        /// Searches for entities by query string
        /// </summary>
        public async Task<IEnumerable<Userman>> SearchAsync(string query)
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
        public async Task<IEnumerable<Userman>> SearchAsyncAll(string query)
        {
            var filters = new List<string>();
            filters.Add("CAST(\"id\" AS TEXT) LIKE @query");
            filters.Add("\"name\" LIKE @query");
            filters.Add("\"email\" LIKE @query");
            if (filters.Count == 0)
            {
                return await GetAllAsync();
            }
            string sql = $"SELECT * FROM \"users\" WHERE {string.Join(" OR ", filters)}";
            return await ExecuteReaderToListAsync(sql, MapReaderToUserman, new[] { CreateParameter("query", "%" + query + "%") });
        }

        /// <summary>
        /// Retrieves a paginated list of entities
        /// </summary>
        public async Task<PaginationModel<Userman>> GetPaginatedAsync(int pageNumber, int pageSize)
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
                var allItems = await ExecuteReaderToListAsync("SELECT * FROM \"users\"", MapReaderToUserman);
                var totalRecords = allItems.Count;
                return new PaginationModel<Userman>()
                {
                    Items = allItems,
                    TotalCount = totalRecords,
                    PageSize = totalRecords,
                    CurrentPage = 1
                };
            }

            string sql = "SELECT * FROM \"users\" ORDER BY \"id\" LIMIT @pageSize OFFSET @offset";
            var items = await ExecuteReaderToListAsync(sql, MapReaderToUserman, new[]
            {
                CreateParameter("offset", (pageNumber - 1) * pageSize),
                CreateParameter("pageSize", pageSize)
            });

            var totalRecordsCount = await ExecuteScalarAsync<int>("SELECT COUNT(*) FROM \"users\"");
            return new PaginationModel<Userman>()
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
        public async Task BulkUploadAsync(List<Userman> dataList)
        {
            if (dataList == null || dataList.Count == 0) return;

            foreach (var item in dataList)
            {
                await AddAsync(item);
            }
        }
    }
}