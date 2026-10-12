import { Bell, ClipboardList, FileText, Gauge, Map, ScrollText, Sun, User, Users, Zap, type LucideIcon } from "lucide-react";

import type { NavIcon } from "@/config/navigation";

export const NAV_ICONS: Record<NavIcon, LucideIcon> = {
  bell: Bell,
  "clipboard-list": ClipboardList,
  "file-text": FileText,
  gauge: Gauge,
  map: Map,
  "scroll-text": ScrollText,
  sun: Sun,
  user: User,
  users: Users,
  zap: Zap,
};
