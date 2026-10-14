import { Badge } from "@/components/ui/badge";
import { TONE_BADGE_CLASS } from "@/config/status";
import { cn } from "@/lib/utils";

import { viabilityOf } from "../labels";

export function ViabilityBadge({ status, className }: { status: string; className?: string }) {
  const { label, tone } = viabilityOf(status);
  return (
    <Badge className={cn(TONE_BADGE_CLASS[tone], className)} data-viability={status}>
      {label}
    </Badge>
  );
}
