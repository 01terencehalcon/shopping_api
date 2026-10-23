using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ClothesShopping.Api.AddressModule
{
    public sealed class Address : IEquatable<Address>
    {
        private int? _addressId;

        public Address() { }

        public int AddressId
        {
            get => _addressId ?? 0;
            set => _addressId = value;
        }

        public int UserId { get; set; }
        public string? RecipientName { get; set; }
        public string? Phone { get; set; }
        public string? Region { get; set; }
        public string? Province { get; set; }
        public string? City { get; set; }
        public string? Barangay { get; set; }
        public string? Street { get; set; }
        public string? PostalCode { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }

        public IReadOnlyList<ValidationResult> Validate()
        {
            var ctx = new ValidationContext(this);
            var results = new List<ValidationResult>();
            Validator.TryValidateObject(this, ctx, results, validateAllProperties: true);
            return results;
        }

        public override string ToString() => $"Address[AddressId={AddressId}, UserId={UserId}, City={City}, Province={Province}]";

        public bool Equals(Address? other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return AddressId == other.AddressId;
        }

        public override bool Equals(object? obj) => Equals(obj as Address);
        public override int GetHashCode() => AddressId.GetHashCode();
    }
}
