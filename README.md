# Walkback

**The safest way to go far.**

Walkback is a mixed-reality prototype for Meta Quest 3 that tells an astronaut on Mars, at every step, whether they can still make it back to the habitat. Meta Quest 3 stands in for the helmet display.

Built by team MARSXR for the NASA International Space Apps Challenge 2026, Santa Cruz (Bolivia) local event.

> NASA does not endorse this project. No NASA logo or insignia is used.
> Built with AI assistance (Claude Code): see [AI_USAGE.md](AI_USAGE.md).

---

## What the app does today

- **Real Mars on the table.** A 1 km × 1 km square of Gale crater (centered at 4.807° S, 137.381° E), built from a 1 m HiRISE elevation model and the 25 cm HiRISE photo mosaic, sits on a table in the room (mixed reality, passthrough).
- **Four map layers:** PHOTO (the HiRISE photo on the relief), HEIGHT, SLOPE (ground over 20° hatched as no-go) and RETURN (minutes back to the habitat from every point of the map, over the relief).
- **Four stops**, all real Curiosity drill sites at the rover's own positions: Altadena, Valle de la Luna, Nevado Sajama and Campo Marte, each with a stop card (science, tasks with minutes, risk).
- **Route and verdict.** The app plans habitat → stops in order → habitat over the relief and shows a verdict card (VIABLE / NOT VIABLE / NO SAFE ROUTE) with three charts: oxygen tank (used / spare / reserve), time (walking and work), and height along the route.
- **WHAT IF.** The left stick click (or key F) re-plans with the tank at 15 %, then at 8 %, then back at 100 %. The verdict changes and the RETURN layer paints red the ground you can no longer walk back from. On this site (ESTIMATE): at 15 % the verdict is NOT VIABLE but nothing is red yet; at 8 %, 57.5 % of the ground that has a way back on foot is red.
- **Globe and 360° photo.** A Mars globe (Viking colour mosaic) to land on the site, and Curiosity's Mastcam 360° panorama at Nevado Sajama (the 360° PHOTO button on that stop's card; the other three stops have no panorama and their cards say so).
- **Helmet visor (HUD):** compass tape, heading, an arrow to the next stop, an oxygen bar with the reserve mark, minutes back to the habitat in green / amber / red, a two-beep alert when TURN BACK NOW comes on, a 2D corner map and a 3D wrist map that can be left in the air (X).
- **Mission mode:** START MISSION on the verdict card takes you onto Mars at 1:1 scale (the same terrain mesh and photo), with the route on the ground, a pillar and an arrival card at each stop, jumps to the next stop (NEXT STOP sign, or left grip) and a room view (the visor and the route over the real floor). *Built and covered by the automatic test; not yet confirmed in the headset (see the status table).*
- **Current conditions card.** The latest Curiosity REMS weather report (sol 4995, Earth date 2026-08-25), season, daylight, surface radiation and the week's space weather, each with its source and date. The card is stored in the app. One line can be newer: at start, if the headset has internet, the app asks NASA DONKI once for the solar particle events of the last 7 days (public DEMO_KEY, 4 s limit) and marks that line "(live)" with the time of the check; with no usable answer it keeps the stored line, marked "(stored in the app)". The weather report is never downloaded live, and **nothing on this card feeds the oxygen estimate.**

### Every number is an estimate

The walking and oxygen model of the team is not in the app yet. The app uses a stand-in ("plan B") and says ESTIMATE next to its numbers on screen: under the verdict (*ESTIMATE: plan B over the relief, not the walking model yet*), in the chart titles, on the visor and in the RETURN layer's legend.

Today's mission plan (ESTIMATE: plan B over the relief, not the walking model yet):

| Quantity | Value (ESTIMATE) |
|---|---|
| Route length | 1.8 km (1812 m) — ESTIMATE |
| Walking time | 43 min — ESTIMATE |
| Work at the four stops | 2 h 12 min — ESTIMATE |
| Total time | 2 h 55 min — ESTIMATE |
| Oxygen used | 181 L of O₂ = 36 % of the tank — ESTIMATE |
| Spare above the reserve | 285 L — ESTIMATE |

How plan B works: each leg is the fastest way over the relief (Dijkstra on the elevation grid, pace by Tobler's hiking function normalised to a flat-ground pace, no step on ground over 20°), smoothed with straight shortcuts where they stay walkable; walking spends oxygen per minute walked, work at a stop spends it at the suit's design rate; the verdict is VIABLE only if, at every stop, the oxygen above the reserve still covers the way back. The constants and where they come from are listed in [NOTICE](NOTICE) (section "Walking and oxygen model").

---

## Status: what was seen in the headset

| Piece | Automatic test (no headset) | Seen in the Quest 3 |
|---|---|---|
| Gale terrain and the four layers | Passes | Yes (14 Nov, earlier build, 72 fps) |
| Pointer, table, globe and jump into the 360° photo | Passes | Yes (14 Nov, earlier build, 72 fps) |
| Verdict board and charts | Passes | Yes (14 Nov, earlier build); the START MISSION button: not yet |
| Helmet HUD, corner map, wrist map left in the air | Passes | Yes, inside the 360° photo; in mission mode: not yet |
| Route over the relief (plan B) | Passes | Not yet |
| Mission mode (Mars at 1:1, jumps, room view) | Passes | **Not yet** |

This table is what the team saw up to the evening of 14 Nov (the 18:26 build). The builds made after it (later layout and mission-mode fixes) had not been confirmed in the headset when this file was written.

---

## NASA and other data used

All data in the app comes from public sources; each one is named on screen and in [NOTICE](NOTICE). Full details, commands and checks: [DATA.md](DATA.md).

| What | Source | Credit |
|---|---|---|
| Elevation, 1 m | MRO HiRISE Gale Crater DEM 1 m (`Gale_DEM_SMG_1m`), NASA Mars Trek | NASA/JPL Mars Trek; HiRISE images NASA/JPL/University of Arizona |
| Base photo, 25 cm | MSL Gale Merged Orthophoto Mosaic 25 cm v3 | Calef and Parker (2016), USGS Astrogeology; HiRISE images NASA/JPL/University of Arizona |
| Curiosity's positions (the stops' places and the slope check; the path itself is not drawn in the app) | MSL waypoints | NASA/JPL-Caltech |
| Stop science and names | NASA Curiosity blog (sols 4568–4569, 4695–4701, 4716–4722, 4893–4899, 4900–4907) | NASA/JPL-Caltech |
| Stop task minutes | Proposed from NASA/TP-2017-219457, table 30 | NASA |
| Weather | Curiosity REMS weather feed, sol 4995 | NASA/JPL-Caltech/CAB (CSIC-INTA) |
| Space weather | NASA DONKI (SEP, GST, CME), stored answer for 7–14 Nov 2026; at start the app can read the last 7 days of SEP live | NASA CCMC |
| Surface radiation | 0.64 mSv/day, Curiosity RAD | Hassler et al. (2014), *Science* 343; NASA/JPL-Caltech/SwRI |
| Globe | Viking MDIM 2.1 colour mosaic | NASA/JPL/USGS |
| 360° panorama | Mastcam mosaic PIA26696, sols 4714–4741 | NASA/JPL-Caltech/MSSS |

How the terrain was checked: the slopes of the Mars Trek DEM differ from Curiosity's own tilt sensor by 2.0° on average (r = 0.77, largest difference 21°) on 85 rover positions (DATA.md, "Checks"). That supports the terrain; it does not validate the oxygen numbers.

---

## Known limits

- **No sand map.** At 1 m the elevation model does not show the sandy boxwork hollows, so the app treats the whole square as rock. The app does not say this on screen; `site.json` and DATA.md do.
- **Small obstacles are not seen.** The route grid is about 3.9 m; rocks and steps smaller than that do not appear (on screen: "Rocks under about 4 m are not on this map").
- **Weather is old and stored.** The card shows the latest REMS report available when the data was built: sol 4995 (Earth date 2026-08-25), 78 sols old on 14 Nov 2026, the day the data was built; the card keeps reading "78 sols old" on later days. The weather report is never downloaded when the app runs; only the space-weather line can be read live at start (see the current conditions card above).
- **The habitat is a proposal.** Its position (on Curiosity's own path, its sol 4580 position) was chosen by the team; there is no habitat there.
- **The walking pace is from Earth.** Tobler's hiking function was measured on Earth without a suit; plan B has no suit metabolic model, no sand and no gravity term.
- **Absolute heights** of the Mars Trek DEM read about 17 m higher than the USGS DEM and the rover; slopes, routes and times do not depend on that offset.
- The two NASA 3D models in `Assets/Walkback/Models3D/` are not shown in the app (they need the glTFast package, not installed).
- Only Nevado Sajama has a 360° panorama; the other three stop cards say "No panorama for this stop yet".

---

## Install on a Meta Quest 3

1. Download `walkback.apk` from this repository's [Releases page](https://github.com/kendal-dev/Walkback/releases).
2. Enable developer mode on the headset, connect it by USB and run `adb install -r walkback.apk`.
3. Open *Walkback* from *Unknown sources* in the app library.

Controls in the app are listed on the CONTROLS card next to the table.

## Build from source

Use exactly these versions (newer ones were not tested):

| Tool | Version |
|---|---|
| Unity | 6000.4.6f1 |
| Meta XR SDK | 209.0.0 |
| OpenXR Plugin | 1.16.1 |
| Universal Render Pipeline | 17.4.0 |
| Input System | 1.19.0 |

1. Install [Git LFS](https://git-lfs.com/), then clone and run `git lfs pull` (the elevation and 3D models are stored with LFS).
2. Open the project with Unity 6000.4.6f1 and Android build support.
3. The scene is generated by code: menu *Walkback > Build Scene*. Do not edit `Walkback.unity` by hand.
4. Build the APK: menu *Walkback > Build APK* (Development build, output `Builds/walkback.apk`), or from the command line:
   `Unity.exe -batchmode -force-d3d11 -projectPath . -buildTarget Android -executeMethod Walkback.EditorTools.BuildApk.Run -logFile TestOutput/build.log -quit`

## Automatic test (no headset)

The project carries its own test (`Assets/Walkback/Editor/AutoTest*.cs`): it builds the scene, loads the Gale package, runs the route, verdict, HUD, panel, pointer, globe and mission-mode checks and saves screenshots in `TestOutput/`. Last count: **424 checks, all passing (15 Nov 2026, 01:33)**.

- Unity open: menu *Walkback > Run Auto Test*.
- Unity closed: `Unity.exe -batchmode -projectPath . -executeMethod Walkback.EditorTools.AutoTest.RunBatch -logFile TestOutput/unity.log`

## Repository layout

| Path | What |
|---|---|
| `Assets/Walkback/` | The app (scripts, materials, editor tools and the automatic test) |
| `Assets/StreamingAssets/Site/` | The Gale site package read by the app (see DATA.md) |
| `data-pipeline/` | Scripts that build the site package from the NASA and USGS sources |
| `pitch/presentacion/` | The presentation for the local defense (in Spanish) |
| `DATA.md` | Data sources, build commands and checks |
| `CLAUDE.md` | Internal working notes and instructions for the AI coding sessions |

---

## Team MARSXR

Team MARSXR, NASA International Space Apps Challenge 2026, Santa Cruz (Bolivia).

## License

Copyright 2026 Team MARSXR. Licensed under the Apache License, Version 2.0: see [LICENSE](LICENSE). Third-party data, packages and assets keep their own terms: see [NOTICE](NOTICE).

NASA does not endorse this project.
