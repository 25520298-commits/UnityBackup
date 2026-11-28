using System.ComponentModel.DataAnnotations;

namespace GameBackend.Models
{
    public class User
    {
        [Key]
        [MaxLength(6)]
        public string Id { get; set; } = string.Empty;

        [Required]
        [MaxLength(150)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string Username { get; set; } = string.Empty;

        [Required]
        [MaxLength(255)]
        public string PasswordHash { get; set; } = string.Empty;

        [MaxLength(128)]
        public string? ResetToken { get; set; }

        public DateTime? ResetTokenExpiry { get; set; }
    }
}