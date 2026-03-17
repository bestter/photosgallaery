namespace PhotoAppApi.Models
{
    public class PhotoViewEvent
    {
        public int PhotoId { get; set; }
        public int? UserId { get; set; } // Null si anonyme
        public DateTime Timestamp { get; set; }
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
    }
}