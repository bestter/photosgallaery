using System.ComponentModel.DataAnnotations;

namespace PhotoAppApi.Models
{
    public class ImageReport
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int PhotoId { get; set; }

        [Required]
        [MaxLength(500)]
        public string Reason { get; set; } = string.Empty;

        public DateTime ReportedAt { get; set; } = DateTime.UtcNow;

        [MaxLength(100)]
        public string ReporterUsername { get; set; } = string.Empty;

        [MaxLength(50)]
        public string Status { get; set; } = "Pending"; // "Pending", "Processed"
    }
}