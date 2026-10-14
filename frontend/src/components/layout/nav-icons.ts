import {
  Bell,
  ChartColumn,
  ClipboardList,
  FileText,
  Gauge,
  Map,
  ScrollText,
  Search,
  Sun,
  TriangleAlert,
  User,
  UserCog,
  Users,
  Zap,
  type LucideIcon,
} from "lucide-react";

import type { NavIcon } from "@/config/navigation";

export const NAV_ICONS: Record<NavIcon, LucideIcon> = {
  alert: TriangleAlert,
  bell: Bell,
  chart: ChartColumn,
  "clipboard-list": ClipboardList,
  "file-text": FileText,
  gauge: Gauge,
  map: Map,
  "scroll-text": ScrollText,
  search: Search,
  sun: Sun,
  user: User,
  "user-cog": UserCog,
  users: Users,
  zap: Zap,
};
