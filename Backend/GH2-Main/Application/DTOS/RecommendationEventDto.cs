using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Application.DTOS
{
    public class RecommendationEventDto
    {
        [JsonPropertyName("event")]
        public required string Event { get; set; }

        [JsonPropertyName("mapping_id")]
        public int MappingId { get; set; }

        [JsonPropertyName("asset")]
        public required string AssetName { get; set; }

        [JsonPropertyName("signal")]
        public required string Signal { get; set; }

        [JsonPropertyName("recommendation_type")]
        public required string recommendation_type { get; set; }

        [JsonPropertyName("current_value")]
        public required float CurrentVal {  get; set; }

        [JsonPropertyName("trigger_value")]
        public float? TriggerVal { get; set; }

        [JsonPropertyName("message")]
        public string? Message { get; set; }

        [JsonPropertyName("timestamp")]
        public DateTime PublishedAt { get;set; }
    }
}
