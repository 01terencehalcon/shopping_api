using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace ClothesShopping.Api.DTOs
{
    public class StaffRegisterRequest
    {
        [Required]
        [StringLength(50, MinimumLength = 3)]
        [JsonPropertyName("username")]
        public string Username { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [JsonPropertyName("email")]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MinLength(8)]
        [JsonPropertyName("password")]
        public string Password { get; set; } = string.Empty;

        [Required]
        [JsonPropertyName("inviteCode")]
        public string InviteCode { get; set; } = string.Empty;
    }
}
