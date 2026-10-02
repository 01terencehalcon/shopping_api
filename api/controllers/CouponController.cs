using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using ClothesShopping.Api.CouponsModule;
using ClothesShopping.Api.DTOs;
using ClothesShopping.Api.Main;

namespace ClothesShopping.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    [Authorize]
    public class CouponController : ControllerBase
    {
        private readonly ICouponRepository _repository;

        public CouponController(ICouponRepository repository)
        {
            _repository = repository;
        }

        private static CouponResponse ToResponse(Coupon c) => new()
        {
            CouponId       = c.CouponId,
            Code           = c.Code,
            Discount       = c.Discount,
            ExpirationDate = c.ExpirationDate,
            Status         = c.Status,
        };

        /// <summary>Get all coupons (AdminAccess)</summary>
        [HttpGet]
        [Authorize(Policy = "AdminAccess")]
        public async Task<ActionResult<IEnumerable<CouponResponse>>> GetAll()
        {
            try
            {
                var result = await _repository.GetAllAsync();
                return Ok(result.Select(ToResponse).ToList());
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        /// <summary>Get coupon by ID (AdminAccess)</summary>
        [HttpGet("{id}")]
        [Authorize(Policy = "AdminAccess")]
        public async Task<ActionResult<CouponResponse>> GetById(int id)
        {
            try
            {
                var entity = await _repository.GetByIdAsync(id);
                if (entity == null) return NotFound(new { message = "Coupon not found" });
                return Ok(ToResponse(entity));
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        /// <summary>Validate a coupon code (UserAccess)</summary>
        [HttpGet("validate/{code}")]
        [Authorize(Policy = "UserAccess")]
        public async Task<ActionResult<ValidateCouponResponse>> Validate(string code)
        {
            try
            {
                var coupon = await _repository.GetByCodeAsync(code);
                if (coupon == null)
                    return Ok(new ValidateCouponResponse { IsValid = false, Message = "Coupon not found." });

                // Check status
                if (!string.Equals(coupon.Status, "active", StringComparison.OrdinalIgnoreCase))
                    return Ok(new ValidateCouponResponse { IsValid = false, Message = "Coupon is not active." });

                // Check expiry
                if (!string.IsNullOrWhiteSpace(coupon.ExpirationDate) &&
                    DateTime.TryParse(coupon.ExpirationDate, out var expiry) &&
                    expiry.Date < DateTime.UtcNow.Date)
                    return Ok(new ValidateCouponResponse { IsValid = false, Message = "Coupon has expired." });

                return Ok(new ValidateCouponResponse { IsValid = true, Message = "Coupon is valid.", Coupon = ToResponse(coupon) });
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        /// <summary>Get paginated coupons (AdminAccess)</summary>
        [HttpGet("paginated")]
        [Authorize(Policy = "AdminAccess")]
        public async Task<ActionResult> GetPaginated(int pageNumber = 1, int pageSize = 25)
        {
            try
            {
                var paged = await _repository.GetPaginatedAsync(pageNumber, pageSize);
                return Ok(new PaginationModel<CouponResponse>
                {
                    Items       = paged.Items.Select(ToResponse).ToList(),
                    TotalCount  = paged.TotalCount,
                    PageSize    = paged.PageSize,
                    CurrentPage = paged.CurrentPage,
                });
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        /// <summary>Create a coupon (AdminAccess)</summary>
        [HttpPost]
        [Authorize(Policy = "AdminAccess")]
        public async Task<IActionResult> Create([FromBody] CreateCouponRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Code))
                    return BadRequest(new { message = "Code is required." });

                var existing = await _repository.GetByCodeAsync(request.Code);
                if (existing != null) return Conflict(new { message = "Coupon code already exists." });

                var entity = new Coupon
                {
                    Code           = request.Code,
                    Discount       = request.Discount,
                    ExpirationDate = request.ExpirationDate,
                    Status         = request.Status,
                };

                var newId = await _repository.AddAsync(entity);
                return Ok(new { message = "Coupon created successfully", couponId = newId });
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        /// <summary>Update a coupon (AdminAccess)</summary>
        [HttpPut("{id}")]
        [Authorize(Policy = "AdminAccess")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateCouponRequest request)
        {
            try
            {
                var existing = await _repository.GetByIdAsync(id);
                if (existing == null) return NotFound(new { message = "Coupon not found" });

                existing.Code           = request.Code ?? existing.Code;
                existing.Discount       = request.Discount;
                existing.ExpirationDate = request.ExpirationDate;
                existing.Status         = request.Status ?? existing.Status;

                await _repository.UpdateAsync(existing);
                return Ok(new { message = "Coupon updated successfully" });
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        /// <summary>Delete a coupon (AdminAccess)</summary>
        [HttpDelete("{id}")]
        [Authorize(Policy = "AdminAccess")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var existing = await _repository.GetByIdAsync(id);
                if (existing == null) return NotFound(new { message = "Coupon not found" });

                await _repository.DeleteAsync(id);
                return Ok(new { message = "Coupon deleted successfully" });
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }
    }
}
