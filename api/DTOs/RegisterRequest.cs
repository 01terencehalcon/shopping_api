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

        [Required]
        [Range(1, 3, ErrorMessage = "RoleId must be 1 (admin), 2 (staff), or 3 (user).")]
        [JsonPropertyName("role_id")]
        public int RoleId { get; set; } = 3;
    }
}