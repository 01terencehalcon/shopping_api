using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ClothesShopping.Api.UsermanModule
{
    public sealed class Userman : IEquatable<Userman>
    {
        public const int USER_NAME_MAX_LENGTH = 100;
        public const int USER_PASS_MAX_LENGTH = 150;

        private int? _userId;
        private string? _userName;
        private string? _email;
        private string? _userPass;
        private int? _userRoleId;
        private string? _status;

        public Userman() { }

        public Userman(string userName, string userPass, int userRoleId, string email = "", string status = "active")
        {
            this.UserName = userName;
            this.UserPass = userPass;
            this.UserRoleId = userRoleId;
            this.Email = email;
            this.Status = status;
        }

        /// <summary>
        /// Gets or sets the UserId.
        /// </summary>
        public int UserId
        {
            get => _userId ?? 0;
            set
            {
                _userId = value;
            }
        }

        /// <summary>
        /// Gets or sets the UserName.
        /// Maximum length: 100 characters.
        /// </summary>
        [StringLength(USER_NAME_MAX_LENGTH)]
        public string UserName
        {
            get => _userName ?? string.Empty;
            set
            {
                var __s = NormalizeSpaces(value);
                if (__s.Length > USER_NAME_MAX_LENGTH)
                    throw new ArgumentException($"UserName exceeds USER_NAME_MAX_LENGTH characters (got {__s.Length}).");
                _userName = __s;
            }
        }

        /// <summary>
        /// Gets or sets the Email.
        /// </summary>
        public string Email
        {
            get => _email ?? string.Empty;
            set => _email = value ?? string.Empty;
        }

        /// <summary>
        /// Gets or sets the UserPass.
        /// Maximum length: 150 characters.
        /// </summary>
        [StringLength(USER_PASS_MAX_LENGTH)]
        public string UserPass
        {
            get => _userPass ?? string.Empty;
            set
            {
                var __s = NormalizeSpaces(value);
                if (__s.Length > USER_PASS_MAX_LENGTH)
                    throw new ArgumentException($"UserPass exceeds USER_PASS_MAX_LENGTH characters (got {__s.Length}).");
                _userPass = __s;
            }
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
        /// Gets or sets the Status (active, pending, rejected).
        /// Default is "active".
        /// </summary>
        public string Status
        {
            get => string.IsNullOrWhiteSpace(_status) ? "active" : _status;
            set
            {
                var val = value?.Trim().ToLowerInvariant() ?? "active";
                if (val != "active" && val != "pending" && val != "rejected")
                {
                    val = "active";
                }
                _status = val;
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

        public override string ToString() => "Userman[" + string.Join(", ", new[]
        {
            "UserId=" + UserId,
            "UserName=" + UserName,
            "Email=" + Email,
            "UserPass=" + UserPass,
            "UserRoleId=" + UserRoleId,
            "Status=" + Status
        }) + "]";

        public bool Equals(Userman? other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            if (UserId > 0 || other.UserId > 0) return UserId == other.UserId;
            return string.Equals(ToString(), other.ToString(), StringComparison.OrdinalIgnoreCase);
        }

        public override bool Equals(object? obj) => Equals(obj as Userman);
        
        public override int GetHashCode()
        {
            if (UserId > 0) return UserId.GetHashCode();
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