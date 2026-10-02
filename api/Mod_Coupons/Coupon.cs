using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ClothesShopping.Api.CouponsModule
{
    public sealed class Coupon : IEquatable<Coupon>
    {
        private int? _couponId;

        public Coupon() { }

        public int CouponId
        {
            get => _couponId ?? 0;
            set => _couponId = value;
        }

        public string? Code { get; set; }
        public double Discount { get; set; }
        public string? ExpirationDate { get; set; }
        public string? Status { get; set; }

        public IReadOnlyList<ValidationResult> Validate()
        {
            var ctx = new ValidationContext(this);
            var results = new List<ValidationResult>();
            Validator.TryValidateObject(this, ctx, results, validateAllProperties: true);
            return results;
        }

        public override string ToString() => $"Coupon[CouponId={CouponId}, Code={Code}, Discount={Discount}, Status={Status}]";

        public bool Equals(Coupon? other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return CouponId == other.CouponId;
        }

        public override bool Equals(object? obj) => Equals(obj as Coupon);
        public override int GetHashCode() => CouponId.GetHashCode();
    }
}
