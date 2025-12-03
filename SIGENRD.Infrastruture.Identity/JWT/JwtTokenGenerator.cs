using Microsoft.IdentityModel.Tokens;
using SIGENRD.Infrastructure.Identity.Entities;
using SIGENRD.Infrastructure.Identity.JWT;
using System.IdentityModel.Tokens.Jwt;  
using System.Security.Claims;
using System.Text;

namespace SIGENRD.Infrastructure.Identity.JWT
{
    public class JwtTokenGenerator(JwtSettings jwtSettings)
    {
        private readonly JwtSettings _jwtSettings = jwtSettings;

        public string GenerateToken(ApplicationUser user, IList<string> roles)
        {
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id),
                new Claim(JwtRegisteredClaimNames.Email, user.Email!),
                new Claim("FullName", user.FullName),
                new Claim("UserType", user.UserType.ToString())
            };

            foreach (var role in roles)
                claims.Add(new Claim(ClaimTypes.Role, role));

            var keyBytes = GetKeyBytes(_jwtSettings.Key);
            // Recommended: at least 32 bytes (256 bits) for HMAC-SHA256
            if (keyBytes.Length < 16)
                throw new InvalidOperationException("The JWT signing key is too short. Use at least 128 bits (16 bytes), recommended 256 bits (32 bytes).");

            var key = new SymmetricSecurityKey(keyBytes);
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _jwtSettings.Issuer,
                audience: _jwtSettings.Audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(_jwtSettings.DurationInMinutes),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private static byte[] GetKeyBytes(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("JwtSettings.Key is not configured or is empty.", nameof(key));

            // Try Base64 first (existing deployments may store Base64)
            try
            {
                return Convert.FromBase64String(key);
            }
            catch (FormatException)
            {
                // Not Base64: treat as plain-text secret (UTF-8)
                return Encoding.UTF8.GetBytes(key);
            }
        }
    }
}
