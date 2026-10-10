using SIGENRD.Core.Domain.Entities;
using SIGENRD.Core.Domain.Enums;
using SIGENRD.Core.Domain.Rules;

namespace SIGENRD.UnitTests.Domain;

/// <summary>
/// Documentos mínimos según el trámite (RN-09, RN-11). Las expectativas salen de los requisitos publicados
/// por Edenorte, EdeEste y Edesur y del reglamento de interconexión de la CNE (2012).
/// </summary>
public class RequiredDocumentsTests
{
    private static ConnectionRequest Request(
        RequestType type = RequestType.Connection, decimal kva = 5,
        UsageType usage = UsageType.Residential, PropertyRelation relation = PropertyRelation.Owner) => new()
    {
        RequestType = type,
        RequestedCapacityKva = kva,
        UsageType = usage,
        PropertyRelation = relation
    };

    private static string[] Codes(ConnectionRequest request, bool legal = false) =>
        RequiredDocuments.For(request, legal).Select(r => r.Code).ToArray();

    // ---------------- conexión de hasta 10 kVA ----------------

    [Fact]
    public void Persona_fisica_hasta_10_kVA_pide_identidad_e_inmueble_y_no_pide_planos()
    {
        var codes = Codes(Request(kva: 5));

        Assert.Equal(new[] { "IDENTIDAD", "INMUEBLE" }, codes);
    }

    [Fact]
    public void El_limite_de_10_kVA_sigue_siendo_contrato()
    {
        Assert.Equal(new[] { "IDENTIDAD", "INMUEBLE" }, Codes(Request(kva: 10)));
    }

    [Fact]
    public void El_inmueble_se_acredita_con_el_titulo_o_con_la_autorizacion_del_dueno()
    {
        var request = Request(kva: 5);

        Assert.Empty(RequiredDocuments.Missing(request, false, new[] { DocumentType.IdentityDocument, DocumentType.ProofOfOwnership }));
        Assert.Empty(RequiredDocuments.Missing(request, false, new[] { DocumentType.IdentityDocument, DocumentType.OwnerAuthorization }));

        var missing = RequiredDocuments.Missing(request, false, new[] { DocumentType.IdentityDocument });
        Assert.Equal("INMUEBLE", Assert.Single(missing).Code);
    }

    [Fact]
    public void Persona_juridica_agrega_los_documentos_de_la_empresa()
    {
        var codes = Codes(Request(kva: 5), legal: true);

        Assert.Equal(
            new[] { "IDENTIDAD", "INMUEBLE", "CARTA", "RNC", "REGISTRO_MERCANTIL", "CONSTITUTIVOS", "ACTA" },
            codes);
    }

    [Fact]
    public void La_tarifa_industrial_exige_el_registro_industrial_solo_a_personas_juridicas()
    {
        var industrial = Request(kva: 5, usage: UsageType.Industrial);

        Assert.Contains("REGISTRO_INDUSTRIAL", Codes(industrial, legal: true));
        Assert.DoesNotContain("REGISTRO_INDUSTRIAL", Codes(industrial, legal: false));
        Assert.DoesNotContain("REGISTRO_INDUSTRIAL", Codes(Request(kva: 5, usage: UsageType.Commercial), legal: true));
    }

    // ---------------- conexión de más de 10 kVA ----------------

    [Fact]
    public void Mas_de_10_kVA_exige_proyecto_de_ingenieria()
    {
        var codes = Codes(Request(kva: 25));

        Assert.Equal(
            new[] { "CARTA_INGENIERO", "IDENTIDAD", "CODIA", "IDENTIDAD_INGENIERO", "MEMORIA", "FACTIBILIDAD", "PLANOS", "PLANOS_MOPC" },
            codes);
    }

    [Fact]
    public void Los_planos_del_MOPC_se_sustituyen_por_una_carta_si_no_se_poseen()
    {
        var request = Request(kva: 25);
        var everythingButMopc = new[]
        {
            DocumentType.RequestLetter, DocumentType.IdentityDocument, DocumentType.CODIALicense,
            DocumentType.EngineerIdentityDocument, DocumentType.DescriptiveMemory,
            DocumentType.FeasibilityRequestForm, DocumentType.Blueprint
        };

        Assert.Equal("PLANOS_MOPC", Assert.Single(RequiredDocuments.Missing(request, false, everythingButMopc)).Code);
        Assert.Empty(RequiredDocuments.Missing(request, false, everythingButMopc.Append(DocumentType.ApprovedPlans)));
        Assert.Empty(RequiredDocuments.Missing(request, false, everythingButMopc.Append(DocumentType.NoApprovedPlansLetter)));
    }

    // ---------------- medición neta ----------------

    [Fact]
    public void Medicion_neta_pide_lo_del_articulo_14_del_reglamento_de_interconexion()
    {
        var codes = Codes(Request(RequestType.NetMetering, kva: 8));

        Assert.Equal(new[] { "IDENTIDAD", "PLANO_SITUACION", "UNIFILAR", "EQUIPOS" }, codes);
    }

    [Fact]
    public void Medicion_neta_ya_no_exige_memoria_de_calculo_ni_esquema_de_interconexion()
    {
        var types = RequiredDocuments.For(Request(RequestType.NetMetering), false).SelectMany(r => r.AnyOf).ToList();

        Assert.DoesNotContain(DocumentType.CalculationMemory, types);
        Assert.DoesNotContain(DocumentType.InterconnectionScheme, types);
    }

    [Theory]
    [InlineData(PropertyRelation.Owner, false)]
    [InlineData(PropertyRelation.CoOwner, true)]
    [InlineData(PropertyRelation.Tenant, true)]
    [InlineData(PropertyRelation.Other, true)]
    public void Si_el_solicitante_no_es_el_dueno_hace_falta_la_autorizacion(PropertyRelation relation, bool expected)
    {
        var codes = Codes(Request(RequestType.NetMetering, relation: relation));

        Assert.Equal(expected, codes.Contains("DUENO"));
    }

    [Fact]
    public void La_potencia_no_cambia_los_requisitos_de_medicion_neta()
    {
        Assert.Equal(
            Codes(Request(RequestType.NetMetering, kva: 5)),
            Codes(Request(RequestType.NetMetering, kva: 150)));
    }

    // ---------------- faltantes ----------------

    [Fact]
    public void Faltantes_son_los_requisitos_sin_ningun_documento_presente()
    {
        var request = Request(RequestType.NetMetering);

        var missing = RequiredDocuments.Missing(
            request, false, new[] { DocumentType.IdentityDocument, DocumentType.Other, DocumentType.Blueprint });

        Assert.Equal(new[] { "PLANO_SITUACION", "EQUIPOS" }, missing.Select(m => m.Code).ToArray());
    }

    [Fact]
    public void Con_todo_presente_no_falta_nada()
    {
        var request = Request(RequestType.NetMetering, relation: PropertyRelation.Tenant);
        var all = RequiredDocuments.For(request, false).Select(r => r.AnyOf[0]);

        Assert.Empty(RequiredDocuments.Missing(request, false, all));
    }

    [Fact]
    public void Cada_requisito_tiene_descripcion_para_mostrarla_al_usuario()
    {
        foreach (var legal in new[] { false, true })
            foreach (var request in new[] { Request(kva: 5), Request(kva: 30), Request(RequestType.NetMetering, relation: PropertyRelation.Tenant) })
                Assert.All(RequiredDocuments.For(request, legal), r =>
                {
                    Assert.False(string.IsNullOrWhiteSpace(r.Description));
                    Assert.NotEmpty(r.AnyOf);
                });
    }
}
