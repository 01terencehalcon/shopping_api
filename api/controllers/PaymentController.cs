using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using ClothesShopping.Api.PaymentsModule;
using ClothesShopping.Api.DTOs;
using ClothesShopping.Api.Main;

namespace ClothesShopping.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    [Authorize]
    public class PaymentController : ControllerBase
    {
        private readonly IPaymentRepository _repository;

        public PaymentController(IPaymentRepository repository)
        {
            _repository = repository;
        }

        private static PaymentResponse ToResponse(Payment p) => new()
        {
            PaymentId     = p.PaymentId,
            OrderId       = p.OrderId,
            PaymentDate   = p.PaymentDate,
            Amount        = p.Amount,
            PaymentMethod = p.PaymentMethod,
            PaymentStatus = p.PaymentStatus,
            CouponId      = p.CouponId,
        };

        /// <summary>Get all payments (AdminAccess)</summary>
        [HttpGet]
        [Authorize(Policy = "AdminAccess")]
        public async Task<ActionResult<IEnumerable<PaymentResponse>>> GetAll()
        {
            try
            {
                var result = await _repository.GetAllAsync();
                return Ok(result.Select(ToResponse).ToList());
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        /// <summary>Get payment by ID (UserAccess)</summary>
        [HttpGet("{id}")]
        [Authorize(Policy = "UserAccess")]
        public async Task<ActionResult<PaymentResponse>> GetById(int id)
        {
            try
            {
                var entity = await _repository.GetByIdAsync(id);
                if (entity == null) return NotFound(new { message = "Payment not found" });
                return Ok(ToResponse(entity));
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        /// <summary>Get payments by order ID (UserAccess)</summary>
        [HttpGet("order/{orderId}")]
        [Authorize(Policy = "UserAccess")]
        public async Task<ActionResult<IEnumerable<PaymentResponse>>> GetByOrder(int orderId)
        {
            try
            {
                var result = await _repository.GetByOrderIdAsync(orderId);
                return Ok(result.Select(ToResponse).ToList());
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        /// <summary>Get paginated payments (AdminAccess)</summary>
        [HttpGet("paginated")]
        [Authorize(Policy = "AdminAccess")]
        public async Task<ActionResult> GetPaginated(int pageNumber = 1, int pageSize = 25)
        {
            try
            {
                var paged = await _repository.GetPaginatedAsync(pageNumber, pageSize);
                return Ok(new PaginationModel<PaymentResponse>
                {
                    Items       = paged.Items.Select(ToResponse).ToList(),
                    TotalCount  = paged.TotalCount,
                    PageSize    = paged.PageSize,
                    CurrentPage = paged.CurrentPage,
                });
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        /// <summary>Create a payment (UserAccess)</summary>
        [HttpPost]
        [Authorize(Policy = "UserAccess")]
        public async Task<IActionResult> Create([FromBody] CreatePaymentRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.PaymentMethod))
                    return BadRequest(new { message = "PaymentMethod is required." });

                var entity = new Payment
                {
                    OrderId       = request.OrderId,
                    Amount        = request.Amount,
                    PaymentMethod = request.PaymentMethod,
                    PaymentStatus = request.PaymentStatus,
                    CouponId      = request.CouponId,
                };

                var newId = await _repository.AddAsync(entity);
                return Ok(new { message = "Payment created successfully", paymentId = newId });
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        /// <summary>Update payment status (AdminAccess)</summary>
        [HttpPut("{id}/status")]
        [Authorize(Policy = "AdminAccess")]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdatePaymentStatusRequest request)
        {
            try
            {
                var existing = await _repository.GetByIdAsync(id);
                if (existing == null) return NotFound(new { message = "Payment not found" });

                await _repository.UpdateStatusAsync(id, request.Status);
                return Ok(new { message = "Payment status updated" });
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        /// <summary>Delete a payment (AdminAccess)</summary>
        [HttpDelete("{id}")]
        [Authorize(Policy = "AdminAccess")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var existing = await _repository.GetByIdAsync(id);
                if (existing == null) return NotFound(new { message = "Payment not found" });

                await _repository.DeleteAsync(id);
                return Ok(new { message = "Payment deleted successfully" });
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }
    }
}
