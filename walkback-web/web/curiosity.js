// Written with Codex (AI), directed and tested by team MARSXR
import { Box3, Group, MathUtils, Vector3 } from "three";
import { GLTFLoader } from "three/addons/loaders/GLTFLoader.js";

// Accept an array of { x, y, headingDeg? } points in local site metres or { points: [...] }.
export function pickTraversePoint(traverse, sideMeters = 1000) {
  const points = Array.isArray(traverse) ? traverse : traverse?.points;
  if (!Array.isArray(points) || !Number.isFinite(sideMeters) || sideMeters <= 0) return null;

  return points.find((point) => point
    && Number.isFinite(point.x) && Number.isFinite(point.y)
    && point.x >= 0 && point.x <= sideMeters
    && point.y >= 0 && point.y <= sideMeters) ?? null;
}

// Heading is clockwise from north (negative Z). The returned group sits at ground level.
export async function addCuriosity(
  scene,
  toWorld,
  { x, y, headingDeg = 0, vex = 2, url = "data/curiosity.glb" },
) {
  try {
    const { scene: model } = await new GLTFLoader().loadAsync(new URL(url, import.meta.url).href);
    const rover = new Group();
    rover.add(model);
    const bounds = new Box3().setFromObject(rover, true);
    const size = bounds.getSize(new Vector3());
    if (![size.x, size.y, size.z].every((axis) => Number.isFinite(axis) && axis > 0)) {
      throw new Error("The model has no usable 3D bounds.");
    }

    // Center the wheelbase footprint and bring the lowest point to the ground.
    const center = bounds.getCenter(new Vector3());
    model.position.sub(center.setY(bounds.min.y));
    rover.scale.set(2.7 / size.x, 2.2 * vex / size.y, 3 / size.z);
    // The NASA asset faces positive Z. Turn its nose north before applying the heading.
    rover.rotation.y = Math.PI - MathUtils.degToRad(headingDeg);
    rover.position.copy(toWorld(x, y));
    scene.add(rover);
    return rover;
  } catch (error) {
    console.warn("Curiosity could not be loaded.", error);
    return null;
  }
}
