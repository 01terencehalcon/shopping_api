using System.ComponentModel.DataAnnotations;

namespace ClothesShopping.Api.DTOs
{
    public class CreateUsermanRequest
    {
        [Required]
        [StringLength(100)]
        public string UserName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        [Required]
        [MinLength(6)]
        public string Password { get; set; } = string.Empty;

        public int UserRoleId { get; set; }

    }
}