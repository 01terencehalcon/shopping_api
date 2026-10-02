using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ClothesShopping.Api.NotificationsModule
{
    public sealed class Notification : IEquatable<Notification>
    {
        private int? _notificationId;

        public Notification() { }

        public int NotificationId
        {
            get => _notificationId ?? 0;
            set => _notificationId = value;
        }

        public int UserId { get; set; }
        public string? Message { get; set; }
        public int IsRead { get; set; } = 0;
        public string CreatedAt { get; set; } = string.Empty;

        public IReadOnlyList<ValidationResult> Validate()
        {
            var ctx = new ValidationContext(this);
            var results = new List<ValidationResult>();
            Validator.TryValidateObject(this, ctx, results, validateAllProperties: true);
            return results;
        }

        public override string ToString() => $"Notification[NotificationId={NotificationId}, UserId={UserId}, IsRead={IsRead}]";

        public bool Equals(Notification? other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return NotificationId == other.NotificationId;
        }

        public override bool Equals(object? obj) => Equals(obj as Notification);
        public override int GetHashCode() => NotificationId.GetHashCode();
    }
}
