namespace PhotoAppApi.Models
{
    public class UserGroup
    {
        public int UserId { get; set; }
        public User User { get; set; } = null!;

        public Guid GroupId { get; set; }
        public Group Group { get; set; } = null!;

        public GroupUserRole Role { get; set; } = GroupUserRole.Member;

        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    }


    public enum GroupUserRole
    {
        None = 0,
        Member = 1,
        Admin = 9999
    }
}
