// Written with Claude Code (AI), directed and tested by team MARSXR
// Walkback pitch: the Gale terrain every slide shares (one mesh, one material, one camera).
// The red of the WHAT IF is not painted by hand: each vertex carries the minutes back to the habitat
// that walkback-model.js computes (the app's own rule), and the shader paints red where those minutes
// are more than the oxygen covers (uReach). Same colours as TerrainLayers.cs.
// Needs: THREE (r128), window.GALE, window.GALE_STOPS, window.WalkbackModel.
// R20 (F4): this version WAS run in headless Chrome (WebGL 2, software renderer) against the repo's gale.js:
// the shaders compile, the route, the hatching, the rings and the red draw. Not seen on the defense laptop or projector.
(function () {
  'use strict';

  var COLORS = {            // WalkbackTheme.cs and TerrainLayers.cs, as hex
    safe: 0x40d966, watch: 0xffb80d, danger: 0xf23829, live: 0x59d1ff, text: 0xebebeb,
    panelBack: 0x0b0d0f, cardBack: 0x1f2229
  };

  function smooth01(t) { t = Math.min(1, Math.max(0, t)); return t * t * (3 - 2 * t); }
  function css(hex) { return '#' + ('000000' + hex.toString(16)).slice(-6); }

  /**
   * Builds the terrain and everything that lies on it. Returns:
   *   group            THREE.Group to add to the scene (1 unit = 1 m, y up, north = -z, centre of the square at 0,0)
   *   P(x, y, lift)    local meters (x east, y north) -> THREE.Vector3 on the ground
   *   plan             the app's plan at a full tank (WalkbackModel.plan)
   *   back             Float32Array, minutes back from every node (Infinity = no way back)
   *   set(name, v)     uniforms: 'slope' 0..1 (hatched no-go), 'ret' 0..1 (return layer), 'reach' minutes (Infinity = no red),
   *                    'route' 0..1 (how much of the route is drawn), 'dots' 0..1 (Curiosity's positions), 'time' seconds,
   *                    'labels' 0..1, 'pillars' 0 or 1
   *   opts             { habitatName, routeRadius, fov (the camera's, default 38),
   *                      labelPx: height of a label's box in px on a 1080 px slide, default 72 (its text is 48 % of that: about 34 px) }
   *   pins             [{ id, name, pillar, label, backMinutes }] habitat first, then the stops in order
   *   refreshLabels()  redraw the labels once the Inter font has loaded
   *   layoutLabels(camera)  call every frame after the camera moves: labels that would overlap go up a row
   *   markReach(min)   red labels for the pins farther back than `min` minutes; returns their names (set('reach') calls it)
   */
  function build(THREE, G, STOPS, M, opts) {
    opts = opts || {};
    var site = M.siteFromGale(G); site.stops = STOPS;
    var n = site.n, side = site.side, step = site.spacing, h = site.heights, base = G.hMin;
    var slope = M.slopeDegrees(site), plan = M.plan(site, slope, STOPS, 100), back = plan.home;

    function P(x, y, lift) { return new THREE.Vector3(x - side / 2, M.heightAt(site, x, y) - base + (lift || 0), side / 2 - y); }

    var pos = new Float32Array(n * n * 3), uv = new Float32Array(n * n * 2), aBack = new Float32Array(n * n);
    for (var r = 0; r < n; r++) for (var c = 0; c < n; c++) {
      var i = r * n + c;
      pos[3 * i] = c * step - side / 2; pos[3 * i + 1] = h[i] - base; pos[3 * i + 2] = r * step - side / 2;
      uv[2 * i] = c / (n - 1); uv[2 * i + 1] = 1 - r / (n - 1);
      aBack[i] = isFinite(back[i]) ? back[i] : -1;                       // -1 = no way back on foot
    }
    var idx = new Uint32Array((n - 1) * (n - 1) * 6), k = 0;
    for (r = 0; r < n - 1; r++) for (c = 0; c < n - 1; c++) {
      var a = r * n + c, b = a + 1, d = a + n, e = d + 1;
      idx[k++] = a; idx[k++] = d; idx[k++] = b; idx[k++] = b; idx[k++] = d; idx[k++] = e;
    }
    var geo = new THREE.BufferGeometry();
    geo.setAttribute('position', new THREE.BufferAttribute(pos, 3));
    geo.setAttribute('uv', new THREE.BufferAttribute(uv, 2));
    geo.setAttribute('slope', new THREE.BufferAttribute(slope, 1));
    geo.setAttribute('back', new THREE.BufferAttribute(aBack, 1));
    geo.setIndex(new THREE.BufferAttribute(idx, 1));
    geo.computeVertexNormals();

    var tex = new THREE.TextureLoader().load(G.ortho);
    tex.anisotropy = 8;
    var uniforms = {
      map: { value: tex }, uLimit: { value: M.SLOPE_LIMIT_DEGREES },
      uSlope: { value: 0 }, uRet: { value: 0 }, uReach: { value: 1e9 }, uTime: { value: 0 }
    };
    var mat = new THREE.ShaderMaterial({
      uniforms: uniforms,
      extensions: { derivatives: true },                                  // fwidth() on WebGL 1
      vertexShader: [
        'attribute float slope; attribute float back;',
        'varying vec2 vUv; varying float vS; varying float vB; varying float vNoWay; varying vec3 vN; varying vec3 vP;',
        'void main(){ vUv = uv; vS = slope; vB = max(back, 0.0); vNoWay = step(back, -0.5); vN = normal; vP = position;',
        '  gl_Position = projectionMatrix * modelViewMatrix * vec4(position, 1.0); }'
      ].join('\n'),
      fragmentShader: [
        'uniform sampler2D map; uniform float uLimit, uSlope, uRet, uReach, uTime;',
        'varying vec2 vUv; varying float vS; varying float vB; varying float vNoWay; varying vec3 vN; varying vec3 vP;',
        'vec3 lin(vec3 c){ return pow(c, vec3(2.2)); }',
        'void main(){',
        '  vec3 photo = lin(texture2D(map, vUv).rgb);',
        '  float light = 0.55 + 0.6 * max(dot(normalize(vN), normalize(vec3(-0.5, 0.9, 0.3))), 0.0);',
        '  vec3 col = dot(photo, vec3(0.333)) * light * vec3(1.35, 0.78, 0.52);   // HiRISE red band is greyscale: tinted, as in the app',
        '  float hatchA = step(0.5, fract((vP.x + vP.z) / 9.0));',
        '  float hatchB = step(0.5, fract((vP.x - vP.z) / 9.0));',
        // slope layer: over the limit, hatched vermilion (TerrainLayers.SlopeNoGo / SlopeNoGoHatch)
        '  float over = step(uLimit, vS) * uSlope;',
        '  col = mix(col, mix(lin(vec3(0.835, 0.369, 0.0)), lin(vec3(0.431, 0.173, 0.0)), hatchA), over * 0.72);',
        // return layer: no way back = hatched gray (NoReturn / NoReturnHatch)
        '  float noWay = step(0.5, vNoWay) * uRet;',
        '  col = mix(col, mix(lin(vec3(0.275, 0.275, 0.290)), lin(vec3(0.549, 0.549, 0.573)), hatchB), noWay * 0.80);',
        // a pale ring every 5 minutes of walking back, about 3 px wide whatever the distance
        '  float rx = vB / 5.0; float rd = abs(fract(rx + 0.5) - 0.5) / max(fwidth(rx), 1e-5);',
        '  float ring = (1.0 - smoothstep(1.0, 2.5, rd)) * step(0.5, rx) * (1.0 - step(0.5, vNoWay)) * uRet;',
        '  col = mix(col, vec3(0.92), ring * 0.75);',
        // WHAT IF: farther back than the oxygen covers = red (TooFar / TooFarHatch). The edge is 0.15 min wide so it does not flicker.
        '  float far = smoothstep(uReach, uReach + 0.15, vB) * (1.0 - step(0.5, vNoWay));',
        '  col = mix(col, mix(lin(vec3(0.949, 0.220, 0.161)), lin(vec3(0.549, 0.102, 0.078)), hatchA), far * 0.82);',
        '  gl_FragColor = vec4(pow(col, vec3(1.0 / 2.2)), 1.0);',
        '}'
      ].join('\n')
    });
    var group = new THREE.Group();
    group.add(new THREE.Mesh(geo, mat));

    // The plan's route (the same path the app draws), as an amber tube that draws itself.
    var routePts = plan.path.map(function (p) { return P(p[0], p[1], 3); });
    var segs = Math.min(1200, routePts.length * 2), radial = 8;
    var tube = new THREE.Mesh(new THREE.TubeGeometry(new THREE.CatmullRomCurve3(routePts, false, 'centripetal'), segs, opts.routeRadius || 3.2, radial, false),
      new THREE.MeshBasicMaterial({ color: new THREE.Color(COLORS.watch).convertSRGBToLinear(), depthTest: false, transparent: true }));
    tube.renderOrder = 6; tube.geometry.setDrawRange(0, 0);
    group.add(tube);

    // Curiosity's 85 positions (curiosity_traverse.json): the places where the slope was checked.
    var dots = new THREE.Points(new THREE.BufferGeometry().setFromPoints(G.traverse.map(function (p) { return P(p[0], p[1], 2.5); })),
      new THREE.PointsMaterial({ color: new THREE.Color(COLORS.live).convertSRGBToLinear(), size: 12, sizeAttenuation: false, transparent: true, opacity: 0, depthTest: false }));
    dots.renderOrder = 4;
    group.add(dots);

    function pillar(color, height, radius) {
      var g = new THREE.CylinderGeometry(radius, radius, height, 24, 1, true);
      g.translate(0, height / 2, 0);
      return new THREE.Mesh(g, new THREE.ShaderMaterial({
        transparent: true, depthWrite: false, blending: THREE.AdditiveBlending, side: THREE.DoubleSide,
        uniforms: { c: { value: new THREE.Color(color) }, t: uniforms.uTime, hgt: { value: height } },
        vertexShader: 'varying float vy; void main(){ vy = position.y; gl_Position = projectionMatrix*modelViewMatrix*vec4(position,1.); }',
        fragmentShader: 'uniform vec3 c; uniform float t; uniform float hgt; varying float vy; void main(){ float f = 1.0 - clamp(vy/hgt,0.,1.); gl_FragColor = vec4(c, pow(f,1.6)*0.85*(0.75+0.25*sin(t*2.0))); }'
      }));
    }
    // Labels keep the same size on screen at any distance (sizeAttenuation off): a box opts.labelPx tall on a 1080 px slide.
    var LABEL_W = 1024, LABEL_H = 192, labelPx = opts.labelPx || 72, fovTan = 2 * Math.tan((opts.fov || 38) * Math.PI / 360);
    function drawLabel(pinObj) {
      var cv = pinObj.canvas, x = cv.getContext('2d');
      x.clearRect(0, 0, LABEL_W, LABEL_H);
      x.font = '800 92px Inter, sans-serif';
      var w = Math.min(1000, x.measureText(pinObj.name).width + 70);
      x.fillStyle = 'rgba(11,13,15,0.90)'; x.fillRect((LABEL_W - w) / 2, 30, w, 132);
      x.fillStyle = pinObj.css; x.textAlign = 'center'; x.textBaseline = 'middle'; x.fillText(pinObj.name, LABEL_W / 2, 98);
      pinObj.label.material.map.needsUpdate = true;
    }
    function makeLabel() {
      var cv = document.createElement('canvas'); cv.width = LABEL_W; cv.height = LABEL_H;
      var map = new THREE.CanvasTexture(cv); map.encoding = THREE.sRGBEncoding;
      var sp = new THREE.Sprite(new THREE.SpriteMaterial({ map: map, depthTest: false, transparent: true, sizeAttenuation: false }));
      var hgt = labelPx / 1080 * fovTan;                                   // sprite height as a share of the view at distance 1
      sp.scale.set(hgt * LABEL_W / LABEL_H, hgt, 1);
      sp.renderOrder = 10;
      return { sprite: sp, canvas: cv };
    }
    var pins = [];
    function pin(id, name, x, y, color, height) {
      var p = pillar(color, height, 5), l = makeLabel();
      p.position.copy(P(x, y, 0)); l.sprite.position.copy(P(x, y, height));
      var row = 0;
      l.sprite.center.set(0.5, -0.15);
      group.add(p); group.add(l.sprite);
      var o = { id: id, name: name, x: x, y: y, row: row, pillar: p, label: l.sprite, canvas: l.canvas, baseCss: css(color), css: css(color),
        backMinutes: id === 'habitat' ? 0 : back[M.nodeOf(site, x, y)] };
      pins.push(o); drawLabel(o);
    }
    pin('habitat', opts.habitatName || 'HABITAT', site.habitat.x, site.habitat.y, COLORS.safe, 150);
    STOPS.forEach(function (s) { pin(s.id, s.name, s.x, s.y, COLORS.watch, 120); });

    /**
     * Keeps the labels from covering each other, whatever the camera does (Valle de la Luna and Nevado Sajama are
     * 10 m apart; from a low camera the habitat and Altadena line up too). Call every frame after moving the camera:
     * the nearest label stays on its pillar, a label that would overlap one already placed goes up a row.
     */
    var _v = new THREE.Vector3();
    function layoutLabels(camera) {
      camera.updateMatrixWorld();
      var hNdc = labelPx / 1080 * 2, placed = [], order = pins.slice();
      order.forEach(function (p) {
        _v.copy(p.label.position); p._dist = _v.distanceTo(camera.position);
        _v.project(camera); p._sx = _v.x; p._sy = _v.y;
        var x = p.canvas.getContext('2d'); x.font = '800 92px Inter, sans-serif';
        p._w = (Math.min(1000, x.measureText(p.name).width + 70) / LABEL_H) * hNdc / camera.aspect;   // box width in NDC
      });
      order.sort(function (a, b) { return a._dist - b._dist; });
      order.forEach(function (p) {
        var row = 0, clash = true;
        while (clash && row < 6) {
          clash = false;
          var y0 = p._sy + (0.15 + row * 0.78) * hNdc, y1 = y0 + 0.70 * hNdc;
          for (var i = 0; i < placed.length; i++) {
            var q = placed[i];
            if (Math.abs(p._sx - q.x) < (p._w + q.w) / 2 && y0 < q.y1 && y1 > q.y0) { clash = true; row++; break; }
          }
          if (!clash) placed.push({ x: p._sx, w: p._w, y0: y0, y1: y1 });
        }
        p.row = row;
        p.label.center.set(0.5, -0.15 - row * 0.78);
      });
    }

    /** Call once the Inter font has loaded (document.fonts.ready): labels drawn before that use the fallback font. */
    function refreshLabels() { pins.forEach(drawLabel); }
    /** Pins farther back than the oxygen covers get a red label; returns their names (for the slide's text). */
    function markReach(reachMinutes) {
      var beyond = [];
      pins.forEach(function (p) {
        var far = isFinite(reachMinutes) && p.backMinutes > reachMinutes, want = far ? css(COLORS.danger) : p.baseCss;
        if (far) beyond.push(p.name);
        if (p.css !== want) { p.css = want; drawLabel(p); }
      });
      return beyond;
    }

    function set(name, v) {
      if (name === 'slope') uniforms.uSlope.value = v;
      else if (name === 'ret') uniforms.uRet.value = v;
      else if (name === 'reach') { uniforms.uReach.value = isFinite(v) ? v : 1e9; markReach(v); }
      else if (name === 'time') uniforms.uTime.value = v;
      else if (name === 'dots') dots.material.opacity = 0.95 * v;
      else if (name === 'route') tube.geometry.setDrawRange(0, Math.floor(Math.min(1, Math.max(0, v)) * segs) * radial * 6 || 0);
      else if (name === 'labels') pins.forEach(function (p) { p.label.material.opacity = v; });
      else if (name === 'pillars') pins.forEach(function (p) { p.pillar.visible = v > 0; });
    }
    return { group: group, P: P, site: site, slope: slope, plan: plan, back: back, set: set, pins: pins, colors: COLORS, model: M,
      refreshLabels: refreshLabels, markReach: markReach, layoutLabels: layoutLabels };
  }

  /**
   * Camera poses by name; goTo() eases from where the camera is to a pose in `seconds`.
   * A pose is { pos: [x, y, z], look: [x, y, z], fov, shift: [sx, sy], orbit }.
   *   shift moves the whole picture on the slide, as a share of its width and height: sx > 0 to the right, sy > 0 down
   *   (sx -0.14 = the map sits 14 % of the width to the left, leaving the right for the text). orbit is radians per second.
   * update(now) must run every frame before rendering, with now in milliseconds (performance.now()).
   * opts.reduced = true (prefers-reduced-motion): goTo() jumps and nothing orbits.
   */
  function cameraRig(THREE, camera, poses, opts) {
    opts = opts || {};
    var from = null, to = null, t0 = 0, dur = 0, look = new THREE.Vector3(), orbit = 0, shift = [0, 0], last = 0, aspect0 = camera.aspect;
    function vec(a) { return new THREE.Vector3(a[0], a[1], a[2]); }
    function apply(p, l, fov, sh) {
      camera.position.copy(p); look.copy(l); camera.lookAt(look);
      shift = sh;
      // setViewOffset() overwrites camera.aspect with fullWidth / fullHeight: give it the slide's own proportions.
      var fw = aspect0 * 1000, fh = 1000;
      if (sh[0] || sh[1]) camera.setViewOffset(fw, fh, -sh[0] * fw, -sh[1] * fh, fw, fh);
      else if (camera.view) { camera.clearViewOffset(); camera.aspect = aspect0; camera.updateProjectionMatrix(); }
      if (fov && camera.fov !== fov) { camera.fov = fov; camera.updateProjectionMatrix(); }
    }
    function snap(name) { var p = poses[name]; from = null; to = null; orbit = opts.reduced ? 0 : (p.orbit || 0); apply(vec(p.pos), vec(p.look), p.fov || camera.fov, p.shift || [0, 0]); }
    return {
      snap: snap,
      goTo: function (name, seconds, now) {
        if (opts.reduced || !(seconds > 0)) { snap(name); return; }
        var p = poses[name];
        from = { pos: camera.position.clone(), look: look.clone(), fov: camera.fov, shift: shift.slice() };
        to = { pos: vec(p.pos), look: vec(p.look), fov: p.fov || camera.fov, shift: p.shift || [0, 0], orbit: p.orbit || 0 };
        t0 = now; dur = seconds * 1000; orbit = 0;
      },
      update: function (now) {
        var dt = last ? Math.min(0.1, Math.max(0, (now - last) / 1000)) : 0; last = now;
        if (to) {
          var k = smooth01((now - t0) / dur);
          apply(from.pos.clone().lerp(to.pos, k), from.look.clone().lerp(to.look, k), from.fov + (to.fov - from.fov) * k,
            [from.shift[0] + (to.shift[0] - from.shift[0]) * k, from.shift[1] + (to.shift[1] - from.shift[1]) * k]);
          if (k >= 1) { orbit = to.orbit; to = null; }
        } else if (orbit) {                                   // a slow turn around the look point once the move is over
          var dx = camera.position.x - look.x, dz = camera.position.z - look.z, a = orbit * dt;
          camera.position.x = look.x + dx * Math.cos(a) - dz * Math.sin(a);
          camera.position.z = look.z + dx * Math.sin(a) + dz * Math.cos(a);
          camera.lookAt(look);
        }
      },
      moving: function () { return !!to; }
    };
  }

  /**
   * The poses tried in headless Chrome at 1920 x 1080 with a 38 degree camera (R20). North is away from the camera.
   * Where the terrain lands on the slide (x0..x1, y0..y1 in px, pillars and labels included), measured:
   *   table     112..1668, 211..847   map across the middle: title above it, the three figures in a row under it
   *   board      81..947,  242..884   map on the left half: the HTML board takes the right half
   *   whatif    123..1208,  83..1011  map on the left two thirds, seen from above: counter and verdict in a right column
   *   evidence  412..1650, 211..882   seen from the east: Curiosity's positions run across the picture
   *   closing   low over Campo Marte, looking home along the route (the terrain fills the lower half; text goes on top)
   * shift: [sx, sy] moves the picture, as a share of the slide; sx > 0 = to the right, sy > 0 = down.
   * Adjust by eye on the defense laptop.
   */
  var POSES = {
    table:    { pos: [640, 720, 1100],   look: [0, 20, 40], shift: [0, 0],          orbit: 0.02 },
    board:    { pos: [-270, 1800, 1350], look: [0, 0, 0],   shift: [-0.266, 0.02] },
    whatif:   { pos: [0, 1700, 410],     look: [0, 0, 10],  shift: [-0.158, 0] },
    evidence: { pos: [1230, 1140, 475],  look: [0, 0, 0],   shift: [0, -0.02],      orbit: -0.015 },
    closing:  { pos: [300, 330, 760],    look: [15, 60, -277], shift: [0.10, 0.05] }
  };

  /**
   * The WHAT IF as an animation. steps = [[percent, atSecond], ...], for example [[100, 0], [15, 2], [8, 6]]:
   * fast down to 15 % (nothing turns red there, as in the app), slow from 15 to 8 %.
   * Returns f(seconds) -> { percent, reach } with reach in minutes by the app's rule (WalkbackModel.reachMinutes).
   */
  function whatIfSteps(M, steps) {
    return function (t) {
      var a = steps[0], b = steps[steps.length - 1];
      if (t <= a[1]) return { percent: a[0], reach: M.reachMinutes(a[0]) };
      if (t >= b[1]) return { percent: b[0], reach: M.reachMinutes(b[0]) };
      for (var i = 1; i < steps.length; i++) {
        if (t <= steps[i][1]) {
          var k = smooth01((t - steps[i - 1][1]) / (steps[i][1] - steps[i - 1][1]));
          var percent = steps[i - 1][0] + (steps[i][0] - steps[i - 1][0]) * k;
          return { percent: percent, reach: M.reachMinutes(percent) };
        }
      }
      return { percent: b[0], reach: M.reachMinutes(b[0]) };
    };
  }
  function whatIfTimeline(M, fromPercent, toPercent, seconds) { return whatIfSteps(M, [[fromPercent, 0], [toPercent, seconds]]); }

  window.WalkbackGale = { build: build, cameraRig: cameraRig, whatIfTimeline: whatIfTimeline, whatIfSteps: whatIfSteps, POSES: POSES, COLORS: COLORS, smooth01: smooth01 };
})();
