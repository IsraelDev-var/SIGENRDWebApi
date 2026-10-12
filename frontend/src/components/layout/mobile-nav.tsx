"use client";

import { Menu } from "lucide-react";
import { useState } from "react";

import { Sheet, SheetContent, SheetHeader, SheetTitle, SheetTrigger } from "@/components/ui/sheet";

import { NavLinks, type NavItem } from "./nav-links";

/** Menú en un panel lateral para pantallas pequeñas. */
export function MobileNav({ items, title = "Menú" }: { items: NavItem[]; title?: string }) {
  const [open, setOpen] = useState(false);

  return (
    <Sheet open={open} onOpenChange={setOpen}>
      <SheetTrigger
        aria-label="Abrir menú"
        className="inline-flex size-10 items-center justify-center rounded-md hover:bg-secondary focus-visible:outline-2 focus-visible:outline-ring md:hidden"
      >
        <Menu aria-hidden="true" className="size-5" />
      </SheetTrigger>
      <SheetContent side="left">
        <SheetHeader>
          <SheetTitle>{title}</SheetTitle>
        </SheetHeader>
        {/* Al elegir un destino se cierra el panel. */}
        <nav aria-label={title} className="px-2" onClick={() => setOpen(false)}>
          <NavLinks items={items} orientation="vertical" />
        </nav>
      </SheetContent>
    </Sheet>
  );
}
