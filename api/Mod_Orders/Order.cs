using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ClothesShopping.Api.OrdersModule
{
    public sealed class Order : IEquatable<Order>
    {
        private int? _id;

        public Order() { }

        public int Id
        {
            get => _id ?? 0;
            set => _id = value;
        }

        public int UserId { get; set; }

        public double Total { get; set; }

        public string OrderDate { get; set; } = string.Empty;

        public string Status { get; set; } = "pending";

        public IReadOnlyList<ValidationResult> Validate()
        {
            var ctx = new ValidationContext(this);
            var results = new List<ValidationResult>();
            Validator.TryValidateObject(this, ctx, results, validateAllProperties: true);
            return results;
        }

        public override string ToString() => $"Order[Id={Id}, UserId={UserId}, Total={Total}, Status={Status}]";

        public bool Equals(Order? other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return Id == other.Id;
        }

        public override bool Equals(object? obj) => Equals(obj as Order);
        public override int GetHashCode() => Id.GetHashCode();
    }
}
