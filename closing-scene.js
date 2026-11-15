// Closing-slide visual: AI-created astronaut cutout over the real Curiosity panorama.
// Static astronaut and terrain; the complete visual plane pans horizontally.
window.createWalkbackAstronautScene = function (canvas, options) {
  const { makeRenderer, texFromDataUrl, reduced, offset } = options;
  if (!window.WALKBACK_ASTRONAUT_TEXTURE || !window.SAJAMA_TEX) return null;
  const scene = new THREE.Scene();
  let needsRender = true;
  const renderer = makeRenderer(canvas, canvas.width, canvas.height);
  renderer.setClearColor(0x000000, 0);
  renderer.shadowMap.enabled = true;
  renderer.shadowMap.type = THREE.PCFSoftShadowMap;
  // Keep the NASA photograph's colours intact and separate from the composited foreground.
  canvas.style.backgroundImage = 'url("' + window.SAJAMA_TEX + '")';
  canvas.style.backgroundSize = '100% 100%';
  canvas.style.backgroundPosition = 'center';

  const sun = new THREE.DirectionalLight(0xfff1dc, 1);
  sun.position.set(-3, 7, 4);
  sun.target.position.set(2.4, 0.7, 0);
  sun.castShadow = true;
  sun.shadow.mapSize.set(2048, 2048);
  Object.assign(sun.shadow.camera, { left: -4, right: 4, top: 4, bottom: -4, near: 0.1, far: 24 });
  sun.shadow.bias = -0.00008;
  sun.shadow.normalBias = 0.005;
  sun.shadow.radius = 3;
  scene.add(sun, sun.target);

  // Alpha-tested silhouette casts a grounded directional shadow; no dark disk under the boots.
  const ground = new THREE.Mesh(new THREE.PlaneGeometry(60, 60),
    new THREE.ShadowMaterial({ opacity: 0.28, depthWrite: false }));
  ground.rotation.x = -Math.PI / 2;
  ground.position.y = -0.004;
  ground.receiveShadow = true;
  scene.add(ground);

  const height = 1.96;
  const geometry = new THREE.PlaneGeometry(height * 1024 / 1536, height);
  geometry.translate(0, height / 2 - height * 16 / 1536, 0);
  const texture = texFromDataUrl(window.WALKBACK_ASTRONAUT_TEXTURE, () => {
    canvas.dataset.astronautReady = 'true';
    needsRender = true;
  });
  const material = new THREE.MeshBasicMaterial({ map: texture, transparent: true,
    alphaTest: 0.04, depthWrite: true, toneMapped: false, side: THREE.DoubleSide });
  const astronaut = new THREE.Mesh(geometry, material);
  astronaut.position.set(2.42, 0, 0);
  astronaut.castShadow = true;
  astronaut.frustumCulled = false;
  astronaut.customDepthMaterial = new THREE.MeshDepthMaterial({ depthPacking: THREE.RGBADepthPacking,
    map: texture, alphaTest: 0.04, side: THREE.DoubleSide });
  scene.add(astronaut);

  const camera = new THREE.PerspectiveCamera(32, canvas.width / canvas.height, 0.1, 100);
  camera.position.set(0.3, 2.1, 8.6);
  camera.lookAt(0.2, 0.96, -10);

  // Move the rendered scene and its background as one plane. No body deformation,
  // camera orbit, walking frames, or relative motion between the boots and the ground.
  canvas.style.transformOrigin = '50% 50%';
  canvas.style.willChange = reduced ? 'auto' : 'transform';
  let t0 = 0, sampleTime = null, panX = 0;
  function pan(time) {
    const progress = Math.max(0, Math.min(1, time / 40));
    const eased = 0.5 - 0.5 * Math.cos(Math.PI * progress);
    panX = reduced ? 0 : -48 + 96 * eased;
    // Fixed overscan hides the frame edges; only horizontal translation animates.
    canvas.style.transform = `translateX(${panX}px) scale(1.08)`;
  }
  pan(0);
  return {
    start() { t0 = performance.now() - offset; sampleTime = null; },
    seek(seconds) { sampleTime = seconds; },
    inspect() { return { panX, position: astronaut.position.toArray(), rotation: astronaut.rotation.toArray().slice(0,3), video: false }; },
    frame(now) {
      pan(sampleTime === null ? Math.max(0,(now - t0) / 1000) : sampleTime);
      if (needsRender) { renderer.render(scene, camera); needsRender = false; }
    },
  };
};
