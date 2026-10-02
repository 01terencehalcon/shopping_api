using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using ClothesShopping.Api.Main;

namespace ClothesShopping.Api.ProductsModule
{
    public class ProductRepository : BaseRepository, IProductRepository
    {
        public ProductRepository(MyCon dbConnection) : base(dbConnection) { }

        private Product MapRow(DbDataReader reader)
        {
            try
            {
                return new Product
                {
                    Id          = ReadValue<int>(reader, "id", 0),
                    Name        = ReadValue<string>(reader, "name", string.Empty),
                    Category    = ReadValue<string>(reader, "category", string.Empty),
                    Price       = ReadValue<double>(reader, "price", 0),
                    Stock       = ReadValue<int>(reader, "stock", 0),
                    Image       = ReadValue<string>(reader, "image", string.Empty),
                    Description = ReadValue<string>(reader, "description", string.Empty),
                };
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to map database row to Product.", ex);
            }
        }

        public async Task<IEnumerable<Product>> GetAllAsync()
            => await ExecuteReaderToListAsync("SELECT * FROM \"products\"", MapRow);

        public async Task<Product?> GetByIdAsync(int id)
        {
            var results = await ExecuteReaderToListAsync(
                "SELECT * FROM \"products\" WHERE \"id\" = @id",
                MapRow,
                new[] { CreateParameter("id", id) });
            return results.FirstOrDefault();
        }

        public async Task<IEnumerable<Product>> GetByCategoryAsync(string category)
            => await ExecuteReaderToListAsync(
                "SELECT * FROM \"products\" WHERE \"category\" = @category",
                MapRow,
                new[] { CreateParameter("category", category) });

        public async Task<IEnumerable<Product>> SearchAsync(string query)
        {
            if (string.IsNullOrWhiteSpace(query)) return await GetAllAsync();
            const string sql = "SELECT * FROM \"products\" WHERE " +
                               "\"name\" LIKE @q OR \"category\" LIKE @q OR \"description\" LIKE @q";
            return await ExecuteReaderToListAsync(sql, MapRow,
                new[] { CreateParameter("q", "%" + query + "%") });
        }

        public async Task<IEnumerable<Product>> GetFilteredExactAsync(IReadOnlyDictionary<string, object?> filters)
        {
            if (filters == null || filters.Count == 0) return await GetAllAsync();

            var sqlFilters = new List<string>();
            var dbParams   = new List<DbParameter>();

            if (filters.TryGetValue("category", out var cat) && cat != null)
            {
                sqlFilters.Add("\"category\" = @category");
                dbParams.Add(CreateParameter("category", cat));
            }
            if (filters.TryGetValue("id", out var id) && id != null)
            {
                sqlFilters.Add("\"id\" = @id");
                dbParams.Add(CreateParameter("id", id));
            }

            if (sqlFilters.Count == 0) return await GetAllAsync();
            return await ExecuteReaderToListAsync(
                $"SELECT * FROM \"products\" WHERE {string.Join(" AND ", sqlFilters)}",
                MapRow, dbParams.ToArray());
        }

        public async Task<IEnumerable<Product>> GetFilteredLikeAsync(IReadOnlyDictionary<string, object?> filters)
        {
            if (filters == null || filters.Count == 0) return await GetAllAsync();

            var sqlFilters = new List<string>();
            var dbParams   = new List<DbParameter>();

            if (filters.TryGetValue("name", out var name) && name != null)
            {
                sqlFilters.Add("\"name\" LIKE @namePattern");
                dbParams.Add(CreateParameter("namePattern", "%" + name + "%"));
            }
            if (filters.TryGetValue("category", out var cat) && cat != null)
            {
                sqlFilters.Add("\"category\" LIKE @categoryPattern");
                dbParams.Add(CreateParameter("categoryPattern", "%" + cat + "%"));
            }

            if (sqlFilters.Count == 0) return await GetAllAsync();
            return await ExecuteReaderToListAsync(
                $"SELECT * FROM \"products\" WHERE {string.Join(" AND ", sqlFilters)}",
                MapRow, dbParams.ToArray());
        }

        public async Task<PaginationModel<Product>> GetPaginatedAsync(int pageNumber, int pageSize)
        {
            if (pageNumber <= 0) pageNumber = 1;
            if (pageSize <= 0)
            {
                var all = await ExecuteReaderToListAsync("SELECT * FROM \"products\"", MapRow);
                return new PaginationModel<Product> { Items = all, TotalCount = all.Count, PageSize = all.Count, CurrentPage = 1 };
            }

            var items = await ExecuteReaderToListAsync(
                "SELECT * FROM \"products\" ORDER BY \"id\" LIMIT @pageSize OFFSET @offset",
                MapRow,
                new[] { CreateParameter("offset", (pageNumber - 1) * pageSize), CreateParameter("pageSize", pageSize) });

            var total = await ExecuteScalarAsync<int>("SELECT COUNT(*) FROM \"products\"");
            return new PaginationModel<Product> { Items = items, TotalCount = total, PageSize = pageSize, CurrentPage = pageNumber };
        }

        public async Task<int> AddAsync(Product entity)
        {
            const string sql = "INSERT INTO \"products\" (\"name\", \"category\", \"price\", \"stock\", \"image\", \"description\") " +
                               "VALUES (@name, @category, @price, @stock, @image, @description); SELECT last_insert_rowid();";
            var id = await ExecuteScalarAsync<long>(sql, new[]
            {
                CreateParameter("name",        entity.Name),
                CreateParameter("category",    entity.Category    as object ?? DBNull.Value),
                CreateParameter("price",       entity.Price),
                CreateParameter("stock",       entity.Stock),
                CreateParameter("image",       entity.Image       as object ?? DBNull.Value),
                CreateParameter("description", entity.Description as object ?? DBNull.Value),
            });
            return (int)id;
        }

        public async Task UpdateAsync(Product entity)
        {
            const string sql = "UPDATE \"products\" SET \"name\"=@name, \"category\"=@category, \"price\"=@price, " +
                               "\"stock\"=@stock, \"image\"=@image, \"description\"=@description WHERE \"id\"=@id";
            await ExecuteNonQueryAsync(sql, new[]
            {
                CreateParameter("id",          entity.Id),
                CreateParameter("name",        entity.Name),
                CreateParameter("category",    entity.Category    as object ?? DBNull.Value),
                CreateParameter("price",       entity.Price),
                CreateParameter("stock",       entity.Stock),
                CreateParameter("image",       entity.Image       as object ?? DBNull.Value),
                CreateParameter("description", entity.Description as object ?? DBNull.Value),
            });
        }

        public async Task DeleteAsync(int id)
            => await ExecuteNonQueryAsync("DELETE FROM \"products\" WHERE \"id\" = @id",
                new[] { CreateParameter("id", id) });

        public async Task BulkUploadAsync(List<Product> dataList)
        {
            if (dataList == null || dataList.Count == 0) return;
            foreach (var item in dataList)
                await AddAsync(item);
        }
    }
}
