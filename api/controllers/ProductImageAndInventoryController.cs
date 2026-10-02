using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using ClothesShopping.Api.ProductImagesModule;
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

        public ProductImageController(IProductImageRepository repository)
        {
            _repository = repository;
        }

        private static ProductImageResponse ToResponse(ProductImage p) => new()
        {
            ImageId   = p.ImageId,
            ProductId = p.ProductId,
            ImageUrl  = p.ImageUrl,
        };

        /// <summary>Get all images for a product (public)</summary>
        [HttpGet("product/{productId}")]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<ProductImageResponse>>> GetByProduct(int productId)
        {
            try
            {
                var result = await _repository.GetByProductIdAsync(productId);
                return Ok(result.Select(ToResponse).ToList());
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        /// <summary>Get image by ID (public)</summary>
        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<ActionResult<ProductImageResponse>> GetById(int id)
        {
            try
            {
                var entity = await _repository.GetByIdAsync(id);
                if (entity == null) return NotFound(new { message = "Image not found" });
                return Ok(ToResponse(entity));
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        /// <summary>Add image to a product (AdminAccess)</summary>
        [HttpPost]
        [Authorize(Policy = "AdminAccess")]
        public async Task<IActionResult> Create([FromBody] CreateProductImageRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.ImageUrl))
                    return BadRequest(new { message = "ImageUrl is required." });

                var entity = new ProductImage
                {
                    ProductId = request.ProductId,
                    ImageUrl  = request.ImageUrl,
                };

                var newId = await _repository.AddAsync(entity);
                return Ok(new { message = "Image added successfully", imageId = newId });
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        /// <summary>Delete an image by ID (AdminAccess)</summary>
        [HttpDelete("{id}")]
        [Authorize(Policy = "AdminAccess")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var existing = await _repository.GetByIdAsync(id);
                if (existing == null) return NotFound(new { message = "Image not found" });

                await _repository.DeleteAsync(id);
                return Ok(new { message = "Image deleted successfully" });
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        /// <summary>Delete all images for a product (AdminAccess)</summary>
        [HttpDelete("product/{productId}")]
        [Authorize(Policy = "AdminAccess")]
        public async Task<IActionResult> DeleteByProduct(int productId)
        {
            try
            {
                await _repository.DeleteByProductIdAsync(productId);
                return Ok(new { message = "All images deleted for product" });
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
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
            try
            {
                var result = await _repository.GetAllAsync();
                return Ok(result.Select(ToResponse).ToList());
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        /// <summary>Get logs for a product (AdminAccess)</summary>
        [HttpGet("product/{productId}")]
        [Authorize(Policy = "AdminAccess")]
        public async Task<ActionResult<IEnumerable<InventoryLogResponse>>> GetByProduct(int productId)
        {
            try
            {
                var result = await _repository.GetByProductIdAsync(productId);
                return Ok(result.Select(ToResponse).ToList());
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        /// <summary>Get inventory log by ID (AdminAccess)</summary>
        [HttpGet("{id}")]
        [Authorize(Policy = "AdminAccess")]
        public async Task<ActionResult<InventoryLogResponse>> GetById(int id)
        {
            try
            {
                var entity = await _repository.GetByIdAsync(id);
                if (entity == null) return NotFound(new { message = "Log not found" });
                return Ok(ToResponse(entity));
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        /// <summary>Get paginated logs (AdminAccess)</summary>
        [HttpGet("paginated")]
        [Authorize(Policy = "AdminAccess")]
        public async Task<ActionResult> GetPaginated(int pageNumber = 1, int pageSize = 25)
        {
            try
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
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        /// <summary>Create an inventory log entry (AdminAccess)</summary>
        [HttpPost]
        [Authorize(Policy = "AdminAccess")]
        public async Task<IActionResult> Create([FromBody] CreateInventoryLogRequest request)
        {
            try
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
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        /// <summary>Delete an inventory log entry (AdminAccess)</summary>
        [HttpDelete("{id}")]
        [Authorize(Policy = "AdminAccess")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var existing = await _repository.GetByIdAsync(id);
                if (existing == null) return NotFound(new { message = "Log not found" });

                await _repository.DeleteAsync(id);
                return Ok(new { message = "Log deleted successfully" });
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }
    }
}
