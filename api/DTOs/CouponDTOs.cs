using System.ComponentModel.DataAnnotations;

namespace ClothesShopping.Api.DTOs
{
    public class CreateCouponRequest
    {
        [Required]
        public string Code { get; set; } = string.Empty;

        [Required]
        [Range(0, 100)]
        public double Discount { get; set; }

        public string? ExpirationDate { get; set; }
        public string? Status { get; set; } = "active";
    }

    public class UpdateCouponRequest
    {
        public string? Code { get; set; }

        [Range(0, 100)]
        public double Discount { get; set; }

        public string? ExpirationDate { get; set; }
        public string? Status { get; set; }
    }

    public class CouponResponse
    {
        public int CouponId { get; set; }
        public string? Code { get; set; }
        public double Discount { get; set; }
        public string? ExpirationDate { get; set; }
        public string? Status { get; set; }
    }

    public class ValidateCouponResponse
    {
        public bool IsValid { get; set; }
        public string? Message { get; set; }
        public CouponResponse? Coupon { get; set; }
    }
}
