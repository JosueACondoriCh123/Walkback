// Written with Claude Code (AI), directed and tested by team MARSXR
//
// Page background: stars, a Milky Way band and two distant galaxies, drawn once
// on a fixed canvas (no images to download, works offline). A seeded random
// generator keeps the same sky on every visit.

const canvas = document.createElement("canvas");
canvas.className = "sky";
canvas.setAttribute("aria-hidden", "true");
document.body.prepend(canvas);
const ctx = canvas.getContext("2d");

function rng(seed) {
  return () => {
    seed = (seed + 0x6d2b79f5) | 0;
    let t = Math.imul(seed ^ (seed >>> 15), 1 | seed);
    t = (t + Math.imul(t ^ (t >>> 7), 61 | t)) ^ t;
    return ((t ^ (t >>> 14)) >>> 0) / 4294967296;
  };
}

function glow(x, y, r, color, alpha) {
  const g = ctx.createRadialGradient(x, y, 0, x, y, r);
  g.addColorStop(0, color.replace("A", alpha));
  g.addColorStop(1, color.replace("A", 0));
  ctx.fillStyle = g;
  ctx.fillRect(x - r, y - r, r * 2, r * 2);
}

function star(x, y, r, a, tint) {
  ctx.globalAlpha = a;
  ctx.fillStyle = tint;
  ctx.beginPath();
  ctx.arc(x, y, r, 0, Math.PI * 2);
  ctx.fill();
}

// A spiral galaxy seen at an angle: soft core + dotted arms.
function galaxy(rand, cx, cy, size, tilt, angle, hue) {
  ctx.save();
  ctx.translate(cx, cy);
  ctx.rotate(angle);
  ctx.scale(1, tilt);
  glow(0, 0, size * 1.1, `hsla(${hue}, 70%, 70%, A)`, 0.10);
  glow(0, 0, size * 0.35, "rgba(255, 236, 214, A)", 0.55);
  for (let arm = 0; arm < 2; arm++) {
    for (let i = 0; i < 260; i++) {
      const t = rand();
      const theta = t * 3.4 * Math.PI + arm * Math.PI;
      const rad = size * (0.12 + t * 0.95);
      const spread = size * 0.09 * (0.4 + t);
      const x = Math.cos(theta) * rad + (rand() - 0.5) * spread;
      const y = Math.sin(theta) * rad + (rand() - 0.5) * spread;
      star(x, y, rand() * 1.1 + 0.3, 0.25 + rand() * 0.5, rand() < 0.3 ? `hsl(${hue}, 80%, 82%)` : "#fff");
    }
  }
  ctx.restore();
  ctx.globalAlpha = 1;
}

function draw() {
  const dpr = Math.min(window.devicePixelRatio || 1, 2);
  // Use the larger screen height so the mobile URL bar does not force redraws.
  const w = window.innerWidth;
  const h = Math.max(window.innerHeight, window.screen?.height || 0);
  canvas.width = Math.round(w * dpr);
  canvas.height = Math.round(h * dpr);
  canvas.style.height = h + "px";
  ctx.setTransform(dpr, 0, 0, dpr, 0, 0);

  const rand = rng(2026);
  ctx.fillStyle = "#06060b";
  ctx.fillRect(0, 0, w, h);

  // Milky Way: a diagonal band of haze, dust and dense small stars.
  const ax = -0.1 * w, ay = 0.95 * h, bx = 1.1 * w, by = 0.05 * h;
  const len = Math.hypot(bx - ax, by - ay);
  const ux = (bx - ax) / len, uy = (by - ay) / len;
  const band = Math.max(w, h) * 0.16;
  for (let i = 0; i < 26; i++) {
    const t = rand();
    const off = (rand() - 0.5) * band * 0.8;
    const x = ax + ux * len * t - uy * off, y = ay + uy * len * t + ux * off;
    glow(x, y, band * (0.5 + rand() * 0.7), rand() < 0.5 ? "rgba(120, 110, 200, A)" : "rgba(200, 120, 150, A)", 0.05);
  }
  const bandStars = Math.round((w * h) / 900);
  for (let i = 0; i < bandStars; i++) {
    const t = rand();
    const off = (rand() + rand() + rand() - 1.5) * band * 0.7;
    const x = ax + ux * len * t - uy * off, y = ay + uy * len * t + ux * off;
    star(x, y, rand() * 0.7 + 0.2, 0.15 + rand() * 0.45, "#fff");
  }

  // Nebula glows, warm near the top to echo Mars.
  glow(w * 0.82, h * 0.18, Math.max(w, h) * 0.35, "rgba(232, 103, 60, A)", 0.10);
  glow(w * 0.12, h * 0.75, Math.max(w, h) * 0.30, "rgba(90, 80, 210, A)", 0.09);

  // Field stars, a few bright ones with a halo.
  const field = Math.round((w * h) / 2600);
  for (let i = 0; i < field; i++) {
    const x = rand() * w, y = rand() * h;
    const r = rand() ** 3 * 1.4 + 0.3;
    const tint = rand() < 0.15 ? "#ffd9b8" : rand() < 0.15 ? "#bcd4ff" : "#ffffff";
    star(x, y, r, 0.3 + rand() * 0.7, tint);
    if (r > 1.35) glow(x, y, r * 6, "rgba(255, 255, 255, A)", 0.12);
  }

  // Two distant galaxies.
  const g = Math.min(w, h);
  galaxy(rand, w * 0.86, h * 0.72, g * 0.11, 0.42, -0.5, 265);
  galaxy(rand, w * 0.18, h * 0.16, g * 0.06, 0.55, 0.7, 20);
  ctx.globalAlpha = 1;
}

let lastW = 0;
function redraw() {
  if (window.innerWidth === lastW && canvas.width) return; // ignore height-only resizes
  lastW = window.innerWidth;
  draw();
}
redraw();
let timer;
window.addEventListener("resize", () => {
  clearTimeout(timer);
  timer = setTimeout(redraw, 150);
});
