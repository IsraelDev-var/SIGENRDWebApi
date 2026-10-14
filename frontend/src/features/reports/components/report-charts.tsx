"use client";

import { Bar, BarChart, CartesianGrid, XAxis, YAxis } from "recharts";

import { ChartContainer, ChartLegend, ChartLegendContent, ChartTooltip, ChartTooltipContent, type ChartConfig } from "@/components/ui/chart";

/**
 * Gráficos de los reportes (RF-27). Cada gráfico tiene al lado su tabla con los mismos números, que es la versión
 * accesible: el gráfico se marca como imagen con una descripción breve. Sin animación: no aporta y respeta a
 * quien reduce el movimiento.
 */

const MONTHLY_CONFIG = {
  approved: { label: "Aprobadas", color: "var(--status-available)" },
  rejected: { label: "Rechazadas", color: "var(--status-critical)" },
  others: { label: "En curso u otras", color: "var(--chart-2)" },
} satisfies ChartConfig;

export type MonthlyPoint = { month: string; label: string; approved: number; rejected: number; others: number };

export function MonthlyChart({ data, description }: { data: MonthlyPoint[]; description: string }) {
  return (
    <ChartContainer config={MONTHLY_CONFIG} className="aspect-auto h-64 w-full" role="img" aria-label={description}>
      <BarChart data={data} accessibilityLayer maxBarSize={56}>
        <CartesianGrid vertical={false} />
        <XAxis dataKey="label" tickLine={false} axisLine={false} tickMargin={8} />
        <YAxis allowDecimals={false} tickLine={false} axisLine={false} width={32} />
        <ChartTooltip content={<ChartTooltipContent />} />
        <ChartLegend content={<ChartLegendContent />} />
        <Bar dataKey="approved" stackId="a" fill="var(--color-approved)" isAnimationActive={false} />
        <Bar dataKey="rejected" stackId="a" fill="var(--color-rejected)" isAnimationActive={false} />
        <Bar dataKey="others" stackId="a" fill="var(--color-others)" radius={[4, 4, 0, 0]} isAnimationActive={false} />
      </BarChart>
    </ChartContainer>
  );
}

const COUNT_CONFIG = { count: { label: "Solicitudes", color: "var(--chart-1)" } } satisfies ChartConfig;

export function CountChart({ data, description }: { data: { label: string; count: number }[]; description: string }) {
  return (
    <ChartContainer config={COUNT_CONFIG} className="aspect-auto h-56 w-full" role="img" aria-label={description}>
      <BarChart data={data} layout="vertical" accessibilityLayer maxBarSize={40} margin={{ left: 8 }}>
        <CartesianGrid horizontal={false} />
        <XAxis type="number" allowDecimals={false} tickLine={false} axisLine={false} />
        <YAxis type="category" dataKey="label" tickLine={false} axisLine={false} width={110} />
        <ChartTooltip content={<ChartTooltipContent hideLabel={false} />} />
        <Bar dataKey="count" fill="var(--color-count)" radius={4} isAnimationActive={false} />
      </BarChart>
    </ChartContainer>
  );
}
