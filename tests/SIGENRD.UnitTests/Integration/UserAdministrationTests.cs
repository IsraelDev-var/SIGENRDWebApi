using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using SIGENRD.Core.Domain.Interfaces;

namespace SIGENRD.UnitTests.Integration;

/// <summary>B-5: el administrador lista, desactiva y reactiva usuarios internos; una cuenta desactivada no inicia sesión.</summary>
[Collection(ApiCollection.Name)]
public class UserAdministrationTests(ApiFactory api)
{
    private static async Task<JsonElement> Data(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return doc.RootElement.GetProperty("data").Clone();
    }

    private const string TemporaryPassword = "Temporal-Admin-2026";

    /// <summary>
    /// Un usuario interno nuevo, dado de alta por el administrador, con el correo confirmado y la contraseña temporal ya
    /// cambiada por <see cref="ApiFactory.TestPassword"/>, como lo usaría la persona.
    /// </summary>
    private async Task<(string Id, string Email)> NewInternalAsync(string role)
    {
        var (id, email) = await NewInternalWithTemporaryPasswordAsync(role);
        var client = api.CreateClient();
        client.DefaultRequestHeaders.Authorization = new("Bearer", await TokenAsync(email, TemporaryPassword));
        var changed = await client.PostAsJsonAsync("/api/v1/Auth/change-password",
            new { currentPassword = TemporaryPassword, newPassword = ApiFactory.TestPassword });
        Assert.Equal(HttpStatusCode.OK, changed.StatusCode);
        return (id, email);
    }

    private async Task<(string Id, string Email)> NewInternalWithTemporaryPasswordAsync(string role)
    {
        var admin = await api.ClientFor(TestRole.Admin);
        var email = "int-" + Guid.NewGuid().ToString("N")[..8] + "@test.energymap.local";
        var created = await admin.PostAsJsonAsync("/api/v1/Auth/register-internal",
            new { fullName = "Interno " + email[..12], email, password = TemporaryPassword, userType = role });
        Assert.Equal(HttpStatusCode.OK, created.StatusCode);

        var mail = api.Emails.Sent.Single(e => e.To == email);
        var href = Regex.Match(mail.Body, "href='([^']+)'").Groups[1].Value;
        (await api.CreateClient().GetAsync(new Uri(href).PathAndQuery)).EnsureSuccessStatusCode();

        var userId = JsonDocument.Parse(await created.Content.ReadAsStringAsync()).RootElement.GetProperty("userId").GetString()!;
        return (userId, email);
    }

    private async Task<string> TokenAsync(string email, string password)
    {
        var login = await api.CreateClient().PostAsJsonAsync("/api/v1/Auth/login", new { email, password });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        return JsonDocument.Parse(await login.Content.ReadAsStringAsync()).RootElement.GetProperty("token").GetString()!;
    }

    [FactIfDatabase]
    public async Task Con_la_contrasena_temporal_solo_se_puede_cambiarla()
    {
        var (_, email) = await NewInternalWithTemporaryPasswordAsync("Engineer");

        var login = await api.CreateClient().PostAsJsonAsync("/api/v1/Auth/login", new { email, password = TemporaryPassword });
        using (var doc = JsonDocument.Parse(await login.Content.ReadAsStringAsync()))
            Assert.True(doc.RootElement.GetProperty("mustChangePassword").GetBoolean());

        var client = api.CreateClient();
        client.DefaultRequestHeaders.Authorization = new("Bearer", await TokenAsync(email, TemporaryPassword));

        var blocked = await client.GetAsync("/api/v1/ConnectionRequest/search");
        Assert.Equal(HttpStatusCode.Forbidden, blocked.StatusCode);
        Assert.Contains("temporal", await blocked.Content.ReadAsStringAsync()); // el JSON escapa la eñe

        // La actual equivocada, o repetir la misma, no sirve.
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/v1/Auth/change-password",
            new { currentPassword = "Otra-Clave-2026", newPassword = "Nueva-Clave-2026" })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/v1/Auth/change-password",
            new { currentPassword = TemporaryPassword, newPassword = TemporaryPassword })).StatusCode);

        var changed = await client.PostAsJsonAsync("/api/v1/Auth/change-password",
            new { currentPassword = TemporaryPassword, newPassword = "Nueva-Clave-2026" });
        Assert.Equal(HttpStatusCode.OK, changed.StatusCode);
        var newToken = JsonDocument.Parse(await changed.Content.ReadAsStringAsync()).RootElement.GetProperty("token").GetString()!;

        var free = api.CreateClient();
        free.DefaultRequestHeaders.Authorization = new("Bearer", newToken);
        Assert.Equal(HttpStatusCode.OK, (await free.GetAsync("/api/v1/ConnectionRequest/search")).StatusCode);

        // La anterior ya no entra; la nueva sí, sin la marca.
        Assert.Equal(HttpStatusCode.BadRequest, (await api.CreateClient().PostAsJsonAsync("/api/v1/Auth/login",
            new { email, password = TemporaryPassword })).StatusCode);
        var again = await api.CreateClient().PostAsJsonAsync("/api/v1/Auth/login", new { email, password = "Nueva-Clave-2026" });
        using (var doc = JsonDocument.Parse(await again.Content.ReadAsStringAsync()))
            Assert.False(doc.RootElement.GetProperty("mustChangePassword").GetBoolean());
    }

    private Task<HttpResponseMessage> Login(string email) =>
        api.CreateClient().PostAsJsonAsync("/api/v1/Auth/login", new { email, password = ApiFactory.TestPassword });

    [FactIfDatabase]
    public async Task Lista_solo_usuarios_internos_y_filtra_por_texto_y_rol()
    {
        var (_, email) = await NewInternalAsync("Engineer");
        var admin = await api.ClientFor(TestRole.Admin);

        var byText = await Data(await admin.GetAsync("/api/v1/Users?text=" + email[..12]));
        var user = Assert.Single(byText.GetProperty("items").EnumerateArray());
        Assert.Equal("Engineer", user.GetProperty("role").GetString());
        Assert.True(user.GetProperty("isActive").GetBoolean());
        Assert.True(user.GetProperty("emailConfirmed").GetBoolean());

        // Clientes e instaladoras no aparecen: se registran solos.
        var all = await Data(await admin.GetAsync("/api/v1/Users?pageSize=100"));
        Assert.DoesNotContain(all.GetProperty("items").EnumerateArray(), u => u.GetProperty("role").GetString() is "Customer" or "InstallerCompany");

        var admins = await Data(await admin.GetAsync("/api/v1/Users?role=Admin&pageSize=100"));
        Assert.All(admins.GetProperty("items").EnumerateArray(), u => Assert.Equal("Admin", u.GetProperty("role").GetString()));

        Assert.Equal(HttpStatusCode.BadRequest, (await admin.GetAsync("/api/v1/Users?role=Customer")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.GetAsync("/api/v1/Users?pageSize=1000")).StatusCode);
    }

    [FactIfDatabase]
    public async Task Una_cuenta_desactivada_no_inicia_sesion_hasta_que_se_reactiva()
    {
        var (id, email) = await NewInternalAsync("Distributor");
        var admin = await api.ClientFor(TestRole.Admin);
        Assert.Equal(HttpStatusCode.OK, (await Login(email)).StatusCode);

        var deactivated = await Data(await admin.PostAsync("/api/v1/Users/" + id + "/deactivate", null));
        Assert.False(deactivated.GetProperty("isActive").GetBoolean());

        // La contraseña es correcta: se dice el motivo (403), no "credenciales inválidas".
        var rejected = await Login(email);
        Assert.Equal(HttpStatusCode.Forbidden, rejected.StatusCode);
        Assert.Contains("desactivada", await rejected.Content.ReadAsStringAsync());

        // Con una contraseña equivocada no se revela que está desactivada.
        var wrong = await api.CreateClient().PostAsJsonAsync("/api/v1/Auth/login", new { email, password = "Otra-Clave-2026" });
        Assert.Equal(HttpStatusCode.BadRequest, wrong.StatusCode);

        var reactivated = await Data(await admin.PostAsync("/api/v1/Users/" + id + "/reactivate", null));
        Assert.True(reactivated.GetProperty("isActive").GetBoolean());
        Assert.Equal(HttpStatusCode.OK, (await Login(email)).StatusCode);

        // Quedó en la auditoría, con quién lo hizo.
        var adminId = (await api.LoginAsync(TestRole.Admin)).UserId;
        await using var context = api.NewAppContext();
        var events = await context.AuditLogs.AsNoTracking().Where(a => a.EntityId == id).Select(a => new { a.Action, a.NewValues }).ToListAsync();
        Assert.Contains(events, e => e.Action == SecurityEvents.AccountDeactivated && e.NewValues!.Contains(adminId));
        Assert.Contains(events, e => e.Action == SecurityEvents.AccountReactivated);
        Assert.Contains(events, e => e.Action == SecurityEvents.LoginRejectedDeactivated);
    }

    [FactIfDatabase]
    public async Task El_administrador_no_se_desactiva_a_si_mismo_ni_toca_cuentas_que_no_son_internas()
    {
        var admin = await api.ClientFor(TestRole.Admin);
        var self = (await api.LoginAsync(TestRole.Admin)).UserId;
        var own = await admin.PostAsync("/api/v1/Users/" + self + "/deactivate", null);
        Assert.Equal(HttpStatusCode.BadRequest, own.StatusCode);
        Assert.Contains("propia cuenta", await own.Content.ReadAsStringAsync());

        var customer = (await api.LoginAsync(TestRole.Customer)).UserId;
        Assert.Equal(HttpStatusCode.NotFound, (await admin.PostAsync("/api/v1/Users/" + customer + "/deactivate", null)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await admin.PostAsync("/api/v1/Users/no-existe/deactivate", null)).StatusCode);
    }

    [FactIfDatabase]
    public async Task Al_desactivar_la_cuenta_su_token_vigente_deja_de_servir()
    {
        // Otro administrador: el token se obtiene antes de desactivarlo y todavía no vence.
        var (otherAdmin, email) = await NewInternalAsync("Admin");
        var token = await TokenAsync(email, ApiFactory.TestPassword);
        var other = api.CreateClient();
        other.DefaultRequestHeaders.Authorization = new("Bearer", token);
        Assert.Equal(HttpStatusCode.OK, (await other.GetAsync("/api/v1/Users")).StatusCode);

        var admin = await api.ClientFor(TestRole.Admin);
        Assert.Equal(HttpStatusCode.OK, (await admin.PostAsync("/api/v1/Users/" + otherAdmin + "/deactivate", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await other.GetAsync("/api/v1/Users")).StatusCode);

        Assert.Equal(HttpStatusCode.OK, (await admin.PostAsync("/api/v1/Users/" + otherAdmin + "/reactivate", null)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await other.GetAsync("/api/v1/Users")).StatusCode);
    }

    [TheoryIfDatabase]
    [InlineData(TestRole.Distributor)]
    [InlineData(TestRole.Engineer)]
    [InlineData(TestRole.Customer)]
    public async Task Solo_el_administrador_gestiona_usuarios(TestRole role)
    {
        var client = await api.ClientFor(role);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/v1/Users")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsync("/api/v1/Users/x/deactivate", null)).StatusCode);
    }
}
