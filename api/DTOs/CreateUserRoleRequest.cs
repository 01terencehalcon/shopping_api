using System.ComponentModel.DataAnnotations;

namespace ClothesShopping.Api.DTOs
{
    public class CreateUserroleRequest
    {
        [Required]
        [StringLength(100)]
        public string UserRole { get; set; } = string.Empty;

    }
}