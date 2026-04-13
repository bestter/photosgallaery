using System.ComponentModel.DataAnnotations.Schema;

namespace PhotoAppApi.Models
{
    public class Photo
    {
        public int Id { get; set; }
        public required string FileName { get; set; }
        public required string Url { get; set; }
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
        public string? UploaderUsername { get; set; }

        public string? FileHash { get; set; }

        public long FileSize { get; set; }
        public int ResolutionWidth { get; set; }
        public int ResolutionHeight { get; set; }
        public DateTime? DateTaken { get; set; }
        public string? CameraModel { get; set; }

        public double? Latitude { get; set; }
        public double? Longitude { get; set; }

        [NotMapped]
        public int LikesCount { get; set; }

        [NotMapped]
        public bool IsLikedByCurrentUser { get; set; }

        [NotMapped]
        public bool IsReportedByCurrentUser { get; set; }

        public ICollection<Tag> Tags { get; set; } = [];

        public int ViewsCount { get; set; } = 0;

        // Optionnel/Nullable pour l'instant afin de supporter les anciennes images
        public Guid? GroupId { get; set; }
        [System.Text.Json.Serialization.JsonIgnore]
        public Group? Group { get; set; }
    }
}   