using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using ClothesShopping.Api.CartModule;
using ClothesShopping.Api.CartItemsModule;
using ClothesShopping.Api.DTOs;
using ClothesShopping.Api.Security;

namespace ClothesShopping.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    [Authorize(Policy = "UserAccess")]
    public class CartController : ControllerBase
    {
        private readonly ICartRepository _cartRepository;
        private readonly ICartItemRepository _cartItemRepository;

        public CartController(ICartRepository cartRepository, ICartItemRepository cartItemRepository)
        {
            _cartRepository     = cartRepository;
            _cartItemRepository = cartItemRepository;
        }

        private static CartItemResponse ToItemResponse(CartItem i) => new()
        {
            CartItemId = i.CartItemId,
            CartId     = i.CartId,
            ProductId  = i.ProductId,
            Quantity   = i.Quantity,
        };

        /// <summary>Get cart with all items for the current user (UserAccess)</summary>
        [HttpGet("user/{userId}")]
        public async Task<ActionResult<CartResponse>> GetCartByUser(int userId)
        {
            var currentUserId = ClaimsHelper.CurrentUserId(User);
            if (!ClaimsHelper.IsAdmin(User) && userId != currentUserId)
                return NotFound(new { message = "Cart not found for user" });

            var cart = await _cartRepository.GetByUserIdAsync(userId);
            if (cart == null)
                return Ok(new CartResponse { UserId = userId, Items = new() });

            var items = await _cartItemRepository.GetByCartIdAsync(cart.CartId);
            return Ok(new CartResponse
            {
                CartId    = cart.CartId,
                UserId    = cart.UserId,
                CreatedAt = cart.CreatedAt,
                Items     = items.Select(ToItemResponse).ToList(),
            });
        }

        /// <summary>Add item to cart (creates cart if not exists) (UserAccess)</summary>
        [HttpPost("items")]
        public async Task<IActionResult> AddItem([FromBody] AddCartItemRequest request)
        {
            if (request.ProductId <= 0) return BadRequest(new { message = "ProductId is required." });

            var currentUserId = ClaimsHelper.CurrentUserId(User);

            // Ensure cart exists for user
            var cart = await _cartRepository.GetByUserIdAsync(currentUserId);
            if (cart == null)
            {
                var cartId = await _cartRepository.AddAsync(new Cart { UserId = currentUserId });
                cart = await _cartRepository.GetByIdAsync(cartId);
            }

            var cartItemId = await _cartItemRepository.AddAsync(new CartItem
            {
                CartId    = cart!.CartId,
                ProductId = request.ProductId,
                Quantity  = request.Quantity,
            });

            return Ok(new { message = "Item added to cart", cartItemId });
        }

        /// <summary>Update cart item quantity (UserAccess — ownership enforced)</summary>
        [HttpPut("items/{cartItemId}")]
        public async Task<IActionResult> UpdateItemQuantity(int cartItemId, [FromBody] UpdateCartItemRequest request)
        {
            var item = await _cartItemRepository.GetByIdAsync(cartItemId);
            if (item == null) return NotFound(new { message = "Cart item not found" });

            // Verify the cart item belongs to the current user
            var cart = await _cartRepository.GetByIdAsync(item.CartId);
            var currentUserId = ClaimsHelper.CurrentUserId(User);
            if (!ClaimsHelper.IsAdmin(User) && (cart == null || cart.UserId != currentUserId))
                return NotFound(new { message = "Cart item not found" });

            await _cartItemRepository.UpdateQuantityAsync(cartItemId, request.Quantity);
            return Ok(new { message = "Cart item updated" });
        }

        /// <summary>Remove a single item from cart (UserAccess — ownership enforced)</summary>
        [HttpDelete("items/{cartItemId}")]
        public async Task<IActionResult> RemoveItem(int cartItemId)
        {
            var item = await _cartItemRepository.GetByIdAsync(cartItemId);
            if (item == null) return NotFound(new { message = "Cart item not found" });

            // Verify ownership
            var cart = await _cartRepository.GetByIdAsync(item.CartId);
            var currentUserId = ClaimsHelper.CurrentUserId(User);
            if (!ClaimsHelper.IsAdmin(User) && (cart == null || cart.UserId != currentUserId))
                return NotFound(new { message = "Cart item not found" });

            await _cartItemRepository.DeleteAsync(cartItemId);
            return Ok(new { message = "Cart item removed" });
        }

        /// <summary>Clear all items in the current user's cart (UserAccess)</summary>
        [HttpDelete("user/{userId}")]
        public async Task<IActionResult> ClearCart(int userId)
        {
            var currentUserId = ClaimsHelper.CurrentUserId(User);
            if (!ClaimsHelper.IsAdmin(User) && userId != currentUserId)
                return NotFound(new { message = "Cart not found for user" });

            var cart = await _cartRepository.GetByUserIdAsync(userId);
            if (cart == null) return NotFound(new { message = "Cart not found for user" });

            await _cartItemRepository.DeleteByCartIdAsync(cart.CartId);
            return Ok(new { message = "Cart cleared successfully" });
        }
    }
}
