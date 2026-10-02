using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using ClothesShopping.Api.CartModule;
using ClothesShopping.Api.CartItemsModule;
using ClothesShopping.Api.DTOs;

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

        /// <summary>Get cart with all items for a user (UserAccess)</summary>
        [HttpGet("user/{userId}")]
        public async Task<ActionResult<CartResponse>> GetCartByUser(int userId)
        {
            try
            {
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
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        /// <summary>Add item to cart (creates cart if not exists) (UserAccess)</summary>
        [HttpPost("items")]
        public async Task<IActionResult> AddItem([FromBody] AddCartItemRequest request)
        {
            try
            {
                if (request.ProductId <= 0) return BadRequest(new { message = "ProductId is required." });

                // Ensure cart exists for user
                var cart = await _cartRepository.GetByUserIdAsync(request.UserId);
                if (cart == null)
                {
                    var cartId = await _cartRepository.AddAsync(new Cart { UserId = request.UserId });
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
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        /// <summary>Update cart item quantity (UserAccess)</summary>
        [HttpPut("items/{cartItemId}")]
        public async Task<IActionResult> UpdateItemQuantity(int cartItemId, [FromBody] UpdateCartItemRequest request)
        {
            try
            {
                var item = await _cartItemRepository.GetByIdAsync(cartItemId);
                if (item == null) return NotFound(new { message = "Cart item not found" });

                await _cartItemRepository.UpdateQuantityAsync(cartItemId, request.Quantity);
                return Ok(new { message = "Cart item updated" });
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        /// <summary>Remove a single item from cart (UserAccess)</summary>
        [HttpDelete("items/{cartItemId}")]
        public async Task<IActionResult> RemoveItem(int cartItemId)
        {
            try
            {
                var item = await _cartItemRepository.GetByIdAsync(cartItemId);
                if (item == null) return NotFound(new { message = "Cart item not found" });

                await _cartItemRepository.DeleteAsync(cartItemId);
                return Ok(new { message = "Cart item removed" });
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        /// <summary>Clear all items in a user's cart (UserAccess)</summary>
        [HttpDelete("user/{userId}")]
        public async Task<IActionResult> ClearCart(int userId)
        {
            try
            {
                var cart = await _cartRepository.GetByUserIdAsync(userId);
                if (cart == null) return NotFound(new { message = "Cart not found for user" });

                await _cartItemRepository.DeleteByCartIdAsync(cart.CartId);
                return Ok(new { message = "Cart cleared successfully" });
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }
    }
}
