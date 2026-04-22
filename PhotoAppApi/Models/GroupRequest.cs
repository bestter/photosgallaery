namespace PhotoAppApi.Models
{
    public class GroupRequest
    {
        // UUID v7 as Primary Key
        public Guid Id { get; set; } = Guid.CreateVersion7();

        public required string Name { get; set; }

        public required string Description { get; set; }

        public required User Requester { get; set; }

        public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
    }
}
