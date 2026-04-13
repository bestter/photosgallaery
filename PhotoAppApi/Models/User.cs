namespace PhotoAppApi.Models
{
    // On définit les rôles possibles ici
    public enum UserRole
    {
        // Forbidden : -1 (pour les utilisateurs bannis ou avec des permissions très limitées)
        Forbidden = -1,
        
        //User : 0 (valeur par défaut pour les utilisateurs réguliers)        
        User = 0,

        //Créateur: Ce rôle peut être utilisé pour les utilisateurs qui ont des permissions spéciales, comme la possibilité de créer du contenu ou d'accéder à certaines fonctionnalités avancées. On lui attribue une valeur de 1 pour le différencier clairement du rôle "User".
        Creator = 1,
        
        // Admin : 9999 (on utilise un nombre très élevé pour éviter les conflits avec d'autres rôles)
        Admin = 9999
    }

    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // On utilise l'Enum au lieu d'un string brut
        public UserRole Role { get; set; } = UserRole.User;

        // Relation avec Utilisateurs-Groupes
        [System.Text.Json.Serialization.JsonIgnore]
        public ICollection<UserGroup> UserGroups { get; set; } = new List<UserGroup>();
    }
}