namespace PhotoAppApi.Models
{
    public class Group
    {
        // UUID v7 as Primary Key
        public Guid Id { get; set; } = Guid.CreateVersion7();
        
        public required string Name { get; set; }

        public required string ShortName { get; set; }

        // UUID v7 for invitation link
        public Guid InviteToken { get; set; } = Guid.CreateVersion7();

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public required string Description { get; set; }

        // Relation avec Utilisateurs-Groupes
        [System.Text.Json.Serialization.JsonIgnore]
        public ICollection<UserGroup> UserGroups { get; set; } = new List<UserGroup>();

        // Photos assignées à ce groupe
        [System.Text.Json.Serialization.JsonIgnore]
        public ICollection<Photo> Photos { get; set; } = new List<Photo>();
    }
}
