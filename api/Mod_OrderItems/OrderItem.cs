using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ClothesShopping.Api.OrderItemsModule
{
    public sealed class OrderItem : IEquatable<OrderItem>
    {
        private int? _id;

        public OrderItem() { }

        public int Id
        {
            get => _id ?? 0;
            set => _id = value;
        }

        public int OrderId { get; set; }
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public double Price { get; set; }

        public IReadOnlyList<ValidationResult> Validate()
        {
            var ctx = new ValidationContext(this);
            var results = new List<ValidationResult>();
            Validator.TryValidateObject(this, ctx, results, validateAllProperties: true);
            return results;
        }

        public override string ToString() => $"OrderItem[Id={Id}, OrderId={OrderId}, ProductId={ProductId}, Quantity={Quantity}]";

        public bool Equals(OrderItem? other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return Id == other.Id;
        }

        public override bool Equals(object? obj) => Equals(obj as OrderItem);
        public override int GetHashCode() => Id.GetHashCode();
    }
}
