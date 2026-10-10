using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Npgsql;
using SIGENRD.Core.Application.DTOs.Email;
using SIGENRD.Core.Application.Interfaces.Services;
using SIGENRD.Infrastructure.Identity.Configurations;
using SIGENRD.Infrastructure.Identity.Seeds;
using SIGENRD.Infrastructure.Persistences.Contexts;

namespace SIGENRD.UnitTests.Integration;

public enum TestRole { Anonymous, Customer, InstallerCompany, Engineer, Distributor, Admin }

public sealed class TheoryIfDatabaseAttribute : TheoryAttribute
{
    public TheoryIfDatabaseAttribute()
    {
        if (string.IsNullOrWhiteSpace(TestDatabase.ServerConnection))
            Skip = "Define " + TestDatabase.EnvironmentVariable + " para ejecutar las pruebas contra PostGIS.";
    }
}

/// <summary>En las pruebas no sale ningún correo: se guardan para poder leer los enlaces de confirmación.</summary>
public class RecordingEmailService : IEmailService
{
    private readonly System.Collections.Concurrent.ConcurrentBag<EmailRequest> _sent = new();

    public IReadOnlyCollection<EmailRequest> Sent => _sent;

    public Task SendAsync(EmailRequest request)
    {
        _sent.Add(request);
        return Task.CompletedTask;
    }
}

public record LoggedUser(string UserId, string Token);

/// <summary>
/// Una sola API y una sola base para todas las pruebas HTTP: la configuración viaja por variables de entorno
/// del proceso, así que dos fábricas a la vez se pisarían entre sí.
/// </summary>
[CollectionDefinition(Name)]
public class ApiCollection : ICollectionFixture<ApiFactory>
{
    public const string Name = "api";
}

/// <summary>
/// Arranca la API real (WebApplicationFactory) contra una base PostgreSQL con PostGIS propia de la ejecución.
/// La configuración se pasa por variables de entorno del proceso de pruebas, que ganan sobre los User Secrets:
/// así las pruebas nunca tocan la base de desarrollo.
/// </summary>
public class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    // Valor solo para pruebas: la base se crea y se borra en cada ejecución.
    public const string TestPassword = "Prueba-Roles-2026!";

    private string _connectionString = string.Empty;
    private readonly Dictionary<TestRole, LoggedUser> _sessions = new();

    public RecordingEmailService Emails { get; } = new();

    public string ConnectionString => _connectionString;

    public static string Email(TestRole role) => role switch
    {
        TestRole.Customer => "customer@" + DevelopmentTestUsers.Domain,
        TestRole.InstallerCompany => "installer@" + DevelopmentTestUsers.Domain,
        TestRole.Engineer => "engineer@" + DevelopmentTestUsers.Domain,
        TestRole.Distributor => "distributor@" + DevelopmentTestUsers.Domain,
        TestRole.Admin => "admin@sigenrd.com",
        _ => throw new ArgumentOutOfRangeException(nameof(role))
    };

    public AppContextSIGENRD NewAppContext()
    {
        var options = new DbContextOptionsBuilder<AppContextSIGENRD>()
            .UseNpgsql(_connectionString, npgsql => npgsql.UseNetTopologySuite())
            .Options;
        return new AppContextSIGENRD(options);
    }

    public async Task InitializeAsync()
    {
        var server = TestDatabase.ServerConnection;
        if (string.IsNullOrWhiteSpace(server)) return;

        _connectionString = new NpgsqlConnectionStringBuilder(server)
        {
            Database = "it_api_" + Guid.NewGuid().ToString("N")[..12]
        }.ConnectionString;

        await using (var app = NewAppContext())
            await app.Database.MigrateAsync();

        var identityOptions = new DbContextOptionsBuilder<IdentityDbContextSIGENRD>().UseNpgsql(_connectionString).Options;
        await using (var identity = new IdentityDbContextSIGENRD(identityOptions))
            await identity.Database.MigrateAsync();

        Environment.SetEnvironmentVariable("ConnectionStrings__Postgres", _connectionString);
        Environment.SetEnvironmentVariable("ConnectionStrings__IdentityServerDB", _connectionString);
        Environment.SetEnvironmentVariable("JwtSettings__Key", Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)));
        Environment.SetEnvironmentVariable("Seed__AdminPassword", TestPassword);
        Environment.SetEnvironmentVariable("Seed__TestUsersPassword", TestPassword);
        Environment.SetEnvironmentVariable("UseInMemoryDatabase", "false");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development"); // siembra los usuarios de prueba por rol

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IEmailService>();
            services.AddSingleton<IEmailService>(Emails);
        });
    }

    /// <summary>Cliente HTTP autenticado con el token del rol indicado, o anónimo.</summary>
    public async Task<HttpClient> ClientFor(TestRole role)
    {
        var client = CreateClient();
        if (role == TestRole.Anonymous) return client;

        var session = await LoginAsync(role);
        client.DefaultRequestHeaders.Authorization = new("Bearer", session.Token);
        return client;
    }

    public async Task<LoggedUser> LoginAsync(TestRole role)
    {
        if (_sessions.TryGetValue(role, out var cached)) return cached;

        var response = await CreateClient().PostAsJsonAsync("/api/v1/Auth/login",
            new { email = Email(role), password = TestPassword });
        response.EnsureSuccessStatusCode();

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var session = new LoggedUser(
            doc.RootElement.GetProperty("userId").GetString()!,
            doc.RootElement.GetProperty("token").GetString()!);

        _sessions[role] = session;
        return session;
    }

    /// <summary>
    /// Un usuario nuevo y sin perfil: se registra por la API, confirma el correo con el enlace enviado y inicia sesión.
    /// Sirve para probar el alta de perfiles, que es única por usuario.
    /// </summary>
    public async Task<(HttpClient Client, string UserId, string Email)> NewUserAsync(bool installer = false)
    {
        var client = CreateClient();
        var email = (installer ? "inst-" : "cli-") + Guid.NewGuid().ToString("N")[..8] + "@test.energymap.local";

        var register = await client.PostAsJsonAsync("/api/v1/Auth/register",
            new { fullName = "Usuario nuevo", email, password = TestPassword, userType = installer ? 3 : 1 });
        register.EnsureSuccessStatusCode();

        var mail = Emails.Sent.Single(e => e.To == email);
        var href = System.Text.RegularExpressions.Regex.Match(mail.Body, "href='([^']+)'").Groups[1].Value;
        (await client.GetAsync(new Uri(href).PathAndQuery)).EnsureSuccessStatusCode();

        var login = await client.PostAsJsonAsync("/api/v1/Auth/login", new { email, password = TestPassword });
        login.EnsureSuccessStatusCode();

        using var doc = JsonDocument.Parse(await login.Content.ReadAsStringAsync());
        var token = doc.RootElement.GetProperty("token").GetString()!;
        var userId = doc.RootElement.GetProperty("userId").GetString()!;

        var authenticated = CreateClient();
        authenticated.DefaultRequestHeaders.Authorization = new("Bearer", token);
        return (authenticated, userId, email);
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await base.DisposeAsync();

        if (string.IsNullOrEmpty(_connectionString)) return;

        await using var app = NewAppContext();
        await app.Database.EnsureDeletedAsync();
        NpgsqlConnection.ClearAllPools();
    }
}
