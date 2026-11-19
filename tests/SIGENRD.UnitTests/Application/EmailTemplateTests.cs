using SIGENRD.Core.Application.Services;
using SIGENRD.UnitTests.Integration;

namespace SIGENRD.UnitTests.Application;

public class EmailTemplateTests
{
    [Fact]
    public void Arma_un_documento_completo_con_marca_firma_y_pie()
    {
        var html = EmailTemplate.Render("Confirma tu correo", "Ana Pérez", new[] { "Gracias por registrarte." });

        Assert.StartsWith("<!DOCTYPE html>", html.TrimStart());
        Assert.Contains("EnergyMap", html);
        Assert.Contains("Hola, " + System.Net.WebUtility.HtmlEncode("Ana Pérez") + ":", html);
        Assert.Contains("Equipo de EnergyMap", html);
        Assert.Contains("no respondas a este correo", html);
    }

    [Fact]
    public void Codifica_los_textos_para_que_no_se_inyecte_HTML()
    {
        var html = EmailTemplate.Render("<b>t</b>", "<script>x</script>", new[] { "a < b & c" });

        Assert.DoesNotContain("<script>x</script>", html);
        Assert.Contains("&lt;script&gt;", html);
        Assert.Contains("a &lt; b &amp; c", html);
    }

    [Fact]
    public void El_boton_lleva_el_enlace_y_se_puede_recuperar_decodificado()
    {
        const string url = "https://web.example/confirmar-correo?userId=u1&code=abc";

        var html = EmailTemplate.Render("Confirma", null, new[] { "p" }, "Confirmar", url);

        Assert.Contains("Confirmar", html);
        Assert.Equal(url, EmailLinks.First(html));
        Assert.Contains("Hola:", html);
    }

    [Fact]
    public void Sin_texto_de_accion_no_hay_boton()
    {
        var html = EmailTemplate.Render("Aviso", null, new[] { "p" });

        Assert.DoesNotContain("href=", html);
    }
}
