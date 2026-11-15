// Written with Claude Code (AI), directed and tested by team MARSXR
//
// Walkback web viewer. Reads web/data/ (copied from session B and C outputs by
// tools/build_data.mjs) and draws the 1 km² site. It never computes routes or
// return times itself: those numbers come from session C's model, the same one
// the headset uses. Only the >20° slope hatching is derived here, from heights.

import * as THREE from "three";
import { OrbitControls } from "three/addons/OrbitControls.js";
import { CSS2DRenderer, CSS2DObject } from "three/addons/CSS2DRenderer.js";

const STEEP_DEG = 20;
const VEX = 2; // vertical exaggeration, display only (slopes use real heights)
const LIFT = 2.5; // metres above ground for drawn lines

const $ = (id) => document.getElementById(id);
const fmtMin = (m) => (m >= 60 ? `${Math.floor(m / 60)} h ${String(Math.round(m % 60)).padStart(2, "0")}` : `${Math.round(m)}`);
const fmtMinUnit = (m) => (m >= 60 ? "" : " min");
const num = (v) => (v === null || v === undefined || v === "" ? NaN : Number(v));

async function fetchOk(url) {
  const r = await fetch(url, { cache: "no-cache" });
  if (!r.ok) throw new Error(`${url}: HTTP ${r.status}`);
  return r;
}
const getJSON = (u) => fetchOk(u).then((r) => r.json());
const getBin = (u) => fetchOk(u).then((r) => r.arrayBuffer());
const maybe = (p) => p.catch(() => null);
function loadImage(url) {
  return new Promise((res) => {
    const img = new Image();
    img.onload = () => res(img);
    img.onerror = () => res(null);
    img.src = url;
  });
}

function el(tag, cls, text) {
  const e = document.createElement(tag);
  if (cls) e.className = cls;
  if (text !== undefined) e.textContent = text;
  return e;
}

// ---------- route.json normalisation (format owned by session C) ----------
function normalizeRoute(r) {
  if (!r) return null;
  const raw = r.points || r.route || r.path || r.waypoints || [];
  const points = raw
    .map((p) => ({
      x: num(p.x),
      y: num(p.y),
      minutes: num(p.minutes ?? p.t ?? p.time ?? p.elapsedMinutes),
      o2: num(p.o2Percent ?? p.o2 ?? p.o2Pct ?? p.o2Remaining),
      stopId: p.stopId ?? p.stop ?? null,
    }))
    .filter((p) => Number.isFinite(p.x) && Number.isFinite(p.y));
  let pnr = r.pointOfNoReturn ?? r.pnr ?? r.noReturn ?? null;
  if (pnr) {
    pnr = { x: num(pnr.x), y: num(pnr.y), minutes: num(pnr.minutes ?? pnr.t), o2: num(pnr.o2Percent ?? pnr.o2) };
    if (!Number.isFinite(pnr.x) || !Number.isFinite(pnr.y)) pnr = null;
  }
  const verdict = String(r.verdict ?? r.result ?? "").toUpperCase();
  const viable = !pnr && verdict.includes("VIABLE") && !verdict.includes("NOT");
  const capacity = num(r.o2CapacityLiters ?? r.capacityLiters ?? r.tankLiters);
  return { points, pnr, verdict, viable, capacity: Number.isFinite(capacity) && capacity > 0 ? capacity : null };
}

// ---------------------------------------------------------------------------
async function main() {
  const [site, hmBuf, stops, routeRaw, retBuf, status, ortho] = await Promise.all([
    getJSON("data/site.json"),
    getBin("data/heightmap.bin"),
    maybe(getJSON("data/stops.json")),
    maybe(getJSON("data/route.json")),
    maybe(getBin("data/return_map.bin")),
    maybe(getJSON("data/status.json")),
    loadImage("data/ortho.jpg"),
  ]);

  if (status?.test) $("test-badge").hidden = false;

  const S = site.sideMeters;
  const HMN = site.heightmapSize;
  const G = site.gridSize;
  const STEP = S / (G - 1);
  const minE = site.minElevation, maxE = site.maxElevation;
  if (hmBuf.byteLength !== HMN * HMN * 2) throw new Error("heightmap size does not match site.json");

  // Heights (metres), row 0 = north.
  const dv = new DataView(hmBuf);
  const elev = new Float32Array(HMN * HMN);
  for (let i = 0; i < elev.length; i++) elev[i] = minE + (dv.getUint16(i * 2, true) / 65535) * (maxE - minE);

  function heightAt(x, y) {
    const fc = Math.min(Math.max((x / S) * (HMN - 1), 0), HMN - 1);
    const fr = Math.min(Math.max(((S - y) / S) * (HMN - 1), 0), HMN - 1);
    const c0 = Math.floor(fc), r0 = Math.floor(fr);
    const c1 = Math.min(c0 + 1, HMN - 1), r1 = Math.min(r0 + 1, HMN - 1);
    const tc = fc - c0, tr = fr - r0;
    const a = elev[r0 * HMN + c0], b = elev[r0 * HMN + c1];
    const c = elev[r1 * HMN + c0], d = elev[r1 * HMN + c1];
    return (a * (1 - tc) + b * tc) * (1 - tr) + (c * (1 - tc) + d * tc) * tr;
  }

  // Grid heights and slope (degrees) on the 257 x 257 contract grid.
  const gh = new Float32Array(G * G);
  for (let r = 0; r < G; r++) for (let c = 0; c < G; c++) gh[r * G + c] = heightAt(c * STEP, S - r * STEP);
  const slope = new Float32Array(G * G);
  for (let r = 0; r < G; r++) {
    for (let c = 0; c < G; c++) {
      const cl = Math.max(c - 1, 0), cr = Math.min(c + 1, G - 1);
      const rn = Math.max(r - 1, 0), rs = Math.min(r + 1, G - 1);
      const gx = (gh[r * G + cr] - gh[r * G + cl]) / ((cr - cl) * STEP);
      const gy = (gh[rn * G + c] - gh[rs * G + c]) / ((rs - rn) * STEP);
      slope[r * G + c] = (Math.atan(Math.hypot(gx, gy)) * 180) / Math.PI;
    }
  }
  const slopeAt = (x, y) => {
    const c = Math.round(Math.min(Math.max(x / STEP, 0), G - 1));
    const r = Math.round(Math.min(Math.max((S - y) / STEP, 0), G - 1));
    return slope[r * G + c];
  };

  // Local site metres (x east, y north, from SW corner) -> three.js world.
  const toWorld = (x, y, lift = 0) => new THREE.Vector3(x - S / 2, (heightAt(x, y) - minE) * VEX + lift, S / 2 - y);

  // ---------- header info ----------
  $("legend-vex").textContent = `Heights ×${VEX}`;

  // ---------- renderer / scene ----------
  const viewer = $("viewer");
  const renderer = new THREE.WebGLRenderer({ antialias: true, alpha: true });
  renderer.setClearColor(0x000000, 0);
  renderer.setPixelRatio(Math.min(window.devicePixelRatio, 2));
  viewer.prepend(renderer.domElement);
  const labels = new CSS2DRenderer();
  labels.domElement.className = "labels";
  viewer.appendChild(labels.domElement);

  const scene = new THREE.Scene();
  scene.fog = new THREE.Fog(0x120d0b, 2600, 5200);
  const camera = new THREE.PerspectiveCamera(42, 1, 1, 8000);
  const controls = new OrbitControls(camera, renderer.domElement);
  controls.enableDamping = true;
  controls.dampingFactor = 0.08;
  controls.maxPolarAngle = Math.PI * 0.47;
  controls.minDistance = 120;
  controls.maxDistance = 2600;
  // Let the page scroll over the 3D view: wheel zooms only with Ctrl (trackpad
  // pinch also sends ctrlKey). Touch pinch is unaffected.
  viewer.addEventListener("wheel", (e) => { if (!e.ctrlKey) e.stopPropagation(); }, { capture: true });
  if (window.matchMedia("(pointer: coarse)").matches) {
    $("hint").innerHTML = "Drag to turn · Pinch to zoom · <b>Tap the ground</b>";
  }
  const midY = ((maxE - minE) * VEX) / 2;
  controls.target.set(0, midY * 0.5, 0);

  scene.add(new THREE.HemisphereLight(0xffeadb, 0x3b2318, 1.6));
  const sun = new THREE.DirectionalLight(0xfff0dd, 2.4);
  sun.position.set(-700, 900, 300);
  scene.add(sun);

  // ---------- terrain mesh ----------
  const pos = new Float32Array(G * G * 3);
  const uv = new Float32Array(G * G * 2);
  for (let r = 0; r < G; r++) {
    for (let c = 0; c < G; c++) {
      const i = r * G + c;
      pos[i * 3] = c * STEP - S / 2;
      pos[i * 3 + 1] = (gh[i] - minE) * VEX;
      pos[i * 3 + 2] = r * STEP - S / 2;
      uv[i * 2] = c / (G - 1);
      uv[i * 2 + 1] = 1 - r / (G - 1);
    }
  }
  const idx = new Uint32Array((G - 1) * (G - 1) * 6);
  let k = 0;
  for (let r = 0; r < G - 1; r++) {
    for (let c = 0; c < G - 1; c++) {
      const a = r * G + c, b = a + 1, d = a + G, e = d + 1;
      idx[k++] = a; idx[k++] = d; idx[k++] = b;
      idx[k++] = b; idx[k++] = d; idx[k++] = e;
    }
  }
  const geo = new THREE.BufferGeometry();
  geo.setAttribute("position", new THREE.BufferAttribute(pos, 3));
  geo.setAttribute("uv", new THREE.BufferAttribute(uv, 2));
  geo.setIndex(new THREE.BufferAttribute(idx, 1));
  geo.computeVertexNormals();

  // Texture: orthoimage (or a height tint until it arrives) + hatching over >20°.
  const T = Math.min(window.innerWidth > 700 ? 2048 : 1024, renderer.capabilities.maxTextureSize);
  const tex = document.createElement("canvas");
  tex.width = tex.height = T;
  const tx = tex.getContext("2d");
  if (ortho) {
    tx.drawImage(ortho, 0, 0, T, T);
  } else {
    const tint = document.createElement("canvas");
    tint.width = tint.height = G;
    const ti = tint.getContext("2d").createImageData(G, G);
    const lo = [122, 62, 36], hi = [214, 146, 96];
    for (let i = 0; i < G * G; i++) {
      const t = (gh[i] - minE) / (maxE - minE || 1);
      for (let ch = 0; ch < 3; ch++) ti.data[i * 4 + ch] = lo[ch] + (hi[ch] - lo[ch]) * t;
      ti.data[i * 4 + 3] = 255;
    }
    tint.getContext("2d").putImageData(ti, 0, 0);
    tx.imageSmoothingEnabled = true;
    tx.drawImage(tint, 0, 0, T, T);
  }
  {
    const mask = document.createElement("canvas");
    mask.width = mask.height = G;
    const mi = mask.getContext("2d").createImageData(G, G);
    for (let i = 0; i < G * G; i++) mi.data[i * 4 + 3] = slope[i] > STEEP_DEG ? 255 : 0;
    mask.getContext("2d").putImageData(mi, 0, 0);

    const st = document.createElement("canvas");
    st.width = st.height = T;
    const sx = st.getContext("2d");
    sx.fillStyle = "rgba(40, 14, 0, 0.45)";
    sx.fillRect(0, 0, T, T);
    sx.strokeStyle = "rgba(255, 204, 66, 0.95)";
    sx.lineWidth = T / 260;
    const gap = T / 110;
    sx.beginPath();
    for (let i = -T; i < T * 2; i += gap) { sx.moveTo(i, 0); sx.lineTo(i - T, T); }
    sx.stroke();
    sx.globalCompositeOperation = "destination-in";
    sx.imageSmoothingEnabled = true;
    sx.drawImage(mask, 0, 0, T, T);
    tx.drawImage(st, 0, 0);
  }
  const map = new THREE.CanvasTexture(tex);
  map.colorSpace = THREE.SRGBColorSpace;
  map.anisotropy = renderer.capabilities.getMaxAnisotropy();
  const terrain = new THREE.Mesh(geo, new THREE.MeshLambertMaterial({ map }));
  scene.add(terrain);

  // Dark base under the tile so it reads as a block of ground.
  {
    const base = new THREE.Mesh(
      new THREE.BoxGeometry(S, 24, S),
      new THREE.MeshLambertMaterial({ color: 0x2a1a12 }),
    );
    base.position.y = -12.5;
    scene.add(base);
  }

  function label(text, cls, obj, y) {
    const div = el("div", `lbl ${cls}`, text);
    const l = new CSS2DObject(div);
    l.position.set(0, y, 0);
    obj.add(l);
    return l;
  }

  // ---------- habitat ----------
  const hab = site.habitat;
  {
    const g = new THREE.Group();
    g.position.copy(toWorld(hab.x, hab.y));
    const dome = new THREE.Mesh(
      new THREE.SphereGeometry(16, 32, 16, 0, Math.PI * 2, 0, Math.PI / 2),
      new THREE.MeshLambertMaterial({ color: 0xf4f1ec }),
    );
    g.add(dome);
    const ring = new THREE.Mesh(new THREE.TorusGeometry(22, 2, 8, 48), new THREE.MeshBasicMaterial({ color: 0xffffff }));
    ring.rotation.x = Math.PI / 2;
    ring.position.y = 1;
    g.add(ring);
    label("Habitat", "lbl-habitat", g, 26);
    scene.add(g);
  }

  // ---------- science stops ----------
  const stopById = new Map();
  for (const s of stops || []) {
    const x = num(s.x), y = num(s.y);
    if (!Number.isFinite(x) || !Number.isFinite(y)) continue;
    stopById.set(String(s.id), s);
    const g = new THREE.Group();
    g.position.copy(toWorld(x, y));
    const pole = new THREE.Mesh(new THREE.CylinderGeometry(1.4, 1.4, 34, 8), new THREE.MeshLambertMaterial({ color: 0xe9e2da }));
    pole.position.y = 17;
    g.add(pole);
    const head = new THREE.Mesh(new THREE.SphereGeometry(8, 20, 12), new THREE.MeshLambertMaterial({ color: 0xffb13b, emissive: 0x4a2a00 }));
    head.position.y = 36;
    g.add(head);
    label(s.name ? `${s.id} · ${s.name}` : String(s.id), "lbl-stop", g, 46);
    scene.add(g);
  }

  // ---------- route + verdict (numbers from session C) ----------
  const route = normalizeRoute(routeRaw);
  renderRoutePanel(route, stopById);
  if (route && route.points.length > 1) {
    let split = route.points.length - 1;
    if (route.pnr) {
      let best = Infinity;
      route.points.forEach((p, i) => {
        const d = Math.hypot(p.x - route.pnr.x, p.y - route.pnr.y);
        if (d < best) { best = d; split = i; }
      });
    }
    const drape = (pts) => {
      const out = [];
      for (let i = 1; i < pts.length; i++) {
        const a = pts[i - 1], b = pts[i];
        const n = Math.max(1, Math.ceil(Math.hypot(b.x - a.x, b.y - a.y) / 4));
        for (let j = i === 1 ? 0 : 1; j <= n; j++) {
          const t = j / n;
          out.push(toWorld(a.x + (b.x - a.x) * t, a.y + (b.y - a.y) * t, LIFT));
        }
      }
      return out;
    };
    const tube = (pts, color) => {
      if (pts.length < 2) return;
      const v = drape(pts);
      const curve = new THREE.CatmullRomCurve3(v, false, "centripetal");
      scene.add(new THREE.Mesh(
        new THREE.TubeGeometry(curve, Math.min(v.length * 2, 4000), 2.4, 8, false),
        new THREE.MeshBasicMaterial({ color }),
      ));
    };
    tube(route.points.slice(0, split + 1), 0x47d1ff);
    if (route.pnr) tube(route.points.slice(split), 0xff5a52);
  }
  if (route?.pnr) {
    $("legend-pnr").hidden = false;
    const g = new THREE.Group();
    g.position.copy(toWorld(route.pnr.x, route.pnr.y));
    const m = new THREE.Mesh(new THREE.SphereGeometry(9, 20, 12), new THREE.MeshBasicMaterial({ color: 0xff5a52 }));
    m.position.y = 10;
    g.add(m);
    label("Point of no return", "lbl-pnr", g, 24);
    scene.add(g);
  }

  // ---------- tap: minutes and % O2 to get home (session C return map) ----------
  const ret = retBuf && retBuf.byteLength === G * G * 8 ? new Float32Array(retBuf) : null;
  const capacity = route?.capacity ?? null;
  const tapMarker = new THREE.Group();
  {
    const pin = new THREE.Mesh(new THREE.CylinderGeometry(1, 1, 26, 8), new THREE.MeshBasicMaterial({ color: 0xffffff }));
    pin.position.y = 13;
    const ball = new THREE.Mesh(new THREE.SphereGeometry(5, 16, 10), new THREE.MeshBasicMaterial({ color: 0xffffff }));
    ball.position.y = 28;
    const disc = new THREE.Mesh(new THREE.RingGeometry(6, 9, 32), new THREE.MeshBasicMaterial({ color: 0xffffff, side: THREE.DoubleSide }));
    disc.rotation.x = -Math.PI / 2;
    disc.position.y = 1.5;
    tapMarker.add(pin, ball, disc);
    tapMarker.visible = false;
    scene.add(tapMarker);
  }

  function showTap(x, y) {
    tapMarker.position.copy(toWorld(x, y));
    tapMarker.visible = true;
    const body = $("tap-body");
    body.replaceChildren();
    const sl = slopeAt(x, y);
    const slopeLine = sl > STEEP_DEG ? el("p", "tap-warn", `Too steep here: ${Math.round(sl)}°`) : "";
    if (!ret) {
      body.append(el("p", "tap-sub", "Way-back numbers are coming soon."), slopeLine);
    } else {
      const c = Math.round(Math.min(Math.max(x / STEP, 0), G - 1));
      const r = Math.round(Math.min(Math.max((S - y) / STEP, 0), G - 1));
      const i = r * G + c;
      const minutes = ret[i * 2], liters = ret[i * 2 + 1];
      const ok = Number.isFinite(minutes) && minutes >= 0 && minutes < 1e6;
      if (!ok) {
        body.append(el("p", "tap-bad", "No safe way back from here"), slopeLine);
      } else {
        const nums = el("div", "tap-nums");
        const a = el("div", "tap-big", fmtMin(minutes));
        a.append(el("small", "", fmtMinUnit(minutes).trim() || ""));
        nums.append(a);
        if (capacity && Number.isFinite(liters)) {
          const b = el("div", "tap-big", `${Math.round((liters / capacity) * 100)}`);
          b.append(el("small", "", "% O₂"));
          nums.append(b);
        }
        body.append(nums);
        if (!capacity && Number.isFinite(liters)) body.append(el("p", "tap-sub", `${liters.toFixed(1)} L of oxygen`));
        body.append(slopeLine);
      }
    }
    $("tap-card").hidden = false;
  }
  $("tap-close").addEventListener("click", () => {
    $("tap-card").hidden = true;
    tapMarker.visible = false;
  });

  const ray = new THREE.Raycaster();
  let down = null;
  const canvas = renderer.domElement;
  canvas.addEventListener("pointerdown", (e) => {
    down = { x: e.clientX, y: e.clientY, t: performance.now() };
    $("hint").classList.add("gone");
  });
  canvas.addEventListener("pointerup", (e) => {
    if (!down) return;
    const moved = Math.hypot(e.clientX - down.x, e.clientY - down.y);
    const quick = performance.now() - down.t < 450;
    down = null;
    if (moved > 8 || !quick) return;
    const rect = canvas.getBoundingClientRect();
    const ndc = new THREE.Vector2(((e.clientX - rect.left) / rect.width) * 2 - 1, -((e.clientY - rect.top) / rect.height) * 2 + 1);
    ray.setFromCamera(ndc, camera);
    const hit = ray.intersectObject(terrain, false)[0];
    if (!hit) return;
    const x = hit.point.x + S / 2, y = S / 2 - hit.point.z;
    if (x < 0 || y < 0 || x > S || y > S) return;
    showTap(x, y);
  });

  // ---------- sizing + loop ----------
  function frame() {
    const w = viewer.clientWidth, h = viewer.clientHeight;
    renderer.setSize(w, h, false); // CSS keeps the canvas at 100% of the viewer
    labels.setSize(w, h);
    camera.aspect = w / h;
    camera.updateProjectionMatrix();
  }
  frame();
  // First view: from the south-west, framed for the screen's shape.
  {
    const dist = camera.aspect < 1 ? 1100 / Math.pow(camera.aspect, 0.85) : 1150;
    const dir = new THREE.Vector3(-0.42, 0.68, 0.6).normalize();
    camera.position.copy(controls.target).addScaledVector(dir, Math.min(dist, controls.maxDistance));
    controls.update();
  }
  new ResizeObserver(frame).observe(viewer);

  renderer.setAnimationLoop(() => {
    controls.update();
    renderer.render(scene, camera);
    labels.render(scene, camera);
  });
  $("loading").remove();

  renderCredits(site.sources);
}

// ---------- page sections ----------
function renderRoutePanel(route, stopById) {
  const box = $("verdict");
  const word = box.querySelector(".verdict-word");
  const text = box.querySelector(".verdict-text");
  if (!route || route.points.length < 2) {
    word.textContent = "Route pending";
    text.textContent = "It comes from session C's model, the same numbers the headset shows.";
    return;
  }
  const last = route.points[route.points.length - 1];
  box.classList.remove("verdict-pending");
  if (route.viable) {
    box.classList.add("verdict-ok");
    word.textContent = "VIABLE";
    text.textContent = Number.isFinite(last.o2)
      ? `The whole walk fits: back home with ${Math.round(last.o2)}% of the oxygen left.`
      : "The whole walk fits, with a safe way home at every step.";
  } else {
    box.classList.add("verdict-bad");
    word.textContent = "POINT OF NO RETURN";
    const p = route.pnr;
    text.textContent = p && Number.isFinite(p.minutes)
      ? `After minute ${Math.round(p.minutes)}${Number.isFinite(p.o2) ? ` (${Math.round(p.o2)}% O₂ left)` : ""} there is no safe way home. Turn back before it.`
      : "Somewhere on this route there is no safe way home. Turn back before the red part.";
  }

  const stopPts = route.points.filter((p) => p.stopId !== null && p.stopId !== undefined);
  $("kpis").hidden = false;
  $("st-min").textContent = Number.isFinite(last.minutes) ? `${fmtMin(last.minutes)}${fmtMinUnit(last.minutes)}` : "–";
  $("st-o2-label").textContent = route.viable ? "O₂ left at home" : "O₂ left at the end";
  $("st-o2").textContent = Number.isFinite(last.o2) ? `${Math.round(last.o2)}%` : "–";

  const ol = $("steps");
  ol.replaceChildren();
  const row = (dotText, dotCls, name, p) => {
    const li = el("li");
    li.append(el("span", `step-dot ${dotCls}`, dotText), el("span", "step-name", name));
    const nums = el("span", "step-nums");
    if (Number.isFinite(p.minutes)) {
      nums.append(el("b", "", `${fmtMin(p.minutes)}${fmtMinUnit(p.minutes)}`));
    }
    if (Number.isFinite(p.o2)) nums.append(document.createTextNode(` · ${Math.round(p.o2)}% O₂`));
    li.append(nums);
    ol.append(li);
  };
  row("H", "home", "Leave the habitat", route.points[0]);
  for (const p of stopPts) {
    const s = stopById.get(String(p.stopId));
    row(String(p.stopId), "", s?.name ?? `Stop ${p.stopId}`, p);
  }
  if (route.pnr) row("!", "pnr", "Point of no return", route.pnr);
  row("H", "home", route.viable ? "Back at the habitat" : "End of route", last);
}

function renderCredits(sources) {
  const ul = $("credits-list");
  ul.replaceChildren();
  // Hide placeholder values so the footer stays clean until real credits arrive.
  const real = (v) => v && !/^(TODO|—|-)$/i.test(String(v).trim());
  const list = Array.isArray(sources) ? sources : [];
  for (const s of list) {
    const li = el("li");
    li.append(el("strong", "", s.layer ?? "Layer"));
    const meta = [s.mission, s.instrument, s.resolution].filter(real).join(" · ");
    if (meta) li.append(el("span", "credit-meta", meta));
    if (real(s.credit)) li.append(el("span", "credit-meta", s.credit));
    if (meta || real(s.credit)) ul.append(li);
  }
  if (!ul.children.length) ul.append(el("li", "muted", "Data credits arrive with the real Gale Crater data."));
}

main().catch((err) => {
  console.error(err);
  const l = $("loading");
  if (l) {
    l.classList.add("error");
    l.textContent = `Could not load the terrain: ${err.message}`;
  }
});
