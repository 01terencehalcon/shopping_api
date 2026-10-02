using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ClothesShopping.Api.TblContactModule
{
    public sealed class TblContact : IEquatable<TblContact>
    {
        public const int NAME_MAX_LENGTH = 50;
        public const int GENDER_MAX_LENGTH = 1;
        public const int ADDRESS_MAX_LENGTH = 120;
        public const int CONTACTNUM_MAX_LENGTH = 12;

        private int? _id;
        private string? _name;
        private string? _gender;
        private string? _birthday;
        private string? _address;
        private string? _contactnum;

        public TblContact() { }

        public TblContact(string name, string gender, string birthday, string address, string contactnum)
        {
            this.Name = name;
            this.Gender = gender;
            this.Birthday = birthday;
            this.Address = address;
            this.Contactnum = contactnum;
        }

        /// <summary>
        /// Gets or sets the Id.
        /// </summary>
        public int Id
        {
            get => _id ?? 0;
            set
            {
                _id = value;
            }
        }

        /// <summary>
        /// Gets or sets the Name.
        /// Maximum length: 50 characters.
        /// </summary>
        [StringLength(NAME_MAX_LENGTH)]
        public string Name
        {
            get => _name ?? string.Empty;
            set
            {
                var __s = NormalizeSpaces(value);
                if (__s.Length > NAME_MAX_LENGTH)
                    throw new ArgumentException($"Name exceeds NAME_MAX_LENGTH characters (got {__s.Length}).");
                _name = __s;
            }
        }

        /// <summary>
        /// Gets or sets the Gender.
        /// Maximum length: 1 characters.
        /// </summary>
        [StringLength(GENDER_MAX_LENGTH)]
        public string Gender
        {
            get => _gender ?? string.Empty;
            set
            {
                var __s = NormalizeSpaces(value);
                if (__s.Length > GENDER_MAX_LENGTH)
                    throw new ArgumentException($"Gender exceeds GENDER_MAX_LENGTH characters (got {__s.Length}).");
                _gender = __s;
            }
        }

        /// <summary>
        /// Gets or sets the Birthday.
        /// </summary>
        public string Birthday
        {
            get => _birthday ?? string.Empty;
            set
            {
                var __s = NormalizeSpaces(value);
                _birthday = __s;
            }
        }

        /// <summary>
        /// Gets or sets the Address.
        /// Maximum length: 120 characters.
        /// </summary>
        [StringLength(ADDRESS_MAX_LENGTH)]
        public string Address
        {
            get => _address ?? string.Empty;
            set
            {
                var __s = NormalizeSpaces(value);
                if (__s.Length > ADDRESS_MAX_LENGTH)
                    throw new ArgumentException($"Address exceeds ADDRESS_MAX_LENGTH characters (got {__s.Length}).");
                _address = __s;
            }
        }

        /// <summary>
        /// Gets or sets the Contactnum.
        /// Maximum length: 12 characters.
        /// </summary>
        [StringLength(CONTACTNUM_MAX_LENGTH)]
        public string Contactnum
        {
            get => _contactnum ?? string.Empty;
            set
            {
                var __s = NormalizeSpaces(value);
                if (__s.Length > CONTACTNUM_MAX_LENGTH)
                    throw new ArgumentException($"Contactnum exceeds CONTACTNUM_MAX_LENGTH characters (got {__s.Length}).");
                _contactnum = __s;
            }
        }

        /// <summary>
        /// Validate using DataAnnotations and semantic rules; returns errors (empty list means valid).
        /// </summary>
        public IReadOnlyList<ValidationResult> Validate()
        {
            var ctx = new ValidationContext(this);
            var results = new List<ValidationResult>();
            Validator.TryValidateObject(this, ctx, results, validateAllProperties: true);
            return results;
        }

        public override string ToString() => "TblContact[" + string.Join(", ", new[]
        {
            "Id=" + Id,
            "Name=" + Name,
            "Gender=" + Gender,
            "Birthday=" + Birthday,
            "Address=" + Address,
            "Contactnum=" + Contactnum
        }) + "]";

        public bool Equals(TblContact? other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            if (Id > 0 || other.Id > 0) return Id == other.Id;
            return string.Equals(ToString(), other.ToString(), StringComparison.OrdinalIgnoreCase);
        }

        public override bool Equals(object? obj) => Equals(obj as TblContact);
        
        public override int GetHashCode()
        {
            if (Id > 0) return Id.GetHashCode();
            return StringComparer.OrdinalIgnoreCase.GetHashCode(ToString());
        }

        private static string NormalizeSpaces(string? input)
        {
            if (string.IsNullOrWhiteSpace(input)) return string.Empty;
            var trimmed = input.Trim();
            var parts = trimmed.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
            return string.Join(' ', parts);
        }
    }
}