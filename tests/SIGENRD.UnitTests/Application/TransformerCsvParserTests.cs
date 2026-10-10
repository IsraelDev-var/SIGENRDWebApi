using System.Text;
using SIGENRD.Core.Application.Features.Transformers.Commands.ImportTransformers;

namespace SIGENRD.UnitTests.Application;

public class TransformerCsvParserTests
{
    private const string Header = "code,serviceZoneId,totalCapacityKva,availableCapacityKva,latitude,longitude";

    private static Stream Csv(params string[] lines) =>
        new MemoryStream(Encoding.UTF8.GetBytes(string.Join("\n", lines)));

    [Fact]
    public async Task Archivo_valido_se_lee_completo()
    {
        var result = await TransformerCsvParser.ParseAsync(Csv(
            Header,
            "TR-001,1,500,350.5,18.4861,-69.9312",
            "TR-002,2,250,0,18.50,-69.90"));

        Assert.Empty(result.Errors);
        Assert.Equal(2, result.Rows.Count);
        Assert.Equal(350.5m, result.Rows[0].AvailableCapacityKva);
        Assert.Equal(-69.9312, result.Rows[0].Longitude);
        Assert.Equal(3, result.Rows[1].Line);
    }

    [Fact]
    public async Task El_orden_de_columnas_y_las_mayusculas_no_importan_y_se_ignora_el_BOM()
    {
        var bytes = Encoding.UTF8.GetPreamble()
            .Concat(Encoding.UTF8.GetBytes("LONGITUDE,Latitude,code,availableCapacityKva,totalCapacityKva,serviceZoneId\n-69.9,18.5,TR-9,10,100,4"))
            .ToArray();

        var result = await TransformerCsvParser.ParseAsync(new MemoryStream(bytes));

        Assert.Empty(result.Errors);
        var row = Assert.Single(result.Rows);
        Assert.Equal("TR-9", row.Code);
        Assert.Equal(4, row.ServiceZoneId);
    }

    [Fact]
    public async Task Campos_entre_comillas_con_comas()
    {
        var result = await TransformerCsvParser.ParseAsync(Csv(Header, "\"TR,001\",1,500,100,18.4,-69.9"));

        Assert.Empty(result.Errors);
        Assert.Equal("TR,001", Assert.Single(result.Rows).Code);
    }

    [Fact]
    public async Task Falta_una_columna_obligatoria()
    {
        var result = await TransformerCsvParser.ParseAsync(Csv("code,serviceZoneId,latitude,longitude", "TR-1,1,18.4,-69.9"));

        Assert.Empty(result.Rows);
        var error = Assert.Single(result.Errors);
        Assert.Contains("totalCapacityKva", error);
        Assert.Contains("availableCapacityKva", error);
    }

    [Fact]
    public async Task Archivo_vacio()
    {
        var result = await TransformerCsvParser.ParseAsync(Csv(""));

        Assert.Contains("vacío", Assert.Single(result.Errors));
    }

    [Theory]
    [InlineData(",1,500,100,18.4,-69.9", "código es obligatorio")]
    [InlineData("TR-1,0,500,100,18.4,-69.9", "serviceZoneId")]
    [InlineData("TR-1,1,0,0,18.4,-69.9", "totalCapacityKva")]
    [InlineData("TR-1,1,500,-1,18.4,-69.9", "availableCapacityKva")]
    [InlineData("TR-1,1,500,501,18.4,-69.9", "no puede superar")]
    [InlineData("TR-1,1,500,100,95,-69.9", "latitude")]
    [InlineData("TR-1,1,500,100,18.4,-181", "longitude")]
    [InlineData("TR-1,1,5,0,100,18.4,-69.9", "latitude")]
    [InlineData("TR-1,uno,500,100,18.4,-69.9", "serviceZoneId")]
    public async Task Filas_invalidas_se_reportan_con_su_linea(string row, string expectedFragment)
    {
        var result = await TransformerCsvParser.ParseAsync(Csv(Header, row));

        Assert.Empty(result.Rows);
        Assert.Contains(result.Errors, e => e.StartsWith("Línea 2:") && e.Contains(expectedFragment));
    }

    [Fact]
    public async Task Codigo_repetido_dentro_del_archivo()
    {
        var result = await TransformerCsvParser.ParseAsync(Csv(
            Header,
            "TR-1,1,500,100,18.4,-69.9",
            "tr-1,1,500,100,18.5,-69.8"));

        Assert.Contains(result.Errors, e => e.StartsWith("Línea 3:") && e.Contains("repetido"));
    }

    [Fact]
    public async Task El_punto_es_el_separador_decimal_sin_importar_la_cultura()
    {
        var previous = Thread.CurrentThread.CurrentCulture;
        Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("es-DO");
        try
        {
            var result = await TransformerCsvParser.ParseAsync(Csv(Header, "TR-1,1,500.75,100.25,18.4861,-69.9312"));

            Assert.Empty(result.Errors);
            Assert.Equal(500.75m, Assert.Single(result.Rows).TotalCapacityKva);
        }
        finally
        {
            Thread.CurrentThread.CurrentCulture = previous;
        }
    }

    [Fact]
    public async Task La_lectura_se_detiene_tras_100_errores()
    {
        var lines = new List<string> { Header };
        for (var i = 0; i < 150; i++) lines.Add("TR-" + i + ",0,500,100,18.4,-69.9");

        var result = await TransformerCsvParser.ParseAsync(Csv(lines.ToArray()));

        Assert.Equal(TransformerCsvParser.MaxErrors + 1, result.Errors.Count);
    }
}
