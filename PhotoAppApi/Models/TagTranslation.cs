using System.Text.Json.Serialization;

namespace PhotoAppApi.Models
{
    public class TagTranslation
    {
        public int TagId { get; set; }

        [JsonIgnore] 
        public Tag Tag { get; set; } = null!;

        public Language Language { get; set; } = Language.Unspecified;
        public string Name { get; set; } = string.Empty;
    }
}
