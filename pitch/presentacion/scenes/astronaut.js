// Written with Claude Code (AI), directed and tested by team MARSXR
// Walkback pitch, closing slide: NASA's public-domain 3D astronaut (NASA 3D Resources, Astronaut.glb), seen from
// behind on the right, looking at the habitat's green light far away. Background: the real Curiosity Mastcam
// photo of Nevado Sajama (PIA26696, NASA/JPL-Caltech/MSSS). The astronaut's place and size and the pillar are
// illustrative. Slow camera push-in over 12 s, then still; the pillar breathes. No fetch: works from file://.
(function () {
  'use strict';
  if (!window.WalkbackScenes) return;

  var PUSH_S = 12;                 // push-in length, seconds
  var PUSH_M = 1.3;                // how far the camera travels, meters
  // Background photo (1920 x 606): columns 0..975 only (the mosaic's empty corners with frame lines sit at the
  // top, right of column 975), scaled to the full canvas width; its top edge sits at PHOTO_TOP of the screen and a
  // dark sky gradient fills above it. Nothing is stretched: one scale for both axes.
  var PHOTO_W = 1920, PHOTO_H = 606, CROP_X0 = 0, CROP_X1 = 975, PHOTO_TOP = 0.165;
  var PILLAR_AT = [0.655, 0.335];  // screen point (x, y from the top, 0..1) where the pillar's base lands at the end
  var ASTRO_AT = [0.80, 0.79];    // screen point of the astronaut's feet at the end
  var ASTRO_H = 1.8;               // meters

  WalkbackScenes.register('astronaut', function (canvas, ctx) {
    var THREE = ctx.THREE || window.THREE;
    if (!THREE || !window.ASTRONAUT_GLB || !THREE.GLTFLoader || !window.SAJAMA_TEX) return null;
    var reduced = !!ctx.reduced;
    var W = canvas.width || 1920, H = canvas.height || 1080, aspect = W / H;

    var renderer = ctx.makeRenderer(THREE, canvas);
    renderer.autoClear = false;
    renderer.setClearColor(0x0b0d0f, 1);

    // ---------- background: the photo, cover-placed by a shader on a full-screen quad ----------
    var photo = ctx.tex(THREE, window.SAJAMA_TEX);
    photo.minFilter = THREE.LinearFilter; photo.generateMipmaps = false;
    photo.wrapS = photo.wrapT = THREE.ClampToEdgeWrapping;
    var scale = W / (CROP_X1 - CROP_X0);                 // screen px per photo px
    var bgMat = new THREE.ShaderMaterial({
      depthTest: false, depthWrite: false,
      uniforms: {
        map: { value: photo },
        res: { value: new THREE.Vector2(W, H) },
        crop: { value: new THREE.Vector4(CROP_X0, scale, PHOTO_TOP * H, 0) },   // x0, scale, top (px), unused
        photoSize: { value: new THREE.Vector2(PHOTO_W, PHOTO_H) },
        zoom: { value: 1 },
        focus: { value: new THREE.Vector2(PILLAR_AT[0] * W, PILLAR_AT[1] * H) },
        skyTop: { value: new THREE.Color(0x0b0d0f) },
        skyLow: { value: new THREE.Color(0x6f6158) }
      },
      vertexShader: 'varying vec2 vUv; void main(){ vUv = uv; gl_Position = vec4(position.xy, 0., 1.); }',
      fragmentShader: [
        'uniform sampler2D map; uniform vec2 res; uniform vec4 crop; uniform vec2 photoSize; uniform float zoom;',
        'uniform vec2 focus; uniform vec3 skyTop; uniform vec3 skyLow; varying vec2 vUv;',
        'vec3 toLin(vec3 c){ return pow(c, vec3(2.2)); }',
        'void main(){',
        '  vec2 s = vec2(vUv.x, 1.0 - vUv.y) * res;                 // screen px, y down',
        '  s = focus + (s - focus) / zoom;                           // slow zoom towards the pillar',
        '  vec2 p = vec2(crop.x + s.x / crop.y, (s.y - crop.z) / crop.y);   // photo px, y down',
        '  vec3 sky = mix(skyTop, skyLow, smoothstep(0.0, crop.z + 30.0, s.y));',
        '  vec3 col = sky;',
        '  if (p.y > 0.0) {',
        '    vec3 ph = texture2D(map, vec2(p.x / photoSize.x, 1.0 - p.y / photoSize.y)).rgb;',
        '    float a = smoothstep(2.0, 34.0, p.y);                  // the ridge melts into the sky',
        '    col = mix(sky, ph, a);',
        '  }',
        '  float u = vUv.x;',
        '  col *= mix(0.30, 1.0, smoothstep(0.16, 0.60, u));         // left side calm and dark for the text',
        '  col *= mix(0.85, 1.0, smoothstep(0.0, 0.45, vUv.y));      // floor a little darker',
        '  vec2 q = vUv - vec2(0.62, 0.55); col *= 1.0 - 0.25 * dot(q, q);',
        '  gl_FragColor = vec4(col, 1.0);',
        '}'
      ].join('\n')
    });
    var bgScene = new THREE.Scene();
    var bgCam = new THREE.OrthographicCamera(-1, 1, 1, -1, 0, 1);
    bgScene.add(new THREE.Mesh(new THREE.PlaneGeometry(2, 2), bgMat));

    // ---------- 3D: camera, lights, pillar, astronaut ----------
    var scene = new THREE.Scene();
    var camera = new THREE.PerspectiveCamera(32, aspect, 0.1, 600);
    var EYE = 1.7, PITCH = -6.5 * Math.PI / 180;
    function placeCamera(k) {                               // k: 0 = start of the push-in, 1 = end
      camera.position.set(0.15 * (1 - k), EYE + 0.12 * (1 - k), PUSH_M * (1 - k));
      camera.rotation.set(PITCH, 0, 0);
      camera.updateMatrixWorld(true);
    }
    // where a screen point meets the ground (y = 0) or a far distance, from the final camera
    function rayAt(sx, sy) {
      placeCamera(1);
      var v = new THREE.Vector3(sx * 2 - 1, 1 - sy * 2, 0.5).unproject(camera);
      return v.sub(camera.position).normalize();
    }

    scene.add(new THREE.HemisphereLight(0xffe6cf, 0x3a2418, 0.5));
    var key = new THREE.DirectionalLight(0xffe2c0, 1.0); key.position.set(-6, 4.5, 2.5); scene.add(key);
    var rim = new THREE.DirectionalLight(0xfff4e6, 1.35); rim.position.set(4, 3.5, -5); scene.add(rim);
    var fill = new THREE.DirectionalLight(0xffd9b8, 0.35); fill.position.set(5, 1.5, 3); scene.add(fill);

    // The habitat: a green light pillar far away, its base pinned to PILLAR_AT at the end of the push-in.
    var GREEN = (ctx.C && ctx.C.green) || '#40D966';
    var FAR = 160;
    var d = rayAt(PILLAR_AT[0], PILLAR_AT[1]);
    var base = camera.position.clone().add(d.multiplyScalar(FAR));
    var beam = ctx.pillar(THREE, GREEN, 70, 0.55);
    beam.position.copy(base); scene.add(beam);
    var halo = new THREE.Sprite(new THREE.SpriteMaterial({
      map: glowTexture(THREE), color: new THREE.Color(GREEN), transparent: true, depthWrite: false,
      blending: THREE.AdditiveBlending, opacity: 0.55
    }));
    halo.position.copy(base); halo.position.y += 1.5; halo.scale.set(18, 7, 1); scene.add(halo);

    // The astronaut: feet pinned to ASTRO_AT on the ground, turned to look at the pillar.
    var dF = rayAt(ASTRO_AT[0], ASTRO_AT[1]);
    var feet = camera.position.clone().add(dF.multiplyScalar(-camera.position.y / dF.y));
    var holder = new THREE.Group(); holder.position.copy(feet); scene.add(holder);
    var toPillar = new THREE.Vector3().subVectors(base, feet);
    holder.rotation.y = Math.atan2(toPillar.x, toPillar.z);   // the model faces +z: turn it to the pillar
    var shadow = new THREE.Mesh(new THREE.PlaneGeometry(1.5, 0.9), new THREE.MeshBasicMaterial({
      map: glowTexture(THREE), color: 0x000000, transparent: true, opacity: 0.55, depthWrite: false
    }));
    shadow.rotation.x = -Math.PI / 2; shadow.position.y = 0.005; holder.add(shadow);

    var failed = false;
    try {
      var bin = atob(window.ASTRONAUT_GLB), buf = new Uint8Array(bin.length);
      for (var i = 0; i < bin.length; i++) buf[i] = bin.charCodeAt(i);
      var loader = new THREE.GLTFLoader();
      if (THREE.DRACOLoader && window.DRACO_DECODER_JS) {
        var draco = new THREE.DRACOLoader();
        draco.setDecoderConfig({ type: 'js' });
        draco._loadLibrary = function () { return Promise.resolve(window.DRACO_DECODER_JS); };  // no fetch
        loader.setDRACOLoader(draco);
      }
      loader.parse(buf.buffer, '', function (gltf) {
        var m = gltf.scene;
        m.traverse(function (o) {
          if (o.isMesh && o.material) {
            [].concat(o.material).forEach(function (mt) {
              if (mt.color) mt.color.setScalar(0.78);        // the file's 0.58 grey factor makes the suit muddy
              if (mt.map) mt.map.anisotropy = 8;
            });
          }
        });
        var box = new THREE.Box3().setFromObject(m), size = box.getSize(new THREE.Vector3());
        m.scale.setScalar(ASTRO_H / size.y);
        var b2 = new THREE.Box3().setFromObject(m), c = b2.getCenter(new THREE.Vector3());
        m.position.set(-c.x, -b2.min.y, -c.z);
        holder.add(m);
      }, function (err) { fail(err); });
    } catch (e) { fail(e); }
    function fail(err) {
      if (failed) return; failed = true;
      console.error('Walkback astronaut scene: the NASA astronaut model did not load; showing the photo and the pillar.', err);
    }

    var t0 = 0;
    function render(now) {
      var t = Math.max(0, (now - t0) / 1000);
      var k = reduced ? 1 : Math.min(t, PUSH_S) / PUSH_S;
      k = 1 - Math.pow(1 - k, 3);                            // ease out: settles, then still
      placeCamera(k);
      bgMat.uniforms.zoom.value = 1 + 0.035 * k;
      var breath = reduced ? 0 : t * 0.45;                   // pillar shader breathes at sin(2t): slow it down
      beam.material.uniforms.t.value = breath;
      halo.material.opacity = reduced ? 0.5 : 0.42 + 0.14 * Math.sin(breath * 2.0);
      renderer.clear();
      renderer.render(bgScene, bgCam);
      renderer.clearDepth();
      renderer.render(scene, camera);
    }

    return {
      enter: function (slide, now) { t0 = now != null ? now : performance.now(); },
      frame: function (now) { render(now != null ? now : performance.now()); },
      leave: function () {}
    };
  });

  var glowCache = null;
  function glowTexture(THREE) {                              // soft radial spot (halo and contact shadow)
    if (glowCache) return glowCache;
    var c = document.createElement('canvas'); c.width = c.height = 128;
    var x = c.getContext('2d'), g = x.createRadialGradient(64, 64, 0, 64, 64, 64);
    g.addColorStop(0, 'rgba(255,255,255,1)'); g.addColorStop(0.35, 'rgba(255,255,255,0.45)'); g.addColorStop(1, 'rgba(255,255,255,0)');
    x.fillStyle = g; x.fillRect(0, 0, 128, 128);
    glowCache = new THREE.CanvasTexture(c);
    return glowCache;
  }
})();
