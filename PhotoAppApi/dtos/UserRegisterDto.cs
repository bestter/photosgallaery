using System.ComponentModel.DataAnnotations;

namespace PhotoAppApi.dtos
{
    public class UserRegisterDto
    {
        [Required]
        [StringLength(100)]
        public string Username { get; set; } = string.Empty;

        [Required, EmailAddress]
        [StringLength(100)]
        public string Email { get; set; } = string.Empty;

        [Required, MinLength(6)]
        [StringLength(100)]
        public string Password { get; set; } = string.Empty;

        [StringLength(500)]
        public string? InviteToken { get; set; }
    }
}
