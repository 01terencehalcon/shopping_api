using System.ComponentModel.DataAnnotations;

namespace ClothesShopping.Api.DTOs
{
    public class CreateNotificationRequest
    {
        [Required]
        public int UserId { get; set; }

        [Required]
        public string Message { get; set; } = string.Empty;
    }

    public class NotificationResponse
    {
        public int NotificationId { get; set; }
        public int UserId { get; set; }
        public string? Message { get; set; }
        public bool IsRead { get; set; }
        public string CreatedAt { get; set; } = string.Empty;
    }
}
