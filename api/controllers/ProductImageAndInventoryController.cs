using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using ClothesShopping.Api.ProductImagesModule;
using ClothesShopping.Api.ProductsModule;
using ClothesShopping.Api.InventoryLogsModule;
using ClothesShopping.Api.DTOs;
using ClothesShopping.Api.Main;

namespace ClothesShopping.Api.Controllers
{
    [ApiController]
    [Route("api/product-images")]
    [Produces("application/json")]
    [Authorize]
    public class ProductImageController : ControllerBase
    {
        private readonly IProductImageRepository _repository;
        private readonly IProductRepository _productRepository;
        private readonly IWebHostEnvironment _environment;

        public ProductImageController(
            IProductImageRepository repository,
            IProductRepository productRepository,
            IWebHostEnvironment environment)
        {
            _repository = repository;
            _productRepository = productRepository;
            _environment = environment;
        }

        private static ProductImageResponse ToResponse(ProductImage p) => new()
        {
            ImageId   = p.ImageId,
            ProductId = p.ProductId,
            ImageUrl  = p.ImageUrl,
        };

        // ── helpers ─────────────────────────────────────────────────────────────

        private string UploadsPath()
        {
            var webRoot = string.IsNullOrWhiteSpace(_environment?.WebRootPath)
                ? Path.Combine(_environment?.ContentRootPath ?? AppContext.BaseDirectory, "wwwroot")
                : _environment.WebRootPath;
            return Path.Combine(webRoot, "uploads", "products");
        }

        // Magic-byte signatures for allowed image types
        private static readonly byte[] MagicJpeg = [0xFF, 0xD8, 0xFF];
        private static readonly byte[] MagicPng  = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
        private static readonly byte[] MagicRiff = [0x52, 0x49, 0x46, 0x46]; // RIFF (WebP outer)
        private static readonly byte[] MagicWebp = [0x57, 0x45, 0x42, 0x50]; // WEBP at bytes 8-11

        private static bool StartsWith(byte[] data, byte[] magic)
        {
            if (data.Length < magic.Length) return false;
            for (int i = 0; i < magic.Length; i++)
                if (data[i] != magic[i]) return false;
            return true;
        }

        /// <summary>Reads the first 12 bytes and detects the real image type.</summary>
        private static (bool ok, string ext) DetectImageType(Stream stream)
        {
            var header = new byte[12];
            var read   = stream.Read(header, 0, header.Length);
            stream.Position = 0;

            var h = header[..read];
            if (StartsWith(h, MagicPng))  return (true, ".png");
            if (StartsWith(h, MagicJpeg)) return (true, ".jpg");
            if (h.Length >= 12 &&
                StartsWith(h, MagicRiff) &&
                h[8] == MagicWebp[0] && h[9] == MagicWebp[1] &&
                h[10] == MagicWebp[2] && h[11] == MagicWebp[3])
                return (true, ".webp");

            return (false, string.Empty);
        }

        /// <summary>Ensures the resolved file path is strictly inside the uploads folder.</summary>
        private static bool IsPathSafe(string uploadsDir, string filePath)
        {
            var fullUploads = Path.GetFullPath(uploadsDir) + Path.DirectorySeparatorChar;
            var fullFile    = Path.GetFullPath(filePath);
            return fullFile.StartsWith(fullUploads, StringComparison.OrdinalIgnoreCase);
        }

        private void DeleteFileOnDisk(string? imageUrl)
        {
            if (string.IsNullOrWhiteSpace(imageUrl)) return;
            var uploadsDir = UploadsPath();
            // imageUrl is "/uploads/products/<guid>.ext" — GetFileName strips any traversal attempts
            var fileName = Path.GetFileName(imageUrl);
            var filePath = Path.Combine(uploadsDir, fileName);
            if (IsPathSafe(uploadsDir, filePath) && System.IO.File.Exists(filePath))
                System.IO.File.Delete(filePath);
        }

        // ── endpoints ────────────────────────────────────────────────────────────

        /// <summary>Get all images for a product (public), ordered by image_id ASC.</summary>
        [HttpGet("product/{productId}")]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<ProductImageResponse>>> GetByProduct(int productId)
        {
            var result = await _repository.GetByProductIdAsync(productId);
            return Ok(result.Select(ToResponse).ToList());
        }

        /// <summary>Get image by ID (public).</summary>
        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<ActionResult<ProductImageResponse>> GetById(int id)
        {
            var entity = await _repository.GetByIdAsync(id);
            if (entity == null) return NotFound(new { message = "Image not found" });
            return Ok(ToResponse(entity));
        }

        /// <summary>
        /// Upload 1-6 image files for a product (ModAccess - Staff &amp; Admin).
        /// Form fields: productId (int), files (one or more IFormFile).
        /// Enforces: max 2 MB per file, max 6 images per product total, magic-byte validation.
        /// </summary>
        [HttpPost("upload")]
        [Authorize(Policy = "ModAccess")]
        [RequestSizeLimit(14_000_000)] // 6 × 2 MB + form overhead
        public async Task<IActionResult> Upload([FromForm] int productId, [FromForm] List<IFormFile> files)
        {
            if (productId <= 0)
                return BadRequest(new { message = "productId is required." });
            if (files == null || files.Count == 0)
                return BadRequest(new { message = "No files provided." });
            if (files.Count > 6)
                return BadRequest(new { message = "You can upload at most 6 files at once." });

            var product = await _productRepository.GetByIdAsync(productId);
            if (product == null)
                return NotFound(new { message = "Product not found." });

            var existing = (await _repository.GetByProductIdAsync(productId)).ToList();
            if (existing.Count + files.Count > 6)
                return BadRequest(new { message = $"A product can have at most 6 images. This product already has {existing.Count}." });

            const long MaxBytes = 2L * 1024 * 1024;
            var uploadsDir = UploadsPath();
            Directory.CreateDirectory(uploadsDir);

            var created = new List<ProductImageResponse>();

            foreach (var file in files)
            {
                if (file.Length == 0)
                    return BadRequest(new { message = $"File '{file.FileName}' is empty." });
                if (file.Length > MaxBytes)
                    return BadRequest(new { message = $"File '{file.FileName}' exceeds the 2 MB limit." });

                bool ok;
                string ext;
                await using (var stream = file.OpenReadStream())
                    (ok, ext) = DetectImageType(stream);

                if (!ok)
                    return BadRequest(new { message = $"'{file.FileName}' is not a valid jpg, png, or webp image." });

                // GUID filename — never use the original file name
                var safeFileName = $"{Guid.NewGuid():N}{ext}";
                var savePath     = Path.Combine(uploadsDir, safeFileName);
                if (!IsPathSafe(uploadsDir, savePath))
                    return BadRequest(new { message = "Invalid file path." });

                await using (var stream = file.OpenReadStream())
                await using (var fs = new FileStream(savePath, FileMode.Create, FileAccess.Write))
                    await stream.CopyToAsync(fs);

                var imageUrl = $"/uploads/products/{safeFileName}";
                var entity   = new ProductImage { ProductId = productId, ImageUrl = imageUrl };
                var newId    = await _repository.AddAsync(entity);

                created.Add(new ProductImageResponse { ImageId = newId, ProductId = productId, ImageUrl = imageUrl });
            }

            // If the product has no main image yet, promote the first uploaded image
            if (string.IsNullOrWhiteSpace(product.Image) && created.Count > 0)
            {
                product.Image = created[0].ImageUrl;
                await _productRepository.UpdateAsync(product);
            }

            return Ok(created);
        }

        /// <summary>Set the main (card) image for a product (ModAccess). Body: { imageId }.</summary>
        [HttpPut("product/{productId}/main")]
        [Authorize(Policy = "ModAccess")]
        public async Task<IActionResult> SetMain(int productId, [FromBody] SetMainImageRequest request)
        {
            var product = await _productRepository.GetByIdAsync(productId);
            if (product == null) return NotFound(new { message = "Product not found." });

            var image = await _repository.GetByIdAsync(request.ImageId);
            if (image == null || image.ProductId != productId)
                return NotFound(new { message = "Image not found for this product." });

            product.Image = image.ImageUrl;
            await _productRepository.UpdateAsync(product);
            return Ok(new { message = "Main image updated.", imageUrl = image.ImageUrl });
        }

        /// <summary>Delete an image by ID; removes file from disk (ModAccess).</summary>
        [HttpDelete("{id}")]
        [Authorize(Policy = "ModAccess")]
        public async Task<IActionResult> Delete(int id)
        {
            var existing = await _repository.GetByIdAsync(id);
            if (existing == null) return NotFound(new { message = "Image not found" });

            var product = await _productRepository.GetByIdAsync(existing.ProductId);

            DeleteFileOnDisk(existing.ImageUrl);
            await _repository.DeleteAsync(id);

            // If this was the main image, reassign to the next remaining image or clear
            if (product != null && product.Image == existing.ImageUrl)
            {
                var remaining = (await _repository.GetByProductIdAsync(existing.ProductId)).ToList();
                product.Image = remaining.Count > 0 ? remaining[0].ImageUrl : string.Empty;
                await _productRepository.UpdateAsync(product);
            }

            return Ok(new { message = "Image deleted successfully" });
        }

        /// <summary>Delete all images for a product and remove their files from disk (ModAccess).</summary>
        [HttpDelete("product/{productId}")]
        [Authorize(Policy = "ModAccess")]
        public async Task<IActionResult> DeleteByProduct(int productId)
        {
            var images = (await _repository.GetByProductIdAsync(productId)).ToList();
            foreach (var img in images)
                DeleteFileOnDisk(img.ImageUrl);
            await _repository.DeleteByProductIdAsync(productId);
            return Ok(new { message = "All images deleted for product" });
        }
    }

    [ApiController]
    [Route("api/inventory-logs")]
    [Produces("application/json")]
    [Authorize]
    public class InventoryLogController : ControllerBase
    {
        private readonly IInventoryLogRepository _repository;

        public InventoryLogController(IInventoryLogRepository repository)
        {
            _repository = repository;
        }

        private static InventoryLogResponse ToResponse(InventoryLog l) => new()
        {
            LogId          = l.LogId,
            ProductId      = l.ProductId,
            ChangeQuantity = l.ChangeQuantity,
            Action         = l.Action,
            LogDate        = l.LogDate,
        };

        /// <summary>Get all inventory logs (AdminAccess)</summary>
        [HttpGet]
        [Authorize(Policy = "AdminAccess")]
        public async Task<ActionResult<IEnumerable<InventoryLogResponse>>> GetAll()
        {
            var result = await _repository.GetAllAsync();
            return Ok(result.Select(ToResponse).ToList());
        }

        /// <summary>Get logs for a product (AdminAccess)</summary>
        [HttpGet("product/{productId}")]
        [Authorize(Policy = "AdminAccess")]
        public async Task<ActionResult<IEnumerable<InventoryLogResponse>>> GetByProduct(int productId)
        {
            var result = await _repository.GetByProductIdAsync(productId);
            return Ok(result.Select(ToResponse).ToList());
        }

        /// <summary>Get inventory log by ID (AdminAccess)</summary>
        [HttpGet("{id}")]
        [Authorize(Policy = "AdminAccess")]
        public async Task<ActionResult<InventoryLogResponse>> GetById(int id)
        {
            var entity = await _repository.GetByIdAsync(id);
            if (entity == null) return NotFound(new { message = "Log not found" });
            return Ok(ToResponse(entity));
        }

        /// <summary>Get paginated logs (AdminAccess)</summary>
        [HttpGet("paginated")]
        [Authorize(Policy = "AdminAccess")]
        public async Task<ActionResult> GetPaginated(int pageNumber = 1, int pageSize = 25)
        {
            var paged = await _repository.GetPaginatedAsync(pageNumber, pageSize);
            return Ok(new PaginationModel<InventoryLogResponse>
            {
                Items       = paged.Items.Select(ToResponse).ToList(),
                TotalCount  = paged.TotalCount,
                PageSize    = paged.PageSize,
                CurrentPage = paged.CurrentPage,
            });
        }

        /// <summary>Create an inventory log entry (AdminAccess)</summary>
        [HttpPost]
        [Authorize(Policy = "AdminAccess")]
        public async Task<IActionResult> Create([FromBody] CreateInventoryLogRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Action))
                return BadRequest(new { message = "Action is required." });

            var entity = new InventoryLog
            {
                ProductId      = request.ProductId,
                ChangeQuantity = request.ChangeQuantity,
                Action         = request.Action,
            };

            var newId = await _repository.AddAsync(entity);
            return Ok(new { message = "Inventory log created", logId = newId });
        }

        /// <summary>Delete an inventory log entry (AdminAccess)</summary>
        [HttpDelete("{id}")]
        [Authorize(Policy = "AdminAccess")]
        public async Task<IActionResult> Delete(int id)
        {
            var existing = await _repository.GetByIdAsync(id);
            if (existing == null) return NotFound(new { message = "Log not found" });

            await _repository.DeleteAsync(id);
            return Ok(new { message = "Log deleted successfully" });
        }
    }
}
