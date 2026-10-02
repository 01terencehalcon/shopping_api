using System.ComponentModel.DataAnnotations;

namespace ClothesShopping.Api.DTOs
{
    public class ForgotPasswordRequest
    {
        [Required]
        public string Username { get; set; } = string.Empty;
    }
}