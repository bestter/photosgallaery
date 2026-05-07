namespace PhotoAppApi.Models
{
    public class ModerationResult
    {
        public bool IsNsfw { get; set; }
        public double NsfwScore { get; set; }
        public double SafeScore { get; set; }
        public string Label { get; set; } = string.Empty;
    }
}
