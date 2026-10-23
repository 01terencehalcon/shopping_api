using System.ComponentModel.DataAnnotations;

namespace ClothesShopping.Api.DTOs
{
    public class CreateProductRequest
    {
        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        [StringLength(100)]
        public string? Category { get; set; }

        [Required]
        [Range(0, double.MaxValue)]
        public double Price { get; set; }

        [Range(0, int.MaxValue)]
        public int Stock { get; set; } = 0;

        public string? Image { get; set; }
        public string? Description { get; set; }
    }

    public class CreateProductWithFileRequest
    {
        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        [StringLength(100)]
        public string? Category { get; set; }

        [Required]
        [Range(0, double.MaxValue)]
        public double Price { get; set; }

        [Range(0, int.MaxValue)]
        public int Stock { get; set; } = 0;

        public string? ImageUrl { get; set; }

        public Microsoft.AspNetCore.Http.IFormFile? ImageFile { get; set; }

        public string? Description { get; set; }
    }

    public class ImageUploadResponse
    {
        public string ImageUrl { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }

    public class UpdateProductRequest
    {
        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        [StringLength(100)]
        public string? Category { get; set; }

        [Required]
        [Range(0, double.MaxValue)]
        public double Price { get; set; }

        [Range(0, int.MaxValue)]
        public int Stock { get; set; }

        public string? Image { get; set; }
        public string? Description { get; set; }
    }

    public class ProductResponse
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Category { get; set; }
        public double Price { get; set; }
        public int Stock { get; set; }
        public string? Image { get; set; }
        public string? Description { get; set; }
    }

    public class TopProductResponse : ProductResponse
    {
        public int SoldCount { get; set; }
        public double AverageRating { get; set; }
        public int ReviewCount { get; set; }
    }
}
