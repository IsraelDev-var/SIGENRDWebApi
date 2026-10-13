// API pública de la feature de simulaciones: formulario, resultado e historial (RF-18 a RF-26, CA-10).
export { SimulationForm } from "./components/simulation-form";
export { ViabilityBadge } from "./components/viability-badge";
export { compareIds, compareSimulations, getSimulation, listMySimulations, MAX_COMPARE, PAGE_SIZE, type Simulation } from "./api";
export { formatDistance, formatKw, formatKwh, formatPayback, formatRoi } from "./format";
export { viabilityOf } from "./labels";
export { emptySimulationForm, locationFrom } from "./schemas";
