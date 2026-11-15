// Written with Codex (AI), directed and tested by team MARSXR
import { spawnSync } from "node:child_process";
import { existsSync } from "node:fs";
import { mkdtemp, open, readdir, rename, rm, stat } from "node:fs/promises";
import { dirname, join, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const web = fileURLToPath(new URL("../", import.meta.url));
const data = join(web, "data");
const output = join(data, "curiosity.glb");
const maxBytes = 10_000_000;

async function findSource(path) {
  if (!(await stat(path)).isDirectory()) return path;

  const entries = await readdir(path, { withFileTypes: true });
  const models = entries.filter((entry) => entry.isFile() && /curiosity.*\.glb$/i.test(entry.name));
  if (models.length !== 1) {
    throw new Error(`Expected one Curiosity GLB in ${path}; pass the original file as an argument.`);
  }
  return join(path, models[0].name);
}

function optimize(input, target) {
  const cli = process.env.GLTF_TRANSFORM_CLI;
  const npxArgs = ["--yes", "@gltf-transform/cli@4.5.0"];
  let command = process.execPath;
  let args;
  if (cli) {
    // Use a local CLI entry point for preparation without Internet access.
    args = [resolve(cli)];
  } else if (process.platform === "win32") {
    // Run the npm entry point directly so Windows shell syntax in paths stays literal.
    const npx = join(dirname(process.execPath), "node_modules", "npm", "bin", "npx-cli.js");
    if (!existsSync(npx)) throw new Error("Set GLTF_TRANSFORM_CLI to the glTF Transform CLI entry point.");
    args = [npx, ...npxArgs];
  } else {
    command = "npx";
    args = npxArgs;
  }
  const result = spawnSync(command, [
    ...args, "optimize", input, target,
    "--compress", "quantize",
    "--weld", "true",
    "--simplify-ratio", "0.65",
    "--simplify-error", "0.001",
    "--texture-compress", "webp",
    "--texture-size", "1024",
  ], { stdio: "inherit" });
  if (result.error) throw result.error;
  if (result.status !== 0) throw new Error("glTF Transform could not optimize the model.");
}

async function main() {
  const input = await findSource(resolve(process.argv[2] ?? "Assets/Walkback/Models3D/"));
  if (input.toLowerCase() === output.toLowerCase()) {
    throw new Error("Pass the original GLB as input. The output is web/data/curiosity.glb.");
  }
  const source = await open(input, "r");
  try {
    const header = Buffer.alloc(12);
    const { bytesRead } = await source.read(header);
    if (bytesRead !== 12 || header.toString("ascii", 0, 4) !== "glTF" || header.readUInt32LE(4) !== 2) {
      throw new Error("The source must be a GLB 2.0 file. For Git LFS assets, retrieve the actual model first.");
    }
  } finally {
    await source.close();
  }

  const work = await mkdtemp(join(data, ".curiosity-"));
  try {
    const candidate = join(work, "curiosity.glb");
    optimize(input, candidate);
    const { size } = await stat(candidate);
    if (size >= maxBytes) throw new Error(`Optimized model is ${size} bytes; it must be under ${maxBytes}.`);
    await rename(candidate, output);
    console.log(`Wrote ${output} (${size} bytes).`);
  } finally {
    await rm(work, { recursive: true, force: true });
  }
}

main().catch((error) => {
  console.error(`Curiosity preparation failed: ${error.message}`);
  process.exitCode = 1;
});
