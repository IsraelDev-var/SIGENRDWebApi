using Microsoft.IdentityModel.Tokens;
using SIGENRD.Infrastructure.Identity.Entities;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;


namespace SIGENRD.Infrastructure.Identity.JWT
{
    public class JwtTokenGenerator
    {
        private readonly JwtSettings _jwtSettings;
        private readonly SigningCredentials _credentials;

        public JwtTokenGenerator(JwtSettings jwtSettings, JwtSigningKeys keys)
        {
            _jwtSettings = jwtSettings;
            _credentials = keys.SigningCredentials; // RS256 con la llave privada
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


    }
}
