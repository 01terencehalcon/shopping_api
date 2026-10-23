using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using ClothesShopping.Api.PaymentsModule;
using ClothesShopping.Api.OrdersModule;
using ClothesShopping.Api.DTOs;
using ClothesShopping.Api.Main;
using ClothesShopping.Api.Security;

namespace ClothesShopping.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    [Authorize]
    public class PaymentController : ControllerBase
    {
        private readonly IPaymentRepository _repository;
        private readonly IOrderRepository _orderRepository;

        public PaymentController(IPaymentRepository repository, IOrderRepository orderRepository)
        {
            _repository = repository;
            _orderRepository = orderRepository;
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
            var result = await _repository.GetAllAsync();
            return Ok(result.Select(ToResponse).ToList());
        }

        /// <summary>Get payment by ID (UserAccess — ownership enforced via order)</summary>
        [HttpGet("{id}")]
        [Authorize(Policy = "UserAccess")]
        public async Task<ActionResult<PaymentResponse>> GetById(int id)
        {
            var entity = await _repository.GetByIdAsync(id);
            if (entity == null) return NotFound(new { message = "Payment not found" });

            // Ownership: look up the parent order
            var order = await _orderRepository.GetByIdAsync(entity.OrderId);
            var currentUserId = ClaimsHelper.CurrentUserId(User);
            if (!ClaimsHelper.IsAdmin(User) && (order == null || order.UserId != currentUserId))
                return NotFound(new { message = "Payment not found" });

            return Ok(ToResponse(entity));
        }

        /// <summary>Get payments by order ID (UserAccess — ownership enforced)</summary>
        [HttpGet("order/{orderId}")]
        [Authorize(Policy = "UserAccess")]
        public async Task<ActionResult<IEnumerable<PaymentResponse>>> GetByOrder(int orderId)
        {
            // Verify order ownership
            var order = await _orderRepository.GetByIdAsync(orderId);
            if (order == null) return NotFound(new { message = "Payment not found" });

            var currentUserId = ClaimsHelper.CurrentUserId(User);
            if (!ClaimsHelper.IsAdmin(User) && order.UserId != currentUserId)
                return NotFound(new { message = "Payment not found" });

            var result = await _repository.GetByOrderIdAsync(orderId);
            return Ok(result.Select(ToResponse).ToList());
        }

        /// <summary>Get paginated payments (AdminAccess)</summary>
        [HttpGet("paginated")]
        [Authorize(Policy = "AdminAccess")]
        public async Task<ActionResult> GetPaginated(int pageNumber = 1, int pageSize = 25)
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

        /// <summary>Create a payment (UserAccess — amount from order, status always pending)</summary>
        [HttpPost]
        [Authorize(Policy = "UserAccess")]
        public async Task<IActionResult> Create([FromBody] CreatePaymentRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.PaymentMethod))
                return BadRequest(new { message = "PaymentMethod is required." });

            // Verify the order exists and belongs to the caller
            var order = await _orderRepository.GetByIdAsync(request.OrderId);
            if (order == null) return NotFound(new { message = "Order not found" });

            var currentUserId = ClaimsHelper.CurrentUserId(User);
            if (!ClaimsHelper.IsAdmin(User) && order.UserId != currentUserId)
                return NotFound(new { message = "Order not found" });

            var entity = new Payment
            {
                OrderId       = request.OrderId,
                Amount        = order.Total,            // Amount comes from the order
                PaymentMethod = request.PaymentMethod,
                PaymentStatus = "pending",              // Non-admins always start as pending
                CouponId      = request.CouponId ?? 0,
            };

            var newId = await _repository.AddAsync(entity);
            return Ok(new { message = "Payment created successfully", paymentId = newId });
        }

        /// <summary>Update payment status (AdminAccess)</summary>
        [HttpPut("{id}/status")]
        [Authorize(Policy = "AdminAccess")]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdatePaymentStatusRequest request)
        {
            var existing = await _repository.GetByIdAsync(id);
            if (existing == null) return NotFound(new { message = "Payment not found" });

            await _repository.UpdateStatusAsync(id, request.Status);
            return Ok(new { message = "Payment status updated" });
        }

        /// <summary>Delete a payment (AdminAccess)</summary>
        [HttpDelete("{id}")]
        [Authorize(Policy = "AdminAccess")]
        public async Task<IActionResult> Delete(int id)
        {
            var existing = await _repository.GetByIdAsync(id);
            if (existing == null) return NotFound(new { message = "Payment not found" });

            await _repository.DeleteAsync(id);
            return Ok(new { message = "Payment deleted successfully" });
        }
    }
}
