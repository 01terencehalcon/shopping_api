using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Linq;
using ClothesShopping.Api.OrdersModule;
using ClothesShopping.Api.OrderItemsModule;
using ClothesShopping.Api.ProductsModule;
using ClothesShopping.Api.CartModule;
using ClothesShopping.Api.CartItemsModule;
using ClothesShopping.Api.CouponsModule;
using ClothesShopping.Api.AddressModule;
using ClothesShopping.Api.NotificationsModule;
using ClothesShopping.Api.OrderTrackingModule;
using ClothesShopping.Api.DTOs;
using ClothesShopping.Api.Main;
using ClothesShopping.Api.Security;

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
        private readonly IProductRepository _productRepository;
        private readonly ICartRepository _cartRepository;
        private readonly ICartItemRepository _cartItemRepository;
        private readonly ICouponRepository _couponRepository;
        private readonly IOrderTrackingRepository? _orderTrackingRepository;
        private readonly IAddressRepository? _addressRepository;
        private readonly INotificationRepository? _notificationRepository;
        private readonly IConfiguration? _configuration;

        public OrderController(
            IOrderRepository orderRepository,
            IOrderItemRepository orderItemRepository,
            IProductRepository productRepository,
            ICartRepository cartRepository,
            ICartItemRepository cartItemRepository,
            ICouponRepository couponRepository)
            : this(orderRepository, orderItemRepository, productRepository, cartRepository, cartItemRepository, couponRepository, null, null, null, null)
        {
        }

        public OrderController(
            IOrderRepository orderRepository,
            IOrderItemRepository orderItemRepository,
            IProductRepository productRepository,
            ICartRepository cartRepository,
            ICartItemRepository cartItemRepository,
            ICouponRepository couponRepository,
            IOrderTrackingRepository? orderTrackingRepository,
            IAddressRepository? addressRepository,
            INotificationRepository? notificationRepository,
            IConfiguration? configuration)
        {
            _orderRepository         = orderRepository;
            _orderItemRepository     = orderItemRepository;
            _productRepository       = productRepository;
            _cartRepository          = cartRepository;
            _cartItemRepository      = cartItemRepository;
            _couponRepository        = couponRepository;
            _orderTrackingRepository = orderTrackingRepository;
            _addressRepository       = addressRepository;
            _notificationRepository  = notificationRepository;
            _configuration           = configuration;
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
            var result = await _orderRepository.GetAllAsync();
            return Ok(result.Select(ToOrderResponse).ToList());
        }

        /// <summary>Get order by ID (UserAccess — ownership enforced)</summary>
        [HttpGet("{id}")]
        [Authorize(Policy = "UserAccess")]
        public async Task<ActionResult<OrderResponse>> GetById(int id)
        {
            var entity = await _orderRepository.GetByIdAsync(id);
            if (entity == null) return NotFound(new { message = "Order not found" });

            var currentUserId = ClaimsHelper.CurrentUserId(User);
            if (!ClaimsHelper.IsAdmin(User) && entity.UserId != currentUserId)
                return NotFound(new { message = "Order not found" });

            return Ok(ToOrderResponse(entity));
        }

        /// <summary>Get orders for the current user (admins can view any user)</summary>
        [HttpGet("user/{userId}")]
        [Authorize(Policy = "UserAccess")]
        public async Task<ActionResult<IEnumerable<OrderResponse>>> GetByUser(int userId)
        {
            var currentUserId = ClaimsHelper.CurrentUserId(User);
            if (!ClaimsHelper.IsAdmin(User) && userId != currentUserId)
                return NotFound(new { message = "Order not found" });

            var result = await _orderRepository.GetByUserIdAsync(userId);
            return Ok(result.Select(ToOrderResponse).ToList());
        }

        /// <summary>Get paginated orders (AdminAccess)</summary>
        [HttpGet("paginated")]
        [Authorize(Policy = "AdminAccess")]
        public async Task<ActionResult> GetPaginated(int pageNumber = 1, int pageSize = 25)
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

        /// <summary>Get items for an order (UserAccess — ownership enforced)</summary>
        [HttpGet("{id}/items")]
        [Authorize(Policy = "UserAccess")]
        public async Task<ActionResult<IEnumerable<OrderItemResponse>>> GetOrderItems(int id)
        {
            // Verify the order exists and belongs to the caller
            var order = await _orderRepository.GetByIdAsync(id);
            if (order == null) return NotFound(new { message = "Order not found" });

            var currentUserId = ClaimsHelper.CurrentUserId(User);
            if (!ClaimsHelper.IsAdmin(User) && order.UserId != currentUserId)
                return NotFound(new { message = "Order not found" });

            var items = await _orderItemRepository.GetByOrderIdAsync(id);
            return Ok(items.Select(ToItemResponse).ToList());
        }

        /// <summary>Create a new order from the user's cart (UserAccess)</summary>
        [HttpPost]
        [Authorize(Policy = "UserAccess")]
        public async Task<IActionResult> Create([FromBody] CreateOrderRequest request)
        {
            var currentUserId = ClaimsHelper.CurrentUserId(User);

            // 1. Get the user's cart
            var cart = await _cartRepository.GetByUserIdAsync(currentUserId);
            if (cart == null)
                return BadRequest(new { message = "Cart is empty." });

            var cartItems = (await _cartItemRepository.GetByCartIdAsync(cart.CartId)).ToList();
            if (cartItems.Count == 0)
                return BadRequest(new { message = "Cart is empty." });

            // 2. Look up product prices and validate stock
            var orderItems = new List<(int ProductId, int Quantity, double Price)>();
            foreach (var ci in cartItems)
            {
                var product = await _productRepository.GetByIdAsync(ci.ProductId);
                if (product == null)
                    return BadRequest(new { message = $"Product {ci.ProductId} not found." });
                if (product.Stock < ci.Quantity)
                    return BadRequest(new { message = $"Insufficient stock for product '{product.Name}'. Available: {product.Stock}, requested: {ci.Quantity}." });

                orderItems.Add((ci.ProductId, ci.Quantity, product.Price));
            }

            // 3. Compute total
            double total = orderItems.Sum(i => i.Price * i.Quantity);

            // 4. Apply coupon discount if provided
            if (!string.IsNullOrWhiteSpace(request.CouponCode))
            {
                var coupon = await _couponRepository.GetByCodeAsync(request.CouponCode);
                if (coupon == null)
                    return BadRequest(new { message = "Invalid coupon code." });

                if (!string.Equals(coupon.Status, "active", StringComparison.OrdinalIgnoreCase))
                    return BadRequest(new { message = "Coupon is not active." });

                if (!string.IsNullOrWhiteSpace(coupon.ExpirationDate) &&
                    DateTime.TryParse(coupon.ExpirationDate, out var expiry) &&
                    expiry.Date < DateTime.UtcNow.Date)
                    return BadRequest(new { message = "Coupon has expired." });

                total *= (1.0 - coupon.Discount / 100.0);
            }

            total = Math.Round(total, 2);

            // 5. Transactional: create order + items + decrement stock + clear cart
            var orderId = await _orderRepository.CreateOrderTransactionalAsync(
                currentUserId, total, orderItems, cart.CartId);

            // 6. Insert initial "pending" checkpoint at store location
            if (_orderTrackingRepository != null)
            {
                var storeLat = _configuration?.GetValue<double?>("StoreLocation:Latitude") ?? 40.7128;
                var storeLng = _configuration?.GetValue<double?>("StoreLocation:Longitude") ?? -74.0060;
                var storeLabel = _configuration?["StoreLocation:Label"] ?? "StyleShop Central Hub";

                await _orderTrackingRepository.AddAsync(new OrderTracking
                {
                    OrderId = orderId,
                    Status = "pending",
                    Latitude = storeLat,
                    Longitude = storeLng,
                    Note = "Order placed and waiting for fulfillment at " + storeLabel,
                    CreatedAt = DateTime.UtcNow.ToString("o")
                });
            }

            return Ok(new { message = "Order created successfully", id = orderId });
        }

        /// <summary>Get order tracking details (UserAccess - customer can only see their own order; admin/staff can see all)</summary>
        [HttpGet("{id}/tracking")]
        [Authorize(Policy = "UserAccess")]
        public async Task<ActionResult<OrderTrackingResponse>> GetTracking(int id)
        {
            var order = await _orderRepository.GetByIdAsync(id);
            if (order == null) return NotFound(new { message = "Order not found" });

            var currentUserId = ClaimsHelper.CurrentUserId(User);
            var roleClaim = User.FindFirst("user_role_id")?.Value;
            bool isStaffOrAdmin = roleClaim is "1" or "2";
            if (!isStaffOrAdmin && order.UserId != currentUserId)
                return NotFound(new { message = "Order not found" });

            var storeLat = _configuration?.GetValue<double?>("StoreLocation:Latitude") ?? 40.7128;
            var storeLng = _configuration?.GetValue<double?>("StoreLocation:Longitude") ?? -74.0060;
            var storeLabel = _configuration?["StoreLocation:Label"] ?? "StyleShop Central Hub";

            // Get customer's saved address
            var addresses = _addressRepository != null
                ? await _addressRepository.GetByUserIdAsync(order.UserId)
                : Enumerable.Empty<Address>();
            var deliveryAddr = addresses.FirstOrDefault();

            var checkpoints = _orderTrackingRepository != null
                ? (await _orderTrackingRepository.GetByOrderIdAsync(id)).ToList()
                : new List<OrderTracking>();

            var response = new OrderTrackingResponse
            {
                OrderId = order.Id,
                Status = order.Status,
                StoreLocation = new LocationPointDto
                {
                    Latitude = storeLat,
                    Longitude = storeLng,
                    Label = storeLabel
                },
                DeliveryAddress = deliveryAddr == null ? null : new DeliveryAddressLocationDto
                {
                    RecipientName = deliveryAddr.RecipientName,
                    Street = deliveryAddr.Street,
                    City = deliveryAddr.City,
                    Province = deliveryAddr.Province,
                    PostalCode = deliveryAddr.PostalCode,
                    Latitude = deliveryAddr.Latitude,
                    Longitude = deliveryAddr.Longitude
                },
                Checkpoints = checkpoints.Select(c => new TrackingCheckpointDto
                {
                    Id = c.Id,
                    OrderId = c.OrderId,
                    Status = c.Status,
                    Latitude = c.Latitude,
                    Longitude = c.Longitude,
                    Note = c.Note,
                    CreatedAt = c.CreatedAt
                }).ToList()
            };

            return Ok(response);
        }

        /// <summary>Add a tracking checkpoint (ModAccess - Admin / Staff only)</summary>
        [HttpPost("{id}/tracking")]
        [Authorize(Policy = "ModAccess")]
        public async Task<IActionResult> AddCheckpoint(int id, [FromBody] AddTrackingCheckpointRequest request)
        {
            if (request == null)
                return BadRequest(new { message = "Request body is required." });

            if (request.Latitude < -90.0 || request.Latitude > 90.0)
                return BadRequest(new { message = "Latitude must be between -90 and 90 degrees." });

            if (request.Longitude < -180.0 || request.Longitude > 180.0)
                return BadRequest(new { message = "Longitude must be between -180 and 180 degrees." });

            var validStatuses = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "pending", "packed", "shipped", "out_for_delivery", "delivered", "cancelled"
            };

            if (string.IsNullOrWhiteSpace(request.Status) || !validStatuses.Contains(request.Status))
            {
                return BadRequest(new { message = "Invalid status. Allowed values: pending, packed, shipped, out_for_delivery, delivered, cancelled." });
            }

            var order = await _orderRepository.GetByIdAsync(id);
            if (order == null) return NotFound(new { message = "Order not found" });

            int checkpointId = 0;
            if (_orderTrackingRepository != null)
            {
                var entity = new OrderTracking
                {
                    OrderId = id,
                    Status = request.Status.ToLowerInvariant(),
                    Latitude = request.Latitude,
                    Longitude = request.Longitude,
                    Note = request.Note,
                    CreatedAt = DateTime.UtcNow.ToString("o")
                };

                checkpointId = await _orderTrackingRepository.AddAsync(entity);
            }

            // Update order status in orders table
            order.Status = request.Status;
            await _orderRepository.UpdateAsync(order);

            // Create notification for customer
            if (_notificationRepository != null)
            {
                await _notificationRepository.AddAsync(new Notification
                {
                    UserId = order.UserId,
                    Message = $"Order #{order.Id} tracking update: {request.Status}." + (string.IsNullOrWhiteSpace(request.Note) ? "" : $" Note: {request.Note}"),
                    IsRead = 0,
                    CreatedAt = DateTime.UtcNow.ToString("o")
                });
            }

            return Ok(new { message = "Tracking checkpoint added successfully", id = checkpointId });
        }

        /// <summary>Update order status/total (AdminAccess)</summary>
        [HttpPut("{id}")]
        [Authorize(Policy = "AdminAccess")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateOrderRequest request)
        {
            var existing = await _orderRepository.GetByIdAsync(id);
            if (existing == null) return NotFound(new { message = "Order not found" });

            existing.Status = request.Status;
            existing.Total  = request.Total;
            await _orderRepository.UpdateAsync(existing);
            return Ok(new { message = "Order updated successfully" });
        }

        /// <summary>Delete an order and its items (AdminAccess)</summary>
        [HttpDelete("{id}")]
        [Authorize(Policy = "AdminAccess")]
        public async Task<IActionResult> Delete(int id)
        {
            var existing = await _orderRepository.GetByIdAsync(id);
            if (existing == null) return NotFound(new { message = "Order not found" });

            await _orderItemRepository.DeleteByOrderIdAsync(id);
            await _orderRepository.DeleteAsync(id);
            return Ok(new { message = "Order deleted successfully" });
        }
    }
}
