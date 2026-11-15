# Written with Claude Code (AI), directed and tested by team MARSXR
"""Local relief (height minus the ~20 m neighbourhood mean) of the central 300 m, for the boxwork check.
Usage: python -I preview_relief.py <dem raw float32 .bin> <n> <out.png> <x,y;x,y>"""
import sys, numpy as np
from PIL import Image, ImageDraw

def box_mean(z, r):
    p = np.pad(z, r, mode='edge'); c = p.cumsum(0).cumsum(1)
    c = np.pad(c, ((1, 0), (1, 0)))
    k = 2 * r + 1
    s = c[k:, k:] - c[:-k, k:] - c[k:, :-k] + c[:-k, :-k]
    return s / (k * k)

raw, n, out = sys.argv[1], int(sys.argv[2]), sys.argv[3]
stops = [tuple(map(float, p.split(','))) for p in sys.argv[4].split(';')]
z = np.fromfile(raw, dtype='<f4').reshape(n, n); cell = 1000.0 / n
rel = z - box_mean(z, int(round(10 / cell)))
cx = sum(s[0] for s in stops) / len(stops); cy = sum(s[1] for s in stops) / len(stops)
c0, r0, w = int((cx - 150) / cell), int((1000 - cy - 150) / cell), int(300 / cell)
sub = np.clip(rel[r0:r0 + w, c0:c0 + w], -1.5, 1.5) / 1.5
rgb = np.zeros(sub.shape + (3,), np.uint8)
rgb[..., 0] = (128 + 127 * np.clip(sub, 0, 1)).astype(np.uint8)          # ridges -> orange
rgb[..., 1] = (128 + 60 * np.clip(sub, 0, 1) - 60 * np.clip(-sub, 0, 1)).astype(np.uint8)
rgb[..., 2] = (128 + 127 * np.clip(-sub, 0, 1)).astype(np.uint8)         # hollows -> blue
im = Image.fromarray(rgb).resize((900, 900), Image.NEAREST); d = ImageDraw.Draw(im); k = 900 / w
for (x, y) in stops:
    c, r = (x / cell - c0) * k, ((1000 - y) / cell - r0) * k
    d.ellipse([c - 14, r - 14, c + 14, r + 14], outline=(0, 0, 0), width=3)
d.line([20, 880, 20 + 100 / cell * k, 880], fill=(0, 0, 0), width=4)
im.save(out)
frac = (rel < -0.5).mean()
print('relief p5/p50/p95 (m):', np.percentile(rel, [5, 50, 95]).round(2), ' hollow(<-0.5 m) fraction: %.1f %%' % (100 * frac))
