// Written with Claude Code (AI), directed and tested by team MARSXR
//
// Hero: the team's Mars globe (data/mars.glb) turning slowly. The CSS planet
// stays as a placeholder until the model is ready, and remains if it fails.

import * as THREE from "three";
import { GLTFLoader } from "three/addons/loaders/GLTFLoader.js";
import { OrbitControls } from "three/addons/OrbitControls.js";

const stage = document.getElementById("hero-stage");
const touch = window.matchMedia("(pointer: coarse)").matches;
const still = window.matchMedia("(prefers-reduced-motion: reduce)").matches;

async function start() {
  const gltf = await new GLTFLoader().loadAsync("data/mars.glb");
  const mars = gltf.scene;

  // Centre and scale the globe to radius 1, whatever units the file uses.
  const sphere = new THREE.Box3().setFromObject(mars).getBoundingSphere(new THREE.Sphere());
  mars.position.sub(sphere.center);
  const globe = new THREE.Group();
  globe.add(mars);
  globe.scale.setScalar(1 / sphere.radius);
  globe.rotation.z = THREE.MathUtils.degToRad(25); // Mars axial tilt

  const renderer = new THREE.WebGLRenderer({ antialias: true, alpha: true });
  renderer.setPixelRatio(Math.min(window.devicePixelRatio, 2));
  renderer.setClearColor(0x000000, 0);

  const scene = new THREE.Scene();
  scene.add(globe);
  scene.add(new THREE.AmbientLight(0xffe6d8, 0.35));
  const sun = new THREE.DirectionalLight(0xfff2e6, 3.2);
  sun.position.set(-3, 1.5, 2.5);
  scene.add(sun);
  const rim = new THREE.DirectionalLight(0xff7a45, 1.2);
  rim.position.set(3, -1, -2);
  scene.add(rim);

  const camera = new THREE.PerspectiveCamera(30, 1, 0.1, 100);
  camera.position.set(0, 0, 4.2);

  // Desktop: drag to turn the globe. Phones: no controls, so the page still scrolls.
  let controls = null;
  if (!touch) {
    controls = new OrbitControls(camera, renderer.domElement);
    controls.enableZoom = false;
    controls.enablePan = false;
    controls.enableDamping = true;
    renderer.domElement.style.cursor = "grab";
  }

  stage.append(renderer.domElement);
  const resize = () => {
    const s = stage.clientWidth;
    renderer.setSize(s, s, false);
  };
  resize();
  new ResizeObserver(resize).observe(stage);

  // Only render while the hero is on screen (saves battery on phones).
  let visible = true;
  new IntersectionObserver(([e]) => { visible = e.isIntersecting; }).observe(stage);

  const clock = new THREE.Clock();
  renderer.setAnimationLoop(() => {
    if (!visible) return;
    const dt = clock.getDelta();
    if (!still) globe.rotateY(dt * 0.12);
    controls?.update();
    renderer.render(scene, camera);
  });
  stage.classList.add("has-model");
}

start().catch((err) => console.warn("Mars model not loaded:", err));
