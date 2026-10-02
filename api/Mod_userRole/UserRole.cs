using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ClothesShopping.Api.UserroleModule
{
    public sealed class Userrole : IEquatable<Userrole>
    {
        public const int USER_ROLE_MAX_LENGTH = 100;

        private int? _userRoleId;
        private string? _userRole;

        public Userrole() { }

        public Userrole(string userRole)
        {
            this.UserRole = userRole;
        }

        /// <summary>
        /// Gets or sets the UserRoleId.
        /// </summary>
        public int UserRoleId
        {
            get => _userRoleId ?? 0;
            set
            {
                _userRoleId = value;
            }
        }

        /// <summary>
        /// Gets or sets the UserRole.
        /// Maximum length: 100 characters.
        /// </summary>
        [StringLength(USER_ROLE_MAX_LENGTH)]
        public string UserRole
        {
            get => _userRole ?? string.Empty;
            set
            {
                var __s = NormalizeSpaces(value);
                if (__s.Length > USER_ROLE_MAX_LENGTH)
                    throw new ArgumentException($"UserRole exceeds USER_ROLE_MAX_LENGTH characters (got {__s.Length}).");
                _userRole = __s;
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

        public override string ToString() => "Userrole[" + string.Join(", ", new[]
        {
            "UserRoleId=" + UserRoleId,
            "UserRole=" + UserRole
        }) + "]";

        public bool Equals(Userrole? other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            if (UserRoleId > 0 || other.UserRoleId > 0) return UserRoleId == other.UserRoleId;
            return string.Equals(ToString(), other.ToString(), StringComparison.OrdinalIgnoreCase);
        }

        public override bool Equals(object? obj) => Equals(obj as Userrole);
        
        public override int GetHashCode()
        {
            if (UserRoleId > 0) return UserRoleId.GetHashCode();
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