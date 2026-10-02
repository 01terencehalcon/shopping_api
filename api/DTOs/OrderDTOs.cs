using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ClothesShopping.Api.DTOs
{
    public class CreateOrderRequest
    {
        [Required]
        public int UserId { get; set; }

        public double Total { get; set; }
        public string Status { get; set; } = "pending";

        public List<CreateOrderItemRequest>? Items { get; set; }
    }

    public class UpdateOrderRequest
    {
        public string Status { get; set; } = string.Empty;
        public double Total { get; set; }
    }

    public class OrderResponse
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public double Total { get; set; }
        public string OrderDate { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }

    public class CreateOrderItemRequest
    {
        [Required]
        public int ProductId { get; set; }

        [Required]
        [Range(1, int.MaxValue)]
        public int Quantity { get; set; }

        [Range(0, double.MaxValue)]
        public double Price { get; set; }
    }

    public class UpdateOrderItemRequest
    {
        [Required]
        [Range(1, int.MaxValue)]
        public int Quantity { get; set; }

        [Range(0, double.MaxValue)]
        public double Price { get; set; }
    }

    public class OrderItemResponse
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public double Price { get; set; }
    }
}
