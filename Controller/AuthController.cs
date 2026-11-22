using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GameBackend.Models;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using System.ComponentModel.DataAnnotations;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;

namespace GameBackend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly IHttpClientFactory _httpClientFactory;

        public AuthController(AppDbContext context, IConfiguration configuration, IHttpClientFactory httpClientFactory)
        {
            _context = context;
            _configuration = configuration;
            _httpClientFactory = httpClientFactory;
        }

        // ==================== API ĐĂNG KÝ ====================
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            var normalizedEmail = request.Email.Trim().ToLowerInvariant();
            var normalizedUsername = request.Username.Trim();

            if (await _context.Users.AnyAsync(u => u.Email == normalizedEmail))
                return BadRequest(new { message = "Email đã được sử dụng!" });

            if (await _context.Users.AnyAsync(u => u.Username == normalizedUsername))
                return BadRequest(new { message = "Tên người dùng đã tồn tại!" });

            var user = new User
            {
                Email = normalizedEmail,
                Username = normalizedUsername,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password)
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Đăng ký thành công!" });
        }

        // ==================== API ĐĂNG NHẬP (GỘP LUỒNG) ====================
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
                ServerCustomId = $"Player_{user.Id}", // SỬA LỖI: Dùng đúng tên trường
                CreateAccount = true
            };

            var response = await client.PostAsJsonAsync($"https://{titleId}.playfabapi.com/Server/LoginWithServerCustomId", playFabRequest);

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
                username = user.Username,
                playFabId = data.GetProperty("PlayFabId").GetString(),
                sessionTicket = data.GetProperty("SessionTicket").GetString(),
                entityToken = data.GetProperty("EntityToken").GetProperty("EntityToken").GetString() // SỬA LỖI RUNTIME
            });
        }

        // ==================== API QUÊN MẬT KHẨU ====================
        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
        {
            var normalizedEmail = request.Email.Trim().ToLowerInvariant();
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == normalizedEmail);

            if (user == null)
                return Ok(new { message = "Nếu email tồn tại, mã khôi phục đã được gửi!" });

            user.ResetToken = Guid.NewGuid().ToString("N");
            user.ResetTokenExpiry = DateTime.UtcNow.AddMinutes(15);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Nếu email tồn tại, mã khôi phục đã được gửi!", devToken = user.ResetToken });
        }

        // ==================== API ĐẶT LẠI MẬT KHẨU ====================
        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => 
                u.ResetToken == request.Token && u.ResetTokenExpiry > DateTime.UtcNow);

            if (user == null)
                return BadRequest(new { message = "Mã khôi phục không hợp lệ hoặc đã hết hạn!" });

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            user.ResetToken = null;
            user.ResetTokenExpiry = null;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Đổi mật khẩu thành công!" });
        }

        // Hàm tạo JWT
        private string GenerateJwtToken(User user)
        {
            var jwtKey = _configuration["Jwt:Key"] ?? throw new InvalidOperationException("Thiếu Jwt:Key");
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(jwtKey);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
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
    }

    // DTOs có Validation
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
    public class ForgotPasswordRequest
    {
        [Required][EmailAddress] public string Email { get; set; } = string.Empty;
    }
    public class ResetPasswordRequest
    {
        [Required] public string Token { get; set; } = string.Empty;
        [Required][MinLength(6)] public string NewPassword { get; set; } = string.Empty;
    }
}