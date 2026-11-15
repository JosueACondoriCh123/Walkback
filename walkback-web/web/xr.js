// Written with Codex (AI), directed and tested by team MARSXR
import { Color, Fog, Vector3 } from "three";
import { VRButton } from "three/addons/webxr/VRButton.js";

const UP = new Vector3(0, 1, 0);
const MARS_SKY = 0xbe8a68;

// Keep the camera unparented and pass the same one to renderer.render().
// While renderer.xr.isPresenting, skip desktop OrbitControls and CSS2D updates.
// The world group sits directly under the scene, and toWorld gives positions inside it.
export function setupXR({ renderer, scene, camera, world, habitat, stops, toWorld, vex = 2, container }) {
  const xr = renderer.xr;
  xr.enabled = true;
  xr.setReferenceSpaceType("local-floor");

  const control = VRButton.createButton(renderer, { requiredFeatures: ["local-floor"] });
  let button = control;
  if (control.tagName === "A") {
    // VRButton uses an external information link when WebXR is unavailable.
    button = document.createElement("button");
    button.textContent = window.isSecureContext ? "VR NOT SUPPORTED" : "VR REQUIRES HTTPS";
    button.style.cssText = control.style.cssText;
    button.disabled = true;
  } else {
    const updateButton = () => {
      if (button.textContent === "ENTER VR") button.textContent = "Enter VR";
      if (button.textContent === "EXIT VR") button.textContent = "Exit VR";
      button.disabled = ["VR NOT SUPPORTED", "VR NOT ALLOWED"].includes(button.textContent);
    };
    const observer = new MutationObserver(updateButton);
    observer.observe(button, { childList: true });
    updateButton();
  }

  button.type = "button";
  Object.assign(button.style, {
    position: "static",
    maxWidth: "100%",
    minHeight: "44px",
    outline: "revert",
  });
  // End the active session without making a new immersive session offer on exit.
  button.addEventListener("click", (event) => {
    if (!xr.isPresenting) return;
    event.stopImmediatePropagation();
    xr.getSession().end().catch((error) => console.warn("Could not exit VR.", error));
  }, { capture: true });
  container.appendChild(button);

  let desktop;
  let poseRequest;
  xr.addEventListener("sessionstart", () => {
    desktop = {
      worldPosition: world.position.clone(),
      worldQuaternion: world.quaternion.clone(),
      worldScale: world.scale.clone(),
      cameraPosition: camera.position.clone(),
      cameraQuaternion: camera.quaternion.clone(),
      cameraScale: camera.scale.clone(),
      fov: camera.fov,
      zoom: camera.zoom,
      near: camera.near,
      projection: camera.projectionMatrix.clone(),
      projectionInverse: camera.projectionMatrixInverse.clone(),
      background: scene.background,
      fog: scene.fog,
    };

    const ground = toWorld(habitat.x, habitat.y);
    const firstStop = stops?.find((stop) => stop
      && Number.isFinite(stop.x) && Number.isFinite(stop.y)
      && (stop.x !== habitat.x || stop.y !== habitat.y));
    world.scale.y = 1 / vex;
    if (firstStop) {
      const direction = toWorld(firstStop.x, firstStop.y).sub(ground)
        .multiply(world.scale).applyQuaternion(world.quaternion);
      world.rotateOnWorldAxis(UP, Math.atan2(direction.x, -direction.z));
    }
    world.position.copy(ground).multiply(world.scale).applyQuaternion(world.quaternion).negate();
    world.updateMatrixWorld(true);

    scene.background = new Color(MARS_SKY);
    scene.fog = new Fog(MARS_SKY, 130, 1000);
    camera.near = 0.08;

    // Some runtimes start the viewer away from the local-floor origin. Recenter
    // horizontally and keep the height and subsequent motion supplied by the headset.
    const session = xr.getSession();
    const floor = xr.getReferenceSpace();
    function alignToViewer(_time, frame) {
      poseRequest = undefined;
      const pose = frame.getViewerPose(floor);
      if (!pose) {
        poseRequest = { session, id: session.requestAnimationFrame(alignToViewer) };
        return;
      }
      const { position, orientation } = pose.transform;
      const forward = new Vector3(0, 0, -1).applyQuaternion(orientation);
      const yaw = firstStop ? Math.atan2(-forward.x, -forward.z) : 0;
      const origin = new XRRigidTransform(
        { x: position.x, y: 0, z: position.z },
        { x: 0, y: Math.sin(yaw / 2), z: 0, w: Math.cos(yaw / 2) },
      );
      xr.setReferenceSpace(floor.getOffsetReferenceSpace(origin));
    }
    poseRequest = { session, id: session.requestAnimationFrame(alignToViewer) };
  });

  xr.addEventListener("sessionend", () => {
    if (poseRequest) {
      poseRequest.session.cancelAnimationFrame(poseRequest.id);
      poseRequest = undefined;
    }
    if (!desktop) return;

    world.position.copy(desktop.worldPosition);
    world.quaternion.copy(desktop.worldQuaternion);
    world.scale.copy(desktop.worldScale);
    world.updateMatrixWorld(true);
    camera.position.copy(desktop.cameraPosition);
    camera.quaternion.copy(desktop.cameraQuaternion);
    camera.scale.copy(desktop.cameraScale);
    camera.fov = desktop.fov;
    camera.zoom = desktop.zoom;
    camera.near = desktop.near;
    camera.projectionMatrix.copy(desktop.projection);
    camera.projectionMatrixInverse.copy(desktop.projectionInverse);
    camera.updateMatrixWorld(true);
    scene.background = desktop.background;
    scene.fog = desktop.fog;
    desktop = undefined;
  });

  return button;
}
