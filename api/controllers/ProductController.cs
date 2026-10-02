using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using ClothesShopping.Api.ProductsModule;
using ClothesShopping.Api.DTOs;
using ClothesShopping.Api.Main;

namespace ClothesShopping.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class ProductController : ControllerBase
    {
        private readonly IProductRepository _repository;

        public ProductController(IProductRepository repository)
        {
            _repository = repository;
        }

        private static ProductResponse ToResponse(Product p) => new()
        {
            Id          = p.Id,
            Name        = p.Name,
            Category    = p.Category,
            Price       = p.Price,
            Stock       = p.Stock,
            Image       = p.Image,
            Description = p.Description,
        };

        /// <summary>Get all products (public)</summary>
        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<ProductResponse>>> GetAll()
        {
            try
            {
                var result = await _repository.GetAllAsync();
                return Ok(result.Select(ToResponse).ToList());
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        /// <summary>Get product by ID (public)</summary>
        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<ActionResult<ProductResponse>> GetById(int id)
        {
            try
            {
                var entity = await _repository.GetByIdAsync(id);
                if (entity == null) return NotFound(new { message = "Product not found" });
                return Ok(ToResponse(entity));
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        /// <summary>Get products by category (public)</summary>
        [HttpGet("category/{category}")]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<ProductResponse>>> GetByCategory(string category)
        {
            try
            {
                var result = await _repository.GetByCategoryAsync(category);
                return Ok(result.Select(ToResponse).ToList());
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        /// <summary>Search products by name, category, or description (public)</summary>
        [HttpGet("search")]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<ProductResponse>>> Search([FromQuery] string query)
        {
            try
            {
                var result = await _repository.SearchAsync(query);
                return Ok(result.Select(ToResponse).ToList());
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        /// <summary>Get paginated products (public)</summary>
        [HttpGet("paginated")]
        [AllowAnonymous]
        public async Task<ActionResult> GetPaginated(int pageNumber = 1, int pageSize = 25)
        {
            try
            {
                var paged = await _repository.GetPaginatedAsync(pageNumber, pageSize);
                return Ok(new PaginationModel<ProductResponse>
                {
                    Items        = paged.Items.Select(ToResponse).ToList(),
                    TotalCount   = paged.TotalCount,
                    PageSize     = paged.PageSize,
                    CurrentPage  = paged.CurrentPage,
                });
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        /// <summary>Filter products exact match (public)</summary>
        [HttpGet("filter-exact")]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<ProductResponse>>> FilterExact([FromQuery] string? category, [FromQuery] int? id)
        {
            try
            {
                var filters = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
                if (!string.IsNullOrWhiteSpace(category)) filters["category"] = category;
                if (id.HasValue) filters["id"] = id.Value;

                if (filters.Count == 0) return BadRequest(new { message = "At least one filter must be provided." });

                var result = await _repository.GetFilteredExactAsync(filters);
                return Ok(result.Select(ToResponse).ToList());
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        /// <summary>Create a new product (AdminAccess)</summary>
        [HttpPost]
        [Authorize(Policy = "AdminAccess")]
        public async Task<IActionResult> Create([FromBody] CreateProductRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Name))
                    return BadRequest(new { message = "Name is required" });

                var entity = new Product
                {
                    Name        = request.Name,
                    Category    = request.Category,
                    Price       = request.Price,
                    Stock       = request.Stock,
                    Image       = request.Image,
                    Description = request.Description,
                };

                var newId = await _repository.AddAsync(entity);
                return Ok(new { message = "Product created successfully", id = newId });
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        /// <summary>Update a product (AdminAccess)</summary>
        [HttpPut("{id}")]
        [Authorize(Policy = "AdminAccess")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateProductRequest request)
        {
            try
            {
                var existing = await _repository.GetByIdAsync(id);
                if (existing == null) return NotFound(new { message = "Product not found" });

                existing.Name        = request.Name;
                existing.Category    = request.Category;
                existing.Price       = request.Price;
                existing.Stock       = request.Stock;
                existing.Image       = request.Image;
                existing.Description = request.Description;

                await _repository.UpdateAsync(existing);
                return Ok(new { message = "Product updated successfully" });
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        /// <summary>Delete a product (AdminAccess)</summary>
        [HttpDelete("{id}")]
        [Authorize(Policy = "AdminAccess")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var existing = await _repository.GetByIdAsync(id);
                if (existing == null) return NotFound(new { message = "Product not found" });

                await _repository.DeleteAsync(id);
                return Ok(new { message = "Product deleted successfully" });
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        /// <summary>Bulk upload products (AdminAccess)</summary>
        [HttpPost("bulk-upload")]
        [Authorize(Policy = "AdminAccess")]
        public async Task<IActionResult> BulkUpload([FromBody] List<CreateProductRequest> dataList)
        {
            try
            {
                if (dataList == null || dataList.Count == 0)
                    return BadRequest(new { message = "Data list cannot be null or empty." });

                var entities = dataList.Select(r => new Product
                {
                    Name        = r.Name,
                    Category    = r.Category,
                    Price       = r.Price,
                    Stock       = r.Stock,
                    Image       = r.Image,
                    Description = r.Description,
                }).ToList();

                await _repository.BulkUploadAsync(entities);
                return Ok(new { message = $"Successfully uploaded {entities.Count} products." });
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }
    }
}
