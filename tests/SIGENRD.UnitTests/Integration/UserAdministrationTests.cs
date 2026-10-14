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

    /// <summary>Un usuario interno nuevo, dado de alta por el administrador y con el correo confirmado.</summary>
    private async Task<(string Id, string Email)> NewInternalAsync(string role)
    {
        var admin = await api.ClientFor(TestRole.Admin);
        var email = "int-" + Guid.NewGuid().ToString("N")[..8] + "@test.energymap.local";
        var created = await admin.PostAsJsonAsync("/api/v1/Auth/register-internal",
            new { fullName = "Interno " + email[..12], email, password = ApiFactory.TestPassword, userType = role });
        Assert.Equal(HttpStatusCode.OK, created.StatusCode);

        var mail = api.Emails.Sent.Single(e => e.To == email);
        var href = Regex.Match(mail.Body, "href='([^']+)'").Groups[1].Value;
        (await api.CreateClient().GetAsync(new Uri(href).PathAndQuery)).EnsureSuccessStatusCode();

        var userId = JsonDocument.Parse(await created.Content.ReadAsStringAsync()).RootElement.GetProperty("userId").GetString()!;
        return (userId, email);
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
        var login = await Login(email);
        var token = JsonDocument.Parse(await login.Content.ReadAsStringAsync()).RootElement.GetProperty("token").GetString()!;
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
