using System.ComponentModel.DataAnnotations;

namespace ClothesShopping.Api.DTOs
{
    public class CreateProductImageRequest
    {
        [Required]
        public int ProductId { get; set; }

        [Required]
        public string ImageUrl { get; set; } = string.Empty;
    }

    public class ProductImageResponse
    {
        public int ImageId { get; set; }
        public int ProductId { get; set; }
        public string? ImageUrl { get; set; }
    }

    public class CreateInventoryLogRequest
    {
        [Required]
        public int ProductId { get; set; }

        [Required]
        public int ChangeQuantity { get; set; }

        [Required]
        public string Action { get; set; } = string.Empty; // e.g. "restock", "sale", "adjustment"
    }

    public class InventoryLogResponse
    {
        public int LogId { get; set; }
        public int ProductId { get; set; }
        public int ChangeQuantity { get; set; }
        public string? Action { get; set; }
        public string LogDate { get; set; } = string.Empty;
    }
}
