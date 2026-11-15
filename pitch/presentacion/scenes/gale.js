// Written with Claude Code (AI), directed and tested by team MARSXR
// Walkback pitch · the Gale scene that slides 2 to 5 share: one terrain (scenes/gale-core.js), one camera that
// travels between poses. The map shifts sideways (camera view offset) so it never sits under the text.
(function () {
  'use strict';
  WalkbackScenes.register('gale', function (canvas, ctx) {
    var THREE = ctx.THREE;
    if (!window.GALE || !window.GALE_STOPS || !window.WalkbackGale || !window.WalkbackModel) return null;
    var WG = window.WalkbackGale, sm = WG.smooth01;
    var renderer = ctx.makeRenderer(THREE, canvas, 1);
    renderer.setClearColor(0x0b0d0f, 1);
    var scene = new THREE.Scene();
    scene.fog = new THREE.Fog(0x0b0d0f, 1500, 3200);
    var G = WG.build(THREE, window.GALE, window.GALE_STOPS, window.WalkbackModel, { habitatName: 'HABITAT' });
    scene.add(G.group);
    G.pins.forEach(function (p) { p.label.scale.multiplyScalar(1.5); p.label.position.y += 20; });
    G.group.children.forEach(function (c) { if (c.isPoints) c.material.size = 24; });   // Curiosity's 85 positions, readable from the back of the room

    // a faint plinth under the square, so it reads as an object on a table
    var plinth = new THREE.Mesh(new THREE.BoxGeometry(1010, 24, 1010), new THREE.MeshBasicMaterial({ color: 0x15181d }));
    plinth.position.y = -14; scene.add(plinth);

    var camera = new THREE.PerspectiveCamera(38, 1920 / 1080, 1, 6000);
    // shift: pixels the map moves on screen (+ = to the right), so the text column stays clear
    var poses = {
      table:    { pos: [720, 860, 1020], look: [0, 30, 0],   fov: 38, orbit: 0.05, shift: 330 },
      board:    { pos: [1000, 1350, 1650], look: [0, 0, 0],  fov: 38, orbit: 0,    shift: -620, lift: 230 },
      whatif:   { pos: [0, 3000, 1000],    look: [0, 0, 60],  fov: 38, orbit: 0,    shift: -330, lift: -30 },
      evidence: { pos: [800, 820, 1050],  look: [0, 40, 40], fov: 38, orbit: 0.04, shift: 380 }
    };
    var rig = WG.cameraRig(THREE, camera, poses);
    var pose = 'table', t0 = 0, last = 0, reachOverride = Infinity;
    var cur = { slope: 0, ret: 0, dots: 0, labels: 1, route: 0, shift: 330, lift: 0 };

    function targets(t) {
      var T = targets0(t); T.lift = poses[pose] ? (poses[pose].lift || 0) : 0; return T;
    }
    function targets0(t) {
      switch (pose) {
        case 'table':    return { slope: sm((t - 1) / 0.8), ret: 0, dots: 0, labels: 1, route: sm((t - 2.4) / 3), shift: poses.table.shift };
        case 'board':    return { slope: 0, ret: 0, dots: 0, labels: 1, route: 1, shift: poses.board.shift };
        case 'whatif':   return { slope: 0, ret: 1, dots: 0, labels: 1, route: 1, shift: poses.whatif.shift };
        default:         return { slope: 1, ret: 0, dots: sm((t - 0.4) / 1.5), labels: 1, route: 0, shift: poses.evidence.shift };
      }
    }
    function applyShift(px, py) { camera.setViewOffset(1920, 1080, -px, -(py || 0), 1920, 1080); }

    function go(name, now, snap) {
      pose = poses[name] ? name : 'evidence';
      t0 = now;
      if (snap || ctx.reduced) rig.snap(pose); else rig.goTo(pose, 1.5, now);
    }

    return {
      enter: function (slide, now, opts) {
        opts = opts || {};
        reachOverride = Infinity;
        go(slide.pose || 'evidence', now, opts.snap);
        if (opts.final || ctx.reduced) {                     // print / reduced motion: jump to the end state
          t0 = now - 60000;
          var T = targets(60);
          Object.keys(T).forEach(function (k) { cur[k] = T[k]; });
        }
      },
      frame: function (now) {
        var t = (now - t0) / 1000, dt = last ? Math.min(0.1, (now - last) / 1000) : 0.016;
        last = now;
        var T = targets(t), k = 1 - Math.exp(-dt * 5);
        ['slope', 'ret', 'dots', 'labels', 'shift', 'lift'].forEach(function (n) { cur[n] += (T[n] - cur[n]) * (ctx.reduced ? 1 : k); });
        cur.route = pose === 'table' ? T.route : cur.route + (T.route - cur.route) * k;
        G.set('slope', cur.slope); G.set('ret', cur.ret); G.set('dots', cur.dots); G.set('labels', cur.labels);
        G.set('route', cur.route); G.set('reach', reachOverride); G.set('time', now / 1000);
        G.pins.forEach(function (p) { p.pillar.material.uniforms.t.value = now / 1000; });
        applyShift(cur.shift, cur.lift);
        rig.update(now);
        // a pillar's name that falls under the text column (left of x = 940 px while the map is shifted right) hides
        camera.updateMatrixWorld();
        G.pins.forEach(function (p) {
          var v = p.label.position.clone().project(camera), px = (v.x + 1) / 2 * 1920;
          p.label.material.opacity = cur.labels * (cur.shift > 100 && px < 1080 ? 0 : 1);   // label centre; a label is ~300 px wide
        });
        renderer.render(scene, camera);
      },
      leave: function () { reachOverride = Infinity; },
      action: function (name, v) {
        if (name === 'reach') reachOverride = v;
        else if (name === 'pose') go(v, performance.now(), false);
      },
      gale: G
    };
  });
})();
