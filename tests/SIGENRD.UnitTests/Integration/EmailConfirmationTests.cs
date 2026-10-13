using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using SIGENRD.Infrastructure.Identity.Services;

namespace SIGENRD.UnitTests.Integration;

/// <summary>El registro no entrega token ni permite iniciar sesión hasta confirmar el correo.</summary>
[Collection(ApiCollection.Name)]
public class EmailConfirmationTests(ApiFactory api)
{
    private static readonly Regex Link = new("href='([^']+)'", RegexOptions.Compiled);

    [FactIfDatabase]
    public async Task Flujo_de_registro_confirmacion_e_inicio_de_sesion()
    {
        var client = await api.ClientFor(TestRole.Anonymous);
        var email = "nuevo-" + Guid.NewGuid().ToString("N")[..8] + "@test.energymap.local";
        var credentials = new { email, password = ApiFactory.TestPassword };

        // 1. Registro: sin token.
        var register = await client.PostAsJsonAsync("/api/v1/Auth/register",
            new { fullName = "Nuevo cliente", email, password = ApiFactory.TestPassword, userType = 1 });
        Assert.Equal(HttpStatusCode.OK, register.StatusCode);

        using (var doc = JsonDocument.Parse(await register.Content.ReadAsStringAsync()))
        {
            var token = doc.RootElement.GetProperty("token");
            Assert.True(token.ValueKind == JsonValueKind.Null, "el registro no debe devolver un token");
        }

        // 2. Sin confirmar no se puede iniciar sesión.
        var early = await client.PostAsJsonAsync("/api/v1/Auth/login", credentials);
        Assert.Equal(HttpStatusCode.BadRequest, early.StatusCode);

        // 3. El enlace del correo confirma la cuenta.
        var mail = Assert.Single(api.Emails.Sent, e => e.To == email);
        var href = Link.Match(mail.Body).Groups[1].Value;
        Assert.False(string.IsNullOrEmpty(href), "el correo debe incluir el enlace de confirmación");

        var confirm = await client.GetAsync(new Uri(href).PathAndQuery);
        Assert.Equal(HttpStatusCode.OK, confirm.StatusCode);
        Assert.Contains("confirmado", await confirm.Content.ReadAsStringAsync());

        // 4. Ahora sí.
        var login = await client.PostAsJsonAsync("/api/v1/Auth/login", credentials);
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);

        using var loginDoc = JsonDocument.Parse(await login.Content.ReadAsStringAsync());
        Assert.False(string.IsNullOrEmpty(loginDoc.RootElement.GetProperty("token").GetString()));
    }

    [FactIfDatabase]
    public async Task Un_codigo_de_confirmacion_alterado_no_confirma_la_cuenta()
    {
        var client = await api.ClientFor(TestRole.Anonymous);
        var email = "manipulado-" + Guid.NewGuid().ToString("N")[..8] + "@test.energymap.local";

        await client.PostAsJsonAsync("/api/v1/Auth/register",
            new { fullName = "Manipulado", email, password = ApiFactory.TestPassword, userType = 1 });

        var mail = Assert.Single(api.Emails.Sent, e => e.To == email);
        var uri = new Uri(Link.Match(mail.Body).Groups[1].Value);
        var tampered = uri.PathAndQuery.Substring(0, uri.PathAndQuery.Length - 4) + "AAAA";

        var confirm = await client.GetAsync(tampered);
        var login = await client.PostAsJsonAsync("/api/v1/Auth/login", new { email, password = ApiFactory.TestPassword });

        // Un enlace que no sirve responde 400 (antes era 200 con un texto de error).
        Assert.Equal(HttpStatusCode.BadRequest, confirm.StatusCode);
        using (var error = JsonDocument.Parse(await confirm.Content.ReadAsStringAsync()))
            Assert.Equal(AuthService.EmailConfirmationFailedMessage, error.RootElement.GetProperty("message").GetString());
        Assert.Equal(HttpStatusCode.BadRequest, login.StatusCode);
    }

    [FactIfDatabase]
    public async Task Un_usuario_inexistente_responde_igual_que_un_codigo_invalido()
    {
        var client = await api.ClientFor(TestRole.Anonymous);

        var unknown = await client.GetAsync("/api/v1/Auth/confirm-email?userId=" + Guid.NewGuid() + "&code=QUJD");
        var garbage = await client.GetAsync("/api/v1/Auth/confirm-email?userId=" + Guid.NewGuid() + "&code=%%%");

        Assert.Equal(HttpStatusCode.BadRequest, unknown.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, garbage.StatusCode);
        Assert.Equal(await unknown.Content.ReadAsStringAsync(), await garbage.Content.ReadAsStringAsync());
    }
}
