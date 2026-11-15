// Written with Claude Code (AI), directed and tested by team MARSXR
// Walkback pitch · the Gale scene that slides 2 to 6 share: one terrain (scenes/gale-core.js, audit R20) and one
// camera that travels between the poses of WalkbackGale.POSES (table, board, whatif, evidence, closing).
// Each pose turns its own layers on; the deck places the text where the pose leaves the slide empty (AUDIT-R20-F4, fix 6).
// Wiring as in prueba-gale.html: rig.update(now), then W.layoutLabels(camera), then render; labels redrawn once Inter loads.
(function () {
  'use strict';
  WalkbackScenes.register('gale', function (canvas, ctx) {
    var THREE = ctx.THREE;
    if (!window.GALE || !window.GALE_STOPS || !window.WalkbackGale || !window.WalkbackModel) return null;
    var WG = window.WalkbackGale, sm = WG.smooth01;
    var renderer = ctx.makeRenderer(THREE, canvas, 1);
    renderer.setClearColor(0x0b0d0f, 1);
    var scene = new THREE.Scene();
    var FOV = 38;
    // Labels keep one size on screen (gale-core: a 72 px box on the 1080 px slide, text about 34 px). No metre scale.
    var G = WG.build(THREE, window.GALE, window.GALE_STOPS, window.WalkbackModel, { habitatName: ctx.habitatName || 'HABITAT', labelPx: 72, fov: FOV });
    scene.add(G.group);

    var camera = new THREE.PerspectiveCamera(FOV, 1920 / 1080, 1, 8000);
    // The deck's poses: WalkbackGale.POSES (audit R20, measured in headless Chrome) adjusted by eye in the deck, so that
    // the labels clear the title and the slow turn never brings the map under the text (checked frame by frame, 15/11):
    //   table     camera 16 % farther, picture 7 % lower, turn 0.006 rad/s (R20: 0.02, about 34° in the slide's 30 s)
    //   board     picture 7 % lower (Altadena's label sat next to the title)
    //   evidence  camera 5 % farther, picture 9.6 % to the right (room for the three lines), turn -0.004 (R20: -0.015)
    //   closing   picture 16 % lower (12 % until 15/11 08:40), so the pillars stay under the title and the
    //             today / next-step diagram (its labels are off)
    // prueba-gale.html keeps R20's own poses.
    var poses = ctx.poses || (function () {
      var p = {};
      Object.keys(WG.POSES).forEach(function (k) { p[k] = Object.assign({}, WG.POSES[k]); });
      Object.assign(p.table, { pos: [744, 834, 1273], shift: [0, 0.07], orbit: 0.006 });
      Object.assign(p.board, { shift: [-0.266, 0.07] });
      Object.assign(p.evidence, { pos: [1295, 1200, 500], shift: [0.096, -0.01], orbit: -0.004 });
      Object.assign(p.closing, { shift: [0.10, 0.16] });
      return p;
    })();
    var rig = WG.cameraRig(THREE, camera, poses, { reduced: !!ctx.reduced });

    // Labels drawn before Inter has loaded use the fallback font: redraw them when it is there, and once more a moment later.
    if (document.fonts && document.fonts.load) {
      document.fonts.load('800 92px Inter').then(function () { G.refreshLabels(); setTimeout(G.refreshLabels, 400); }, function () {});
    }

    var pose = 'table', t0 = 0, last = 0, reach = Infinity;
    var cur = { slope: 0, ret: 0, dots: 0, labels: 1, route: 0 };

    // What each pose shows, t seconds after the camera started moving to it.
    function targets(t) {
      switch (pose) {
        case 'table':    return { slope: sm((t - 1) / 0.8), ret: 0, dots: 0, labels: 1, route: sm((t - 2.4) / 3) };
        case 'board':    return { slope: 0, ret: 0, dots: 0, labels: 1, route: 1 };
        case 'whatif':   return { slope: 0, ret: 1, dots: 0, labels: 1, route: 1 };
        case 'closing':  return { slope: 0, ret: 0, dots: 0, labels: 0, route: 1 };   // low view: the names would sit on the title
        default:         return { slope: 1, ret: 0, dots: sm((t - 0.4) / 1.5), labels: 1, route: 0 };   // evidence
      }
    }

    function go(name, now, snap) {
      pose = poses[name] ? name : 'evidence';
      t0 = now;
      if (snap || ctx.reduced) rig.snap(pose); else rig.goTo(pose, 1.5, now);
    }

    return {
      enter: function (slide, now, opts) {
        opts = opts || {};
        reach = Infinity;
        go(slide.pose || 'evidence', now, opts.snap);
        if (opts.final || ctx.reduced) {                     // print / reduced motion: jump to the end state
          t0 = now - 60000;
          var T = targets(60);
          Object.keys(T).forEach(function (k) { cur[k] = T[k]; });
        }
      },
      frame: function (now) {
        var t = (now - t0) / 1000, dt = last ? Math.min(0.1, Math.max(0, (now - last) / 1000)) : 0.016;
        last = now;
        var T = targets(t), k = ctx.reduced ? 1 : 1 - Math.exp(-dt * 5);
        ['slope', 'ret', 'dots', 'labels'].forEach(function (n) { cur[n] += (T[n] - cur[n]) * k; });
        cur.route = pose === 'table' ? T.route : cur.route + (T.route - cur.route) * k;
        G.set('slope', cur.slope); G.set('ret', cur.ret); G.set('dots', cur.dots); G.set('labels', cur.labels);
        G.set('route', cur.route); G.set('reach', reach); G.set('time', now / 1000);
        rig.update(now);
        G.layoutLabels(camera);
        renderer.render(scene, camera);
      },
      leave: function () { reach = Infinity; },
      action: function (name, v, opts) {
        if (name === 'reach') reach = v;                     // minutes the oxygen covers (Infinity = no red); red labels too
        else if (name === 'pose') go(v, performance.now(), opts && opts.snap);
        else if (name === 'habitat') { G.pins[0].name = v; G.refreshLabels(); }
        else if (name === 'final') {                         // jump to the end state of the current pose (print)
          t0 = performance.now() - 60000;
          var T = targets(60);
          Object.keys(T).forEach(function (k) { cur[k] = T[k]; });
        }
      },
      gale: G, camera: camera, rig: rig
    };
  });
})();
