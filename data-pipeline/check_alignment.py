# Written with Claude Code (AI), directed and tested by team MARSXR
"""Control: does the Mars Trek DEM sit where the USGS DEM (the source of guion §5.1) sits?

Compares a DEM already warped to the local grid (1000 x 1000, 1 m, row 0 = north) with a strip
of the USGS DEM warped to the same grid, searching the horizontal shift that best matches the
relief, and reports the height offset. Also reports the height step between the two drill sites.

Usage: python -I check_alignment.py <dem_local_1m.bin> <usgs_strip_local_1m.bin> [max_shift_m]
Both inputs: raw float32 little-endian, 1000 x 1000; nodata in the strip = -32767.
"""
import sys
import numpy as np

N = 1000
dem = np.fromfile(sys.argv[1], dtype='<f4').reshape(N, N)
strip = np.fromfile(sys.argv[2], dtype='<f4').reshape(N, N)
max_shift = int(sys.argv[3]) if len(sys.argv) > 3 else 40

valid = strip > -30000
rows = np.where(valid.any(axis=1))[0]
cols = np.where(valid.any(axis=0))[0]
r0, r1 = rows.min(), rows.max() + 1
c0, c1 = cols.min(), cols.max() + 1
print('USGS strip on the local grid: rows %d-%d (y %d-%d m), columns %d-%d' % (r0, r1 - 1, N - r1 + 1, N - r0, c0, c1 - 1))

# Keep an inner box so every shift stays inside the DEM.
m = max_shift
r0i, r1i = max(r0, m), min(r1, N - m)
c0i, c1i = max(c0, m), min(c1, N - m)
ref = strip[r0i:r1i, c0i:c1i]
ok = ref > -30000

best = None
for dy in range(-m, m + 1):
    for dx in range(-m, m + 1):
        sub = dem[r0i + dy:r1i + dy, c0i + dx:c1i + dx]
        d = (sub - ref)[ok]
        s = d.std()
        if best is None or s < best[0]:
            best = (s, dx, dy, d.mean())
s, dx, dy, off = best
d0 = (dem[r0i:r1i, c0i:c1i] - ref)[ok]
print('No shift:   std of the difference %.2f m, mean %.2f m' % (d0.std(), d0.mean()))
print('Best shift: DEM moved %+d m east, %+d m north matches best (positive = DEM content sits that far east/south of USGS)' % (dx, -dy))
print('            std of the difference %.2f m, mean height offset (DEM - USGS) %.2f m' % (s, off))


def at(z, x, y):
    """Bilinear height at local meters (x east, y north) on the 1 m grid (pixel centers at .5)."""
    c = x - 0.5
    r = (N - y) - 0.5
    c_i, r_i = int(np.floor(c)), int(np.floor(r))
    fc, fr = c - c_i, r - r_i
    q = z[r_i:r_i + 2, c_i:c_i + 2]
    return (q[0, 0] * (1 - fc) + q[0, 1] * fc) * (1 - fr) + (q[1, 0] * (1 - fc) + q[1, 1] * fc) * fr


stops = {'Valle de la Luna (rover, sol 4687)': (502.1, 450.3), 'Nevado Sajama (rover, sol 4711)': (500.2, 440.3)}
for label, z in (('USGS strip', strip), ('DEM', dem)):
    h = {k: at(z, *p) for k, p in stops.items()}
    a, b = list(h.values())
    print('%-10s heights: Valle de la Luna %.2f m, Nevado Sajama %.2f m, step %.2f m' % (label, a, b, b - a))
