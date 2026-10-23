using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ClothesShopping.Api.OrderTrackingModule
{
    public sealed class OrderTracking : IEquatable<OrderTracking>
    {
        private int? _id;

        public OrderTracking() { }

        public int Id
        {
            get => _id ?? 0;
            set => _id = value;
        }

        public int OrderId { get; set; }
        public string Status { get; set; } = "pending";
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string? Note { get; set; }
        public string CreatedAt { get; set; } = string.Empty;

        public IReadOnlyList<ValidationResult> Validate()
        {
            var ctx = new ValidationContext(this);
            var results = new List<ValidationResult>();
            Validator.TryValidateObject(this, ctx, results, validateAllProperties: true);
            return results;
        }

        public override string ToString() => $"OrderTracking[Id={Id}, OrderId={OrderId}, Status={Status}, Lat={Latitude}, Lng={Longitude}]";

        public bool Equals(OrderTracking? other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return Id == other.Id;
        }

        public override bool Equals(object? obj) => Equals(obj as OrderTracking);
        public override int GetHashCode() => Id.GetHashCode();
    }
}
