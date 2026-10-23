using System.ComponentModel.DataAnnotations;

namespace ClothesShopping.Api.DTOs
{
    public class AddCartItemRequest
    {
        [Required]
        public int ProductId { get; set; }

        [Required]
        [Range(1, int.MaxValue)]
        public int Quantity { get; set; } = 1;
    }

    public class UpdateCartItemRequest
    {
        [Required]
        [Range(1, int.MaxValue)]
        public int Quantity { get; set; }
    }

    public class CartItemResponse
    {
        public int CartItemId { get; set; }
        public int CartId { get; set; }
        public int ProductId { get; set; }
        public int Quantity { get; set; }
    }

    public class CartResponse
    {
        public int CartId { get; set; }
        public int UserId { get; set; }
        public string CreatedAt { get; set; } = string.Empty;
        public List<CartItemResponse> Items { get; set; } = new();
    }
}
