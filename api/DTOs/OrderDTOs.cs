using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ClothesShopping.Api.DTOs
{
    public class CreateOrderRequest
    {
        public string? CouponCode { get; set; }
        public int? AddressId { get; set; }
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

        // Delivery snapshot
        public string? ShipRecipient { get; set; }
        public string? ShipPhone { get; set; }
        public string? ShipStreet { get; set; }
        public string? ShipCity { get; set; }
        public string? ShipProvince { get; set; }
        public string? ShipPostalCode { get; set; }
        public double? ShipLatitude { get; set; }
        public double? ShipLongitude { get; set; }
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
