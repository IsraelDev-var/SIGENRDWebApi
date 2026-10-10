using SIGENRD.Core.Domain.Entities;
using SIGENRD.Core.Domain.Enums;
using SIGENRD.Core.Domain.Exceptions;
using SIGENRD.Core.Domain.Rules;

namespace SIGENRD.UnitTests.Domain;

/// <summary>RN-12, RN-23, RN-03 y RN-15: transiciones y reserva de capacidad, sin base de datos.</summary>
public class RequestWorkflowTests
{
    private const decimal A = 70, C = 30;

    private static Transformer NewTransformer(decimal total = 1000, decimal available = 800, decimal reserved = 0) => new()
    {
        TotalCapacityKva = total,
        AvailableCapacityKva = available,
        ReservedCapacityKva = reserved
    };

    private static ConnectionRequest NewRequest(RequestStatus status = RequestStatus.Received, decimal requested = 100, decimal reservedKva = 0) => new()
    {
        Status = status,
        RequestedCapacityKva = requested,
        ReservedKva = reservedKva
    };

    // ---------------- tabla de transiciones ----------------

    private static readonly RequestStatus[] All = Enum.GetValues<RequestStatus>();

    private static readonly HashSet<(RequestStatus, RequestStatus)> Valid = new()
    {
        (RequestStatus.Received, RequestStatus.UnderReview),
        (RequestStatus.Received, RequestStatus.Cancelled),
        (RequestStatus.UnderReview, RequestStatus.Observed),
        (RequestStatus.UnderReview, RequestStatus.Approved),
        (RequestStatus.UnderReview, RequestStatus.Rejected),
        (RequestStatus.Observed, RequestStatus.InCorrection),
        (RequestStatus.Observed, RequestStatus.Cancelled),
        (RequestStatus.InCorrection, RequestStatus.UnderReview),
        (RequestStatus.InCorrection, RequestStatus.Cancelled),
    };

    [Fact]
    public void Solo_las_nueve_transiciones_de_RN_12_y_RN_23_son_validas()
    {
        foreach (var from in All)
            foreach (var to in All)
                Assert.Equal(Valid.Contains((from, to)), RequestWorkflow.CanTransition(from, to));
    }

    [Theory]
    [InlineData(RequestStatus.Approved)]
    [InlineData(RequestStatus.Rejected)]
    [InlineData(RequestStatus.Cancelled)]
    public void Los_estados_finales_no_tienen_salida(RequestStatus status)
    {
        Assert.True(RequestWorkflow.IsFinal(status));
        Assert.Empty(RequestWorkflow.NextStates(status));
    }

    [Fact]
    public void Una_solicitud_aprobada_no_vuelve_a_ningun_estado_previo()
    {
        foreach (var to in All)
            Assert.False(RequestWorkflow.CanTransition(RequestStatus.Approved, to));
    }

    // ---------------- reserva de capacidad ----------------

    [Fact]
    public void Entrar_en_revision_reserva_la_capacidad_solicitada()
    {
        var transformer = NewTransformer(total: 1000, available: 800);
        var request = NewRequest(requested: 300);

        request.StartReview(transformer, A, C);

        Assert.Equal(RequestStatus.UnderReview, request.Status);
        Assert.Equal(300, request.ReservedKva);
        Assert.Equal(300, transformer.ReservedCapacityKva);
        Assert.Equal(500, transformer.EffectiveAvailableKva);
        Assert.Equal(50m, transformer.AvailabilityPct);
        Assert.Equal(TransformerStatus.Conditioned, transformer.Status);
    }

    [Fact]
    public void Sin_capacidad_suficiente_la_solicitud_no_entra_en_revision_ni_cambia_nada()
    {
        var transformer = NewTransformer(total: 1000, available: 800, reserved: 600);
        var request = NewRequest(requested: 300);

        var ex = Assert.Throws<InsufficientCapacityException>(() => request.StartReview(transformer, A, C));

        Assert.Equal(200, ex.AvailableKva);
        Assert.Equal(RequestStatus.Received, request.Status);
        Assert.Equal(0, request.ReservedKva);
        Assert.Equal(600, transformer.ReservedCapacityKva);
    }

    [Fact]
    public void Un_transformador_en_mantenimiento_no_acepta_reservas()
    {
        var transformer = NewTransformer();
        transformer.EnterMaintenance();

        Assert.Throws<InsufficientCapacityException>(() => NewRequest().StartReview(transformer, A, C));
    }

    [Fact]
    public void Dos_solicitudes_no_pueden_reservar_mas_de_lo_disponible()
    {
        var transformer = NewTransformer(total: 1000, available: 500);
        var first = NewRequest(requested: 300);
        var second = NewRequest(requested: 300);

        first.StartReview(transformer, A, C);

        Assert.Throws<InsufficientCapacityException>(() => second.StartReview(transformer, A, C));
        Assert.Equal(300, transformer.ReservedCapacityKva);
    }

    [Fact]
    public void Rechazar_libera_la_reserva()
    {
        var transformer = NewTransformer(total: 1000, available: 800, reserved: 300);
        var request = NewRequest(RequestStatus.UnderReview, requested: 300, reservedKva: 300);

        request.Reject(transformer, A, C);

        Assert.Equal(RequestStatus.Rejected, request.Status);
        Assert.Equal(0, request.ReservedKva);
        Assert.Equal(0, transformer.ReservedCapacityKva);
        Assert.Equal(800, transformer.AvailableCapacityKva);
    }

    [Theory]
    [InlineData(RequestStatus.Observed)]
    [InlineData(RequestStatus.InCorrection)]
    public void Cancelar_con_reserva_la_libera(RequestStatus status)
    {
        var transformer = NewTransformer(total: 1000, available: 800, reserved: 300);
        var request = NewRequest(status, requested: 300, reservedKva: 300);

        request.Cancel(transformer, A, C);

        Assert.Equal(RequestStatus.Cancelled, request.Status);
        Assert.Equal(0, transformer.ReservedCapacityKva);
    }

    [Fact]
    public void Cancelar_una_solicitud_recibida_no_toca_el_transformador()
    {
        var transformer = NewTransformer(total: 1000, available: 800, reserved: 150);
        var request = NewRequest(RequestStatus.Received, requested: 300);

        request.Cancel(transformer, A, C);

        Assert.Equal(150, transformer.ReservedCapacityKva);
    }

    [Fact]
    public void Aprobar_descuenta_la_reserva_de_la_capacidad_disponible()
    {
        var transformer = NewTransformer(total: 1000, available: 800, reserved: 300);
        var request = NewRequest(RequestStatus.UnderReview, requested: 300, reservedKva: 300);

        request.Approve(transformer, A, C);

        Assert.Equal(RequestStatus.Approved, request.Status);
        Assert.Equal(0, request.ReservedKva);
        Assert.Equal(0, transformer.ReservedCapacityKva);
        Assert.Equal(500, transformer.AvailableCapacityKva);
        Assert.Equal(50m, transformer.AvailabilityPct);
    }

    [Fact]
    public void Aprobar_con_la_capacidad_caida_falla_y_no_cambia_nada()
    {
        // Edesur bajó la capacidad disponible a 200 mientras la solicitud de 300 estaba en revisión.
        var transformer = NewTransformer(total: 1000, available: 200, reserved: 300);
        var request = NewRequest(RequestStatus.UnderReview, requested: 300, reservedKva: 300);

        Assert.Throws<InsufficientCapacityException>(() => request.Approve(transformer, A, C));

        Assert.Equal(RequestStatus.UnderReview, request.Status);
        Assert.Equal(300, request.ReservedKva);
        Assert.Equal(200, transformer.AvailableCapacityKva);
    }

    // ---------------- transiciones inválidas ----------------

    [Theory]
    [InlineData(RequestStatus.Received)]
    [InlineData(RequestStatus.Observed)]
    [InlineData(RequestStatus.InCorrection)]
    [InlineData(RequestStatus.Approved)]
    [InlineData(RequestStatus.Rejected)]
    [InlineData(RequestStatus.Cancelled)]
    public void Solo_se_aprueba_lo_que_esta_en_revision(RequestStatus status)
    {
        var request = NewRequest(status);

        Assert.Throws<InvalidStateTransitionException>(() => request.Approve(NewTransformer(), A, C));
        Assert.Equal(status, request.Status);
    }

    [Theory]
    [InlineData(RequestStatus.UnderReview)]
    [InlineData(RequestStatus.Approved)]
    [InlineData(RequestStatus.Rejected)]
    [InlineData(RequestStatus.Cancelled)]
    public void No_se_cancela_lo_que_esta_en_revision_ni_lo_que_ya_termino(RequestStatus status)
    {
        Assert.Throws<InvalidStateTransitionException>(() => NewRequest(status).Cancel(NewTransformer(), A, C));
    }

    [Theory]
    [InlineData(RequestStatus.Received)]
    [InlineData(RequestStatus.UnderReview)]
    [InlineData(RequestStatus.Observed)]
    [InlineData(RequestStatus.Approved)]
    public void Solo_se_reenvia_desde_correccion(RequestStatus status)
    {
        var request = NewRequest(status);

        Assert.Throws<InvalidStateTransitionException>(() => request.Resubmit());
        Assert.Equal(status, request.Status);
    }

    [Theory]
    [InlineData(RequestStatus.UnderReview)]
    [InlineData(RequestStatus.Observed)]
    [InlineData(RequestStatus.InCorrection)]
    public void Solo_se_inicia_la_revision_desde_recibida_y_no_reserva_dos_veces(RequestStatus status)
    {
        var transformer = NewTransformer(total: 1000, available: 800, reserved: 100);
        var request = NewRequest(status, requested: 100, reservedKva: 100);

        Assert.Throws<InvalidStateTransitionException>(() => request.StartReview(transformer, A, C));
        Assert.Equal(100, transformer.ReservedCapacityKva);
    }

    [Fact]
    public void Una_solicitud_aprobada_no_se_modifica()
    {
        var request = NewRequest(RequestStatus.Approved);
        var transformer = NewTransformer();

        Assert.Throws<InvalidStateTransitionException>(() => request.Observe());
        Assert.Throws<InvalidStateTransitionException>(() => request.StartCorrection());
        Assert.Throws<InvalidStateTransitionException>(() => request.Resubmit());
        Assert.Throws<InvalidStateTransitionException>(() => request.Reject(transformer, A, C));
        Assert.Throws<InvalidStateTransitionException>(() => request.Cancel(transformer, A, C));
        Assert.Throws<InvalidStateTransitionException>(() => request.StartReview(transformer, A, C));
    }

    [Fact]
    public void El_ciclo_de_correccion_conserva_la_reserva()
    {
        var transformer = NewTransformer(total: 1000, available: 800);
        var request = NewRequest(requested: 200);

        request.StartReview(transformer, A, C);
        request.Observe();
        request.StartCorrection();
        request.Resubmit();

        Assert.Equal(RequestStatus.UnderReview, request.Status);
        Assert.Equal(200, request.ReservedKva);
        Assert.Equal(200, transformer.ReservedCapacityKva);
    }
}
