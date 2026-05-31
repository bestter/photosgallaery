using System.Text.Json.Serialization;

namespace PhotoAppApi.Models
{
    public class ModerationResult
    {
        [JsonPropertyName("is_nsfw")]
        public bool IsNsfw { get; set; }

        [JsonPropertyName("nsfw_score")]
        public double NsfwScore { get; set; }

        [JsonPropertyName("safe_score")]
        public double SafeScore { get; set; }

        [JsonPropertyName("label")]
        public string Label { get; set; } = string.Empty;
    }
}
