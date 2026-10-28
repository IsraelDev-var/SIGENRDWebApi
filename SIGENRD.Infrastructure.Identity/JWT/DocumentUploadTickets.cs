using Microsoft.IdentityModel.Tokens;
using SIGENRD.Core.Application.Interfaces.Services;
using SIGENRD.Core.Domain.Enums;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace SIGENRD.Infrastructure.Identity.JWT
{
    /// <summary>
    /// Emite los permisos de subida directa como JWT RS256 con la misma llave que la sesión, pero con otra audiencia:
    /// el esquema de sesión los rechaza y el de subida (DocumentUploadTicketDefaults.Scheme) rechaza los de sesión.
    /// </summary>
    public class DocumentUploadTickets(JwtSettings settings, JwtSigningKeys keys) : IDocumentUploadTickets
    {
        public DocumentUploadTicket Issue(string userId, string fullName, string role, int requestId, DocumentType documentType)
        {
            var expiresAt = DateTime.UtcNow.Add(DocumentUploadTicketDefaults.Lifetime);
            var token = new JwtSecurityToken(
                issuer: settings.Issuer,
                audience: settings.Audience + DocumentUploadTicketDefaults.AudienceSuffix,
                claims:
                [
                    new Claim(JwtRegisteredClaimNames.Sub, userId),
                    new Claim("FullName", fullName),
                    new Claim(ClaimTypes.Role, role),
                    new Claim(DocumentUploadTicketDefaults.RequestIdClaim, requestId.ToString()),
                    new Claim(DocumentUploadTicketDefaults.DocumentTypeClaim, documentType.ToString()),
                    // Identifica el permiso para que se use una sola vez.
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
                ],
                expires: expiresAt,
                signingCredentials: keys.SigningCredentials);

            return new DocumentUploadTicket(new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
        }
    }
}
