# Written with Claude Code (AI), directed and tested by team MARSXR
"""Control: does a DEM give the slopes Curiosity actually stood on?

The MSL waypoints file carries the rover's own tilt at every position, measured on board. That is
independent of any orbital DEM, so it checks the slope layer that routes and verdicts depend on.
Compares the DEM slope (1 m grid, 3 x 3 m window around the rover) with that tilt.

Usage: python -I check_tilt.py <MSL_waypoints.json> <dem_local_1m.bin> [more .bin files]
Each .bin: raw float32 little-endian, 1000 x 1000, 1 m, row 0 = north, centered on 4.807 S 137.381 E.
"""
import json
import math
import sys

import numpy as np

R = 3396190.0
LAT0, LON0 = -4.807, 137.381
E0, N0 = R * math.radians(LON0), R * math.radians(LAT0)
K = math.cos(math.radians(LAT0))

points = []
for f in json.load(open(sys.argv[1], encoding='utf-8'))['features']:
    p = f['properties']
    x = (p['easting'] - E0) * K + 500
    y = (p['northing'] - N0) + 500
    if 5 < x < 995 and 5 < y < 995 and p.get('tilt') is not None:
        points.append((x, y, p['tilt']))
tilt = np.array([t for _, _, t in points])
print('%d rover positions inside the square; rover tilt median %.1f, max %.1f deg' % (len(points), np.median(tilt), tilt.max()))

for path in sys.argv[2:]:
    z = np.fromfile(path, dtype='<f4').reshape(1000, 1000).astype(np.float64)
    gy, gx = np.gradient(z, 1.0)
    slope = np.degrees(np.arctan(np.hypot(gx, gy)))
    dem = np.array([slope[int(1000 - y) - 1:int(1000 - y) + 2, int(x) - 1:int(x) + 2].mean() for x, y, _ in points])
    print('%s: DEM slope at the rover median %.1f, max %.1f deg | mean difference to the tilt %.1f deg | r = %.2f'
          % (path.replace('\\', '/').split('/')[-1], np.median(dem), dem.max(), np.abs(dem - tilt).mean(), np.corrcoef(dem, tilt)[0, 1]))
