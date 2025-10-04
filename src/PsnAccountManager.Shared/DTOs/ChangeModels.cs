using PsnAccountManager.Shared.Enums;
using System.Text.Json.Serialization;

namespace PsnAccountManager.Shared.DTOs
{
    public class ChangeDetails
    {
        public ChangeType ChangeType { get; set; } = ChangeType.NoChange;
        public List<FieldChange> Changes { get; set; } = new();
        public DateTime DetectedAt { get; set; } = DateTime.UtcNow;

        [JsonIgnore]
        public bool HasChanges => Changes.Any();

        public void AddChange(string field, string? oldValue, string? newValue)
        {
            Changes.Add(new FieldChange
            {
                Field = field,
                OldValue = oldValue,
                NewValue = newValue
            });
        }

        public string ToJson()
        {
            return System.Text.Json.JsonSerializer.Serialize(this, new System.Text.Json.JsonSerializerOptions
            {
                PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
            });
        }

        public static ChangeDetails? FromJson(string? json)
        {
            if (string.IsNullOrEmpty(json)) return null;

            try
            {
                return System.Text.Json.JsonSerializer.Deserialize<ChangeDetails>(json, new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
                });
            }
            catch
            {
                return null;
            }
        }
    }

    public class FieldChange
    {
        public string Field { get; set; } = string.Empty;
        public string? OldValue { get; set; }
        public string? NewValue { get; set; }

        public bool IsSignificant => !string.Equals(OldValue?.Trim(), NewValue?.Trim(), StringComparison.OrdinalIgnoreCase);
    }
}
