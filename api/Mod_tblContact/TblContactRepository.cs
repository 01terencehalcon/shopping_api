using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Data.Common;
using ClothesShopping.Api.Main;

namespace ClothesShopping.Api.TblContactModule
{
    public class TblContactRepository : BaseRepository, ITblContactRepository
    {
        public TblContactRepository(MyCon dbConnection) : base(dbConnection) { }


        private TblContact MapReaderToTblContact(DbDataReader reader)
        {
            try
            {
                return new TblContact()
                {
                    Id = ReadValue<int>(reader, "ID", 0),
                    Name = ReadValue<string>(reader, "Name", string.Empty),
                    Gender = ReadValue<string>(reader, "Gender", string.Empty),
                    Birthday = ReadValue<string>(reader, "Birthday", string.Empty),
                    Address = ReadValue<string>(reader, "Address", string.Empty),
                    Contactnum = ReadValue<string>(reader, "ContactNum", string.Empty),
                };
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    "Failed to map database row to TblContact. Check schema/type alignment for generated columns.",
                    ex);
            }
        }

        /// <summary>
        /// Retrieves all entities from the database
        /// </summary>
        public async Task<IEnumerable<TblContact>> GetAllAsync()
        {
            return await ExecuteReaderToListAsync("SELECT * FROM \"tbl_Contact\"", MapReaderToTblContact);
        }

        /// <summary>
        /// Retrieves an entity by Id
        /// </summary>
        public async Task<TblContact?> GetByIdAsync(int id)
        {
            var results = await ExecuteReaderToListAsync(
                "SELECT * FROM \"tbl_Contact\" WHERE \"ID\" = @id", 
                MapReaderToTblContact, 
                new[] { CreateParameter("id", id) });
            return results.FirstOrDefault();
        }


        /// <summary>
        /// Adds a new entity to the database
        /// </summary>
        public async Task AddAsync(TblContact entity)
        {
            var sql = "INSERT INTO \"tbl_Contact\" (\"Name\", \"Gender\", \"Birthday\", \"Address\", \"ContactNum\") VALUES (@name, @gender, @birthday, @address, @contactnum)";
            var dbParameters = new List<DbParameter>();
            dbParameters.Add(CreateParameter("name", entity.Name));
            dbParameters.Add(CreateParameter("gender", entity.Gender));
            dbParameters.Add(CreateParameter("birthday", entity.Birthday));
            dbParameters.Add(CreateParameter("address", entity.Address));
            dbParameters.Add(CreateParameter("contactnum", entity.Contactnum));
            await ExecuteNonQueryAsync(sql, dbParameters.ToArray());
        }

        /// <summary>
        /// Updates an existing entity in the database
        /// </summary>
        public async Task UpdateAsync(TblContact entity)
        {
            var sql = "UPDATE \"tbl_Contact\" SET \"Name\" = @name, \"Gender\" = @gender, \"Birthday\" = @birthday, \"Address\" = @address, \"ContactNum\" = @contactnum WHERE \"ID\" = @id";
            var dbParameters = new List<DbParameter>();
            dbParameters.Add(CreateParameter("id", entity.Id));
            dbParameters.Add(CreateParameter("name", entity.Name));
            dbParameters.Add(CreateParameter("gender", entity.Gender));
            dbParameters.Add(CreateParameter("birthday", entity.Birthday));
            dbParameters.Add(CreateParameter("address", entity.Address));
            dbParameters.Add(CreateParameter("contactnum", entity.Contactnum));
            await ExecuteNonQueryAsync(sql, dbParameters.ToArray());
        }

        /// <summary>
        /// Deletes an entity from the database by its Id
        /// </summary>
        public async Task DeleteAsync(int id)
        {
            await ExecuteNonQueryAsync("DELETE FROM \"tbl_Contact\" WHERE \"ID\" = @id", new[] { CreateParameter("id", id) });
        }

        /// <summary>
        /// Deletes all entities from the database
        /// </summary>
        public async Task DeleteAllAsync()
        {
            await ExecuteNonQueryAsync("DELETE FROM \"tbl_Contact\"");
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


        public async Task<IEnumerable<TblContact>> GetFilteredExactAsync(IReadOnlyDictionary<string, object?> filters)
        {
            if (filters == null || filters.Count == 0)
            {
                return await GetAllAsync();
            }

            var sqlFilters = new List<string>();
            var dbParameters = new List<DbParameter>();
            if (filters.TryGetValue("id", out object? idFilter) && idFilter != null)
            {
                sqlFilters.Add("\"ID\" = @id");
                dbParameters.Add(CreateParameter("id", idFilter));
            }

            IEnumerable<TblContact> queryable = sqlFilters.Count == 0
                ? await GetAllAsync()
                : await ExecuteReaderToListAsync(
                    $"SELECT * FROM \"tbl_Contact\" WHERE {string.Join(" AND ", sqlFilters)}",
                    MapReaderToTblContact,
                    dbParameters.ToArray());
            return queryable.ToList();
        }

        public async Task<IEnumerable<TblContact>> GetFilteredLikeAsync(IReadOnlyDictionary<string, object?> filters)
        {
            if (filters == null || filters.Count == 0)
            {
                return await GetAllAsync();
            }

            var sqlFilters = new List<string>();
            var dbParameters = new List<DbParameter>();
            string? idFilter = GetFilterText(filters, "id");
            if (!string.IsNullOrWhiteSpace(idFilter))
            {
                string idPattern = "%" + idFilter + "%";
                sqlFilters.Add("CAST(\"ID\" AS TEXT) LIKE @idPattern");
                dbParameters.Add(CreateParameter("idPattern", idPattern));
            }

            IEnumerable<TblContact> queryable = sqlFilters.Count == 0
                ? await GetAllAsync()
                : await ExecuteReaderToListAsync(
                    $"SELECT * FROM \"tbl_Contact\" WHERE {string.Join(" AND ", sqlFilters)}",
                    MapReaderToTblContact,
                    dbParameters.ToArray());
            return queryable.ToList();
        }

        /// <summary>
        /// Searches for entities by query string
        /// </summary>
        public async Task<IEnumerable<TblContact>> SearchAsync(string query)
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
        public async Task<IEnumerable<TblContact>> SearchAsyncAll(string query)
        {
            var filters = new List<string>();
            filters.Add("CAST(\"ID\" AS TEXT) LIKE @query");
            if (filters.Count == 0)
            {
                return await GetAllAsync();
            }
            string sql = $"SELECT * FROM \"tbl_Contact\" WHERE {string.Join(" OR ", filters)}";
            return await ExecuteReaderToListAsync(sql, MapReaderToTblContact, new[] { CreateParameter("query", "%" + query + "%") });
        }

        /// <summary>
        /// Retrieves a paginated list of entities
        /// </summary>
        public async Task<PaginationModel<TblContact>> GetPaginatedAsync(int pageNumber, int pageSize)
        {
            if (pageNumber <= 0)
            {
                pageNumber = 1;
            }

            pageSize = Math.Clamp(pageSize, 1, 100);

            string sql = "SELECT * FROM \"tbl_Contact\" ORDER BY \"ID\" LIMIT @pageSize OFFSET @offset";
            var items = await ExecuteReaderToListAsync(sql, MapReaderToTblContact, new[]
            {
                CreateParameter("offset", (pageNumber - 1) * pageSize),
                CreateParameter("pageSize", pageSize)
            });

            var totalRecordsCount = await ExecuteScalarAsync<int>("SELECT COUNT(*) FROM \"tbl_Contact\"");
            return new PaginationModel<TblContact>()
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
        public async Task BulkUploadAsync(List<TblContact> dataList)
        {
            if (dataList == null || dataList.Count == 0) return;

            foreach (var item in dataList)
            {
                await AddAsync(item);
            }
        }
    }
}