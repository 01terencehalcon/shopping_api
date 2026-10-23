using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace ClothesShopping.Api.DTOs
{
    public class RegisterRequest
    {
        [Required]
        [StringLength(100)]
        [JsonPropertyName("username")]
        public string Username { get; set; } = string.Empty;

        [JsonPropertyName("email")]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MinLength(6)]
        [JsonPropertyName("password")]
        public string Password { get; set; } = string.Empty;
    }
}