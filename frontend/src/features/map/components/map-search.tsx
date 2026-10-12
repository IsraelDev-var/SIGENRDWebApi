"use client";

import { Search } from "lucide-react";
import { useState, type FormEvent } from "react";

import { AvailabilityValue } from "@/components/shared/availability-value";
import { TransformerStatusBadge } from "@/components/shared/transformer-status-badge";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";

import { MapApiError, searchByCode, searchNear, type MapPoint } from "../lib/map-api";
import { parseCoordinates } from "../lib/viewport";

type Props = {
  /** Se eligió un resultado: el mapa se centra en él y abre su detalle. */
  onPick: (point: MapPoint) => void;
};

/**
 * Búsqueda por código o por coordenadas (RF-04). La búsqueda por dirección espera el geocodificador (decisión D-3).
 */
export function MapSearch({ onPick }: Props) {
  const [text, setText] = useState("");
  const [results, setResults] = useState<MapPoint[] | null>(null);
  const [message, setMessage] = useState<string | null>(null);
  const [pending, setPending] = useState(false);

  async function submit(event: FormEvent) {
    event.preventDefault();
    const term = text.trim();
    setResults(null);
    if (term.length < 2) {
      setMessage("Escribe al menos 2 caracteres del código, o unas coordenadas como 18.47, -69.90.");
      return;
    }

    const coordinates = parseCoordinates(term);
    setPending(true);
    setMessage(null);
    try {
      const found = coordinates ? await searchNear(coordinates.latitude, coordinates.longitude) : await searchByCode(term);
      setResults(found);
      if (found.length === 0) {
        setMessage(coordinates ? "No hay transformadores a menos de 2 km de esas coordenadas." : `Ningún transformador tiene un código parecido a "${term}".`);
      } else if (found.length === 1) {
        onPick(found[0]!);
      }
    } catch (error) {
      setMessage(error instanceof MapApiError ? error.message : "No pudimos buscar. Inténtalo de nuevo.");
    } finally {
      setPending(false);
    }
  }

  return (
    <div className="grid gap-2">
      <form role="search" onSubmit={submit} className="flex gap-2">
        <label htmlFor="busqueda-mapa" className="sr-only">
          Buscar transformador por código o coordenadas
        </label>
        <Input
          id="busqueda-mapa"
          value={text}
          onChange={(e) => setText(e.target.value)}
          placeholder="Código o coordenadas (18.47, -69.90)"
          autoComplete="off"
          aria-describedby="busqueda-mapa-ayuda"
        />
        <Button type="submit" size="icon" aria-label="Buscar" disabled={pending}>
          <Search aria-hidden="true" />
        </Button>
      </form>
      <p id="busqueda-mapa-ayuda" className="text-xs text-muted-foreground">
        La búsqueda por dirección estará disponible más adelante.
      </p>
      <div aria-live="polite" className="grid gap-1">
        {message && <p className="text-sm text-muted-foreground">{message}</p>}
        {results && results.length > 1 && (
          <>
            <p className="text-sm text-muted-foreground">{results.length} resultados</p>
            <ul className="grid gap-1">
              {results.map((point) => (
                <li key={point.id}>
                  <button
                    type="button"
                    onClick={() => onPick(point)}
                    className="flex w-full items-center justify-between gap-2 rounded-md px-2 py-1.5 text-left text-sm hover:bg-secondary focus-visible:outline-2 focus-visible:outline-ring"
                  >
                    <span className="font-mono">{point.code}</span>
                    <span className="flex items-center gap-2">
                      <AvailabilityValue pct={point.availabilityPct} status={point.status} className="text-xs" />
                      <TransformerStatusBadge status={point.status ?? ""} />
                    </span>
                  </button>
                </li>
              ))}
            </ul>
          </>
        )}
      </div>
    </div>
  );
}
