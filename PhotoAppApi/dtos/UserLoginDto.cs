using System.ComponentModel.DataAnnotations;

namespace PhotoAppApi.dtos
{
    public class UserLoginDto
    {
        [StringLength(100)]
        public string Username { get; set; } = string.Empty;

        // 🛡️ Sentinel: Enforce max length of 72 bytes to prevent Bcrypt Hash Denial of Service (DoS) and truncation
        [StringLength(72)]
        public string Password { get; set; } = string.Empty;
    }
}
