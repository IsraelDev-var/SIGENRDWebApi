import { act, render } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";

import { VIEWPORT_DEBOUNCE_MS, ViewportWatcher } from "./viewport-watcher";

type Handler = () => void;

const handlers = new Map<string, Set<Handler>>();
const fakeMap = {
  getBounds: () => ({ getSouth: () => 18.4, getWest: () => -70.1, getNorth: () => 18.5, getEast: () => -70 }),
  getZoom: () => 12,
  on: (event: string, handler: Handler) => {
    if (!handlers.has(event)) handlers.set(event, new Set());
    handlers.get(event)!.add(handler);
  },
  off: (event: string, handler: Handler) => {
    handlers.get(event)?.delete(handler);
  },
};

vi.mock("@/components/ui/map", () => ({ useMap: () => ({ map: fakeMap, isLoaded: true }) }));

function moveEnd() {
  act(() => handlers.get("moveend")?.forEach((h) => h()));
}

describe("ViewportWatcher", () => {
  beforeEach(() => {
    vi.useFakeTimers();
    handlers.clear();
  });
  afterEach(() => {
    vi.useRealTimers();
  });

  it("avisa del área visible al montar, sin esperar", () => {
    const onChange = vi.fn();
    render(<ViewportWatcher onChange={onChange} />);
    expect(onChange).toHaveBeenCalledTimes(1);
    expect(onChange).toHaveBeenCalledWith({
      bounds: { minLatitude: 18.4, minLongitude: -70.1, maxLatitude: 18.5, maxLongitude: -70 },
      zoom: 12,
    });
  });

  it("junta varios moveend seguidos en un solo aviso tras la espera", () => {
    const onChange = vi.fn();
    render(<ViewportWatcher onChange={onChange} />);
    onChange.mockClear();

    moveEnd();
    act(() => vi.advanceTimersByTime(VIEWPORT_DEBOUNCE_MS - 50));
    moveEnd();
    act(() => vi.advanceTimersByTime(VIEWPORT_DEBOUNCE_MS - 50));
    moveEnd();
    expect(onChange).not.toHaveBeenCalled();

    act(() => vi.advanceTimersByTime(VIEWPORT_DEBOUNCE_MS));
    expect(onChange).toHaveBeenCalledTimes(1);
  });

  it("no avisa después de desmontar", () => {
    const onChange = vi.fn();
    const { unmount } = render(<ViewportWatcher onChange={onChange} />);
    onChange.mockClear();

    moveEnd();
    unmount();
    act(() => vi.advanceTimersByTime(VIEWPORT_DEBOUNCE_MS * 2));
    expect(onChange).not.toHaveBeenCalled();
    expect(handlers.get("moveend")?.size ?? 0).toBe(0);
  });
});
