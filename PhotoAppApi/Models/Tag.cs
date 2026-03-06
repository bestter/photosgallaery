using System.Text.Json.Serialization; // 👈 N'oublie pas cet import en haut !

namespace PhotoAppApi.Models
{
    public class Tag
    {
        public int Id { get; set; }
        public bool IsActive { get; set; } = true;
        public bool IsMetaTag { get; set; }

        // On dit au JSON de ne pas inclure ceci pour éviter la boucle
        [JsonIgnore]
        public ICollection<Photo> Photos { get; set; } = new List<Photo>();

        public ICollection<TagTranslation> Translations { get; set; } = new List<TagTranslation>();
    }
}