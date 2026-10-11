using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SIGENRD.Core.Domain.Interfaces;
using SIGENRD.Infrastructure.Identity.Services;

namespace SIGENRD.UnitTests.Integration;

/// <summary>
/// Regresión de los hallazgos P1 de la auditoría del 2026-10-10:
/// SEC-06 RS256, SEC-07 política de contraseñas, SEC-08 enumeración en el registro,
/// SEC-09 recuperación de contraseña, SEC-12 auditoría de autenticación y OPS-01 health.
/// </summary>
[Collection(ApiCollection.Name)]
public class AccountSecurityTests(ApiFactory api)
{
    private const string Issuer = "SIGENRD.API";
    private const string Audience = "SIGENRD.Client";
    private const string StaffOnlyUrl = "/api/v1/Transformers/critical-zones";

    private static readonly Regex Link = new("href='([^']+)'", RegexOptions.Compiled);

    private static string NewEmail(string prefix) => prefix + "-" + Guid.NewGuid().ToString("N")[..8] + "@test.energymap.local";

    private static Task<HttpResponseMessage> Login(HttpClient client, string email, string password)
        => client.PostAsJsonAsync("/api/v1/Auth/login", new { email, password });

    // ---------------- SEC-06: RS256 ----------------

    [FactIfDatabase]
    public async Task El_token_se_firma_con_RS256_e_identifica_la_llave()
    {
        var session = await api.LoginAsync(TestRole.Customer);

        var token = new JwtSecurityTokenHandler().ReadJwtToken(session.Token);

        Assert.Equal(SecurityAlgorithms.RsaSha256, token.Header.Alg);
        Assert.False(string.IsNullOrEmpty(token.Header.Kid));
    }

    private static string ForgeAdminToken(SigningCredentials credentials) =>
        new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken(
            issuer: Issuer,
            audience: Audience,
            claims: new[] { new Claim(JwtRegisteredClaimNames.Sub, "intruso"), new Claim(ClaimTypes.Role, "Admin") },
            expires: DateTime.UtcNow.AddMinutes(10),
            signingCredentials: credentials));

    private async Task<HttpStatusCode> CallStaffEndpointWith(string token)
    {
        var client = api.CreateClient();
        client.DefaultRequestHeaders.Authorization = new("Bearer", token);
        return (await client.GetAsync(StaffOnlyUrl)).StatusCode;
    }

    [FactIfDatabase]
    public async Task Un_token_firmado_con_la_llave_de_la_API_es_aceptado()
    {
        // Control: confirma que los tokens falsificados de abajo solo fallan por la firma.
        var token = ForgeAdminToken(new SigningCredentials(new RsaSecurityKey(api.SigningKey), SecurityAlgorithms.RsaSha256));

        Assert.Equal(HttpStatusCode.OK, await CallStaffEndpointWith(token));
    }

    [FactIfDatabase]
    public async Task Un_token_firmado_con_otra_llave_RSA_es_401()
    {
        using var otherKey = RSA.Create(2048);
        var token = ForgeAdminToken(new SigningCredentials(new RsaSecurityKey(otherKey), SecurityAlgorithms.RsaSha256));

        Assert.Equal(HttpStatusCode.Unauthorized, await CallStaffEndpointWith(token));
    }

    [FactIfDatabase]
    public async Task Un_token_HS256_firmado_con_la_llave_publica_es_401()
    {
        // Confusión de algoritmo: la llave pública no es secreta, no debe servir como llave HMAC.
        var publicKeyBytes = api.SigningKey.ExportSubjectPublicKeyInfo();
        var token = ForgeAdminToken(new SigningCredentials(new SymmetricSecurityKey(publicKeyBytes), SecurityAlgorithms.HmacSha256));

        Assert.Equal(HttpStatusCode.Unauthorized, await CallStaffEndpointWith(token));
    }

    [FactIfDatabase]
    public async Task Un_token_sin_firma_es_401()
    {
        var header = Base64UrlEncoder.Encode("{\"alg\":\"none\",\"typ\":\"JWT\"}");
        var payload = Base64UrlEncoder.Encode(JsonSerializer.Serialize(new Dictionary<string, object>
        {
            ["sub"] = "intruso",
            ["role"] = "Admin",
            ["iss"] = Issuer,
            ["aud"] = Audience,
            ["exp"] = DateTimeOffset.UtcNow.AddMinutes(10).ToUnixTimeSeconds()
        }));

        Assert.Equal(HttpStatusCode.Unauthorized, await CallStaffEndpointWith(header + "." + payload + "."));
    }

    // ---------------- SEC-07: política de contraseñas ----------------

    [TheoryIfDatabase]
    [InlineData("Corta1A")]          // menos de 10
    [InlineData("sinmayuscula123")]  // sin mayúscula
    [InlineData("SINMINUSCULA123")]  // sin minúscula
    [InlineData("SinNumerosAqui")]   // sin número
    [InlineData("Password2026!")]    // palabra común
    [InlineData("Edesur12345")]      // palabra del dominio
    public async Task El_registro_rechaza_contraseñas_debiles(string password)
    {
        var response = await api.CreateClient().PostAsJsonAsync("/api/v1/Auth/register",
            new { fullName = "Débil", email = NewEmail("debil"), password, userType = 1 });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [FactIfDatabase]
    public async Task El_registro_rechaza_una_contraseña_que_contiene_el_correo()
    {
        var email = NewEmail("mariaperez");
        var password = "X" + email.Split('@')[0] + "9";

        var response = await api.CreateClient().PostAsJsonAsync("/api/v1/Auth/register",
            new { fullName = "Correo", email, password, userType = 1 });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ---------------- SEC-08: enumeración en el registro ----------------

    [FactIfDatabase]
    public async Task Registrar_un_correo_existente_responde_igual_que_uno_nuevo()
    {
        var client = api.CreateClient();
        var existing = ApiFactory.Email(TestRole.Customer);
        var fresh = NewEmail("enum");

        var forExisting = await client.PostAsJsonAsync("/api/v1/Auth/register",
            new { fullName = "Otro", email = existing, password = ApiFactory.TestPassword, userType = 1 });
        var forNew = await client.PostAsJsonAsync("/api/v1/Auth/register",
            new { fullName = "Otro", email = fresh, password = ApiFactory.TestPassword, userType = 1 });

        Assert.Equal(HttpStatusCode.OK, forExisting.StatusCode);
        Assert.Equal(HttpStatusCode.OK, forNew.StatusCode);
        Assert.Equal(
            (await forNew.Content.ReadAsStringAsync()).Replace(fresh, "{email}"),
            (await forExisting.Content.ReadAsStringAsync()).Replace(existing, "{email}"));

        // El aviso de "ya tienes cuenta" solo le llega al dueño del correo.
        Assert.Contains(api.Emails.Sent, e => e.To == existing && e.Subject.Contains("Ya tienes una cuenta"));
    }

    [FactIfDatabase]
    public async Task Con_un_correo_existente_una_contraseña_debil_falla_igual_que_con_uno_nuevo()
    {
        var response = await api.CreateClient().PostAsJsonAsync("/api/v1/Auth/register",
            new { fullName = "Otro", email = ApiFactory.Email(TestRole.Customer), password = "corta", userType = 1 });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ---------------- SEC-09: recuperación de contraseña ----------------

    private async Task<string> RequestResetCodeAsync(HttpClient client, string email)
    {
        var forgot = await client.PostAsJsonAsync("/api/v1/Auth/forgot-password", new { email });
        Assert.Equal(HttpStatusCode.OK, forgot.StatusCode);

        var mail = api.Emails.Sent.Last(e => e.To == email && e.Subject.Contains("Restablecer"));
        var href = Link.Match(mail.Body).Groups[1].Value;
        var query = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(new Uri(href).Query);
        return query["code"].ToString();
    }

    [FactIfDatabase]
    public async Task Flujo_de_recuperacion_cambia_la_contraseña_y_el_codigo_no_se_reutiliza()
    {
        var (_, _, email) = await api.NewUserAsync();
        var client = api.CreateClient();
        const string newPassword = "Nueva-Clave-Segura-77";

        var code = await RequestResetCodeAsync(client, email);

        var reset = await client.PostAsJsonAsync("/api/v1/Auth/reset-password", new { email, code, newPassword });
        Assert.Equal(HttpStatusCode.OK, reset.StatusCode);

        Assert.Equal(HttpStatusCode.BadRequest, (await Login(client, email, ApiFactory.TestPassword)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await Login(client, email, newPassword)).StatusCode);

        var reused = await client.PostAsJsonAsync("/api/v1/Auth/reset-password",
            new { email, code, newPassword = "Otra-Clave-Segura-88" });
        Assert.Equal(HttpStatusCode.BadRequest, reused.StatusCode);
    }

    [FactIfDatabase]
    public async Task Pedir_recuperacion_de_un_correo_inexistente_responde_igual_y_no_envia_nada()
    {
        var email = NewEmail("fantasma");

        var response = await api.CreateClient().PostAsJsonAsync("/api/v1/Auth/forgot-password", new { email });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains(AuthService.ForgotPasswordMessage, await response.Content.ReadAsStringAsync());
        Assert.DoesNotContain(api.Emails.Sent, e => e.To == email);
    }

    [FactIfDatabase]
    public async Task Un_codigo_alterado_no_restablece_la_contraseña()
    {
        var (_, _, email) = await api.NewUserAsync();
        var client = api.CreateClient();
        var code = await RequestResetCodeAsync(client, email);

        var reset = await client.PostAsJsonAsync("/api/v1/Auth/reset-password",
            new { email, code = code[..^4] + "AAAA", newPassword = "Nueva-Clave-Segura-77" });

        Assert.Equal(HttpStatusCode.BadRequest, reset.StatusCode);
        using (var doc = JsonDocument.Parse(await reset.Content.ReadAsStringAsync()))
            Assert.Equal(AuthService.InvalidResetCodeMessage, doc.RootElement.GetProperty("message").GetString());
        Assert.Equal(HttpStatusCode.OK, (await Login(client, email, ApiFactory.TestPassword)).StatusCode);
    }

    [FactIfDatabase]
    public async Task Restablecer_la_contraseña_levanta_el_bloqueo()
    {
        var (_, _, email) = await api.NewUserAsync();
        var client = api.CreateClient();
        for (var i = 0; i < 5; i++)
            await Login(client, email, "Incorrecta-12345");
        Assert.Equal(HttpStatusCode.TooManyRequests, (await Login(client, email, ApiFactory.TestPassword)).StatusCode);

        const string newPassword = "Nueva-Clave-Segura-77";
        var code = await RequestResetCodeAsync(client, email);
        (await client.PostAsJsonAsync("/api/v1/Auth/reset-password", new { email, code, newPassword })).EnsureSuccessStatusCode();

        Assert.Equal(HttpStatusCode.OK, (await Login(client, email, newPassword)).StatusCode);
    }

    // ---------------- SEC-12: auditoría de autenticación ----------------

    [FactIfDatabase]
    public async Task Los_inicios_de_sesion_quedan_en_la_auditoria()
    {
        var (_, userId, email) = await api.NewUserAsync(); // incluye un inicio de sesión correcto
        var client = api.CreateClient();
        var unknown = NewEmail("desconocido");

        await Login(client, email, "Incorrecta-12345");
        await Login(client, unknown, "Incorrecta-12345");

        await using var db = api.NewAppContext();
        // NewValues es jsonb: se filtra el correo en memoria.
        var events = await db.AuditLogs.AsNoTracking()
            .Where(a => a.EntityName == SecurityEvents.EntityName && (a.UserId == userId || a.UserId == null))
            .ToListAsync();

        Assert.Contains(events, e => e.Action == SecurityEvents.LoginSucceeded && e.UserId == userId);
        Assert.Contains(events, e => e.Action == SecurityEvents.LoginFailed && e.UserId == userId);
        Assert.Contains(events, e => e.Action == SecurityEvents.LoginFailed && e.UserId == null && e.NewValues!.Contains(unknown));
    }

    // ---------------- OPS-01: health ----------------

    [TheoryIfDatabase]
    [InlineData("/health")]
    [InlineData("/health/live")]
    public async Task Health_responde_Healthy_con_la_base_disponible(string url)
    {
        var response = await api.CreateClient().GetAsync(url);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
    }
}
