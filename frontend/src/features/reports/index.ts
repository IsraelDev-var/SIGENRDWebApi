// API pública de la feature de reportes: estadísticas, ranking y zonas críticas (RF-27 a RF-30).
export { getCriticalZones, getInstallerRanking, getStatistics } from "./api";
export { periodFrom, periodQuery, type Period } from "./period";
export { CountChart, MonthlyChart } from "./components/report-charts";
export { formatDays, formatMonth, monthlyPoints } from "./format";
