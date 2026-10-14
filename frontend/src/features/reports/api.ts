import "server-only";

import type { components } from "@/lib/api/schema";
import { apiData } from "@/server/api-client";
import type { Session } from "@/server/session";

import { periodQuery, type Period } from "./period";

type Schemas = components["schemas"];
export type RequestStatistics = Schemas["RequestStatisticsDto"];
export type InstallerRanking = Schemas["InstallerRankingItem"];
export type ZoneRisk = Schemas["ZoneRiskDto"];

export function getStatistics(session: Session, period: Period) {
  return apiData<RequestStatistics>(`/Reports/requests/statistics?${periodQuery(period)}`, { token: session.token });
}

export function getInstallerRanking(session: Session, period: Period, top = 10) {
  const query = periodQuery(period);
  query.set("Top", String(top));
  return apiData<InstallerRanking[]>(`/Reports/installers/ranking?${query}`, { token: session.token });
}

export function getCriticalZones(session: Session) {
  return apiData<ZoneRisk[]>("/Transformers/critical-zones", { token: session.token });
}
