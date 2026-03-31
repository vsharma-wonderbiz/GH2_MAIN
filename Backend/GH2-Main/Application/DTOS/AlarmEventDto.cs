using System.Text.Json;
using System.Text.Json.Serialization;

public class AlarmEventDto
{
    [JsonPropertyName("event")]
    public string Event { get; set; }

    [JsonPropertyName("mapping_id")]
    public int MappingId { get; set; }

    [JsonPropertyName("signal")]
    public string Signal { get; set; }

    [JsonPropertyName("alarm_type")]
    public string AlarmType { get; set; }

    [JsonPropertyName("current_value")]
    public float CurrentValue { get; set; }

    [JsonPropertyName("limit_breached")]
    public float LimitBreached { get; set; }

    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; }
}