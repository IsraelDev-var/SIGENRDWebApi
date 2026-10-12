using System.Net;
using System.Net.Http.Json;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace SIGENRD.UnitTests.Integration;

/// <summary>
/// Contrato OpenAPI de la API (B-8). El documento generado por Swagger debe coincidir con docs/api/openapi-v1.json,
/// que es de donde el frontend genera sus tipos. Si cambias la API, regenera el archivo con
/// ENERGYMAP_UPDATE_OPENAPI=1 y súbelo en el mismo commit.
/// </summary>
[Collection(ApiCollection.Name)]
public class OpenApiContractTests(ApiFactory api)
{
    public const string UpdateVariable = "ENERGYMAP_UPDATE_OPENAPI";

    private static readonly JsonSerializerOptions Indented = new()
    {
        WriteIndented = true,
        // Tildes y eñes tal cual: el archivo se revisa en los PR.
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private async Task<JsonNode> GenerateAsync()
    {
        var response = await api.CreateClient().GetAsync("/swagger/v1/swagger.json");
        response.EnsureSuccessStatusCode();
        return JsonNode.Parse(await response.Content.ReadAsStringAsync())!;
    }

    private static string SnapshotPath()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "SIGENRD.sln")))
            dir = dir.Parent;

        if (dir is null) throw new InvalidOperationException("No se encontró la raíz del repositorio (SIGENRD.sln).");
        return Path.Combine(dir.FullName, "docs", "api", "openapi-v1.json");
    }

    // Saltos de línea LF fijos para que el archivo sea idéntico en Windows y en Linux.
    private static string Normalize(JsonNode document) => document.ToJsonString(Indented).Replace("\r\n", "\n") + "\n";

    [FactIfDatabase]
    public async Task Enums_se_documentan_por_nombre()
    {
        var document = await GenerateAsync();

        var status = document["components"]!["schemas"]!["TransformerStatus"]!;
        Assert.Equal("string", status["type"]!.GetValue<string>());
        Assert.Equal(
            ["Available", "Conditioned", "Critical", "Saturated", "Maintenance"],
            status["enum"]!.AsArray().Select(v => v!.GetValue<string>()));
    }

    [FactIfDatabase]
    public async Task El_cuerpo_acepta_enums_por_nombre()
    {
        // Los números se siguen aceptando: ApiFactory.NewUserAsync registra con userType = 3.
        var email = "enum-" + Guid.NewGuid().ToString("N")[..8] + "@test.energymap.local";
        var response = await api.CreateClient().PostAsJsonAsync("/api/v1/Auth/register",
            new { fullName = "Enum por nombre", email, password = ApiFactory.TestPassword, userType = "Customer" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [FactIfDatabase]
    public async Task El_documento_coincide_con_el_archivo_versionado()
    {
        var generated = Normalize(await GenerateAsync());
        var path = SnapshotPath();

        if (Environment.GetEnvironmentVariable(UpdateVariable) == "1")
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            await File.WriteAllTextAsync(path, generated);
            return;
        }

        Assert.True(File.Exists(path), "Falta " + path + ". Ejecuta las pruebas con " + UpdateVariable + "=1 para crearlo.");
        var committed = (await File.ReadAllTextAsync(path)).Replace("\r\n", "\n");
        Assert.True(committed == generated,
            "La API cambió y docs/api/openapi-v1.json está desactualizado. Ejecuta las pruebas con "
            + UpdateVariable + "=1 y sube el archivo regenerado.");
    }
}
