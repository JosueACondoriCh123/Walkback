// Written with Claude Code (AI), directed and tested by team MARSXR
// Walkback pitch · title slide: the Viking colour mosaic of Mars (NASA/JPL/USGS) turns until Gale faces us,
// marked as in the app's opening globe. Big, colour-enhanced (redder) and cut by the right edge; the title is on the left.
(function () {
  'use strict';
  WalkbackScenes.register('globe', function (canvas, ctx) {
    var THREE = ctx.THREE;
    if (!window.GLOBE_TEX) return null;
    var renderer = ctx.makeRenderer(THREE, canvas, 1);
    renderer.setClearColor(0x0b0d0f, 1);
    var scene = new THREE.Scene(), globe = new THREE.Group();
    globe.position.set(1.22, -0.04, 0); scene.add(globe);   // centre on the right edge: half the planet shows
    var mars = new THREE.Mesh(new THREE.SphereGeometry(1, 128, 96), new THREE.MeshStandardMaterial({ map: ctx.tex(THREE, window.GLOBE_TEX), color: 0xffa27a, roughness: 1, metalness: 0 }));   // tint: title art only, said in the credit
    globe.add(mars);
    globe.add(new THREE.Mesh(new THREE.SphereGeometry(1.04, 64, 48), new THREE.ShaderMaterial({        // thin dusty atmosphere
      transparent: true, side: THREE.BackSide, blending: THREE.AdditiveBlending, depthWrite: false,
      vertexShader: 'varying vec3 n; void main(){ n = normalize(normalMatrix*normal); gl_Position = projectionMatrix*modelViewMatrix*vec4(position,1.); }',
      fragmentShader: 'varying vec3 n; void main(){ float i = pow(0.72 - dot(n, vec3(0,0,1.)), 3.0); gl_FragColor = vec4(1.0,0.32,0.14,1.0)*i*1.4; }'
    })));
    // Gale: 4.807 S, 137.381 E (texture left edge 180 W)
    var lat = -4.807 * Math.PI / 180, phi = (137.381 + 180) / 360 * Math.PI * 2;
    var dir = new THREE.Vector3(-Math.cos(phi) * Math.cos(lat), Math.sin(lat), Math.sin(phi) * Math.cos(lat));
    var amber = new THREE.Color(ctx.C.amber);
    var mark = new THREE.Mesh(new THREE.SphereGeometry(0.016, 16, 12), new THREE.MeshBasicMaterial({ color: amber }));
    mark.position.copy(dir.clone().multiplyScalar(1.004)); globe.add(mark);
    var ring = new THREE.Mesh(new THREE.RingGeometry(0.03, 0.038, 64), new THREE.MeshBasicMaterial({ color: amber, transparent: true, side: THREE.DoubleSide }));
    ring.position.copy(dir.clone().multiplyScalar(1.006)); ring.lookAt(dir.clone().multiplyScalar(2)); globe.add(ring);
    var beam = ctx.pillar(THREE, ctx.C.amber, 0.38, 0.006); beam.position.copy(dir); beam.quaternion.setFromUnitVectors(new THREE.Vector3(0, 1, 0), dir); globe.add(beam);

    scene.add(new THREE.AmbientLight(0xff9a70, 0.22));
    var sun = new THREE.DirectionalLight(0xffd2b0, 1.6); sun.position.set(-4, 1.2, 3); scene.add(sun);
    var camera = new THREE.PerspectiveCamera(30, 1920 / 1080, 0.1, 50); camera.position.set(0, 0, 2.55);
    var faceY = Math.atan2(dir.x, dir.z), t0 = 0;
    return {
      enter: function (slide, now, opts) { t0 = (opts && opts.final) || ctx.reduced ? now - 60000 : now; },
      frame: function (now) {
        var t = (now - t0) / 1000, spin = ctx.reduced ? 0 : Math.max(0, 1 - t / 7);
        spin = spin * spin * (3 - 2 * spin);
        globe.rotation.y = -faceY - 0.6 - spin * 2.6 + (ctx.reduced ? 0 : Math.sin(t * 0.22) * 0.07 * (1 - spin));
        globe.rotation.x = 0.08;
        var pulse = (t % 2) / 2; ring.scale.setScalar(1 + pulse * 2.4); ring.material.opacity = 1 - pulse;
        beam.material.uniforms.t.value = t;
        renderer.render(scene, camera);
      },
      leave: function () {}
    };
  });
})();
