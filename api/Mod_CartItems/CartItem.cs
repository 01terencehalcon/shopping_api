using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ClothesShopping.Api.CartItemsModule
{
    public sealed class CartItem : IEquatable<CartItem>
    {
        private int? _cartItemId;

        public CartItem() { }

        public int CartItemId
        {
            get => _cartItemId ?? 0;
            set => _cartItemId = value;
        }

        public int CartId { get; set; }
        public int ProductId { get; set; }
        public int Quantity { get; set; } = 1;

        public IReadOnlyList<ValidationResult> Validate()
        {
            var ctx = new ValidationContext(this);
            var results = new List<ValidationResult>();
            Validator.TryValidateObject(this, ctx, results, validateAllProperties: true);
            return results;
        }

        public override string ToString() => $"CartItem[CartItemId={CartItemId}, CartId={CartId}, ProductId={ProductId}, Quantity={Quantity}]";

        public bool Equals(CartItem? other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return CartItemId == other.CartItemId;
        }

        public override bool Equals(object? obj) => Equals(obj as CartItem);
        public override int GetHashCode() => CartItemId.GetHashCode();
    }
}
