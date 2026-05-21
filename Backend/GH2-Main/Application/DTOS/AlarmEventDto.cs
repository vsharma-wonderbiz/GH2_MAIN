using System.Text.Json;
using System.Text.Json.Serialization;

namespace Application.DTOS
{
    public class AlarmEventDto
    {
        [JsonPropertyName("event")]
        public required string Event { get; set; }

        [JsonPropertyName("mapping_id")]
        public int MappingId { get; set; }

        [JsonPropertyName("asset")]
        public required string AssetName { get; set; }

        [JsonPropertyName("signal")]
        public required string Signal { get; set; }

        [JsonPropertyName("alarm_type")]
        public required string AlarmType { get; set; }

        [JsonPropertyName("current_value")]
        public float CurrentValue { get; set; }

        [JsonPropertyName("limit_breached")]
        public float LimitBreached { get; set; }

        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; }
    }


    public class AlarmReslvedEventDto
    {
        [JsonPropertyName("event")]
        public required string Event { get; set; }

        [JsonPropertyName("mapping_id")]
        public int MappingId { get; set; }

        [JsonPropertyName("signal")]
        public required string Signal { get; set; }

        [JsonPropertyName("previous_alarm_type")]
        public required string PreviousAlarmType { get; set; }

        [JsonPropertyName("current_value")]
        public float CurrentValue { get; set; }

        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; }
    }
}