# Walkback — CLAUDE.md

**Walkback** — *The safest way to go far.* NASA Space Apps Challenge 2026, Santa Cruz (Bolivia) local event, team MARSXR. A mixed-reality tool for Meta Quest 3 that tells an astronaut on Mars, at every step, whether they can still make it back to the habitat. *Meta Quest 3 stands in for the helmet display.*

Deadlines: code freeze Sun 15/11 10:00 · submit 11:00 · the venue closes submissions at 12:00 · defense 14:00 (5 minutes of pure presentation).

## If you are a new session picking up session A (the app) — start here

**Which model (Daniel, 14/11 10:50): save Fable's limit for the very important things.**

- **Opus 5.5 is the default for session A from now on.** The recipe is written and the first piece is in the repository: follow its style (`SceneBuilder.cs`, `AutoTest.cs`, `MissionPanel.cs`). Use Opus for: terrain and layers, the panel, pointer, table, globe and jump, commits, pushes, reading logs, fixing compile errors, updating this file.
- **Fable 5.2 only for:** (1) wiring C's model into the app (route, return map, verdict: a wrong number on screen loses the defense), (2) mission mode and the MR ↔ VR button (guion §22.9), (3) a bug Opus could not close in two tries, (4) the last check before the freeze on Sunday. Close the Fable session when that piece is done.
- Check Claude usage at noon and at 17:00; if Fable is above 70 % of its limit, everything goes to Opus.

**Where things stand (14/11, 17:10):**

1. **`main` on GitHub has every piece** (last push 18:05, round 11). The automatic test passed 388 of 388 at 18:02. History rewritten without AI co-author lines at 12:57 (see below).
2. **Session A was not running at 12:35.** The session that did the data (session B, "Gale boxwork DEM analysis") took over A's role: it reviewed, tested and committed the four agents' pieces and wrote the helmet HUD.
3. **B's site package is on `main`:** real Gale from the Mars Trek HiRISE DEM (its slopes match Curiosity's own tilt sensor within 2.0° on 85 positions; the USGS v3 DEM was worse, 6.2°: `DATA.md`), the HiRISE 25 cm photo, two stops at the rover's drill positions, Curiosity's path, current conditions (REMS sol 4995, DONKI), the Viking globe, the Nevado Sajama 360° photo, two NASA 3D models (they need glTFast, not installed).
4. **App pieces done:** terrain with four layers (PHOTO first: the HiRISE photo on the relief; HEIGHT, SLOPE, RETURN), panel, pillars, pointer and table, globe and jump, helmet HUD with the 2D corner map and the 3D wrist map. **Plan B from a Fable 5.2 audit, applied 15:20** (patches 0001-0003, 0006, 0008-0010, one commit each): the RETURN layer is the fastest way back over the relief (`ReturnFallback`: Dijkstra, ficha A's Tobler pace normalised to 19.3 min/km, no step over 20°); `MissionFallback` plans habitat -> stops -> habitat over the relief and gives the verdict card VIABLE / NOT VIABLE / NO SAFE ROUTE with time, oxygen and what is spare; key F or the left stick click is the WHAT IF (tank at 15 % and 8 %: red where you no longer walk back from); the HUD's way back reads the same return map. Everything says ESTIMATE / PLACEHOLDER on screen. Patch 0005 (table and globe in front of the user at start, headset only) applied 15:50 at Daniel's request: still to check in the headset (start facing 90° and 180° away). Not applied: 0004 (measure the landing fps first).
5. **Mission mode is in (16:20):** Fable 5.2's patches 0011 (`MissionProgress`: legs, jumps that count as walked, the visor's numbers from the headset) and 0012 (`MissionMode`: Mars at 1:1 with the terrain's own mesh and photo, the route on the ground, the pillar and the arrival card, the fade curtain `Walkback/Fade`, the room view). Local fix: the jump lands 5 m from the stop in a straight line (the grid route zigzags; it gave 1.84 m). Decisions for Daniel are in the vault prompt file, round 10. **Fable's round 3 applied 17:05** (patches 0013-0022, one commit each): fade into and out of the 360° photo; panel buttons 100 mm tall and the stops in a 2×2 grid; the short ESTIMATE line on the visor (below the cards in mission mode, above them in a 360° photo); a CONTROLS card next to the table; heading arrow on the "you" mark of both maps; a two-beep alert (880 Hz) when TURN BACK NOW comes on; text in light gray 0.92; pillar labels that cover each other go up; the plan's route smoothed with straight shortcuts over walkable ground (1887 m -> 1812 m). Local fix: the shortcuts read the ground every quarter grid step, and the wall test's wall now crosses the straight line (it ended on it and left a 0.07 m detour). **Fable's round 11 written straight into the repository (18:02, three commits, 388 of 388):** (a) the wrist map comes off the wrist: X (left controller, or the X key) leaves it in the air where the hand is, 40 cm wide and fixed in the room; in mission mode it lies flat with its north on Mars' north; X again takes it back (the old "X keeps it on the wrist" is gone); (b) the visor: a compass tape between the two top cards with an amber bar at the next stop's bearing, the heading as "042° NE", an arrow to the next stop next to the NEXT card, the stop's bearing and the height difference from the relief on the NEXT card, an oxygen bar with the reserve mark, the minutes back in green / amber / red (`HudEstimate.StateOf`: amber when the oxygen above the reserve covers the way back less than twice, a proposal) with the oxygen margin under them; inside a 360° photo north is not known, so the compass, the heading and the arrow hide; (c) the main board is 1.50 × 1.00 m (bottom edge still at 1.15 m) with the verdict's icon (check / cross / bar) and three charts that fill while the route draws itself: oxygen tank (used / spare / reserve, WHAT IF included), time (walking and work in order) and height along the route with the stops (`PlanCharts`, `MissionCharts`, `ChartGraphic`). All of it reads the plan B numbers and says ESTIMATE. **Next (Fable pieces):** when C's model lands (`Assets/Walkback/Model/`, still on no branch at 15:20), replace `ReturnFallback`, `MissionFallback` and `HudEstimate` with it; then mission mode and the MR ↔ VR button.
6. **Data at 15:40:** four stops, all Curiosity drill sites at the rover's positions, checked against the NASA blog: Altadena (200 m from the habitat), Valle de la Luna, Nevado Sajama, Campo Marte (716 m, 77 m higher). Mission (after round 3's shortcuts, 17:00): 1.8 km (1812 m), 43 min walking, 2 h 12 min of work, 181 L of O₂ = 36 % of the tank, 285 L spare over the reserve (estimate; the pitch and README still say 1.9 km / 44 min / 37 %: session C must update them). C's model: `origin/model` still has nothing at 17:05. THEMIS: Astropedia did not answer. Project settings of the working build (hand tracking on) committed 15:25; `DevAgentSettings.asset` untracked because the SDK writes a token into it (the old token is in the history: treat it as burned). **Waiting on Daniel:** the habitat (proposal), patches 0004/0005 in the headset, the product name "Walkback" (Player Settings; it may change the app id), the `pitch` branch (another session's worktree, still on the old history), README/NOTICE/AI_USAGE drafts from the audit (session C's files, with TODOs).

7. Running the test with Unity open: the trigger file can be picked up *before* Unity recompiles new code, so the first run after an edit may test the old scripts. Give Unity focus, wait for the compile, then create `Temp/walkback-run-test` again and check the report's time. During a player build the runner says NO UNITY (the window title changes): wait.
   **Never start a player build while a session is in the middle of a piece:** at 17:58 a build started with half-written scripts on disk failed with "script class layout is incompatible between the editor and the player", and after it the editor did not recompile by itself (the test kept running the 17:50 assemblies and passing). Sign of it: the report's check count does not change after an edit. Cure: change any script (a comment is enough) and run the test again; then build.
8. Unity closed by itself at 10:42 with a GPU error (`d3d12: device removed`, 887a0005) right after importing scripts. Not caused by the code. If it repeats, tell Daniel and start Unity with `-force-d3d11`.
9. Still unseen in the Quest: everything after the first build (Daniel's build at 13:44 has the HUD and the photo). Read `adb logcat` yourself.

**Parallel burst (Daniel, 14/11 11:20: "abrí aunque sea 3 agentes que cada uno toque partes diferentes"), started 11:35:**

- Session B (data) runs as its own session in this folder (it had not started before). It commits only its own paths and never pushes.
- Four background agents of session A write one piece each, only in their own files: **panel** (`MissionPanel.cs`, `Panel*.cs`, `Conditions.cs`), **route view and pillars** (`Route*.cs`, `StopPillars.cs`, `Pillar*.cs`), **pointer and table** (`Pointer*.cs` except `PointerTarget.cs`, `TableControls.cs`), **globe and jump** (`Globe*.cs`, `SiteJump*.cs`); plus each one's `WalkbackApp.<Piece>.cs`, `SceneBuilder.<Piece>.cs` and `AutoTest.<Piece>.cs`. They do not commit; session A reviews, tests and commits each piece.
- Shared files stay with session A: `WalkbackApp.cs` (hooks, `SelectStop`, `RequestJump`, `MapControlsLocked`), `SceneBuilder.cs`, `AutoTest.cs` (partial classes with one hook per piece), `SiteData.cs` (site, stops), `PointerTarget.cs` (`IPointerTarget`), `GaleTerrain.cs`, `TerrainLayers.cs`, `WalkbackTheme.cs`, `Tools/`.
- **Running the test while several agents share Unity:** `powershell -NoProfile -ExecutionPolicy Bypass -File Tools/run-autotest.ps1` takes a lock in `Temp/`, gives Unity focus, prints compile errors and the report. The report's first check says whether the scripts compiled; if not, it tested the old scripts.

**Pushes and commits (Daniel, 14/11 10:48: "automatizá hasta los push y commits"):** session A commits and pushes `main` by itself after each piece that passes the automatic test. This replaces "only Daniel pushes" for session A's own work. Still ask Daniel before: force-pushing, deleting branches, merging `model`, `docs` or `web` into `main`, making the repository public, tagging the release.

**Commits carry no AI co-author line (Daniel, 14/11 ~12:55: "borra los commits viejos… lo pondremos en datos de la NASA en el formulario"):** no `Co-Authored-By: Claude` in any commit. Daniel's global `commit-msg` hook strips it and is switched on again here (`git config --local hook.strip-ai-attribution.enabled true`). At 12:57 the history of `main`, `model`, `docs` and `web` was rewritten without that line (same files, new hashes) and force-pushed with Daniel's request; the old hashes are kept locally in `refs/original/`. Anyone with an older clone: `git fetch origin` and `git reset --hard origin/<branch>` (no local commits) or `git rebase --onto origin/<branch> <old base> <branch>`. AI use is still declared (rule 3): bitácora §1, `AI_USAGE.md` and the AI field of the submission form.

## Read before writing anything

1. The handoff, from its first line: `C:\Users\kevin\Segundo_Cerebro\31_CARRERA\04_EVENTOS\SPACE-APPS-2026\space-apps-2026-traspaso.md`
2. The Saturday script, the main file: `...\space-apps-2026-guion-del-sabado.md` — its top notices, §22 entire (it rules), §13.6 and §6, then what your session's prompt names (§22.7).
3. Decisions: `...\space-apps-2026-continuidad.md` §4 (team) and §5 (decisions taken).
4. Log: `...\space-apps-2026-bitacora.md` §1 (AI use, with time) and §3 (organizer answers).

`...` = `C:\Users\kevin\Segundo_Cerebro\31_CARRERA\04_EVENTOS\SPACE-APPS-2026`. The vault has its own rules (`C:\Users\kevin\Segundo_Cerebro\CLAUDE.md`): one fact, one home; nothing is shortened (old text is marked 🗄️); every organizer answer carries a name and a date. Read them before editing the vault.

## Four rules

1. The terrain and every number on screen come from real data with a source; nothing invented is shown as data.
2. Daniel directs and approves. Product ideas are the team's: what Claude proposes is marked as a proposal and Daniel decides.
3. Every AI use is logged (bitácora §1) and declared (`AI_USAGE.md` and the submission's AI field).
4. Say only what the app does.

## Who writes what (no two sessions write the same files)

| Session | Model · machine | Owns |
|---|---|---|
| A · the app | Opus 5.5 by default, Fable 5.2 for the pieces listed above · Daniel's laptop | `Assets/` except B's and C's folders · `CLAUDE.md` |
| B · the data | Opus 5.5 · Daniel's laptop | `data-pipeline/` · `DATA.md` · `Assets/StreamingAssets/Site/` · `Assets/Walkback/Models3D/` |
| C · the model and the pitch | Fable 5.2 · Orlando's laptop | `Assets/Walkback/Model/` · `model-tests/` · `docs/` · `pitch/` · `video/` · `README.md` · `AI_USAGE.md` |
| D · the web | Claude Code · Josué's laptop | `web/` · `.github/workflows/pages.yml` |

The data contract (`site.json`, `heightmap.r16`, `stops.json`…), the model interface (`WalkModel`, `RoutePlanner`, `ReturnMap`, `Verdict`, `SuitFailure`, `Probe`, `Mission`, `GridMapping`) and each session's prompt: guion §22.7. Need something in another session's folder → ask Daniel. Check Claude usage at noon.

## Team

- **Daniel**: directs, compiles, tests in the headset, presents. Session A pushes its own pieces to `main` (delegated by Daniel, see the top); every other push is Daniel's.
- **Orlando**: runs session C. When Daniel is away he approves Claude's permissions: yes to reading, compiling and testing inside the project; ask Daniel before deleting, pushing, installing or touching anything outside it. Handles the headset in the live demo.
- **Miranda**: the five-person tests (Sun 8:30) and photos.
- **Josué**: the web page (session D). His working doc: https://claude.ai/artifact/72XQbTwu35auhjCZe3MBbu

## Versions that work — do not update anything

Unity **6000.4.6f1** (the 2022.3 also installed is not used) · Meta XR SDK **209.0.0** · OpenXR **1.16.1** · URP **17.4.0** · Input System **1.19.0**. On the practice project: 72 fps locked on the Quest 3; first build ~11 min, later ones ~4 min.

## State on Saturday 14/11, 10:15

- **Project folder:** `C:\Users\kevin\walkback-NASA-Space-Apps`, created by Meta Quest Developer Hub (it opened in Unity at 10:07). `C:\Users\kevin\practica-spaceapps\Walkback` is a failed first attempt kept as a backup: don't use it. Nothing is copied from the Tupiza practice.
- **OpenXR step:** `Assets/Resources/OculusRuntimeSettings.asset` has `handSkeletonVersion: 1`. Confirm with Meta's Project Setup Tool on the first open.
- **Repository:** `github.com/kendal-dev/Walkback` already exists (Daniel created it: private, empty, description and topics set). Don't create it again. Plan in guion §13.6: `git init` · Unity `.gitignore` · `.gitattributes` with LFS for `*.r16 *.tif *.tiff *.glb *.fbx *.psd *.mp4` and the big site photos, then `web/** -filter -diff -merge` (GitHub Pages can't serve LFS) · `LICENSE` Apache 2.0 · `git remote add origin`. **Commit 1** = the project as the Developer Hub made it, without this file; **commit 2** = this `CLAUDE.md`. Branches: `main` (Daniel merges), `model` and `docs` (C), `web` (D). The repo goes public when Daniel says so.
- **Network:** the venue WiFi (5 GHz) doesn't reach `packages.unity.com` and reaches Meta's servers slowly. Missing packages → the offline kit `C:\Users\kevin\kit-sin-internet\` (its `LEEME.md` has the `manifest.json` lines) or the phone's hotspot.
- **Tools:** GDAL in `C:\Program Files\QGIS 3.44.15\bin` (not on PATH) · ffmpeg 9.0.1 · scrcpy 4.1 and its `adb` in `C:\Users\kevin\AppData\Local\Microsoft\WinGet\Packages\Genymobile.scrcpy_Microsoft.Winget.Source_8wekyb3d8bbwe\scrcpy-win64-v4.1\` · `gh` logged in as `kendal-dev`.
- **Laptop:** 16 GB of RAM for Unity plus several Claude sessions: close browsers and other apps before a build. Keep it plugged in (unplugging it closed Unity on 11/11).

## Status, piece by piece (update when a piece closes)

| Piece | Written | Compiles | Passes the test without headset | Seen in the Quest |
|---|---|---|---|---|
| Scene built by code + automatic test | ✅ | ✅ | ✅ 13:20, 246 checks | ⬜ |
| Gale terrain and layers (photo, height, slope, return) | ✅ | ✅ | ✅ 13:20, real Gale (return is a placeholder) | ⬜ |
| Route, return map, stops and pillars (with C's model) | Plan B route (smoothed, 1812 m) and return over the relief ✅; with C's model ⬜ | ✅ | ✅ 17:00, 336 checks | ⬜ |
| Panel: verdict with icon, charts (oxygen, time, height along the route), legend, conditions, stop cards, controls card | ✅ (plan B numbers, ESTIMATE) | ✅ | ✅ 18:02, 388 checks | ⬜ |
| Pointer, table, globe and jump | ✅ | ✅ | ✅ 13:20 | ⬜ |
| Helmet HUD (compass, arrow, oxygen bar, margin), 2D corner map, 3D wrist map that can be left in the air (X) | ✅ (numbers are estimates until C; heading arrow, TURN BACK beep) | ✅ | ✅ 18:02 | ⬜ |
| Mission mode + MR ↔ VR button | ✅ Fable patches 0011-0012 + jump fix (menu button / M: table → Mars → room → table; left grip / J: jump; B, Y / N: recenter) | ✅ | ✅ 17:00, 336 checks | ⬜ |

**The test without the headset** (`Assets/Walkback/Editor/AutoTest.cs`; report and screenshots in `TestOutput/`, not committed; ~50 s): Unity closed → `Unity.exe -batchmode -projectPath . -executeMethod Walkback.EditorTools.AutoTest.RunBatch -logFile TestOutput/unity.log` · Unity open → menu *Walkback > Run Auto Test*, or create the file `Temp/walkback-run-test` and the editor runs it by itself once its window has had focus. The scene is rebuilt by `Walkback > Build Scene` (`SceneBuilder.cs`): never edit `Walkback.unity` by hand. Since 11:56 the terrain on the table is B's real Gale package; without it the app falls back to the synthetic `TestTerrain`, labelled as not Mars data.

A piece is done when it compiles, passes the automatic test without the headset (with screenshots) and Daniel has seen it, in a screenshot or in the headset (guion §24).

## Milestones

H1 13:00 real Mars on the table · H2 16:30 the route with time, oxygen and verdict · H3 18:30 in the Quest · 20:00 P0 closed and installed · 20:00–23:30 mission mode · Sun 10:00 freeze, tag `v1.0-submission`, APK in Releases. The scene and its test were due at 9:30 and the project only opened at 10:07: if a milestone slips, propose the cut in guion §16.3 before going on.

## Code and commits

- Everything the jury sees: in English and readable on a projector (guion §18.1).
- First line of every script: `// Written with Claude Code (AI), directed and tested by team MARSXR`
- One commit per piece, message in English, without an AI co-author line (see "Commits carry no AI co-author line" above).
- Test without the headset before asking for a build; build at milestones. With Unity open, the batchmode test can't run: say so.
- Unity's C# has no `PriorityQueue` (.NET Standard 2.1): use your own heap.
- Read SDK names from the installed packages, never from memory.
- No NASA logos. Nothing copied from the practice.
- The app writes its own log file (the Quest keeps about one minute of logcat); build *Development* so logcat shows its lines.
- The 21 traps already stepped on: guion §7. Product decisions, the walking and oxygen model (continuidad §5; 1 L/min of O₂ ≈ 1,157 BTU/hr, never 238), the priority list P0–P2 (guion §17.3, §18.2, §20, §21) and the places (§21.2): read them there, don't copy them here.

## When something fails

- Unity: `%LOCALAPPDATA%\Unity\Editor\Editor.log`. Quest: `adb logcat`. **Read logs yourself; never ask Daniel to paste them.**
- Headset to screen: scrcpy; one eye: `--crop=1600:1100:232:554`.
- Build times and what was seen in the headset → bitácora §2 and the table above.

## Talking to Daniel

Simple Rioplatense Spanish, conclusion first, numbered steps. After each piece, three lines: what you did, how you tested it, what's missing. Run `Get-Date` before writing any time. At night, remind him to sleep.
