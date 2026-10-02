using System.ComponentModel.DataAnnotations;

namespace ClothesShopping.Api.DTOs
{
    public class UpdateUsermanRequest
    {
        [Required]
        [StringLength(100)]
        public string UserName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        [MinLength(6)]
        public string? NewPassword { get; set; }

        public int UserRoleId { get; set; }

    }
}