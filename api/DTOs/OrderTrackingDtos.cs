using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ClothesShopping.Api.DTOs
{
    public class LocationPointDto
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string? Label { get; set; }
    }

    public class DeliveryAddressLocationDto
    {
        public string? RecipientName { get; set; }
        public string? Street { get; set; }
        public string? City { get; set; }
        public string? Province { get; set; }
        public string? PostalCode { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
    }

    public class TrackingCheckpointDto
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public string Status { get; set; } = string.Empty;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string? Note { get; set; }
        public string CreatedAt { get; set; } = string.Empty;
    }

    public class OrderTrackingResponse
    {
        public int OrderId { get; set; }
        public string Status { get; set; } = string.Empty;
        public LocationPointDto StoreLocation { get; set; } = new();
        public DeliveryAddressLocationDto? DeliveryAddress { get; set; }
        public List<TrackingCheckpointDto> Checkpoints { get; set; } = new();
    }

    public class AddTrackingCheckpointRequest
    {
        [Required]
        public string Status { get; set; } = string.Empty;

        [Required]
        [Range(-90.0, 90.0, ErrorMessage = "Latitude must be between -90 and 90 degrees.")]
        public double Latitude { get; set; }

        [Required]
        [Range(-180.0, 180.0, ErrorMessage = "Longitude must be between -180 and 180 degrees.")]
        public double Longitude { get; set; }

        public string? Note { get; set; }
    }
}
