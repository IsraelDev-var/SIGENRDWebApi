using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace SIGENRD.UnitTests.Integration;

/// <summary>B-6 (RF-09): el solicitante edita nombre y contacto; lo que lo identifica no cambia por aquí.</summary>
[Collection(ApiCollection.Name)]
public class ProfileEditingTests(ApiFactory api)
{
    private static async Task<JsonElement> Data(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return doc.RootElement.GetProperty("data").Clone();
    }

    [FactIfDatabase]
    public async Task El_cliente_edita_nombre_y_contacto_pero_no_la_cedula()
    {
        var (client, _, _) = await api.NewUserAsync();
        var cedula = "001" + Random.Shared.Next(10_000_000, 99_999_999);
        await Data(await client.PostAsJsonAsync("/api/v1/Customers/me", new { fullName = "Ana Pérez", nationalIdOrRnc = cedula, isLegalEntity = false }));

        // Aunque se envíen, la cédula y el tipo de persona no cambian.
        var updated = await Data(await client.PutAsJsonAsync("/api/v1/Customers/me",
            new { fullName = "Ana María Pérez", email = "ana@correo.do", phone = "809-555-0199", address = "Calle 2", nationalIdOrRnc = "999", isLegalEntity = true }));
        Assert.Equal("Ana María Pérez", updated.GetProperty("fullName").GetString());
        Assert.Equal("809-555-0199", updated.GetProperty("phone").GetString());
        Assert.Equal(cedula, updated.GetProperty("nationalIdOrRnc").GetString());
        Assert.False(updated.GetProperty("isLegalEntity").GetBoolean());

        var read = await Data(await client.GetAsync("/api/v1/Customers/me"));
        Assert.Equal("ana@correo.do", read.GetProperty("email").GetString());

        // Vaciar un dato opcional lo borra.
        var cleared = await Data(await client.PutAsJsonAsync("/api/v1/Customers/me", new { fullName = "Ana María Pérez", phone = "  " }));
        Assert.Equal(JsonValueKind.Null, cleared.GetProperty("phone").ValueKind);
    }

    [FactIfDatabase]
    public async Task Rechaza_datos_invalidos_y_pide_el_perfil_antes()
    {
        var (client, _, _) = await api.NewUserAsync();
        Assert.Equal(HttpStatusCode.NotFound, (await client.PutAsJsonAsync("/api/v1/Customers/me", new { fullName = "Sin perfil" })).StatusCode);

        await Data(await client.PostAsJsonAsync("/api/v1/Customers/me",
            new { fullName = "Luis", nationalIdOrRnc = "002" + Random.Shared.Next(10_000_000, 99_999_999), isLegalEntity = false }));
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PutAsJsonAsync("/api/v1/Customers/me", new { fullName = "" })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PutAsJsonAsync("/api/v1/Customers/me", new { fullName = "Luis", email = "no-es-correo" })).StatusCode);
    }

    [FactIfDatabase]
    public async Task La_instaladora_edita_sus_datos_pero_no_el_RNC_ni_la_validacion()
    {
        var (client, _, _) = await api.NewUserAsync(installer: true);
        var rnc = "1" + Random.Shared.Next(10_000_000, 99_999_999);
        await Data(await client.PostAsJsonAsync("/api/v1/InstallerCompanies/me", new { tradeName = "Sol SRL", rnc }));

        var updated = await Data(await client.PutAsJsonAsync("/api/v1/InstallerCompanies/me",
            new { tradeName = "Sol del Caribe SRL", contactPhone = "809-555-0123", rnc = "000", validationStatus = "Validated" }));
        Assert.Equal("Sol del Caribe SRL", updated.GetProperty("tradeName").GetString());
        Assert.Equal(rnc, updated.GetProperty("rnc").GetString());
        Assert.Equal("Pending", updated.GetProperty("validationStatus").GetString());
    }

    [TheoryIfDatabase]
    [InlineData(TestRole.Engineer, "/api/v1/Customers/me")]
    [InlineData(TestRole.InstallerCompany, "/api/v1/Customers/me")]
    [InlineData(TestRole.Customer, "/api/v1/InstallerCompanies/me")]
    public async Task Cada_rol_edita_solo_su_tipo_de_perfil(TestRole role, string path)
    {
        var response = await (await api.ClientFor(role)).PutAsJsonAsync(path, new { fullName = "X", tradeName = "X" });
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
