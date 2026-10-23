using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using ClothesShopping.Api.ProductsModule;
using ClothesShopping.Api.ProductImagesModule;
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
        private readonly IProductImageRepository _imageRepository;
        private readonly IWebHostEnvironment _environment;

        [ActivatorUtilitiesConstructor]
        public ProductController(
            IProductRepository repository,
            IProductImageRepository imageRepository,
            IWebHostEnvironment environment)
        {
            _repository = repository;
            _imageRepository = imageRepository;
            _environment = environment;
        }

        public ProductController(IProductRepository repository)
            : this(repository, null!, null!)
        {
        }

        public ProductController(IProductRepository repository, IWebHostEnvironment environment)
            : this(repository, null!, environment)
        {
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

        /// <summary>Get top products (best sellers) (public)</summary>
        [HttpGet("top")]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<TopProductResponse>>> GetTop([FromQuery] int count = 8)
        {
            int clampedCount = Math.Clamp(count, 1, 20);
            var result = await _repository.GetTopAsync(clampedCount);
            return Ok(result);
        }

        /// <summary>Get all products (public)</summary>
        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<ProductResponse>>> GetAll()
        {
            var result = await _repository.GetAllAsync();
            return Ok(result.Select(ToResponse).ToList());
        }

        /// <summary>Get product by ID (public)</summary>
        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<ActionResult<ProductResponse>> GetById(int id)
        {
            var entity = await _repository.GetByIdAsync(id);
            if (entity == null) return NotFound(new { message = "Product not found" });
            return Ok(ToResponse(entity));
        }

        /// <summary>Get products by category (public)</summary>
        [HttpGet("category/{category}")]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<ProductResponse>>> GetByCategory(string category)
        {
            var result = await _repository.GetByCategoryAsync(category);
            return Ok(result.Select(ToResponse).ToList());
        }

        /// <summary>Search products by name, category, or description (public)</summary>
        [HttpGet("search")]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<ProductResponse>>> Search([FromQuery] string query)
        {
            var result = await _repository.SearchAsync(query);
            return Ok(result.Select(ToResponse).ToList());
        }

        /// <summary>Get paginated products (public)</summary>
        [HttpGet("paginated")]
        [AllowAnonymous]
        public async Task<ActionResult> GetPaginated(int pageNumber = 1, int pageSize = 25)
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

        /// <summary>Filter products exact match (public)</summary>
        [HttpGet("filter-exact")]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<ProductResponse>>> FilterExact([FromQuery] string? category, [FromQuery] int? id)
        {
            var filters = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            if (!string.IsNullOrWhiteSpace(category)) filters["category"] = category;
            if (id.HasValue) filters["id"] = id.Value;

            if (filters.Count == 0) return BadRequest(new { message = "At least one filter must be provided." });

            var result = await _repository.GetFilteredExactAsync(filters);
            return Ok(result.Select(ToResponse).ToList());
        }

        private static readonly HashSet<string> AllowedImageExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg", ".jpeg", ".png", ".webp", ".gif"
        };
        private const long MaxImageSizeBytes = 5 * 1024 * 1024; // 5 MB

        private async Task<string> SaveImageFileAsync(IFormFile file)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("No file provided.");

            if (file.Length > MaxImageSizeBytes)
                throw new ArgumentException("Image file size exceeds 5MB limit.");

            var ext = Path.GetExtension(file.FileName);
            if (string.IsNullOrEmpty(ext) || !AllowedImageExtensions.Contains(ext))
                throw new ArgumentException("Invalid image file format. Supported formats: .jpg, .jpeg, .png, .webp, .gif");

            var webRoot = _environment.WebRootPath;
            if (string.IsNullOrWhiteSpace(webRoot))
            {
                webRoot = Path.Combine(_environment.ContentRootPath, "wwwroot");
            }
            var uploadsDir = Path.Combine(webRoot, "uploads");
            Directory.CreateDirectory(uploadsDir);

            var fileName = $"{Guid.NewGuid():N}{ext.ToLowerInvariant()}";
            var filePath = Path.Combine(uploadsDir, fileName);

            await using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return $"/uploads/{fileName}";
        }

        /// <summary>Upload a product image file (ModAccess - Staff & Admin)</summary>
        [HttpPost("upload-image")]
        [Authorize(Policy = "ModAccess")]
        public async Task<IActionResult> UploadImage(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { message = "Please select an image file to upload." });

            try
            {
                var imageUrl = await SaveImageFileAsync(file);
                return Ok(new { imageUrl, message = "Image uploaded successfully." });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>Create a new product (ModAccess - Staff & Admin)</summary>
        [HttpPost]
        [Authorize(Policy = "ModAccess")]
        public async Task<IActionResult> Create([FromBody] CreateProductRequest request)
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

        /// <summary>Create a new product with an uploaded image file or URL (ModAccess - Staff & Admin)</summary>
        [HttpPost("with-image")]
        [Authorize(Policy = "ModAccess")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> CreateWithImage([FromForm] CreateProductWithFileRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
                return BadRequest(new { message = "Name is required" });

            string? image = request.ImageUrl;
            if (request.ImageFile != null && request.ImageFile.Length > 0)
            {
                try
                {
                    image = await SaveImageFileAsync(request.ImageFile);
                }
                catch (ArgumentException ex)
                {
                    return BadRequest(new { message = ex.Message });
                }
            }

            var entity = new Product
            {
                Name        = request.Name,
                Category    = request.Category,
                Price       = request.Price,
                Stock       = request.Stock,
                Image       = image,
                Description = request.Description,
            };

            var newId = await _repository.AddAsync(entity);
            return Ok(new { message = "Product created successfully", id = newId, imageUrl = image });
        }

        /// <summary>Update a product (ModAccess - Staff & Admin)</summary>
        [HttpPut("{id}")]
        [Authorize(Policy = "ModAccess")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateProductRequest request)
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

        /// <summary>Delete a product and all its images from DB and disk (ModAccess - Staff & Admin)</summary>
        [HttpDelete("{id}")]
        [Authorize(Policy = "ModAccess")]
        public async Task<IActionResult> Delete(int id)
        {
            var existing = await _repository.GetByIdAsync(id);
            if (existing == null) return NotFound(new { message = "Product not found" });

            // Cascade: delete product_images rows and their files from disk
            if (_imageRepository != null)
            {
                var images = (await _imageRepository.GetByProductIdAsync(id)).ToList();
                foreach (var img in images)
                    DeleteImageFileOnDisk(img.ImageUrl);
                await _imageRepository.DeleteByProductIdAsync(id);
            }

            await _repository.DeleteAsync(id);
            return Ok(new { message = "Product deleted successfully" });
        }

        private void DeleteImageFileOnDisk(string? imageUrl)
        {
            if (string.IsNullOrWhiteSpace(imageUrl)) return;
            var webRoot = string.IsNullOrWhiteSpace(_environment?.WebRootPath)
                ? Path.Combine(_environment?.ContentRootPath ?? AppContext.BaseDirectory, "wwwroot")
                : _environment.WebRootPath;
            var uploadsDir = Path.Combine(webRoot, "uploads", "products");
            var fileName   = Path.GetFileName(imageUrl);
            var filePath   = Path.Combine(uploadsDir, fileName);
            var fullDir    = Path.GetFullPath(uploadsDir) + Path.DirectorySeparatorChar;
            var fullFile   = Path.GetFullPath(filePath);
            if (fullFile.StartsWith(fullDir, StringComparison.OrdinalIgnoreCase) && System.IO.File.Exists(filePath))
                System.IO.File.Delete(filePath);
        }

        /// <summary>Bulk upload products (ModAccess - Staff & Admin)</summary>
        [HttpPost("bulk-upload")]
        [Authorize(Policy = "ModAccess")]
        public async Task<IActionResult> BulkUpload([FromBody] List<CreateProductRequest> dataList)
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
    }
}
