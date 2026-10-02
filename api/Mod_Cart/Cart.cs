using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ClothesShopping.Api.CartModule
{
    public sealed class Cart : IEquatable<Cart>
    {
        private int? _cartId;

        public Cart() { }

        public int CartId
        {
            get => _cartId ?? 0;
            set => _cartId = value;
        }

        public int UserId { get; set; }

        public string CreatedAt { get; set; } = string.Empty;

        public IReadOnlyList<ValidationResult> Validate()
        {
            var ctx = new ValidationContext(this);
            var results = new List<ValidationResult>();
            Validator.TryValidateObject(this, ctx, results, validateAllProperties: true);
            return results;
        }

        public override string ToString() => $"Cart[CartId={CartId}, UserId={UserId}, CreatedAt={CreatedAt}]";

        public bool Equals(Cart? other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return CartId == other.CartId;
        }

        public override bool Equals(object? obj) => Equals(obj as Cart);
        public override int GetHashCode() => CartId.GetHashCode();
    }
}
