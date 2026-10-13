import { HelpCircle } from "lucide-react";

import { Badge } from "@/components/ui/badge";
import { asTransformerStatus, TRANSFORMER_STATUS } from "@/config/status";
import { cn } from "@/lib/utils";

type Props = {
  /** Estado tal como lo devuelve la API ("Critical"). */
  status: string;
  className?: string;
};

/** Estado del transformador con texto e icono: nunca solo color (WCAG 1.4.1). */
export function TransformerStatusBadge({ status, className }: Props) {
  const known = asTransformerStatus(status);

  if (!known) {
    return (
      <Badge variant="outline" className={className}>
        <HelpCircle aria-hidden="true" />
        Sin estado
      </Badge>
    );
  }

  const { label, icon: Icon, badgeClass } = TRANSFORMER_STATUS[known];
  return (
    <Badge className={cn(badgeClass, className)} data-status={known}>
      <Icon aria-hidden="true" />
      {label}
    </Badge>
  );
}
