using System.Text.Json.Serialization; // N'oublie pas cet import pour le JsonIgnore

namespace PhotoAppApi.Models
{    
    public class PhotoLike
    {
        public int Id { get; set; }

        public required int PhotoId { get; set; }

        [JsonIgnore]
        public required Photo Photo { get; set; } // Propriété de navigation vers la photo

        // Assure-toi que "string" ou "int" correspond au type de la clé primaire de ton User
        // Souvent dans ASP.NET Identity, l'ID est un string. Si tu utilises une entité User personnalisée avec un int, garde int.
        public required int UserId { get; set; }

        [JsonIgnore]
        public required User User { get; set; } // Propriété de navigation vers l'utilisateur

        public DateTime LikedAt { get; set; } = DateTime.UtcNow;
    }
}
