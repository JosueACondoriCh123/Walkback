# Written with Claude Code (AI), directed and tested by team MARSXR
"""The colour of the ground as Curiosity's Mastcam saw it, to tint the black-and-white HiRISE photo.

The HiRISE orthophoto mosaic is a single (red) band: on the table and at real scale it looks grey.
This takes the median colour of the ground in a Mastcam 360° mosaic: only below the horizon, only
pixels with a photo (the mosaic's black is skipped), only ground-coloured pixels (red > green > blue,
not too dark or too bright), which leaves out the sky and the grey and white parts of the rover.

Usage: python -I photo_tint.py <mosaic.jpg> <horizon row as a fraction of the height, from the top>
Prints the colour (0-255) and the share of pixels it comes from.
"""
import sys

import numpy as np
from PIL import Image

Image.MAX_IMAGE_PIXELS = None
src, horizon = sys.argv[1], float(sys.argv[2])
im = Image.open(src).convert('RGB')
w, h = im.size
small = im.resize((w // 4, h // 4), Image.BILINEAR)          # the median does not need every pixel
a = np.asarray(small, dtype=np.int32)
below = a[int(horizon * a.shape[0]) + max(2, a.shape[0] // 40):]
r, g, b = below[..., 0], below[..., 1], below[..., 2]
ground = (r > g) & (g > b) & (below.max(axis=2) > 50) & (below.max(axis=2) < 235) & ((r - b) > 25)
pixels = below[ground]
med = np.median(pixels, axis=0)
print('ground colour (RGB 0-255): %d %d %d   from %d pixels, %.1f %% of the ground band'
      % (med[0], med[1], med[2], len(pixels), 100.0 * ground.mean()))
