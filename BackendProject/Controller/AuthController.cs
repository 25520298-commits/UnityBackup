using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using GameBackend.Models;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using MimeKit;

namespace GameBackend.Controllers
{
    [EnableRateLimiting("AuthPolicy")]
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IWebHostEnvironment _env;

        private static readonly TimeSpan DefaultResetTokenLifetime = TimeSpan.FromMinutes(15);
        private static readonly TimeSpan ResendCooldown = TimeSpan.FromSeconds(60);

        public AuthController(
            AppDbContext context,
            IConfiguration configuration,
            IHttpClientFactory httpClientFactory,
            IWebHostEnvironment env)
        {
            _context = context;
            _configuration = configuration;
            _httpClientFactory = httpClientFactory;
            _env = env;
        }

        private TimeSpan GetResetTokenLifetime()
        {
            var minutes = _configuration.GetValue<int?>("Auth:ResetTokenMinutes");
            return (minutes.HasValue && minutes.Value > 0)
                ? TimeSpan.FromMinutes(minutes.Value)
                : DefaultResetTokenLifetime;
        }

        private static bool IsResetTokenExpired(User user, DateTime utcNow)
        {
            return string.IsNullOrWhiteSpace(user.ResetToken) ||
                   !user.ResetTokenExpiry.HasValue ||
                   user.ResetTokenExpiry.Value <= utcNow;
        }

        private async Task ClearResetTokenAsync(User user)
        {
            user.ResetToken = null;
            user.ResetTokenExpiry = null;
            await _context.SaveChangesAsync();
        }

        // ==================== 1. API ĐĂNG KÝ ====================
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            var normalizedEmail = request.Email.Trim().ToLowerInvariant();
            var normalizedUsername = request.Username.Trim();

            if (await _context.Users.AnyAsync(u => u.Email == normalizedEmail))
                return BadRequest(new { message = "Email đã được sử dụng!" });

            if (await _context.Users.AnyAsync(u => u.Username == normalizedUsername))
                return BadRequest(new { message = "Tên người dùng đã tồn tại!" });

            var sixDigitId = await GenerateUniqueSixDigitIdAsync();

            var user = new User
            {
                Id = sixDigitId,
                Email = normalizedEmail,
                Username = normalizedUsername,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password)
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Đăng ký thành công!", userId = user.Id });
        }

        // ==================== 2. API ĐĂNG NHẬP (GỘP LUỒNG PLAYFAB) ====================
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var normalizedEmail = request.Email.Trim().ToLowerInvariant();
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == normalizedEmail);

            if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
                return Unauthorized(new { message = "Sai email hoặc mật khẩu!" });

            var jwtToken = GenerateJwtToken(user);

            var titleId = _configuration["PlayFab:TitleId"];
            var secretKey = _configuration["PlayFab:SecretKey"];

            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Add("X-SecretKey", secretKey);

            var playFabRequest = new
            {
                ServerCustomId = $"Player_{user.Id}",
                CreateAccount = true
            };

            var response = await client.PostAsJsonAsync(
                $"https://{titleId}.playfabapi.com/Server/LoginWithServerCustomId",
                playFabRequest);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                return StatusCode(502, new { message = "Không thể kết nối dịch vụ PlayFab", detail = error });
            }

            using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
            var data = doc.RootElement.GetProperty("data");

            return Ok(new
            {
                token = jwtToken,
                userId = user.Id,
                username = user.Username,
                playFabId = data.GetProperty("PlayFabId").GetString(),
                sessionTicket = data.GetProperty("SessionTicket").GetString(),
                entityToken = data.GetProperty("EntityToken").GetProperty("EntityToken").GetString()
            });
        }

        // ==================== 3. API ĐỔI MẬT KHẨU (YÊU CẦU JWT) ====================
        [Authorize]
        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized(new { message = "Token không hợp lệ!" });

            var user = await _context.Users.FindAsync(userId);
            if (user == null || !BCrypt.Net.BCrypt.Verify(request.OldPassword, user.PasswordHash))
                return BadRequest(new { message = "Mật khẩu hiện tại không chính xác!" });

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Đổi mật khẩu thành công!" });
        }

        // ==================== 4. API QUÊN MẬT KHẨU (GỬI MAILKIT) ====================
        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
        {
            var normalizedEmail = request.Email.Trim().ToLowerInvariant();
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == normalizedEmail);

            if (user == null)
                return Ok(new { message = "Nếu email tồn tại, mã khôi phục đã được gửi!" });

            var now = DateTime.UtcNow;
            var lifetime = GetResetTokenLifetime();

            // Dọn token cũ nếu đã quá hạn
            if (!string.IsNullOrEmpty(user.ResetToken) && IsResetTokenExpired(user, now))
            {
                await ClearResetTokenAsync(user);
            }

            // Chặn gửi trùng trong khoảng thời gian cooldown khi token vẫn còn hiệu lực
            if (!string.IsNullOrEmpty(user.ResetToken) &&
                user.ResetTokenExpiry.HasValue &&
                user.ResetTokenExpiry.Value > now &&
                user.ResetTokenExpiry.Value > now.Add(lifetime - ResendCooldown))
            {
                return Ok(new { message = "Mã khôi phục đã được gửi trước đó. Vui lòng kiểm tra hộp thư hoặc thử lại sau 1 phút!" });
            }

            var newToken = RandomNumberGenerator.GetInt32(100_000, 1_000_000).ToString("D6");
            var expiresAt = now.Add(lifetime);

            user.ResetToken = newToken;
            user.ResetTokenExpiry = expiresAt;
            await _context.SaveChangesAsync();

            var appPassword = _configuration["EmailSettings:AppPassword"];
            bool hasEmailConfig = !string.IsNullOrWhiteSpace(appPassword)
                                  && appPassword != "16_KY_TU_APP_PASSWORD_GMAIL";

            if (hasEmailConfig)
            {
                await SendResetEmailAsync(user.Email, user.ResetToken, (int)lifetime.TotalMinutes);
                return Ok(new
                {
                    message = "Nếu email tồn tại, mã khôi phục đã được gửi!",
                    expiresAtUtc = expiresAt
                });
            }

            if (_env.IsDevelopment())
            {
                return Ok(new
                {
                    message = "Chế độ Dev: Chưa cấu hình SMTP, dùng mã bên dưới để test.",
                    devToken = user.ResetToken,
                    expiresAtUtc = expiresAt
                });
            }

            return StatusCode(500, new { message = "Dịch vụ gửi email chưa được cấu hình trên server." });
        }

        // ==================== 5. API ĐẶT LẠI MẬT KHẨU ====================
        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
        {
            var normalizedToken = request.Token?.Trim();
            if (string.IsNullOrWhiteSpace(normalizedToken))
                return BadRequest(new { message = "Vui lòng nhập mã xác nhận!" });

            var normalizedEmail = string.IsNullOrWhiteSpace(request.Email)
                ? null
                : request.Email.Trim().ToLowerInvariant();

            var now = DateTime.UtcNow;

            User? user;
            if (normalizedEmail != null)
            {
                user = await _context.Users.FirstOrDefaultAsync(u => u.Email == normalizedEmail);
            }
            else
            {
                user = await _context.Users.FirstOrDefaultAsync(u => u.ResetToken == normalizedToken);
            }

            if (user == null || string.IsNullOrWhiteSpace(user.ResetToken))
                return BadRequest(new { message = "Mã xác nhận không hợp lệ hoặc đã hết hạn!" });

            // Kiểm tra hết hạn trước và xóa token hết hạn khỏi DB
            if (IsResetTokenExpired(user, now))
            {
                await ClearResetTokenAsync(user);
                return BadRequest(new { message = "Mã xác nhận đã hết hạn! Vui lòng yêu cầu gửi lại mã mới." });
            }

            if (!string.Equals(user.ResetToken.Trim(), normalizedToken, StringComparison.Ordinal))
                return BadRequest(new { message = "Mã xác nhận không chính xác!" });

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            user.ResetToken = null;
            user.ResetTokenExpiry = null;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Đổi mật khẩu thành công!" });
        }

        // ==================== HÀM PHỤ TRỢ: SINH ID 6 CHỮ SỐ, TẠO JWT & GỬI EMAIL ====================
        private async Task<string> GenerateUniqueSixDigitIdAsync()
        {
            while (true)
            {
                var candidate = RandomNumberGenerator.GetInt32(100_000, 1_000_000).ToString("D6");
                var exists = await _context.Users.AnyAsync(u => u.Id == candidate);
                if (!exists)
                    return candidate;
            }
        }

        private string GenerateJwtToken(User user)
        {
            var jwtKey = _configuration["Jwt:Key"] ?? throw new InvalidOperationException("Thiếu Jwt:Key");
            Console.WriteLine($"[DEBUG JWT] KeyLen={jwtKey.Length} | Key={jwtKey}");

            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(jwtKey);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id),
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim(ClaimTypes.Name, user.Username)
                }),
                Expires = DateTime.UtcNow.AddHours(2),
                Issuer = _configuration["Jwt:Issuer"],
                Audience = _configuration["Jwt:Audience"],
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        private async Task SendResetEmailAsync(string toEmail, string resetToken, int expireMinutes)
        {
            var email = new MimeMessage();
            email.From.Add(new MailboxAddress(
                _configuration["EmailSettings:SenderName"] ?? "Game Support",
                _configuration["EmailSettings:SenderEmail"]));
            email.To.Add(MailboxAddress.Parse(toEmail));
            email.Subject = "Mã khôi phục mật khẩu tài khoản Game";
            email.Body = new TextPart("html")
            {
                Text = $@"
                    <h3>Yêu cầu đặt lại mật khẩu</h3>
                    <p>Mã xác nhận của bạn là: <b>{resetToken}</b></p>
                    <p>Mã có hiệu lực trong {expireMinutes} phút. Vui lòng không chia sẻ mã này cho bất kỳ ai.</p>"
            };

            using var smtp = new SmtpClient();
            var smtpServer = _configuration["EmailSettings:SmtpServer"] ?? "smtp.gmail.com";
            var smtpPort = int.Parse(_configuration["EmailSettings:SmtpPort"] ?? "587");

            await smtp.ConnectAsync(smtpServer, smtpPort, SecureSocketOptions.StartTls);
            await smtp.AuthenticateAsync(
                _configuration["EmailSettings:SenderEmail"],
                _configuration["EmailSettings:AppPassword"]);
            await smtp.SendAsync(email);
            await smtp.DisconnectAsync(true);
        }
    }

    // ==================== DTOs CÓ VALIDATION ====================
    public class RegisterRequest
    {
        [Required][EmailAddress] public string Email { get; set; } = string.Empty;
        [Required][MinLength(3)] public string Username { get; set; } = string.Empty;
        [Required][MinLength(6)] public string Password { get; set; } = string.Empty;
    }

    public class LoginRequest
    {
        [Required][EmailAddress] public string Email { get; set; } = string.Empty;
        [Required] public string Password { get; set; } = string.Empty;
    }

    public class ChangePasswordRequest
    {
        [Required] public string OldPassword { get; set; } = string.Empty;
        [Required][MinLength(6)] public string NewPassword { get; set; } = string.Empty;
    }

    public class ForgotPasswordRequest
    {
        [Required][EmailAddress] public string Email { get; set; } = string.Empty;
    }

    public class ResetPasswordRequest
    {
        public string? Email { get; set; }
        [Required] public string Token { get; set; } = string.Empty;
        [Required][MinLength(6)] public string NewPassword { get; set; } = string.Empty;
    }
}