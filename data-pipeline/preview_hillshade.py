# Written with Claude Code (AI), directed and tested by team MARSXR
"""Hillshade previews of the km2 (whole and zoom on the stops) for the boxwork check.
Usage: python -I preview_hillshade.py <dem1m.tif as raw float32 .bin> <out_dir> [stops x,y;x,y]"""
import sys, json, numpy as np
from PIL import Image, ImageDraw

def hillshade(z, cell, az=315.0, alt=35.0, zf=1.0):
    gy, gx = np.gradient(z * zf, cell)          # rows go north->south
    slope = np.arctan(np.hypot(gx, gy))
    aspect = np.arctan2(-gx, gy)
    a, b = np.radians(360.0 - az + 90.0), np.radians(alt)
    hs = np.sin(b) * np.cos(slope) + np.cos(b) * np.sin(slope) * np.cos(a - aspect)
    return np.clip(hs, 0, 1)

raw, n, out = sys.argv[1], int(sys.argv[2]), sys.argv[3]
stops = [tuple(map(float, p.split(','))) for p in sys.argv[4].split(';')] if len(sys.argv) > 4 else []
z = np.fromfile(raw, dtype='<f4').reshape(n, n)
cell = 1000.0 / n
hs = hillshade(z, cell, zf=2.0)
img = Image.fromarray((hs * 255).astype(np.uint8)).convert('RGB')
d = ImageDraw.Draw(img)
for (x, y) in stops:
    c, r = x / cell, (1000 - y) / cell
    d.ellipse([c - 8, r - 8, c + 8, r + 8], outline=(255, 140, 0), width=3)
img.save(out + '_full.png')
# zoom: 300 m around the stops
if stops:
    cx = sum(s[0] for s in stops) / len(stops); cy = sum(s[1] for s in stops) / len(stops)
    c0, r0 = int((cx - 150) / cell), int((1000 - cy - 150) / cell)
    w = int(300 / cell)
    sub = hillshade(z[r0:r0 + w, c0:c0 + w], cell, alt=25.0, zf=4.0)
    im2 = Image.fromarray((sub * 255).astype(np.uint8)).resize((900, 900), Image.LANCZOS).convert('RGB')
    d2 = ImageDraw.Draw(im2); k = 900 / w
    for (x, y) in stops:
        c, r = (x / cell - c0) * k, ((1000 - y) / cell - r0) * k
        d2.ellipse([c - 14, r - 14, c + 14, r + 14], outline=(255, 140, 0), width=3)
    d2.line([20, 880, 20 + 100 / cell * k, 880], fill=(255, 140, 0), width=4)
    d2.text((24, 860), '100 m', fill=(255, 140, 0))
    im2.save(out + '_zoom300m.png')
