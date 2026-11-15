// Written with Claude Code (AI), directed and tested by team MARSXR
// Walkback pitch · slide 1: Curiosity's real Mastcam photo at Nevado Sajama (PIA26696, NASA/JPL-Caltech/MSSS),
// on a curved screen the camera pans across slowly. data/sajama.js is a 1920 x 606 crop of the 360° mosaic.
(function () {
  'use strict';
  WalkbackScenes.register('panorama', function (canvas, ctx) {
    var THREE = ctx.THREE;
    if (!window.SAJAMA_TEX) return null;
    var renderer = ctx.makeRenderer(THREE, canvas, 1);
    renderer.setClearColor(0x0b0d0f, 1);
    var scene = new THREE.Scene();
    var aspect = 1920 / 606, radius = 10, arc = 1.9;                        // a cylinder slice, ~109°, seen from its axis
    var height = radius * arc / aspect;
    var geo = new THREE.CylinderGeometry(radius, radius, height, 96, 1, true, Math.PI - arc / 2, arc);
    var mat = new THREE.MeshBasicMaterial({ map: ctx.tex(THREE, window.SAJAMA_TEX), side: THREE.BackSide });
    var screen = new THREE.Mesh(geo, mat);
    screen.scale.x = -1;                                                    // seen from inside: un-mirror the photo
    scene.add(screen);
    var camera = new THREE.PerspectiveCamera(28, 1920 / 1080, 0.1, 100);
    camera.position.set(0, -height * 0.08, 0);
    var t0 = 0;
    return {
      enter: function (slide, now, opts) { t0 = (opts && opts.final) || ctx.reduced ? now - 10000 : now; },
      frame: function (now) {
        var t = Math.min(20, (now - t0) / 1000), k = ctx.reduced ? 0.5 : t / 20;
        camera.rotation.set(0, 0.16 - 0.32 * k, 0, 'YXZ');                      // looks at -z (the slice's middle); a slow pan, 18° in 20 s
        renderer.render(scene, camera);
      },
      leave: function () {}
    };
  });
})();
