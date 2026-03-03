namespace PhotoAppApi.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        // C'est ici que le mot de passe "brouillé" sera stocké
        public string PasswordHash { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // LA NOUVELLE SOLUTION :
        public string Role { get; set; } = "User"; // Par défaut, tout le monde est un simple utilisateur
    }
}
