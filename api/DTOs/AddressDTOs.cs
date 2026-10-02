using System.ComponentModel.DataAnnotations;

namespace ClothesShopping.Api.DTOs
{
    public class CreateAddressRequest
    {
        [Required]
        public int UserId { get; set; }

        public string? RecipientName { get; set; }
        public string? Phone { get; set; }
        public string? Street { get; set; }
        public string? City { get; set; }
        public string? Province { get; set; }
        public string? PostalCode { get; set; }
    }

    public class UpdateAddressRequest
    {
        public string? RecipientName { get; set; }
        public string? Phone { get; set; }
        public string? Street { get; set; }
        public string? City { get; set; }
        public string? Province { get; set; }
        public string? PostalCode { get; set; }
    }

    public class AddressResponse
    {
        public int AddressId { get; set; }
        public int UserId { get; set; }
        public string? RecipientName { get; set; }
        public string? Phone { get; set; }
        public string? Street { get; set; }
        public string? City { get; set; }
        public string? Province { get; set; }
        public string? PostalCode { get; set; }
    }
}
