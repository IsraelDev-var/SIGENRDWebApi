using Microsoft.IdentityModel.Tokens;
using SIGENRD.Infrastructure.Identity.Entities;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;


namespace SIGENRD.Infrastructure.Identity.JWT
{
    public class JwtTokenGenerator
    {
        private readonly JwtSettings _jwtSettings;
        private readonly SymmetricSecurityKey _securityKey;
        private readonly SigningCredentials _credentials;

        public JwtTokenGenerator(JwtSettings jwtSettings)
        {
            _jwtSettings = jwtSettings;

            _securityKey = BuildSecurityKey(jwtSettings.Key);
            _credentials = new SigningCredentials(_securityKey, SecurityAlgorithms.HmacSha256);
        }

        public string GenerateToken(ApplicationUser user, IList<string> roles)
        {
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id),
                new Claim(JwtRegisteredClaimNames.Email, user.Email!),
                new Claim("FullName", user.FullName),
                new Claim("UserType", user.UserType.ToString()),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            // Agregar roles
            claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

            var token = new JwtSecurityToken(
                issuer: _jwtSettings.Issuer,
                audience: _jwtSettings.Audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(_jwtSettings.DurationInMinutes),
                signingCredentials: _credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }


        // ==========================
        // MÉTODOS PRIVADOS
        // ==========================

        private static SymmetricSecurityKey BuildSecurityKey(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("JwtSettings.Key is empty or missing.");

            var keyBytes = DecodeBase64Key(key);

            if (keyBytes.Length < 32) // 32 bytes = 256 bits
            {
                throw new InvalidOperationException(
                    $"JWT key too short: {keyBytes.Length * 8} bits. Minimum 256 bits required."
                );
            }

            return new SymmetricSecurityKey(keyBytes);
        }

        private static byte[] DecodeBase64Key(string key)
        {
            try
            {
                return Convert.FromBase64String(key);
            }
            catch
            {
                throw new InvalidOperationException(
                    "JwtSettings.Key must be a valid Base64 string representing at least 32 bytes."
                );
            }
        }
    }
}