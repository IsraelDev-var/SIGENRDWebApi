import { PublicShell } from "@/components/layout/shells";
import { PUBLIC_NAV } from "@/config/navigation";
import { PublicSessionLink } from "@/features/auth";

export default function PublicLayout({ children }: { children: React.ReactNode }) {
  return (
    <PublicShell nav={PUBLIC_NAV} actions={<PublicSessionLink />}>
      {children}
    </PublicShell>
  );
}
