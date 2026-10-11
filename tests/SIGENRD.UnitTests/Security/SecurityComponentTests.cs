using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using SIGENRD.Infrastructure.Identity.Configurations;
using SIGENRD.Infrastructure.Identity.Entities;
using SIGENRD.Infrastructure.Identity.JWT;
using SIGENRD.Infrastructure.Identity.Validators;
using SIGENRD.Infrastructure.Persistences.Contexts;
using SIGENRDWebApi.HealthChecks;

namespace SIGENRD.UnitTests.Security;

/// <summary>Piezas de seguridad que no necesitan base de datos: llaves del JWT, contraseñas comunes y health check.</summary>
public class SecurityComponentTests
{
    // ---------------- llaves del JWT ----------------

    [Fact]
    public void Carga_la_llave_en_Base64_de_PKCS8_y_en_PEM_con_el_mismo_kid()
    {
        using var rsa = RSA.Create(2048);

        var fromBase64 = JwtSigningKeys.FromPrivateKey(Convert.ToBase64String(rsa.ExportPkcs8PrivateKey()));
        var fromPem = JwtSigningKeys.FromPrivateKey(rsa.ExportPkcs8PrivateKeyPem());

        Assert.Equal(SecurityAlgorithms.RsaSha256, fromBase64.SigningCredentials.Algorithm);
        Assert.Equal(fromBase64.ValidationKey.KeyId, fromPem.ValidationKey.KeyId);
    }

    [Fact]
    public void La_llave_de_validacion_no_contiene_la_parte_privada()
    {
        using var rsa = RSA.Create(2048);
        var keys = JwtSigningKeys.FromPrivateKey(Convert.ToBase64String(rsa.ExportPkcs8PrivateKey()));

        Assert.Null(keys.ValidationKey.Parameters.D);
        Assert.Equal(PrivateKeyStatus.DoesNotExist, keys.ValidationKey.PrivateKeyStatus);
    }

    [Fact]
    public void Rechaza_una_llave_de_menos_de_2048_bits()
    {
        using var rsa = RSA.Create(1024);

        var error = Assert.Throws<InvalidOperationException>(() =>
            JwtSigningKeys.FromPrivateKey(Convert.ToBase64String(rsa.ExportPkcs8PrivateKey())));
        Assert.Contains("2048", error.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData("__USER_SECRET__")]
    [InlineData("bm8gZXMgdW5hIGxsYXZl")] // Base64 válido que no es una llave
    public void Rechaza_una_llave_ausente_o_invalida(string value)
    {
        Assert.Throws<InvalidOperationException>(() => JwtSigningKeys.FromPrivateKey(value));
    }

    // ---------------- contraseñas comunes ----------------

    private static async Task<bool> Accepts(string password, string email = "persona@correo.do")
    {
        var result = await new CommonPasswordValidator().ValidateAsync(null!, new ApplicationUser { Email = email }, password);
        return result.Succeeded;
    }

    [Theory]
    [InlineData("Password2026!")]
    [InlineData("Contraseña123")]
    [InlineData("QWERTY123456")]
    [InlineData("EnergyMap2026")]
    [InlineData("1234567890")]
    public async Task Rechaza_palabras_comunes_con_numeros_o_simbolos(string password)
    {
        Assert.False(await Accepts(password));
    }

    [Fact]
    public async Task Rechaza_la_contraseña_que_contiene_el_correo()
    {
        Assert.False(await Accepts("Persona2026x", "persona@correo.do"));
    }

    [Theory]
    [InlineData("Prueba-Roles-2026!")]
    [InlineData("Mango-Verde-Tarde-41")]
    public async Task Acepta_contraseñas_no_triviales(string password)
    {
        Assert.True(await Accepts(password));
    }

    // ---------------- health check ----------------

    private static AppContextSIGENRD ContextFor(string connectionString) => new(
        new DbContextOptionsBuilder<AppContextSIGENRD>()
            .UseNpgsql(connectionString, npgsql => npgsql.UseNetTopologySuite()).Options);

    [Fact]
    public async Task Sin_base_de_datos_el_health_check_es_Unhealthy()
    {
        // Puerto 1: nada escucha ahí, la conexión se rechaza al instante.
        await using var app = ContextFor("Host=127.0.0.1;Port=1;Database=x;Username=x;Password=x;Timeout=2");
        await using var identity = new IdentityDbContextSIGENRD(new DbContextOptionsBuilder<IdentityDbContextSIGENRD>()
            .UseNpgsql("Host=127.0.0.1;Port=1;Database=x;Username=x;Password=x;Timeout=2").Options);

        var appResult = await new DatabaseHealthCheck<AppContextSIGENRD>(app).CheckHealthAsync(new HealthCheckContext());
        var identityResult = await new DatabaseHealthCheck<IdentityDbContextSIGENRD>(identity).CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Unhealthy, appResult.Status);
        Assert.Equal(HealthStatus.Unhealthy, identityResult.Status);
    }

    [Fact]
    public async Task Una_base_que_acepta_la_conexion_y_no_responde_es_Unhealthy_dentro_del_limite()
    {
        // Servidor que acepta TCP y nunca contesta: Npgsql esperaría sus 15 s por defecto.
        using var listener = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, 0);
        listener.Start();
        var port = ((System.Net.IPEndPoint)listener.LocalEndpoint).Port;
        _ = listener.AcceptTcpClientAsync();

        await using var app = ContextFor($"Host=127.0.0.1;Port={port};Database=x;Username=x;Password=x;Timeout=15");
        var watch = System.Diagnostics.Stopwatch.StartNew();

        var result = await new DatabaseHealthCheck<AppContextSIGENRD>(app, TimeSpan.FromSeconds(1))
            .CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
        Assert.True(watch.Elapsed < TimeSpan.FromSeconds(5), $"tardó {watch.Elapsed.TotalSeconds:0.0} s");
    }
}
