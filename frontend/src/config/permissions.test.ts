import { describe, expect, it } from "vitest";

import { can, canEnterZone, homeFor, PERMISSIONS, ROLES, zoneOf, type Action, type Role } from "@/config/permissions";

/**
 * Tabla 3.2 del documento EnergyMap v1.2, copiada fila por fila.
 * Columnas: Público, Cliente, Instaladora, Ingeniero, Distribuidora, Admin.
 */
const MATRIZ_3_2: Record<Action, [boolean, boolean, boolean, boolean, boolean, boolean]> = {
  viewMap: [true, true, true, true, true, true], // Ver mapa y detalle de transformador
  manageOwnSimulations: [false, true, true, false, false, false], // Crear y consultar simulaciones propias
  createRequest: [false, true, true, false, false, false], // Crear solicitud (instaladora: si está validada)
  uploadOwnDocuments: [false, true, true, false, false, false], // Subir o resubir documentos propios
  viewOwnRequests: [false, true, true, false, false, false], // Ver sus propias solicitudes
  viewOthersRequests: [false, false, false, true, true, true], // Ver solicitudes ajenas
  reviewRequests: [false, false, false, true, true, true], // Pasar a revisión y emitir observaciones
  decideRequests: [false, false, false, false, true, true], // Aprobar o rechazar
  validateInstallers: [false, false, false, false, true, true], // Validar empresas instaladoras
  manageCatalogs: [false, false, false, false, true, true], // Mantener transformadores, zonas y catálogos
  viewReports: [false, false, false, false, true, true], // Ver reportes y estadísticas
  manageInternalUsers: [false, false, false, false, false, true], // Gestionar usuarios internos
  viewAudit: [false, false, false, false, false, true], // Consultar auditoría
  modifyApprovedRequest: [false, false, false, false, false, false], // Modificar o borrar una solicitud aprobada
};

const COLUMNAS: (Role | null)[] = [null, "Customer", "InstallerCompany", "Engineer", "Distributor", "Admin"];

describe("matriz de permisos 3.2", () => {
  it("cubre todas las filas del documento, ni más ni menos", () => {
    expect(Object.keys(PERMISSIONS).sort()).toEqual(Object.keys(MATRIZ_3_2).sort());
  });

  for (const [action, fila] of Object.entries(MATRIZ_3_2) as [Action, boolean[]][]) {
    it(`${action}`, () => {
      expect(COLUMNAS.map((role) => can(role, action))).toEqual(fila);
    });
  }
});

describe("zonas", () => {
  it("reconoce las rutas de cada zona sin confundir prefijos", () => {
    expect(zoneOf("/portal")).toBe("portal");
    expect(zoneOf("/portal/solicitudes/3")).toBe("portal");
    expect(zoneOf("/gestion/auditoria")).toBe("gestion");
    expect(zoneOf("/portales")).toBeNull();
    expect(zoneOf("/")).toBeNull();
  });

  it("el portal es de quien solicita y la gestión del personal de Edesur y administración", () => {
    expect(ROLES.filter((r) => canEnterZone(r, "portal"))).toEqual(["Customer", "InstallerCompany"]);
    expect(ROLES.filter((r) => canEnterZone(r, "gestion"))).toEqual(["Engineer", "Distributor", "Admin"]);
  });

  it("cada rol empieza en su zona", () => {
    expect(ROLES.map(homeFor)).toEqual(["/portal", "/portal", "/gestion", "/gestion", "/gestion"]);
  });
});
