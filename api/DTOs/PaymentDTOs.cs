using System.ComponentModel.DataAnnotations;

namespace ClothesShopping.Api.DTOs
{
    public class CreatePaymentRequest
    {
        [Required]
        public int OrderId { get; set; }

        [Required]
        [Range(0, double.MaxValue)]
        public double Amount { get; set; }

        [Required]
        public string PaymentMethod { get; set; } = string.Empty;

        public string PaymentStatus { get; set; } = "pending";

        public int CouponId { get; set; } = 0;
    }

    public class UpdatePaymentStatusRequest
    {
        [Required]
        public string Status { get; set; } = string.Empty;
    }

    public class PaymentResponse
    {
        public int PaymentId { get; set; }
        public int OrderId { get; set; }
        public string PaymentDate { get; set; } = string.Empty;
        public double Amount { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
        public string PaymentStatus { get; set; } = string.Empty;
        public int CouponId { get; set; }
    }
}
