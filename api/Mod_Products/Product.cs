using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ClothesShopping.Api.ProductsModule
{
    public sealed class Product : IEquatable<Product>
    {
        public const int NAME_MAX_LENGTH = 200;
        public const int CATEGORY_MAX_LENGTH = 100;

        private int? _id;
        private string? _name;
        private string? _category;
        private double _price;
        private int _stock;
        private string? _image;
        private string? _description;

        public Product() { }

        public Product(string name, double price)
        {
            Name = name;
            Price = price;
        }

        public int Id
        {
            get => _id ?? 0;
            set => _id = value;
        }

        [Required]
        [StringLength(NAME_MAX_LENGTH)]
        public string Name
        {
            get => _name ?? string.Empty;
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                    throw new ArgumentException("Name is required.");
                if (value.Length > NAME_MAX_LENGTH)
                    throw new ArgumentException($"Name exceeds {NAME_MAX_LENGTH} characters.");
                _name = value.Trim();
            }
        }

        [StringLength(CATEGORY_MAX_LENGTH)]
        public string? Category
        {
            get => _category;
            set => _category = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        public double Price
        {
            get => _price;
            set
            {
                if (value < 0)
                    throw new ArgumentException("Price cannot be negative.");
                _price = value;
            }
        }

        public int Stock
        {
            get => _stock;
            set
            {
                if (value < 0)
                    throw new ArgumentException("Stock cannot be negative.");
                _stock = value;
            }
        }

        public string? Image
        {
            get => _image;
            set => _image = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        public string? Description
        {
            get => _description;
            set => _description = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        public IReadOnlyList<ValidationResult> Validate()
        {
            var ctx = new ValidationContext(this);
            var results = new List<ValidationResult>();
            Validator.TryValidateObject(this, ctx, results, validateAllProperties: true);
            return results;
        }

        public override string ToString() => $"Product[Id={Id}, Name={Name}, Price={Price}, Stock={Stock}]";

        public bool Equals(Product? other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            if (Id > 0 || other.Id > 0) return Id == other.Id;
            return string.Equals(Name, other.Name, StringComparison.OrdinalIgnoreCase);
        }

        public override bool Equals(object? obj) => Equals(obj as Product);

        public override int GetHashCode()
        {
            if (Id > 0) return Id.GetHashCode();
            return StringComparer.OrdinalIgnoreCase.GetHashCode(Name);
        }
    }
}
