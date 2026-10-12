using SIGENRD.Infrastructure.Identity.Services;

namespace SIGENRD.UnitTests.Security;

/// <summary>Enlace del correo de confirmación: a la página del frontend si está configurada (F2).</summary>
public class EmailConfirmationLinkTests
{
    [Fact]
    public void Con_la_url_del_frontend_apunta_a_su_pagina()
    {
        var link = AuthService.BuildEmailConfirmationLink(
            "https://energymap.example/confirmar-correo/", "https://api.example", "user-1", "Q09ERQ");

        Assert.Equal("https://energymap.example/confirmar-correo?userId=user-1&code=Q09ERQ", link);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void Sin_configurar_conserva_el_enlace_a_la_api(string? frontendUrl)
    {
        var link = AuthService.BuildEmailConfirmationLink(frontendUrl, "https://api.example/", "user-1", "Q09ERQ");

        Assert.Equal("https://api.example/api/v1/Auth/confirm-email?userId=user-1&code=Q09ERQ", link);
    }

    [Fact]
    public void Codifica_el_id_del_usuario()
    {
        var link = AuthService.BuildEmailConfirmationLink("https://f.example/c", "https://api.example", "a b&c", "X");

        Assert.Contains("userId=a%20b%26c&", link);
    }
}
