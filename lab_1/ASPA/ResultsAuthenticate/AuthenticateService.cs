using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Authenticate
{
    public class AuthenticateService : IAuthenticateService
    {
        private readonly IConfiguration _config;
        private readonly Dictionary<string, (string Password, string Role)> _users = new()
        {
            ["reader"] = ("reader123", "READER"),
            ["writer"] = ("writer123", "WRITER")
        };

        public AuthenticateService(IConfiguration config)
        {
            _config = config;
        }

        public async Task<bool> SignInAsync(HttpContext context, string login, string password)
        {
            if (!_users.TryGetValue(login, out var user) || user.Password != password)
                return false;

            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Convert.FromBase64String(_config["Jwt:Secret"]!);

            var claims = new List<Claim>
            {
                new(ClaimTypes.Name, login),
                new(ClaimTypes.Role, user.Role)
            };

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddHours(1),
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            var tokenString = tokenHandler.WriteToken(token);

            context.Response.Cookies.Append("auth-token", tokenString, new CookieOptions
            {
                HttpOnly = false, // Убираем HttpOnly, чтобы токен был виден в DevTools
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTime.UtcNow.AddHours(1)
            });

            return true;
        }

        public async Task SignOutAsync(HttpContext context)
        {
            // Проверяем наличие токена в cookie
            if (context.Request.Cookies.TryGetValue("auth-token", out var token))
            {
                // Валидируем токен
                try
                {
                    var tokenHandler = new JwtSecurityTokenHandler();
                    var key = Convert.FromBase64String(_config["Jwt:Secret"]!);
                    
                    var validationParameters = new TokenValidationParameters
                    {
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(key),
                        ValidateIssuer = false,
                        ValidateAudience = false,
                        ValidateLifetime = false // Не проверяем срок действия при выходе
                    };

                    var principal = tokenHandler.ValidateToken(token, validationParameters, out SecurityToken validatedToken);
                }
                catch
                {
                    // Игнорируем ошибки валидации при выходе
                }

                // Оставляем токен в cookies (не удаляем)
                // Обновляем cookie, чтобы токен оставался видимым
                context.Response.Cookies.Append("auth-token", token, new CookieOptions
                {
                    HttpOnly = false, // Убираем HttpOnly, чтобы токен был виден в DevTools
                    Secure = true,
                    SameSite = SameSiteMode.Strict,
                    Expires = DateTime.UtcNow.AddHours(1) // Оставляем срок действия
                });
            }
        }
    }
}