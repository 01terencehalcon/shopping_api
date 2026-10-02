using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using ClothesShopping.Api.OrdersModule;
using ClothesShopping.Api.OrderItemsModule;
using ClothesShopping.Api.DTOs;
using ClothesShopping.Api.Main;

namespace ClothesShopping.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    [Authorize]
    public class OrderController : ControllerBase
    {
        private readonly IOrderRepository _orderRepository;
        private readonly IOrderItemRepository _orderItemRepository;

        public OrderController(IOrderRepository orderRepository, IOrderItemRepository orderItemRepository)
        {
            _orderRepository     = orderRepository;
            _orderItemRepository = orderItemRepository;
        }

        private static OrderResponse ToOrderResponse(Order o) => new()
        {
            Id        = o.Id,
            UserId    = o.UserId,
            Total     = o.Total,
            OrderDate = o.OrderDate,
            Status    = o.Status,
        };

        private static OrderItemResponse ToItemResponse(OrderItem i) => new()
        {
            Id        = i.Id,
            OrderId   = i.OrderId,
            ProductId = i.ProductId,
            Quantity  = i.Quantity,
            Price     = i.Price,
        };

        /// <summary>Get all orders (AdminAccess)</summary>
        [HttpGet]
        [Authorize(Policy = "AdminAccess")]
        public async Task<ActionResult<IEnumerable<OrderResponse>>> GetAll()
        {
            try
            {
                var result = await _orderRepository.GetAllAsync();
                return Ok(result.Select(ToOrderResponse).ToList());
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        /// <summary>Get order by ID (UserAccess)</summary>
        [HttpGet("{id}")]
        [Authorize(Policy = "UserAccess")]
        public async Task<ActionResult<OrderResponse>> GetById(int id)
        {
            try
            {
                var entity = await _orderRepository.GetByIdAsync(id);
                if (entity == null) return NotFound(new { message = "Order not found" });
                return Ok(ToOrderResponse(entity));
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        /// <summary>Get orders by user ID (UserAccess)</summary>
        [HttpGet("user/{userId}")]
        [Authorize(Policy = "UserAccess")]
        public async Task<ActionResult<IEnumerable<OrderResponse>>> GetByUser(int userId)
        {
            try
            {
                var result = await _orderRepository.GetByUserIdAsync(userId);
                return Ok(result.Select(ToOrderResponse).ToList());
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        /// <summary>Get paginated orders (AdminAccess)</summary>
        [HttpGet("paginated")]
        [Authorize(Policy = "AdminAccess")]
        public async Task<ActionResult> GetPaginated(int pageNumber = 1, int pageSize = 25)
        {
            try
            {
                var paged = await _orderRepository.GetPaginatedAsync(pageNumber, pageSize);
                return Ok(new PaginationModel<OrderResponse>
                {
                    Items       = paged.Items.Select(ToOrderResponse).ToList(),
                    TotalCount  = paged.TotalCount,
                    PageSize    = paged.PageSize,
                    CurrentPage = paged.CurrentPage,
                });
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        /// <summary>Get items for an order (UserAccess)</summary>
        [HttpGet("{id}/items")]
        [Authorize(Policy = "UserAccess")]
        public async Task<ActionResult<IEnumerable<OrderItemResponse>>> GetOrderItems(int id)
        {
            try
            {
                var items = await _orderItemRepository.GetByOrderIdAsync(id);
                return Ok(items.Select(ToItemResponse).ToList());
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        /// <summary>Create a new order with optional items (UserAccess)</summary>
        [HttpPost]
        [Authorize(Policy = "UserAccess")]
        public async Task<IActionResult> Create([FromBody] CreateOrderRequest request)
        {
            try
            {
                var order = new Order
                {
                    UserId    = request.UserId,
                    Total     = request.Total,
                    OrderDate = DateTime.UtcNow.ToString("o"),
                    Status    = request.Status,
                };

                var orderId = await _orderRepository.AddAsync(order);

                if (request.Items != null && request.Items.Count > 0)
                {
                    foreach (var item in request.Items)
                    {
                        await _orderItemRepository.AddAsync(new OrderItem
                        {
                            OrderId   = orderId,
                            ProductId = item.ProductId,
                            Quantity  = item.Quantity,
                            Price     = item.Price,
                        });
                    }
                }

                return Ok(new { message = "Order created successfully", id = orderId });
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        /// <summary>Update order status/total (AdminAccess)</summary>
        [HttpPut("{id}")]
        [Authorize(Policy = "AdminAccess")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateOrderRequest request)
        {
            try
            {
                var existing = await _orderRepository.GetByIdAsync(id);
                if (existing == null) return NotFound(new { message = "Order not found" });

                existing.Status = request.Status;
                existing.Total  = request.Total;
                await _orderRepository.UpdateAsync(existing);
                return Ok(new { message = "Order updated successfully" });
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        /// <summary>Delete an order and its items (AdminAccess)</summary>
        [HttpDelete("{id}")]
        [Authorize(Policy = "AdminAccess")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var existing = await _orderRepository.GetByIdAsync(id);
                if (existing == null) return NotFound(new { message = "Order not found" });

                await _orderItemRepository.DeleteByOrderIdAsync(id);
                await _orderRepository.DeleteAsync(id);
                return Ok(new { message = "Order deleted successfully" });
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }
    }
}
