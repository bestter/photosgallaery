// Modèle de l'entité Entity Framework (Table PhotoViews)
using System.Text.Json.Serialization;

namespace PhotoAppApi.Models
{
    public class PhotoView
    {
        public int Id { get; set; }
        public int PhotoId { get; set; }
        public int? UserId { get; set; } // Optionnel
        public DateTime Timestamp { get; set; }
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }

        // Navigation properties (Assure-toi d'ajouter DbSet<PhotoView> PhotoViews dans AppDbContext)
        [JsonIgnore]
        public Photo Photo { get; set; } = null!;
    }
}
