using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ClothesShopping.Api.ProductImagesModule
{
    public sealed class ProductImage : IEquatable<ProductImage>
    {
        private int? _imageId;

        public ProductImage() { }

        public int ImageId
        {
            get => _imageId ?? 0;
            set => _imageId = value;
        }

        public int ProductId { get; set; }
        public string? ImageUrl { get; set; }

        public IReadOnlyList<ValidationResult> Validate()
        {
            var ctx = new ValidationContext(this);
            var results = new List<ValidationResult>();
            Validator.TryValidateObject(this, ctx, results, validateAllProperties: true);
            return results;
        }

        public override string ToString() => $"ProductImage[ImageId={ImageId}, ProductId={ProductId}, ImageUrl={ImageUrl}]";

        public bool Equals(ProductImage? other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return ImageId == other.ImageId;
        }

        public override bool Equals(object? obj) => Equals(obj as ProductImage);
        public override int GetHashCode() => ImageId.GetHashCode();
    }
}
