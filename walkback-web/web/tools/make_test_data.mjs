// Written with Claude Code (AI), directed and tested by team MARSXR
//
// TODO(test data): invents a fake 1 km² site with the SAME shape as session B's
// contract (site.json, heightmap 513x513 16-bit LE, stops.json) and fake session C
// outputs (route.json, return_map.bin 257x257 float32 pairs). It is only here so the
// page can be built and published before the real Gale data arrives.
// Once real data exists, run build_data.mjs instead; it overwrites everything.
//
// Usage: node web/tools/make_test_data.mjs

import { mkdirSync, writeFileSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";

const OUT = join(dirname(fileURLToPath(import.meta.url)), "..", "data");
mkdirSync(OUT, { recursive: true });

const SIDE = 1000;
const HM = 513;
const GRID = 257;
const STEP = SIDE / (GRID - 1); // 3.90625 m

const gauss = (x, y, cx, cy, amp, sig) =>
  amp * Math.exp(-((x - cx) ** 2 + (y - cy) ** 2) / (2 * sig * sig));

function ridge(x, y, ax, ay, bx, by, amp, w) {
  const dx = bx - ax, dy = by - ay;
  const t = Math.max(0, Math.min(1, ((x - ax) * dx + (y - ay) * dy) / (dx * dx + dy * dy)));
  const d = Math.hypot(x - (ax + t * dx), y - (ay + t * dy));
  return amp * Math.exp(-(d * d) / (2 * w * w));
}

// Invented terrain, elevation in metres (x east, y north, from SW corner).
function height(x, y) {
  let h = -4480 + 0.012 * y;
  h += 3 * Math.sin(x / 90) * Math.cos(y / 120) + 1.5 * Math.sin(x / 37 + y / 53);
  h += gauss(x, y, 750, 260, 45, 45);
  h += gauss(x, y, 230, 700, 30, 35);
  h += ridge(x, y, 820, 620, 980, 980, 35, 25);
  const r = Math.hypot(x - 560, y - 160);
  h += -15 * Math.exp(-(r * r) / (2 * 30 * 30)) + 6 * Math.exp(-((r - 55) ** 2) / (2 * 10 * 10));
  return h;
}

// --- heightmap.r16 equivalent (written as heightmap.bin, same bytes) ---
const raw = new Float64Array(HM * HM);
let min = Infinity, max = -Infinity;
for (let r = 0; r < HM; r++) {
  for (let c = 0; c < HM; c++) {
    const x = (c / (HM - 1)) * SIDE;
    const y = SIDE - (r / (HM - 1)) * SIDE; // row 0 = north
    const h = height(x, y);
    raw[r * HM + c] = h;
    if (h < min) min = h;
    if (h > max) max = h;
  }
}
min = Math.floor(min);
max = Math.ceil(max);
const hm = Buffer.alloc(HM * HM * 2);
for (let i = 0; i < raw.length; i++) {
  hm.writeUInt16LE(Math.round(((raw[i] - min) / (max - min)) * 65535), i * 2);
}
writeFileSync(join(OUT, "heightmap.bin"), hm);

const habitat = { x: 250, y: 250 };
const site = {
  name: "TODO test terrain (not Gale)",
  centerLat: -4.59,
  centerLon: 137.44,
  sideMeters: SIDE,
  heightmapSize: HM,
  gridSize: GRID,
  minElevation: min,
  maxElevation: max,
  habitat,
  crs: "TODO",
  sources: [
    { layer: "Elevation", mission: "TODO", instrument: "TODO", resolution: "TODO", id: "TODO", credit: "Invented test terrain. Real credits arrive with session B data." },
    { layer: "Orthoimage", mission: "TODO", instrument: "TODO", resolution: "TODO", id: "TODO", credit: "TODO" },
  ],
};
writeFileSync(join(OUT, "site.json"), JSON.stringify(site, null, 2));

const stops = [
  { id: "A", name: "Layered outcrop", x: 430, y: 420, science: "TODO: fake stop" },
  { id: "B", name: "Dark sand ripples", x: 690, y: 560, science: "TODO: fake stop" },
  { id: "C", name: "Vein-filled bedrock", x: 520, y: 800, science: "TODO: fake stop" },
];
writeFileSync(join(OUT, "stops.json"), JSON.stringify(stops, null, 2));

// --- fake walking model (Tobler hiking function, slowed for a suit) ---
const node = new Float64Array(GRID * GRID);
for (let r = 0; r < GRID; r++)
  for (let c = 0; c < GRID; c++) node[r * GRID + c] = height(c * STEP, SIDE - r * STEP);

const slopeDeg = new Float64Array(GRID * GRID);
for (let r = 0; r < GRID; r++) {
  for (let c = 0; c < GRID; c++) {
    const hx = (node[r * GRID + Math.min(c + 1, GRID - 1)] - node[r * GRID + Math.max(c - 1, 0)]) / (2 * STEP);
    const hy = (node[Math.max(r - 1, 0) * GRID + c] - node[Math.min(r + 1, GRID - 1) * GRID + c]) / (2 * STEP);
    slopeDeg[r * GRID + c] = (Math.atan(Math.hypot(hx, hy)) * 180) / Math.PI;
  }
}

const SUIT = 0.55;
const speedMpm = (s) => ((6 * Math.exp(-3.5 * Math.abs(s + 0.05)) * 1000) / 60) * SUIT;
const LPM = 1.5; // fake litres of O2 per minute
const CAPACITY = 140; // fake tank, litres

function legMinutes(ax, ay, bx, by) {
  const ha = height(ax, ay), hb = height(bx, by);
  const d = Math.hypot(bx - ax, by - ay);
  return d === 0 ? 0 : d / speedMpm((hb - ha) / d);
}

// Dijkstra from the habitat over reversed edges => minutes to walk back home.
const N = GRID * GRID;
const dist = new Float64Array(N).fill(Infinity);
const heap = [];
const push = (d, i) => {
  heap.push([d, i]);
  let k = heap.length - 1;
  while (k > 0) {
    const p = (k - 1) >> 1;
    if (heap[p][0] <= heap[k][0]) break;
    [heap[p], heap[k]] = [heap[k], heap[p]];
    k = p;
  }
};
const pop = () => {
  const top = heap[0], last = heap.pop();
  if (heap.length) {
    heap[0] = last;
    let k = 0;
    for (;;) {
      const l = 2 * k + 1, r = l + 1;
      let m = k;
      if (l < heap.length && heap[l][0] < heap[m][0]) m = l;
      if (r < heap.length && heap[r][0] < heap[m][0]) m = r;
      if (m === k) break;
      [heap[m], heap[k]] = [heap[k], heap[m]];
      k = m;
    }
  }
  return top;
};
const hr = Math.round((SIDE - habitat.y) / STEP), hc = Math.round(habitat.x / STEP);
dist[hr * GRID + hc] = 0;
push(0, hr * GRID + hc);
const nbrs = [[-1, -1], [-1, 0], [-1, 1], [0, -1], [0, 1], [1, -1], [1, 0], [1, 1]];
while (heap.length) {
  const [d, u] = pop();
  if (d > dist[u]) continue;
  const ur = (u / GRID) | 0, uc = u % GRID;
  for (const [dr, dc] of nbrs) {
    const nr = ur + dr, nc = uc + dc;
    if (nr < 0 || nc < 0 || nr >= GRID || nc >= GRID) continue;
    const v = nr * GRID + nc;
    if (slopeDeg[v] > 25) continue; // fake: too steep to walk
    const h = STEP * Math.hypot(dr, dc);
    const cost = h / speedMpm((node[u] - node[v]) / h); // walking v -> u (toward home)
    if (d + cost < dist[v]) {
      dist[v] = d + cost;
      push(dist[v], v);
    }
  }
}
const ret = Buffer.alloc(N * 8);
for (let i = 0; i < N; i++) {
  const m = dist[i];
  ret.writeFloatLE(Number.isFinite(m) ? m : NaN, i * 8);
  ret.writeFloatLE(Number.isFinite(m) ? m * LPM : NaN, i * 8 + 4);
}
writeFileSync(join(OUT, "return_map.bin"), ret);

// --- fake route: habitat -> A -> B -> C -> habitat, straight legs ---
const legs = [habitat, ...stops, habitat];
const points = [];
let minutes = 0;
points.push({ x: habitat.x, y: habitat.y, minutes: 0, o2Percent: 100 });
for (let i = 1; i < legs.length; i++) {
  const a = legs[i - 1], b = legs[i];
  const n = Math.max(1, Math.round(Math.hypot(b.x - a.x, b.y - a.y) / 25));
  for (let k = 1; k <= n; k++) {
    const t0 = (k - 1) / n, t1 = k / n;
    const x0 = a.x + (b.x - a.x) * t0, y0 = a.y + (b.y - a.y) * t0;
    const x1 = a.x + (b.x - a.x) * t1, y1 = a.y + (b.y - a.y) * t1;
    minutes += legMinutes(x0, y0, x1, y1);
    const p = { x: +x1.toFixed(1), y: +y1.toFixed(1), minutes: +minutes.toFixed(1), o2Percent: +(100 - (minutes * LPM * 100) / CAPACITY).toFixed(1) };
    if (k === n && b.id) p.stopId = b.id;
    points.push(p);
  }
}
const last = points[points.length - 1];
const route = {
  note: "TODO: fake route. Replace with session C model-tests/out/route.json",
  verdict: last.o2Percent > 0 ? "VIABLE" : "NO_RETURN",
  o2CapacityLiters: CAPACITY,
  totalMinutes: last.minutes,
  pointOfNoReturn: null,
  points,
};
writeFileSync(join(OUT, "route.json"), JSON.stringify(route, null, 1));

writeFileSync(
  join(OUT, "status.json"),
  JSON.stringify({ test: true, note: "Invented test terrain (TODO). Real Gale data replaces it.", builtAt: new Date().toISOString() }, null, 2),
);

const steep = [...slopeDeg].filter((s) => s > 20).length;
console.log(`test data -> ${OUT}`);
console.log(`elevation ${min}..${max} m, ${steep} nodes over 20°, route ${last.minutes.toFixed(0)} min, O2 left ${last.o2Percent}%`);
