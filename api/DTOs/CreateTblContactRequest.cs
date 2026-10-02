using System.ComponentModel.DataAnnotations;

namespace ClothesShopping.Api.DTOs
{
    public class CreateTblContactRequest
    {
        [Required]
        [StringLength(50)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [StringLength(1)]
        public string Gender { get; set; } = string.Empty;

        [Required]
        public string Birthday { get; set; } = string.Empty;

        [Required]
        [StringLength(120)]
        public string Address { get; set; } = string.Empty;

        [Required]
        [StringLength(12)]
        public string Contactnum { get; set; } = string.Empty;

    }
}