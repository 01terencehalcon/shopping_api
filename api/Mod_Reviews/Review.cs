using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ClothesShopping.Api.ReviewsModule
{
    public sealed class Review : IEquatable<Review>
    {
        private int? _reviewId;
        private int? _userId;
        private int? _productId;
        private int _rating;
        private string? _comment;
        private string? _reviewDate;

        public Review() { }

        public int ReviewId
        {
            get => _reviewId ?? 0;
            set => _reviewId = value;
        }

        public int UserId 
        {
            get => _userId ?? 0;
            set => _userId = value;
        }
        public int ProductId 
        {
            get => _productId ?? 0;
            set => _productId = value;
        }

        [Range(1, 5)]
        public int Rating
        {
            get => _rating;
            set
            {
                if (value < 1 || value > 5)
                    throw new ArgumentException("Rating must be between 1 and 5.");
                _rating = value;
            }
        }

        public string? Comment 
        {
            get => _comment;
            set => _comment = value;
        }
        public string ReviewDate 
        {
            get => _reviewDate ?? string.Empty;
            set => _reviewDate = value;
        }

        public IReadOnlyList<ValidationResult> Validate()
        {
            var ctx = new ValidationContext(this);
            var results = new List<ValidationResult>();
            Validator.TryValidateObject(this, ctx, results, validateAllProperties: true);
            return results;
        }

        public override string ToString() => $"Review[ReviewId={ReviewId}, UserId={UserId}, ProductId={ProductId}, Rating={Rating}]";

        public bool Equals(Review? other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return ReviewId == other.ReviewId;
        }

        public override bool Equals(object? obj) => Equals(obj as Review);
        public override int GetHashCode() => ReviewId.GetHashCode();
    }
}
