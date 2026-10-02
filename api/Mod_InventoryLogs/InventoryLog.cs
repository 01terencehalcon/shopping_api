using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ClothesShopping.Api.InventoryLogsModule
{
    public sealed class InventoryLog : IEquatable<InventoryLog>
    {
        private int? _logId;

        public InventoryLog() { }

        public int LogId
        {
            get => _logId ?? 0;
            set => _logId = value;
        }

        public int ProductId { get; set; }
        public int ChangeQuantity { get; set; }
        public string? Action { get; set; }
        public string LogDate { get; set; } = string.Empty;

        public IReadOnlyList<ValidationResult> Validate()
        {
            var ctx = new ValidationContext(this);
            var results = new List<ValidationResult>();
            Validator.TryValidateObject(this, ctx, results, validateAllProperties: true);
            return results;
        }

        public override string ToString() => $"InventoryLog[LogId={LogId}, ProductId={ProductId}, Action={Action}, ChangeQuantity={ChangeQuantity}]";

        public bool Equals(InventoryLog? other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return LogId == other.LogId;
        }

        public override bool Equals(object? obj) => Equals(obj as InventoryLog);
        public override int GetHashCode() => LogId.GetHashCode();
    }
}
