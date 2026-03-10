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

        [NotMapped]
        public int LikesCount { get; set; }

        [NotMapped]
        public bool IsLikedByCurrentUser { get; set; }

        public ICollection<Tag> Tags { get; set; } = [];
    }
}   