// Written with Antigravity (AI), directed and tested by team MARSXR
//
// Pre-publish check for Walkback static web site (no external dependencies).
// Usage: node web/tools/check_web.mjs [path-to-web-or-walkback-root]

import { execFileSync } from "node:child_process";
import { closeSync, existsSync, openSync, readdirSync, readFileSync, readSync, statSync } from "node:fs";
import { dirname, extname, join, relative, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const MAX_FILE_BYTES = 10 * 1024 * 1024;
const MAX_TOTAL_BYTES = 50 * 1024 * 1024;
const HM_SIZE = 513;
const GRID_SIZE = 257;
const HM_BYTES = HM_SIZE * HM_SIZE * 2;
const RET_BYTES = GRID_SIZE * GRID_SIZE * 8;
const AUTHORSHIP_TEXT = "directed and tested by team MARSXR";
const LFS_PREFIX = "version https://git-lfs.github.com/spec/";

function resolveTargets(arg) {
  const defaultWeb = resolve(dirname(fileURLToPath(import.meta.url)), "..");
  if (!arg) return { webDir: defaultWeb, repoDir: resolve(defaultWeb, "..") };
  const given = resolve(arg);
  const nestedWeb = join(given, "web");
  if (!existsSync(join(given, "index.html")) && existsSync(join(nestedWeb, "index.html"))) {
    return { webDir: nestedWeb, repoDir: given };
  }
  return { webDir: given, repoDir: resolve(given, "..") };
}

const { webDir: WEB, repoDir: REPO } = resolveTargets(process.argv[2]);
const toPosix = (p) => p.replaceAll("\\", "/");
const relToWeb = (p) => toPosix(relative(WEB, p));
const relToRepo = (p) => toPosix(relative(REPO, p));
const normCmp = (p) => (process.platform === "win32" ? resolve(p).toLowerCase() : resolve(p));

function walkFiles(dir, out = []) {
  for (const entry of readdirSync(dir, { withFileTypes: true })) {
    const abs = join(dir, entry.name);
    if (entry.isDirectory()) {
      walkFiles(abs, out);
    } else if (entry.isFile()) {
      out.push({ abs, rel: relToWeb(abs), size: statSync(abs).size });
    }
  }
  return out;
}

function isOutsideVendor(relPath) {
  return relPath !== "vendor" && !relPath.startsWith("vendor/");
}

function isExternalUrl(url) {
  return /^(?:https?:)?\/\//i.test(url.trim());
}

function isNonFileUri(url) {
  const u = url.trim();
  return !u || /^(?:data:|blob:|mailto:|tel:|javascript:|#)/i.test(u);
}

function cleanQueryHash(url) {
  return url.trim().split(/[?#]/, 1)[0];
}

function stripHtmlComments(src) {
  return src.replace(/<!--[\s\S]*?-->/g, (m) => m.replace(/[^\n]/g, " "));
}

function stripCssComments(src) {
  let out = "";
  let i = 0;
  let quote = null;
  while (i < src.length) {
    const ch = src[i];
    if (quote) {
      out += ch;
      if (ch === "\\" && i + 1 < src.length) {
        out += src[++i];
      } else if (ch === quote) {
        quote = null;
      }
      i++;
      continue;
    }
    if (ch === "'" || ch === '"') {
      quote = ch;
      out += ch;
      i++;
      continue;
    }
    if (ch === "/" && src[i + 1] === "*") {
      const end = src.indexOf("*/", i + 2);
      const stop = end === -1 ? src.length : end + 2;
      out += src.slice(i, stop).replace(/[^\n]/g, " ");
      i = stop;
      continue;
    }
    out += ch;
    i++;
  }
  return out;
}

function stripJsComments(src) {
  let out = "";
  let i = 0;
  let quote = null;
  const stack = []; // entries: { kind: "tpl", startIdx: number, hasExpr: boolean } or { kind: "expr", depth: number }

  while (i < src.length) {
    const ch = src[i];
    const top = stack[stack.length - 1];

    if (quote) {
      out += ch;
      if (ch === "\\" && i + 1 < src.length) {
        out += src[++i];
      } else if (ch === quote) {
        quote = null;
      }
      i++;
      continue;
    }

    if (top?.kind === "tpl") {
      if (ch === "\\" && i + 1 < src.length) {
        out += " ";
        out += src[i + 1] === "\n" ? "\n" : " ";
        i += 2;
        continue;
      }
      if (ch === "$" && src[i + 1] === "{") {
        top.hasExpr = true;
        stack.push({ kind: "expr", depth: 1 });
        out += "  ";
        i += 2;
        continue;
      }
      if (ch === "`") {
        stack.pop();
        if (top.hasExpr) {
          out = out.slice(0, top.startIdx) + " " + out.slice(top.startIdx + 1) + " ";
        } else {
          out += ch;
        }
        i++;
        continue;
      }
      out += ch === "\n" ? "\n" : " ";
      i++;
      continue;
    }

    if (ch === "'" || ch === '"') {
      quote = ch;
      out += ch;
      i++;
      continue;
    }

    if (ch === "`") {
      // Preserve simple template literals (no ${}) and static external-URL prefixes like `https://.../${x}`.
      const rest = src.slice(i + 1);
      const end = rest.indexOf("`");
      if (end !== -1 && !rest.slice(0, end).includes("${") && !rest.slice(0, end).includes("\n")) {
        out += src.slice(i, i + 1 + end + 1);
        i += 1 + end + 1;
        continue;
      }
      if (/^(?:https?:)?\/\//i.test(rest.trimStart())) {
        const prefix = rest.match(/^[^`$]+/)?.[0] || "https://";
        out += `"${prefix.replace(/"/g, "")}"`;
      }
      stack.push({ kind: "tpl", startIdx: out.length, hasExpr: false });
      out += ch;
      i++;
      continue;
    }

    if (top?.kind === "expr") {
      if (ch === "{") {
        top.depth++;
      } else if (ch === "}") {
        top.depth--;
        if (top.depth === 0) {
          stack.pop();
          out += " ";
          i++;
          continue;
        }
      }
    }

    if (ch === "/" && src[i + 1] === "/") {
      const nl = src.indexOf("\n", i + 2);
      const stop = nl === -1 ? src.length : nl;
      out += " ".repeat(stop - i);
      i = stop;
      continue;
    }

    if (ch === "/" && src[i + 1] === "*") {
      const end = src.indexOf("*/", i + 2);
      const stop = end === -1 ? src.length : end + 2;
      out += src.slice(i, stop).replace(/[^\n]/g, " ");
      i = stop;
      continue;
    }

    out += ch;
    i++;
  }
  return out;
}

function parseImportMap(htmlNoComments) {
  const combined = { imports: {}, scopes: {} };
  const maps = [];
  const re = /<script\b([^>]*)>([\s\S]*?)<\/script>/gi;
  for (const m of htmlNoComments.matchAll(re)) {
    const attrs = m[1];
    if (!/\btype\s*=\s*(?:"importmap"|'importmap'|importmap\b)/i.test(attrs)) continue;
    const raw = m[2].trim();
    if (!raw) continue;
    try {
      const json = JSON.parse(raw);
      maps.push(json);
      if (json.imports && typeof json.imports === "object") Object.assign(combined.imports, json.imports);
      if (json.scopes && typeof json.scopes === "object") Object.assign(combined.scopes, json.scopes);
    } catch (err) {
      maps.push({ __parseError: err.message });
    }
  }
  return { combined, maps };
}

function resolveImportSpecifier(specifier, importerDir, importMap) {
  if (specifier.startsWith("node:")) return { builtin: true };
  if (specifier.startsWith("./") || specifier.startsWith("../")) {
    return { path: resolve(importerDir, cleanQueryHash(specifier)) };
  }
  if (specifier.startsWith("/")) {
    return { path: resolve(WEB, cleanQueryHash(specifier).slice(1)) };
  }
  const imports = importMap?.imports || {};
  if (Object.hasOwn(imports, specifier)) {
    return { mappedTarget: imports[specifier] };
  }
  let bestPrefix = "";
  for (const key of Object.keys(imports)) {
    if (key.endsWith("/") && specifier.startsWith(key) && key.length > bestPrefix.length) {
      bestPrefix = key;
    }
  }
  if (bestPrefix) {
    return { mappedTarget: imports[bestPrefix] + specifier.slice(bestPrefix.length) };
  }
  return { unmapped: true };
}

function extractCssUrls(cssClean) {
  const urls = [];
  for (const m of cssClean.matchAll(/url\(\s*(?:"([^"]*)"|'([^']*)'|([^)"'\s]+))\s*\)/gi)) {
    urls.push(m[1] ?? m[2] ?? m[3] ?? "");
  }
  for (const m of cssClean.matchAll(/@import\s+(?:"([^"]+)"|'([^']+)')/gi)) {
    urls.push(m[1] ?? m[2] ?? "");
  }
  return urls;
}

function extractJsModuleSpecs(jsClean) {
  const specs = [];
  for (const m of jsClean.matchAll(/(?:^|[;{}\r\n])\s*(?:import|export)\s+(?:[^;"'`]*?\s+from\s+)?["']([^"']+)["']/gm)) {
    specs.push(m[1]);
  }
  for (const m of jsClean.matchAll(/\bimport\s*\(\s*["']([^"']+)["']\s*\)/g)) {
    specs.push(m[1]);
  }
  return specs;
}

function extractJsRuntimeUrls(jsClean) {
  const urls = [];
  for (const m of jsClean.matchAll(/\b(?:fetch|getJSON|getBin|loadImage)\s*\(\s*["'`]([^"'`]+)["'`]/g)) {
    urls.push(m[1]);
  }
  for (const m of jsClean.matchAll(/\.src\s*=\s*["'`]([^"'`]+)["'`]/g)) {
    urls.push(m[1]);
  }
  return urls;
}

function checkLocalTarget(targetPath, originLabel, orthoPathNorm, brokenRefs, warnings) {
  if (normCmp(targetPath) === orthoPathNorm) {
    if (!existsSync(targetPath)) {
      warnings.push(`${originLabel}: optional file data/ortho.jpg is missing`);
    }
    return;
  }
  if (!existsSync(targetPath)) {
    brokenRefs.push(`${originLabel} -> missing ${relToWeb(targetPath)}`);
  }
}

function runInternetAndRefChecks(allFiles) {
  const externalErrors = [];
  const brokenRefs = [];
  const warnings = [];
  const orthoPathNorm = normCmp(join(WEB, "data", "ortho.jpg"));

  const codeFiles = allFiles.filter(
    (f) => isOutsideVendor(f.rel) && [".html", ".js", ".mjs", ".css"].includes(extname(f.rel).toLowerCase()),
  );

  // Load default importmap from web/index.html if present.
  let defaultImportMap = { imports: {}, scopes: {} };
  const indexHtmlPath = join(WEB, "index.html");
  if (existsSync(indexHtmlPath)) {
    const indexClean = stripHtmlComments(readFileSync(indexHtmlPath, "utf8"));
    defaultImportMap = parseImportMap(indexClean).combined;
  }

  for (const file of codeFiles) {
    const ext = extname(file.rel).toLowerCase();
    const raw = readFileSync(file.abs, "utf8");
    const fileDir = dirname(file.abs);

    if (ext === ".html") {
      const clean = stripHtmlComments(raw);
      const { combined: htmlImportMap, maps } = parseImportMap(clean);

      for (const mapObj of maps) {
        if (mapObj.__parseError) {
          brokenRefs.push(`${file.rel}: invalid importmap JSON (${mapObj.__parseError})`);
          continue;
        }
        const entries = [
          ...Object.entries(mapObj.imports || {}),
          ...Object.values(mapObj.scopes || {}).flatMap((s) => (s && typeof s === "object" ? Object.entries(s) : [])),
        ];
        for (const [k, v] of entries) {
          if (typeof v !== "string") continue;
          if (isExternalUrl(k) || isExternalUrl(v)) {
            externalErrors.push(`${file.rel}: importmap points outside (${k} -> ${v})`);
          } else if (!isNonFileUri(v)) {
            const target = resolve(fileDir, cleanQueryHash(v));
            if (v.endsWith("/")) {
              if (!existsSync(target) || !statSync(target).isDirectory()) {
                brokenRefs.push(`${file.rel}: importmap prefix "${k}" -> missing directory ${relToWeb(target)}`);
              }
            } else {
              checkLocalTarget(target, `${file.rel} (importmap "${k}")`, orthoPathNorm, brokenRefs, warnings);
            }
          }
        }
      }

      // Any src="..." or src='...' or unquoted src=...
      for (const m of clean.matchAll(/<([a-zA-Z0-9:-]+)\b[^>]*?\bsrc\s*=\s*(?:"([^"]*)"|'([^']*)'|([^\s>]+))/gi)) {
        const val = m[2] ?? m[3] ?? m[4] ?? "";
        if (isExternalUrl(val)) {
          externalErrors.push(`${file.rel}: <${m[1]} src="${val}"> loads an external resource`);
        } else if (!isNonFileUri(val)) {
          const target = resolve(fileDir, cleanQueryHash(val));
          checkLocalTarget(target, `${file.rel} (<${m[1]} src="${val}">)`, orthoPathNorm, brokenRefs, warnings);
        }
      }

      // <link ... href="..."> (allow <a href="...">)
      for (const m of clean.matchAll(/<link\b[^>]*>/gi)) {
        const tag = m[0];
        const hm = tag.match(/\bhref\s*=\s*(?:"([^"]*)"|'([^']*)'|([^\s>]+))/i);
        if (!hm) continue;
        const val = hm[1] ?? hm[2] ?? hm[3] ?? "";
        if (isExternalUrl(val)) {
          externalErrors.push(`${file.rel}: <link href="${val}"> loads an external resource`);
        } else if (!isNonFileUri(val)) {
          const target = resolve(fileDir, cleanQueryHash(val));
          checkLocalTarget(target, `${file.rel} (<link href="${val}">)`, orthoPathNorm, brokenRefs, warnings);
        }
      }

      // Inline <style>
      for (const m of clean.matchAll(/<style\b[^>]*>([\s\S]*?)<\/style>/gi)) {
        const cssClean = stripCssComments(m[1]);
        for (const u of extractCssUrls(cssClean)) {
          if (isExternalUrl(u)) {
            externalErrors.push(`${file.rel} (inline style): external url(${u})`);
          } else if (!isNonFileUri(u)) {
            checkLocalTarget(resolve(fileDir, cleanQueryHash(u)), `${file.rel} (inline style url(${u}))`, orthoPathNorm, brokenRefs, warnings);
          }
        }
      }

      // Inline <script> (non-importmap)
      const activeMap = Object.keys(htmlImportMap.imports).length ? htmlImportMap : defaultImportMap;
      for (const m of clean.matchAll(/<script\b([^>]*)>([\s\S]*?)<\/script>/gi)) {
        if (/\btype\s*=\s*(?:"importmap"|'importmap'|importmap\b)/i.test(m[1])) continue;
        const jsClean = stripJsComments(m[2]);
        for (const spec of extractJsModuleSpecs(jsClean)) {
          if (isExternalUrl(spec)) {
            externalErrors.push(`${file.rel} (inline script): external import "${spec}"`);
            continue;
          }
          const res = resolveImportSpecifier(spec, fileDir, activeMap);
          if (res.builtin) continue;
          if (res.unmapped) {
            brokenRefs.push(`${file.rel} (inline script): unmapped import "${spec}"`);
          } else if (res.mappedTarget) {
            if (!isExternalUrl(res.mappedTarget)) {
              const baseDir = Object.keys(htmlImportMap.imports).length ? fileDir : WEB;
              checkLocalTarget(resolve(baseDir, cleanQueryHash(res.mappedTarget)), `${file.rel} (import "${spec}")`, orthoPathNorm, brokenRefs, warnings);
            }
          } else if (res.path) {
            checkLocalTarget(res.path, `${file.rel} (import "${spec}")`, orthoPathNorm, brokenRefs, warnings);
          }
        }
        for (const u of extractJsRuntimeUrls(jsClean)) {
          if (isExternalUrl(u)) {
            externalErrors.push(`${file.rel} (inline script): external resource "${u}"`);
          } else if (!isNonFileUri(u)) {
            checkLocalTarget(resolve(fileDir, cleanQueryHash(u)), `${file.rel} (inline script "${u}")`, orthoPathNorm, brokenRefs, warnings);
          }
        }
      }
    } else if (ext === ".css") {
      const clean = stripCssComments(raw);
      for (const u of extractCssUrls(clean)) {
        if (isExternalUrl(u)) {
          externalErrors.push(`${file.rel}: external url(${u})`);
        } else if (!isNonFileUri(u)) {
          checkLocalTarget(resolve(fileDir, cleanQueryHash(u)), `${file.rel} (url(${u}))`, orthoPathNorm, brokenRefs, warnings);
        }
      }
    } else if (ext === ".js" || ext === ".mjs") {
      const clean = stripJsComments(raw);
      for (const spec of extractJsModuleSpecs(clean)) {
        if (isExternalUrl(spec)) {
          externalErrors.push(`${file.rel}: external module import "${spec}"`);
          continue;
        }
        const res = resolveImportSpecifier(spec, fileDir, defaultImportMap);
        if (res.builtin) continue;
        if (res.unmapped) {
          brokenRefs.push(`${file.rel}: unmapped module import "${spec}"`);
        } else if (res.mappedTarget) {
          if (!isExternalUrl(res.mappedTarget)) {
            checkLocalTarget(resolve(WEB, cleanQueryHash(res.mappedTarget)), `${file.rel} (import "${spec}")`, orthoPathNorm, brokenRefs, warnings);
          }
        } else if (res.path) {
          checkLocalTarget(res.path, `${file.rel} (import "${spec}")`, orthoPathNorm, brokenRefs, warnings);
        }
      }

      const isBrowserScript = ext === ".js" && !file.rel.startsWith("tools/");
      for (const u of extractJsRuntimeUrls(clean)) {
        if (isExternalUrl(u)) {
          externalErrors.push(`${file.rel}: external resource "${u}"`);
        } else if (isBrowserScript && !isNonFileUri(u)) {
          const base = u.startsWith("./") || u.startsWith("../") ? fileDir : WEB;
          checkLocalTarget(resolve(base, cleanQueryHash(u)), `${file.rel} ("${u}")`, orthoPathNorm, brokenRefs, warnings);
        }
      }
    }
  }

  return { externalErrors, brokenRefs, warnings };
}

function checkAuthorship(allFiles) {
  const failures = [];
  const targets = allFiles
    .filter((f) => isOutsideVendor(f.rel) && [".html", ".js", ".mjs", ".css"].includes(extname(f.rel).toLowerCase()))
    .map((f) => ({ abs: f.abs, label: f.rel, ext: extname(f.rel).toLowerCase() }));

  const workflowPath = join(REPO, ".github", "workflows", "pages.yml");
  if (existsSync(workflowPath)) {
    targets.push({ abs: workflowPath, label: relToRepo(workflowPath), ext: ".yml" });
  }

  for (const item of targets) {
    const content = readFileSync(item.abs, "utf8").replace(/^\uFEFF/, "");
    const firstLine = (content.split(/\r?\n/, 1)[0] || "").trim();
    const isComment =
      (item.ext === ".html" && firstLine.startsWith("<!--")) ||
      (item.ext === ".css" && firstLine.startsWith("/*")) ||
      ((item.ext === ".js" || item.ext === ".mjs") && (firstLine.startsWith("//") || firstLine.startsWith("/*"))) ||
      (item.ext === ".yml" && firstLine.startsWith("#"));

    if (!isComment || !firstLine.includes(AUTHORSHIP_TEXT)) {
      failures.push(`${item.label}: line 1 must be a comment containing "${AUTHORSHIP_TEXT}"`);
    }
  }
  return { count: targets.length, failures };
}

function readHeaderLatin1(absPath, maxBytes = 64) {
  const fd = openSync(absPath, "r");
  try {
    const buf = Buffer.alloc(maxBytes);
    const n = readSync(fd, buf, 0, maxBytes, 0);
    return buf.subarray(0, n).toString("latin1");
  } finally {
    closeSync(fd);
  }
}

function checkLfsPointersAndSizes(allFiles) {
  const errors = [];
  let totalBytes = 0;

  for (const file of allFiles) {
    totalBytes += file.size;
    if (file.size <= 1024 && readHeaderLatin1(file.abs).startsWith(LFS_PREFIX)) {
      errors.push(`${file.rel} is a Git LFS pointer`);
    }
    if (file.size > MAX_FILE_BYTES) {
      errors.push(`${file.rel} exceeds 10 MB (${(file.size / 1048576).toFixed(2)} MB)`);
    }
  }
  if (totalBytes > MAX_TOTAL_BYTES) {
    errors.push(`web/ total size exceeds 50 MB (${(totalBytes / 1048576).toFixed(2)} MB)`);
  }

  const top10 = [...allFiles].sort((a, b) => b.size - a.size).slice(0, 10);
  return { errors, totalBytes, top10 };
}

function checkGitLfs() {
  let gitRoot;
  try {
    gitRoot = execFileSync("git", ["rev-parse", "--show-toplevel"], {
      cwd: WEB,
      encoding: "utf8",
      stdio: ["ignore", "pipe", "pipe"],
    }).trim();
  } catch {
    return { status: "warn", message: "Not inside a git repository; skipping git lfs ls-files check." };
  }

  let out;
  try {
    out = execFileSync("git", ["lfs", "ls-files", "-n"], {
      cwd: gitRoot,
      encoding: "utf8",
      stdio: ["ignore", "pipe", "pipe"],
    });
  } catch {
    return { status: "warn", message: "git-lfs is unavailable or failed; skipping git lfs ls-files check." };
  }

  const webRel = toPosix(relative(resolve(gitRoot), WEB));
  const webPrefix = webRel && webRel !== "." ? `${webRel}/` : "";
  const cmp = (s) => (process.platform === "win32" ? s.toLowerCase() : s);
  const hits = out
    .split(/\r?\n/)
    .map((line) => toPosix(line.trim()))
    .filter(Boolean)
    .filter((p) => (!webPrefix ? !p.startsWith("../") : cmp(p).startsWith(cmp(webPrefix)) || cmp(p) === cmp(webRel)));

  if (hits.length) {
    return { status: "fail", hits };
  }
  return { status: "pass" };
}

function isFiniteNum(v) {
  return typeof v === "number" && Number.isFinite(v);
}

function isNonEmptyStr(v) {
  return typeof v === "string" && v.trim().length > 0;
}

function checkData() {
  const errors = [];
  const dataDir = join(WEB, "data");

  // 1. site.json
  const sitePath = join(dataDir, "site.json");
  if (!existsSync(sitePath)) {
    errors.push("missing data/site.json");
  } else {
    try {
      const s = JSON.parse(readFileSync(sitePath, "utf8"));
      if (!isNonEmptyStr(s.name)) errors.push("site.json: missing or empty name");
      if (!isFiniteNum(s.centerLat)) errors.push("site.json: centerLat must be a finite number");
      if (!isFiniteNum(s.centerLon)) errors.push("site.json: centerLon must be a finite number");
      if (!isFiniteNum(s.sideMeters) || s.sideMeters <= 0) errors.push("site.json: sideMeters must be > 0");
      if (s.heightmapSize !== HM_SIZE) errors.push(`site.json: heightmapSize is ${s.heightmapSize}, expected ${HM_SIZE}`);
      if (s.gridSize !== GRID_SIZE) errors.push(`site.json: gridSize is ${s.gridSize}, expected ${GRID_SIZE}`);
      if (!isFiniteNum(s.minElevation)) errors.push("site.json: minElevation must be a finite number");
      if (!isFiniteNum(s.maxElevation)) errors.push("site.json: maxElevation must be a finite number");
      if (isFiniteNum(s.minElevation) && isFiniteNum(s.maxElevation) && s.maxElevation <= s.minElevation) {
        errors.push("site.json: maxElevation must be > minElevation");
      }
      if (!s.habitat || typeof s.habitat !== "object" || !isFiniteNum(s.habitat.x) || !isFiniteNum(s.habitat.y)) {
        errors.push("site.json: habitat must have finite x and y");
      } else if (s.habitat.x < 0 || s.habitat.x > 1000 || s.habitat.y < 0 || s.habitat.y > 1000) {
        errors.push(`site.json: habitat (${s.habitat.x}, ${s.habitat.y}) outside 0..1000`);
      }
      if (!isNonEmptyStr(s.crs)) errors.push("site.json: missing or empty crs");
      if (!Array.isArray(s.sources) || s.sources.length === 0) {
        errors.push("site.json: sources must be a non-empty array");
      } else {
        s.sources.forEach((src, i) => {
          if (!src || typeof src !== "object") {
            errors.push(`site.json: sources[${i}] is not an object`);
            return;
          }
          for (const field of ["layer", "mission", "instrument", "resolution", "id", "credit"]) {
            if (!isNonEmptyStr(src[field])) errors.push(`site.json: sources[${i}].${field} missing or empty`);
          }
        });
      }
    } catch (err) {
      errors.push(`data/site.json invalid JSON: ${err.message}`);
    }
  }

  // 2. heightmap.bin
  const hmPath = join(dataDir, "heightmap.bin");
  if (!existsSync(hmPath)) {
    errors.push("missing data/heightmap.bin");
  } else {
    const size = statSync(hmPath).size;
    if (size !== HM_BYTES) {
      errors.push(`data/heightmap.bin is ${size} bytes, expected ${HM_BYTES} (${HM_SIZE}x${HM_SIZE}x2)`);
    }
  }

  // 3. return_map.bin (optional)
  const retPath = join(dataDir, "return_map.bin");
  if (existsSync(retPath)) {
    const size = statSync(retPath).size;
    if (size !== RET_BYTES) {
      errors.push(`data/return_map.bin is ${size} bytes, expected ${RET_BYTES} (${GRID_SIZE}x${GRID_SIZE}x8)`);
    }
  }

  // 4. stops.json
  const stopsPath = join(dataDir, "stops.json");
  if (!existsSync(stopsPath)) {
    errors.push("missing data/stops.json");
  } else {
    try {
      const stops = JSON.parse(readFileSync(stopsPath, "utf8"));
      if (!Array.isArray(stops)) {
        errors.push("stops.json: root must be an array");
      } else {
        stops.forEach((st, i) => {
          if (!st || typeof st !== "object") {
            errors.push(`stops.json[${i}]: must be an object`);
            return;
          }
          const validId = isNonEmptyStr(st.id) || isFiniteNum(st.id);
          if (!validId) errors.push(`stops.json[${i}]: missing id`);
          if (!isNonEmptyStr(st.name)) errors.push(`stops.json[${i}]: missing name`);
          if (!isFiniteNum(st.x) || !isFiniteNum(st.y)) {
            errors.push(`stops.json[${i}]: x and y must be finite numbers`);
          } else if (st.x < 0 || st.x > 1000 || st.y < 0 || st.y > 1000) {
            errors.push(`stops.json[${i}] (${st.id}): coordinates (${st.x}, ${st.y}) outside 0..1000`);
          }
        });
      }
    } catch (err) {
      errors.push(`data/stops.json invalid JSON: ${err.message}`);
    }
  }

  return errors;
}

function checkReadiness() {
  const warnings = [];
  const statusPath = join(WEB, "data", "status.json");
  if (existsSync(statusPath)) {
    try {
      const status = JSON.parse(readFileSync(statusPath, "utf8"));
      if (status?.test === true) {
        warnings.push("STILL TEST DATA (web/data/status.json has \"test\": true)");
      }
    } catch (err) {
      warnings.push(`web/data/status.json could not be parsed (${err.message})`);
    }
  }

  const indexPath = join(WEB, "index.html");
  const todos = [];
  if (existsSync(indexPath)) {
    const lines = readFileSync(indexPath, "utf8").split(/\r?\n/);
    lines.forEach((line, idx) => {
      if (line.includes("TODO") || line.includes("aria-disabled")) {
        todos.push(`index.html:${idx + 1}: ${line.trim()}`);
      }
    });
  }
  return { warnings, todos };
}

function fmtBytes(bytes) {
  if (bytes >= 1048576) return `${(bytes / 1048576).toFixed(2)} MB`;
  if (bytes >= 1024) return `${(bytes / 1024).toFixed(1)} KB`;
  return `${bytes} B`;
}

function main() {
  if (!existsSync(WEB)) {
    console.error(`✖ web directory not found: ${WEB}`);
    process.exit(1);
  }

  const allFiles = walkFiles(WEB);
  let hasErrors = false;
  const printStatus = (icon, label, items = []) => {
    console.log(`${icon} ${label}`);
    for (const item of items) console.log(`    - ${item}`);
  };

  const { externalErrors, brokenRefs, warnings: refWarnings } = runInternetAndRefChecks(allFiles);
  if (externalErrors.length) {
    hasErrors = true;
    printStatus("✖", `No internet (${externalErrors.length} external resource(s))`, externalErrors);
  } else {
    printStatus("✔", "No internet (all resources are local)");
  }

  if (brokenRefs.length) {
    hasErrors = true;
    printStatus("✖", `References (${brokenRefs.length} broken reference(s))`, brokenRefs);
  } else if (refWarnings.length) {
    printStatus("⚠", "References (all required files found, optional warnings)", refWarnings);
  } else {
    printStatus("✔", "References (all local references exist)");
  }

  const { count: authCount, failures: authFailures } = checkAuthorship(allFiles);
  if (authFailures.length) {
    hasErrors = true;
    printStatus("✖", `Authorship comment (${authFailures.length} missing out of ${authCount})`, authFailures);
  } else {
    printStatus("✔", `Authorship comment (${authCount} files verified)`);
  }

  const { errors: sizeErrors, totalBytes, top10 } = checkLfsPointersAndSizes(allFiles);
  if (sizeErrors.length) {
    hasErrors = true;
    printStatus("✖", `LFS pointers & sizes (total ${fmtBytes(totalBytes)})`, sizeErrors);
  } else {
    printStatus("✔", `LFS pointers & sizes (${allFiles.length} files, total ${fmtBytes(totalBytes)} / 50 MB limit)`);
  }
  console.log("  Top 10 heaviest files in web/:");
  for (const f of top10) {
    console.log(`    ${fmtBytes(f.size).padStart(9)}  ${f.rel}`);
  }

  const gitLfs = checkGitLfs();
  if (gitLfs.status === "fail") {
    hasErrors = true;
    printStatus("✖", `Git LFS (${gitLfs.hits.length} file(s) under web/ tracked by LFS)`, gitLfs.hits);
  } else if (gitLfs.status === "warn") {
    printStatus("⚠", `Git LFS (${gitLfs.message})`);
  } else {
    printStatus("✔", "Git LFS (no files under web/ listed by git lfs ls-files)");
  }

  const dataErrors = checkData();
  if (dataErrors.length) {
    hasErrors = true;
    printStatus("✖", `Data contract (${dataErrors.length} error(s))`, dataErrors);
  } else {
    printStatus("✔", "Data contract (site.json, heightmap.bin, return_map.bin, stops.json)");
  }

  const { warnings: readyWarnings, todos } = checkReadiness();
  if (readyWarnings.length || todos.length) {
    const items = [...readyWarnings, ...todos];
    printStatus("⚠", "Publish readiness", items);
  } else {
    printStatus("✔", "Publish readiness (real data, no TODO/aria-disabled left in index.html)");
  }

  console.log(hasErrors ? "\nFAILED: fix ✖ items before publishing." : "\nOK: ready (only ⚠ advisories, if any).");
  process.exit(hasErrors ? 1 : 0);
}

main();
