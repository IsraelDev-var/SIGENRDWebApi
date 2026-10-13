import { Badge } from "@/components/ui/badge";
import { asRequestStatus, REQUEST_STATUS, TONE_BADGE_CLASS } from "@/config/status";
import { cn } from "@/lib/utils";

type Props = {
  /** Estado tal como lo devuelve la API ("UnderReview"). */
  status: string;
  className?: string;
};

export function RequestStatusBadge({ status, className }: Props) {
  const known = asRequestStatus(status);
  const { label, tone } = known ? REQUEST_STATUS[known] : { label: "Sin estado", tone: "neutral" as const };

  return (
    <Badge className={cn(TONE_BADGE_CLASS[tone], className)} data-status={known ?? "unknown"}>
      {label}
    </Badge>
  );
}
