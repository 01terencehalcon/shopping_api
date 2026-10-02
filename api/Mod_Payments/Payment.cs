using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ClothesShopping.Api.PaymentsModule
{
    public sealed class Payment : IEquatable<Payment>
    {
        private int? _paymentId;

        public Payment() { }

        public int PaymentId
        {
            get => _paymentId ?? 0;
            set => _paymentId = value;
        }

        public int OrderId { get; set; }
        public string PaymentDate { get; set; } = string.Empty;
        public double Amount { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
        public string PaymentStatus { get; set; } = "pending";
        public int CouponId { get; set; }

        public IReadOnlyList<ValidationResult> Validate()
        {
            var ctx = new ValidationContext(this);
            var results = new List<ValidationResult>();
            Validator.TryValidateObject(this, ctx, results, validateAllProperties: true);
            return results;
        }

        public override string ToString() => $"Payment[PaymentId={PaymentId}, OrderId={OrderId}, Amount={Amount}, Status={PaymentStatus}]";

        public bool Equals(Payment? other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return PaymentId == other.PaymentId;
        }

        public override bool Equals(object? obj) => Equals(obj as Payment);
        public override int GetHashCode() => PaymentId.GetHashCode();
    }
}
