using System;

namespace GameBackend.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string? ResetToken { get; set; } // Dùng cho quên mật khẩu
        public DateTime? ResetTokenExpiry { get; set; }
    }
}