

using Microsoft.IdentityModel.Tokens;
using SIGENRD.Infrastruture.Identity.Entities;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
namespace SIGENRD.Infrastruture.Identity.JWT
{
    public class JwtTokenGenerator(JwtSettings jwtSettings)
    {
        private readonly JwtSettings _jwtSettings = jwtSettings;


        public string GeneratedToken(ApplicationUser user, IList<string> roles)
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

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Key));
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

    }
}
