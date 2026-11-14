# DATA.md: the Gale site package

What the app reads from `Assets/StreamingAssets/Site/`, where each file comes from, the exact commands that make it, and the checks run on it. The raw NASA and USGS files are linked here and never committed (`data-pipeline/cache/` is ignored). Written by session B (data) of team MARSXR with Claude Code (AI); every step was run on 14 Nov 2026.

## The package (data contract: guion §22.7)

| File | What | Bytes | From |
|---|---|---|---|
| `site.json` | Name, center, 1000 m side, 513 heightmap, 257 grid, height range, habitat, CRS, one source per layer | 1 595 | `data-pipeline/sites/gale.json` + build |
| `heightmap.r16` | 513 x 513, uint16 little-endian, row 0 = north, height = min + v / 65535 x (max - min) | 526 338 | Elevation (see below) |
| `ortho.jpg` | Base photo, 4096 x 4096, 0.244 m/px, edge to edge of the square | 4 988 703 | HiRISE orthophoto, 25 cm |
| `sand.png` | 257 x 257, 255 = sandy hollow, 0 = rock. **All 0 for now** (plan B, see decisions) | 143 | Calculated from the elevation |
| `stops.json` | Valle de la Luna and Nevado Sajama: position, science (under 20 words), tasks with minutes, risk | 1 648 | Rover positions + NASA blog |
| `curiosity_traverse.json` | 85 rover positions inside the square, sols 4548 to 4914, `{sol, x, y}` | 4 586 | MSL waypoints |

Local coordinates: meters from the south-west corner, x east, y north. The square is centered on 4.807° S, 137.381° E; the projection is equirectangular on the Mars 2000 sphere (R = 3 396 190 m) with true scale at the center latitude.

## Sources

| Layer | Product | Link | Credit |
|---|---|---|---|
| Elevation (in use) | MRO HiRISE Gale Crater DEM 1 m, `Gale_DEM_SMG_1m` (NASA Mars Trek) | `https://trek.nasa.gov/mars/trekarcgis/rest/services/Gale_DEM_SMG_1m/ImageServer` | NASA/JPL Mars Trek; HiRISE NASA/JPL/University of Arizona |
| Elevation (reference, guion §5.1) | MSL Gale Merged DEM 1m v3 | `https://planetarymaps.usgs.gov/mosaic/Mars/MSL/MSL_Gale_DEM_Mosaic_1m_v3.tif` (3.87 GB) | Parker and Calef (2016), USGS Astrogeology |
| Base photo | MSL Gale Merged Orthophoto Mosaic 25cm v3 | `https://planetarymaps.usgs.gov/mosaic/Mars/MSL/MSL_Gale_Orthophoto_Mosaic_25cm_v3.tif` (24.4 GB) | Calef and Parker (2016), USGS Astrogeology; HiRISE NASA/JPL/University of Arizona |
| Curiosity positions | MSL waypoints (one point per drive) | `https://mars.nasa.gov/mmgis-maps/MSL/Layers/json/MSL_waypoints.json` | NASA/JPL-Caltech |
| Stop science | Curiosity blog, sols 4695-4701 (13 Nov 2025) and sols 4716-4722 (18 Nov 2025) | `science.nasa.gov/blog/` | NASA/JPL-Caltech |
| Stop task times | Proposed from NASA/TP-2017-219457, table 30 | | NASA |

## How it is built

GDAL 3.13.3 from QGIS (`C:\Program Files\QGIS 3.44.15\bin`, not on PATH). In Git Bash, prefix GDAL calls with `MSYS_NO_PATHCONV=1`. Only the window of the square is read from the servers, never the whole file.

1. **Elevation, Mars Trek** (4.2 MB, 64 s): one `exportImage` request for the window at 1 m.
   `curl -o data-pipeline/cache/marstrek/gale_dem_window.tif "https://trek.nasa.gov/mars/trekarcgis/rest/services/Gale_DEM_SMG_1m/ImageServer/exportImage?bbox=137.37253,-4.81544,137.38947,-4.79856&bboxSR=104905&size=1004,1001&format=tiff&pixelType=F32&f=image"`
2. **Elevation, USGS** (not finished: see decisions). The file is stored in strips one row high, LZW: the square needs about 1030 full rows of 33 km, ~80 MB.
   `gdal_translate -srcwin 14706 39638 1036 1028 /vsicurl/https://asc-pds-services.s3.us-west-2.amazonaws.com/mosaic/Mars/MSL/MSL_Gale_DEM_Mosaic_1m_v3.tif data-pipeline/cache/usgs/gale_dem_window.tif`
   A 60-row strip across the stops was read (`strip_rows40192_60.tif`, 0.24 MB) and is used for the alignment check.
3. **Base photo** (16.2 MB): the same window at 25 cm, 4026 x 4012 px.
   `gdal_translate -projwin 8142714 -284432 8143720.5 -285435 /vsicurl/https://asc-pds-services.s3.us-west-2.amazonaws.com/mosaic/Mars/MSL/MSL_Gale_Orthophoto_Mosaic_25cm_v3.tif data-pipeline/cache/ortho/gale_ortho_window.tif`
4. **Curiosity positions** (0.67 MB): `curl -o data-pipeline/cache/waypoints/MSL_waypoints.json https://mars.nasa.gov/mmgis-maps/MSL/Layers/json/MSL_waypoints.json`
5. **The package** (2 s):
   `python -I data-pipeline/build_site.py --site data-pipeline/sites/gale.json --dem-source marstrek --dem data-pipeline/cache/marstrek/gale_dem_window.tif --ortho data-pipeline/cache/ortho/gale_ortho_window.tif --waypoints data-pipeline/cache/waypoints/MSL_waypoints.json --habitat 514.8,777.0 --sand none --out Assets/StreamingAssets/Site --work data-pipeline/cache/work --report data-pipeline/reports/gale_controls.json`
   It warps the DEM to the 513 points (`average`), the photo to 4096 px (`cubic`, linear stretch 0.5-99.5 %), puts the stops at the rover's positions and writes the controls report. The same program serves any other site: a new `data-pipeline/sites/<site>.json` and its two windows.
6. **Checks**: `python -I data-pipeline/check_alignment.py <dem 1 m local> <usgs strip 1 m local>` and `python -I data-pipeline/preview_package.py Assets/StreamingAssets/Site <out prefix>` (photo, hillshade and slope side by side).

## Checks (14 Nov 2026, 11:56; full numbers in `data-pipeline/reports/gale_controls.json`)

| Check | Result |
|---|---|
| Mars Trek DEM against the USGS DEM, on the 60-row strip across the stops | Same place: best match at 2 m east, 1 m north (under one grid cell). Heights: Mars Trek is **17.9 m higher**; the difference varies 0.98 m (std) cell to cell |
| DEM against Curiosity's own elevation, 85 positions | Mars Trek - rover = 17.13 m mean, 1.04 m std. The USGS strip matches the rover within 0.3 m at both stops |
| The step from the hollow to the ridge (NASA: 10 m apart, 2 m higher) | USGS 2.80 m · Mars Trek 2.41 m · rover 2.71 m |
| Slope along the rover's path (a rover drove there) | Median 8.8°, 90 % under 13.1°, max 16.7° |
| Slope over the square | 53 % under 10°, 28 % 10-20°, 19 % over 20° (real scarps: the mesa to the west and the Mount Sharp slopes to the east, both visible in the photo) |
| Heightmap quantization | Under 1.74 mm |
| **Does the 1 m DEM show the boxwork hollows?** | **No.** Hollows from the DEM (more than 0.5 m below the 20 m mean) cover 3.2 % of the grid and are no darker in the photo (116.5 vs 117.8). At 1 m only 6.3 % of the dark ground in the photo is a DEM hollow (3.2 % by chance). The USGS strip does no better (r = 0.03 against the photo, Mars Trek 0.15) |
| Unity automatic test with this package (session A's test, 11:56) | 215 of 217. Every site check passes (contract, r16, sources, panel naming mission, instrument and resolution, pillars on the ground). The two failures are session A's: the panel data line wraps out of its one-line box, and the habitat pillar has no collider |

## Decisions (proposed by Claude; Daniel decides)

1. **Elevation from Mars Trek** (plan B of guion §5.1 and §5.6). The USGS server read 54 KB/s and then 5 KB/s; the square needs ~80 MB from it. Swap when the network allows: rerun step 2, then step 5 with `--dem-source usgs --dem data-pipeline/cache/usgs/gale_dem_window.tif`.
2. **No sand map** (plan B of guion §22.5): rock everywhere (η 1.2) and the legend says the 1 m elevation misses the hollows. `--sand dem` writes the DEM hollows instead.
3. **Habitat at x 514.8, y 777.0**: on Curiosity's own path (its sol 4580 position), 330 m north of the stops, 5° slope. *Proposal.*
4. **Stops at the rover's positions** during each drill campaign: Valle de la Luna at the sol 4687 position (parked to sol 4709; drilled sols 4689-4695), Nevado Sajama at the sol 4711 position (parked to sol 4741; drilled sol 4718). They are 10 m apart.

## Still to make

`thermal.png` (THEMIS: needs the tile link) · `conditions.json` (REMS values from the CAB page, DONKI) · `panorama_<id>.jpg` (PIA26696 for Nevado Sajama) · `mars_global.jpg` (Viking MDIM 2.1, for the globe) · the 3D models in `Assets/Walkback/Models3D/`.
