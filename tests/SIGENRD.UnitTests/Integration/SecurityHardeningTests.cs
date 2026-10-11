using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using SIGENRD.Core.Application.Wrappers;
using SIGENRDWebApi.Options;

namespace SIGENRD.UnitTests.Integration;

/// <summary>
/// Regresión de los hallazgos P0 de la auditoría del 2026-10-10:
/// SEC-02 bloqueo de cuenta, SEC-03 CORS, SEC-04 límite de peticiones, SEC-05 cabeceras, API-01 listado paginado.
/// </summary>
[Collection(ApiCollection.Name)]
public class SecurityHardeningTests(ApiFactory api)
{
    // ---------------- SEC-02: bloqueo de cuenta ----------------

    [FactIfDatabase]
    public async Task Tras_cinco_fallos_la_cuenta_se_bloquea_aun_con_la_contraseña_correcta()
    {
        var (_, _, email) = await api.NewUserAsync();
        var client = api.CreateClient();

        for (var i = 0; i < 4; i++)
        {
            var failed = await client.PostAsJsonAsync("/api/v1/Auth/login", new { email, password = "Incorrecta-123" });
            Assert.Equal(HttpStatusCode.BadRequest, failed.StatusCode);
        }

        // El quinto fallo es el que bloquea.
        var fifth = await client.PostAsJsonAsync("/api/v1/Auth/login", new { email, password = "Incorrecta-123" });
        Assert.Equal(HttpStatusCode.TooManyRequests, fifth.StatusCode);

        var correct = await client.PostAsJsonAsync("/api/v1/Auth/login", new { email, password = ApiFactory.TestPassword });
        Assert.Equal(HttpStatusCode.TooManyRequests, correct.StatusCode);
        Assert.DoesNotContain("token", await correct.Content.ReadAsStringAsync());
    }

    [FactIfDatabase]
    public async Task Un_acierto_reinicia_el_contador_de_fallos()
    {
        var (_, _, email) = await api.NewUserAsync();
        var client = api.CreateClient();

        for (var i = 0; i < 4; i++)
            await client.PostAsJsonAsync("/api/v1/Auth/login", new { email, password = "Incorrecta-123" });
        Assert.Equal(HttpStatusCode.OK,
            (await client.PostAsJsonAsync("/api/v1/Auth/login", new { email, password = ApiFactory.TestPassword })).StatusCode);

        for (var i = 0; i < 4; i++)
            await client.PostAsJsonAsync("/api/v1/Auth/login", new { email, password = "Incorrecta-123" });
        Assert.Equal(HttpStatusCode.OK,
            (await client.PostAsJsonAsync("/api/v1/Auth/login", new { email, password = ApiFactory.TestPassword })).StatusCode);
    }

    // ---------------- SEC-03: CORS ----------------

    [FactIfDatabase]
    public async Task Un_origen_no_listado_no_recibe_cabeceras_CORS()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/health");
        request.Headers.Add("Origin", "https://sitio-ajeno.example");

        var response = await api.CreateClient().SendAsync(request);

        Assert.False(response.Headers.Contains("Access-Control-Allow-Origin"));
    }

    [FactIfDatabase]
    public async Task El_origen_del_frontend_configurado_si_recibe_cabeceras_CORS()
    {
        // appsettings.Development.json: Cors:AllowedOrigins = http://localhost:3000
        var request = new HttpRequestMessage(HttpMethod.Get, "/health");
        request.Headers.Add("Origin", "http://localhost:3000");

        var response = await api.CreateClient().SendAsync(request);

        Assert.Equal("http://localhost:3000", Assert.Single(response.Headers.GetValues("Access-Control-Allow-Origin")));
    }

    // ---------------- SEC-04: límite de peticiones ----------------

    [FactIfDatabase]
    public async Task Superar_el_limite_en_seguimiento_publico_y_en_login_devuelve_429()
    {
        // Instancia propia con límites bajos, para no frenar al resto de las pruebas.
        using var limited = api.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
            services.PostConfigure<RateLimitSettings>(o =>
            {
                o.PublicTrackingPermitLimit = 3;
                o.AuthPermitLimit = 3;
            })));
        var client = limited.CreateClient();

        for (var i = 0; i < 3; i++)
            Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/api/v1/ConnectionRequest/track/REQ-NOEXISTE")).StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, (await client.GetAsync("/api/v1/ConnectionRequest/track/REQ-NOEXISTE")).StatusCode);

        for (var i = 0; i < 3; i++)
            Assert.Equal(HttpStatusCode.BadRequest,
                (await client.PostAsJsonAsync("/api/v1/Auth/login", new { email = "nadie@test.energymap.local", password = "x" })).StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests,
            (await client.PostAsJsonAsync("/api/v1/Auth/login", new { email = "nadie@test.energymap.local", password = "x" })).StatusCode);
    }

    // ---------------- SEC-05: cabeceras de seguridad ----------------

    [TheoryIfDatabase]
    [InlineData("/health")]
    [InlineData("/api/v1/Transformers/999999")] // respuesta de error (404)
    public async Task Toda_respuesta_lleva_las_cabeceras_de_seguridad(string url)
    {
        var response = await api.CreateClient().GetAsync(url);

        Assert.Equal("nosniff", Assert.Single(response.Headers.GetValues("X-Content-Type-Options")));
        Assert.Equal("DENY", Assert.Single(response.Headers.GetValues("X-Frame-Options")));
        Assert.Equal("no-referrer", Assert.Single(response.Headers.GetValues("Referrer-Policy")));
        Assert.Contains("default-src 'none'", Assert.Single(response.Headers.GetValues("Content-Security-Policy")));
    }

    // ---------------- API-01: listado paginado ----------------

    [FactIfDatabase]
    public async Task El_listado_de_solicitudes_viene_paginado_por_defecto()
    {
        var response = await (await api.ClientFor(TestRole.Admin)).GetAsync("/api/v1/ConnectionRequest");
        response.EnsureSuccessStatusCode();

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var data = doc.RootElement.GetProperty("data");
        Assert.Equal(PagedDto<object>.DefaultPageSize, data.GetProperty("pageSize").GetInt32());
        Assert.True(data.GetProperty("items").GetArrayLength() <= PagedDto<object>.DefaultPageSize);
    }

    [FactIfDatabase]
    public async Task El_listado_rechaza_paginas_mayores_que_el_maximo()
    {
        var response = await (await api.ClientFor(TestRole.Admin))
            .GetAsync("/api/v1/ConnectionRequest?pageSize=" + (PagedDto<object>.MaxPageSize + 1));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
