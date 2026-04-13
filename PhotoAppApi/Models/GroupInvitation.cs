using System.ComponentModel.DataAnnotations;

namespace PhotoAppApi.Models
{
    public class GroupInvitation
    {
        [Key]
        public Guid Id { get; set; } = Guid.CreateVersion7();
        
        public Guid GroupId { get; set; }
        public Group? Group { get; set; }
        
        public int InviterId { get; set; }
        public User? Inviter { get; set; }

        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Message { get; set; }

        public Guid InviteToken { get; set; } = Guid.CreateVersion7();
        
        // Pending, Accepted, Expired
        public string Status { get; set; } = "Pending"; 
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
