import { fireEvent, render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";

import { AvailabilityValue } from "./availability-value";
import { CapacityMeter } from "./capacity-meter";
import { FormField } from "./form-field";
import { KvaValue } from "./kva-value";
import { LastUpdated } from "./last-updated";
import { RequestStatusBadge } from "./request-status-badge";
import { RouteError } from "./route-error";
import { TransformerStatusBadge } from "./transformer-status-badge";

describe("TransformerStatusBadge", () => {
  it("muestra el estado en texto con un icono decorativo", () => {
    const { container } = render(<TransformerStatusBadge status="Saturated" />);

    expect(screen.getByText("Saturada")).toBeInTheDocument();
    expect(container.querySelector("svg")).toHaveAttribute("aria-hidden", "true");
  });

  it("no inventa un estado si la API envía uno desconocido", () => {
    render(<TransformerStatusBadge status="Overloaded" />);
    expect(screen.getByText("Sin estado")).toBeInTheDocument();
  });
});

describe("RequestStatusBadge", () => {
  it("traduce el estado de la API", () => {
    render(<RequestStatusBadge status="InCorrection" />);
    expect(screen.getByText("En corrección")).toBeInTheDocument();
  });
});

describe("CapacityMeter", () => {
  it("muestra el porcentaje de la API sin recalcularlo", () => {
    render(<CapacityMeter availabilityPct={52.3} status="Conditioned" />);

    const meter = screen.getByRole("meter", { name: "Capacidad disponible" });
    expect(meter).toHaveAttribute("aria-valuenow", "52.3");
    expect(meter).toHaveAttribute("aria-valuetext", "52.3 %");
  });

  it("con disponibilidad negativa vacía la barra pero conserva el valor", () => {
    render(<CapacityMeter availabilityPct={-4} status="Saturated" />);

    const meter = screen.getByRole("meter");
    expect(meter).toHaveAttribute("aria-valuenow", "0");
    expect(meter).toHaveAttribute("aria-valuetext", "-4.0 %");
  });

  it("en Mantenimiento no muestra barra porque no se calcula (RN-02)", () => {
    render(<CapacityMeter availabilityPct={80} status="Maintenance" />);

    expect(screen.queryByRole("meter")).not.toBeInTheDocument();
    expect(screen.getByText(/no se calcula durante el mantenimiento/)).toBeInTheDocument();
  });

  it("sin dato lo dice en lugar de mostrar 0 %", () => {
    render(<CapacityMeter availabilityPct={null} status="Available" />);
    expect(screen.getByText(/sin dato/)).toBeInTheDocument();
  });
});

describe("KvaValue", () => {
  it("dice 'Sin dato' en lugar de 0", () => {
    render(<KvaValue value={undefined} />);
    expect(screen.getByText("Sin dato")).toBeInTheDocument();
  });
});

describe("LastUpdated", () => {
  const now = new Date("2026-10-12T15:00:00Z");

  it("muestra fecha y antigüedad", () => {
    render(<LastUpdated date="2026-10-07T15:00:00Z" now={now} />);
    expect(screen.getByText(/hace 5 días/)).toBeInTheDocument();
    expect(screen.queryByText(/Dato con más de/)).not.toBeInTheDocument();
  });

  it("avisa cuando el dato supera el umbral", () => {
    render(<LastUpdated date="2026-08-01T15:00:00Z" now={now} staleAfterDays={30} />);
    expect(screen.getByText(/Dato con más de 30 días/)).toBeInTheDocument();
  });

  it("sin fecha lo dice", () => {
    render(<LastUpdated date={null} now={now} />);
    expect(screen.getByText("Sin fecha de actualización")).toBeInTheDocument();
  });
});

describe("AvailabilityValue", () => {
  it("en Mantenimiento no muestra el número guardado (RN-02)", () => {
    render(<AvailabilityValue pct={-9.8} status="Maintenance" />);
    expect(screen.getByText("No se calcula")).toBeInTheDocument();
    expect(screen.queryByText(/-9.8/)).not.toBeInTheDocument();
  });

  it("en los demás estados muestra el porcentaje de la API", () => {
    render(<AvailabilityValue pct={-4} status="Saturated" />);
    expect(screen.getByText("-4.0 %")).toBeInTheDocument();
  });
});

describe("RouteError", () => {
  it("muestra el código de referencia, no el mensaje del error, y reintenta", () => {
    const retry = vi.fn();
    vi.spyOn(console, "error").mockImplementation(() => {});
    const error = Object.assign(new Error("detalle interno de la API"), { digest: "1498288179" });

    render(<RouteError error={error} retry={retry} homeHref="/portal" homeLabel="Ir al inicio del portal" />);

    expect(screen.getByRole("alert")).toHaveTextContent("No pudimos cargar esta página");
    expect(screen.getByText("Código de referencia: 1498288179")).toBeInTheDocument();
    expect(screen.queryByText(/detalle interno/)).not.toBeInTheDocument();
    expect(screen.getByRole("link", { name: "Ir al inicio del portal" })).toHaveAttribute("href", "/portal");

    fireEvent.click(screen.getByRole("button", { name: "Reintentar" }));
    expect(retry).toHaveBeenCalledOnce();
  });
});

describe("FormField de contraseña", () => {
  it("muestra y oculta la contraseña sin enviar el formulario", () => {
    const onSubmit = vi.fn((e: { preventDefault: () => void }) => e.preventDefault());
    render(
      <form onSubmit={onSubmit}>
        <FormField name="password" label="Contraseña" type="password" defaultValue="Secreta-2026" />
      </form>,
    );
    const input = screen.getByLabelText("Contraseña");
    expect(input).toHaveAttribute("type", "password");

    fireEvent.click(screen.getByRole("button", { name: "Mostrar contraseña" }));
    expect(input).toHaveAttribute("type", "text");
    expect(screen.getByRole("button", { name: "Ocultar contraseña" })).toHaveAttribute("aria-pressed", "true");

    fireEvent.click(screen.getByRole("button", { name: "Ocultar contraseña" }));
    expect(input).toHaveAttribute("type", "password");
    expect(onSubmit).not.toHaveBeenCalled();
  });

  it("los demás campos no llevan el botón", () => {
    render(<FormField name="email" label="Correo" type="email" />);
    expect(screen.getByLabelText("Correo")).toHaveAttribute("type", "email");
    expect(screen.queryByRole("button")).not.toBeInTheDocument();
  });
});
