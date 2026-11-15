// Written with Claude Code (AI), directed and tested by team MARSXR
// Walkback pitch: what every 3D scene shares. A scene registers a factory; deck.js creates it on its own
// full-stage canvas (1920 x 1080) and calls enter / frame / leave. Only the visible scene renders.
//
// Contract for a scene module:
//   WalkbackScenes.register('name', function (canvas, ctx) {
//     // ctx: { THREE, reduced (prefers-reduced-motion), C (colours), model (deck.model, see deck.js), makeRenderer, tex, pillar }
//     return {
//       enter: function (slide, now) {},   // slide = the content.js entry (id, pose, ...); now = performance.now()
//       frame: function (now) {},          // render one frame
//       leave: function () {},
//       action: function (name, now) {}    // optional: 'backup' (key R) etc.
//     };                                   // return null when the scene cannot run (missing data): the slide shows its fallback image
//   });
(function () {
  'use strict';
  var factories = {};

  var C = {                                   // WalkbackTheme.cs / TerrainLayers.cs (AUDIT-R19, section 2)
    back: '#0B0D0F', card: '#1F2229', text: '#EBEBEB', soft: '#CCD1D9',
    amber: '#FFB80D', green: '#40D966', red: '#F23829', redHatch: '#8C1A14', sky: '#59D1FF',
    slope: '#D55E00', slopeHatch: '#6E2C00', gray: '#46464A', grayHatch: '#8C8C92'
  };

  function makeRenderer(THREE, canvas, maxRatio) {
    var r = new THREE.WebGLRenderer({ canvas: canvas, antialias: true, alpha: true, preserveDrawingBuffer: true, powerPreference: 'high-performance' });
    r.setPixelRatio(Math.min(window.devicePixelRatio || 1, maxRatio || 1.5));
    r.setSize(canvas.width, canvas.height, false);
    r.outputEncoding = THREE.sRGBEncoding;
    return r;
  }

  function tex(THREE, dataUrl) {
    var t = new THREE.TextureLoader().load(dataUrl);
    t.encoding = THREE.sRGBEncoding;
    t.anisotropy = 8;
    return t;
  }

  /** A soft vertical beam of light (the app's pillars), additive, fading upwards. */
  function pillar(THREE, color, height, radius) {
    var g = new THREE.CylinderGeometry(radius, radius, height, 24, 1, true);
    g.translate(0, height / 2, 0);
    var m = new THREE.Mesh(g, new THREE.ShaderMaterial({
      transparent: true, depthWrite: false, blending: THREE.AdditiveBlending, side: THREE.DoubleSide,
      uniforms: { c: { value: new THREE.Color(color) }, t: { value: 0 }, hgt: { value: height } },
      vertexShader: 'varying float vy; void main(){ vy = position.y; gl_Position = projectionMatrix*modelViewMatrix*vec4(position,1.); }',
      fragmentShader: 'uniform vec3 c; uniform float t; uniform float hgt; varying float vy; void main(){ float f = 1.0 - clamp(vy/hgt,0.,1.); gl_FragColor = vec4(c, pow(f,1.6)*0.85*(0.75+0.25*sin(t*2.0))); }'
    }));
    return m;
  }

  window.WalkbackScenes = {
    C: C,
    register: function (name, factory) { factories[name] = factory; },
    factory: function (name) { return factories[name]; },
    names: function () { return Object.keys(factories); },
    helpers: { makeRenderer: makeRenderer, tex: tex, pillar: pillar }
  };
})();
