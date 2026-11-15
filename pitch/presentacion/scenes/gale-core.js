// Written with Claude Code (AI), directed and tested by team MARSXR
// Walkback pitch: the Gale terrain every slide shares (one mesh, one material, one camera).
// The red of the WHAT IF is not painted by hand: each vertex carries the minutes back to the habitat
// that walkback-model.js computes (the app's own rule), and the shader paints red where those minutes
// are more than the oxygen covers (uReach). Same colours as TerrainLayers.cs.
// Needs: THREE (r128), window.GALE, window.GALE_STOPS, window.WalkbackModel.
// NOT opened in a browser by the audit: only syntax-checked. See AUDIT-R19, section 4.
(function () {
  'use strict';

  var COLORS = {            // WalkbackTheme.cs and TerrainLayers.cs, as hex
    safe: 0x40d966, watch: 0xffb80d, danger: 0xf23829, live: 0x59d1ff, text: 0xebebeb,
    panelBack: 0x0b0d0f, cardBack: 0x1f2229
  };

  function smooth01(t) { t = Math.min(1, Math.max(0, t)); return t * t * (3 - 2 * t); }

  /**
   * Builds the terrain and everything that lies on it. Returns:
   *   group            THREE.Group to add to the scene (1 unit = 1 m, y up, north = -z, centre of the square at 0,0)
   *   P(x, y, lift)    local meters (x east, y north) -> THREE.Vector3 on the ground
   *   plan             the app's plan at a full tank (WalkbackModel.plan)
   *   back             Float32Array, minutes back from every node (Infinity = no way back)
   *   set(name, v)     uniforms: 'slope' 0..1 (hatched no-go), 'ret' 0..1 (return layer), 'reach' minutes (Infinity = no red),
   *                    'route' 0..1 (how much of the route is drawn), 'dots' 0..1 (Curiosity's positions), 'time' seconds
   *   pins             [{ id, name, pillar, label }] habitat first, then the stops in order
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
        // a pale ring every 5 minutes of walking back
        '  float ring = (1.0 - smoothstep(0.0, 0.06, abs(fract(vB / 5.0 + 0.5) - 0.5))) * (1.0 - step(0.5, vNoWay)) * uRet;',
        '  col = mix(col, vec3(0.92), ring * 0.55);',
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
      new THREE.MeshBasicMaterial({ color: COLORS.watch, depthTest: false, transparent: true }));
    tube.renderOrder = 6; tube.geometry.setDrawRange(0, 0);
    group.add(tube);

    // Curiosity's 85 positions (curiosity_traverse.json): the places where the slope was checked.
    var dots = new THREE.Points(new THREE.BufferGeometry().setFromPoints(G.traverse.map(function (p) { return P(p[0], p[1], 2.5); })),
      new THREE.PointsMaterial({ color: COLORS.live, size: 9, sizeAttenuation: true, transparent: true, opacity: 0, depthTest: false }));
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
    function label(text, cssColor) {
      var cv = document.createElement('canvas'); cv.width = 1024; cv.height = 192;
      var x = cv.getContext('2d');
      x.font = '800 92px Inter, sans-serif';
      var w = Math.min(1000, x.measureText(text).width + 70);
      x.fillStyle = 'rgba(11,13,15,0.88)'; x.fillRect((1024 - w) / 2, 30, w, 132);
      x.fillStyle = cssColor; x.textAlign = 'center'; x.textBaseline = 'middle'; x.fillText(text, 512, 98);
      var s = new THREE.Sprite(new THREE.SpriteMaterial({ map: new THREE.CanvasTexture(cv), depthTest: false, transparent: true }));
      s.renderOrder = 10; s.scale.set(220, 41, 1);
      return s;
    }
    var pins = [];
    function pin(id, name, x, y, color, css, height) {
      var p = pillar(color, height, 5), l = label(name, css);
      p.position.copy(P(x, y, 0)); l.position.copy(P(x, y, height + 25));
      group.add(p); group.add(l);
      pins.push({ id: id, name: name, x: x, y: y, pillar: p, label: l });
    }
    pin('habitat', opts.habitatName || 'HABITAT', site.habitat.x, site.habitat.y, COLORS.safe, '#40d966', 150);
    STOPS.forEach(function (s) { pin(s.id, s.name, s.x, s.y, COLORS.watch, '#ffb80d', 120); });

    var tubeCount = segs * radial * 6;
    function set(name, v) {
      if (name === 'slope') uniforms.uSlope.value = v;
      else if (name === 'ret') uniforms.uRet.value = v;
      else if (name === 'reach') uniforms.uReach.value = isFinite(v) ? v : 1e9;
      else if (name === 'time') uniforms.uTime.value = v;
      else if (name === 'dots') dots.material.opacity = 0.95 * v;
      else if (name === 'route') tube.geometry.setDrawRange(0, Math.floor(Math.min(1, Math.max(0, v)) * segs) * radial * 6 || 0);
      else if (name === 'labels') pins.forEach(function (p) { p.label.material.opacity = v; });
    }
    void tubeCount;
    return { group: group, P: P, site: site, slope: slope, plan: plan, back: back, set: set, pins: pins, colors: COLORS, model: M };
  }

  /**
   * Camera poses by name; goTo() eases from where the camera is to a pose in `seconds`.
   * A pose is { pos: [x, y, z], look: [x, y, z], fov }. update(now) must run every frame before rendering.
   */
  function cameraRig(THREE, camera, poses) {
    var from = null, to = null, t0 = 0, dur = 0, look = new THREE.Vector3(), orbit = null;
    function apply(p, l, fov) { camera.position.copy(p); look.copy(l); camera.lookAt(look); if (fov && camera.fov !== fov) { camera.fov = fov; camera.updateProjectionMatrix(); } }
    function vec(a) { return new THREE.Vector3(a[0], a[1], a[2]); }
    return {
      snap: function (name) { var p = poses[name]; from = null; to = null; orbit = p.orbit || null; apply(vec(p.pos), vec(p.look), p.fov); },
      goTo: function (name, seconds, now) {
        var p = poses[name];
        from = { pos: camera.position.clone(), look: look.clone(), fov: camera.fov };
        to = { pos: vec(p.pos), look: vec(p.look), fov: p.fov || camera.fov }; t0 = now; dur = Math.max(0.001, seconds * 1000); orbit = p.orbit || null;
      },
      update: function (now) {
        if (to) {
          var k = smooth01((now - t0) / dur);
          apply(from.pos.clone().lerp(to.pos, k), from.look.clone().lerp(to.look, k), from.fov + (to.fov - from.fov) * k);
          if (k >= 1) { to = null; }
        } else if (orbit) {                                   // a slow turn around the look point once the move is over
          var dx = camera.position.x - look.x, dz = camera.position.z - look.z, a = orbit * 0.016;
          camera.position.x = look.x + dx * Math.cos(a) - dz * Math.sin(a);
          camera.position.z = look.z + dx * Math.sin(a) + dz * Math.cos(a);
          camera.lookAt(look);
        }
      },
      moving: function () { return !!to; }
    };
  }

  /** The WHAT IF as an animation: tank percent over time -> reach in minutes (the app's rule) for the shader. */
  function whatIfTimeline(M, fromPercent, toPercent, seconds) {
    return function (t) {
      var k = smooth01(t / seconds), percent = fromPercent + (toPercent - fromPercent) * k;
      return { percent: percent, reach: M.reachMinutes(percent) };
    };
  }

  window.WalkbackGale = { build: build, cameraRig: cameraRig, whatIfTimeline: whatIfTimeline, COLORS: COLORS, smooth01: smooth01 };
})();
