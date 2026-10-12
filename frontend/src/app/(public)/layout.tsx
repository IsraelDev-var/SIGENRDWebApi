import { PublicShell } from "@/components/layout/shells";
import { PUBLIC_NAV } from "@/config/navigation";

export default function PublicLayout({ children }: LayoutProps<"/">) {
  return <PublicShell nav={PUBLIC_NAV}>{children}</PublicShell>;
}
