using System.Text.Json.Serialization;

namespace SparkFlow.Domain.Models.Accounts;

public sealed class AccountProfile
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("active")]
    public bool Active { get; set; } = true;

    // ✅ FIX: changed from int → string (matches IEmulatorInstanceControlService)
    [JsonPropertyName("instanceId")]
    public string InstanceId { get; set; } = "-1";

    [JsonPropertyName("adbSerial")]
    public string? AdbSerial { get; set; }

    // =============================
    // Binding
    // =============================

    [JsonPropertyName("binding")]
    public DeviceBindingData? Binding { get; set; }

    // =============================
    // Validation
    // =============================

    [JsonPropertyName("validationStatus")]
    public ProfileValidationStatus ValidationStatus { get; set; }
        = ProfileValidationStatus.Draft;

    [JsonPropertyName("validationError")]
    public string? ValidationError { get; set; }

    [JsonPropertyName("validatedAt")]
    public DateTimeOffset? ValidatedAt { get; set; }

    // =============================
    // Game binding
    // =============================

    [JsonPropertyName("gameId")]
    public string GameId { get; set; } = "war_and_order";

    [JsonPropertyName("lastRun")]
    public DateTimeOffset? LastRun { get; set; }

    // =============================
    // metadata
    // =============================

    [JsonPropertyName("createdAt")]
    public DateTimeOffset? CreatedAt { get; set; }

    [JsonPropertyName("updatedAt")]
    public DateTimeOffset? UpdatedAt { get; set; }

    [JsonPropertyName("details")]
    public string? Details { get; set; }

    public AccountProfile Clone()
        => (AccountProfile)MemberwiseClone();
}