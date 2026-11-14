# Written with Claude Code (AI), directed and tested by team MARSXR
"""Looks at a built site package the way the app will: photo, hillshade, slope and hollows side by side.

Usage: python -I preview_package.py <package dir> <out prefix> [zoom center x,y] [zoom side m]
Writes <out>_km2.png (the whole square) and <out>_zoom.png (around the center, default the stops).
"""
import json
import os
import sys

import numpy as np
from PIL import Image, ImageDraw

pkg, out = sys.argv[1], sys.argv[2]
info = json.load(open(os.path.join(pkg, 'site.json'), encoding='utf-8'))
stops = json.load(open(os.path.join(pkg, 'stops.json'), encoding='utf-8'))
trav = json.load(open(os.path.join(pkg, 'curiosity_traverse.json'), encoding='utf-8'))
n, side = info['heightmapSize'], info['sideMeters']
v = np.fromfile(os.path.join(pkg, 'heightmap.r16'), dtype='<u2').reshape(n, n).astype(np.float64)
z = info['minElevation'] + v / 65535 * (info['maxElevation'] - info['minElevation'])
g = z[::2, ::2]
sp = side / (g.shape[0] - 1)
gy, gx = np.gradient(g, sp)
slope = np.degrees(np.arctan(np.hypot(gx, gy)))
sand = np.array(Image.open(os.path.join(pkg, 'sand.png'))) > 127
photo = Image.open(os.path.join(pkg, 'ortho.jpg')).convert('L')

zy, zx = np.gradient(z, side / (n - 1))
az, alt = np.radians(315 - 90), np.radians(35)
hs = np.sin(alt) * np.cos(np.arctan(np.hypot(zx, zy))) + np.cos(alt) * np.sin(np.arctan(np.hypot(zx, zy))) * np.cos(az - np.arctan2(-zx, zy))
hs = Image.fromarray((np.clip(hs, 0, 1) * 255).astype(np.uint8), 'L')

# slope colours as the app paints them (Okabe-Ito): blue < 10, orange 10-20, vermilion > 20
sc = np.zeros(slope.shape + (3,), np.uint8)
sc[slope < 10] = (0, 114, 178)
sc[(slope >= 10) & (slope <= 20)] = (230, 159, 0)
sc[slope > 20] = (213, 94, 0)
sc[sand] = (255, 255, 255)
slope_img = Image.fromarray(sc, 'RGB')


def panel(img, x0, y0, w, size):
    """Crop [x0, x0+w] x [y0, y0+w] (local meters) of an image covering the square, resized to size."""
    W = img.size[0]
    k = W / side
    box = (int(x0 * k), int((side - y0 - w) * k), int((x0 + w) * k), int((side - y0) * k))
    return img.crop(box).resize((size, size), Image.NEAREST if img.size[0] < 600 else Image.LANCZOS).convert('RGB')


def marks(im, x0, y0, w, size):
    d = ImageDraw.Draw(im)
    k = size / w
    pts = [((t['x'] - x0) * k, (y0 + w - t['y']) * k) for t in trav]
    if len(pts) > 1:
        d.line(pts, fill=(255, 0, 255), width=2)
    for s in stops:
        cx, cy = (s['x'] - x0) * k, (y0 + w - s['y']) * k
        d.ellipse([cx - 7, cy - 7, cx + 7, cy + 7], outline=(255, 255, 0), width=3)
    h = info['habitat']
    cx, cy = (h['x'] - x0) * k, (y0 + w - h['y']) * k
    d.rectangle([cx - 7, cy - 7, cx + 7, cy + 7], outline=(0, 255, 0), width=3)
    return im


def sheet(x0, y0, w, size, path, title):
    tiles = [marks(panel(i, x0, y0, w, size), x0, y0, w, size) for i in (photo, hs, slope_img)]
    S = Image.new('RGB', (size * 3 + 20, size + 30), (20, 20, 20))
    d = ImageDraw.Draw(S)
    for i, (t, name) in enumerate(zip(tiles, ('HiRISE photo 25 cm', 'Hillshade of the DEM', 'Slope: blue <10, orange 10-20, red >20, white = hollow'))):
        S.paste(t, (i * (size + 10), 30))
        d.text((i * (size + 10) + 4, 8), name, fill=(255, 255, 255))
    d.text((size * 3 - 260, 8), title, fill=(255, 200, 0))
    S.save(path)


sheet(0, 0, side, 600, out + '_km2.png', 'whole km2 · magenta = Curiosity · yellow = stops · green = habitat')
if len(sys.argv) > 3:
    cx, cy = [float(t) for t in sys.argv[3].split(',')]
else:
    cx, cy = sum(s['x'] for s in stops) / len(stops), sum(s['y'] for s in stops) / len(stops)
w = float(sys.argv[4]) if len(sys.argv) > 4 else 200
sheet(cx - w / 2, cy - w / 2, w, 600, out + '_zoom.png', '%d m around the stops' % w)

# slope along the rover's path: a rover drove here, so the path should be mostly gentle
on_path = []
for t in trav:
    r, c = int(round((side - t['y']) / sp)), int(round(t['x'] / sp))
    on_path.append(slope[r, c])
print('slope at the %d rover positions: median %.1f, p90 %.1f, max %.1f deg' % (len(on_path), np.median(on_path), np.percentile(on_path, 90), max(on_path)))
