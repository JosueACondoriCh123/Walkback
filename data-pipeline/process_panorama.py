# Written with Claude Code (AI), directed and tested by team MARSXR
"""Turns a Mastcam 360° mosaic into Site/panorama_<id>.jpg for the app's sphere (guion §5.1).

The mosaic covers 360° across but only a band in height, with black where no photo was taken
(gaps between the sky frames, the masked rover deck). It is scaled to 4096 px wide (trap 13), put
in a 4096 x 2048 equirectangular frame with its horizon on the middle row, and every place without
a photo (outside the band, and black inside it) is filled with one plain colour: the sky's above
the horizon, the ground's below. Nothing is painted in. Not mirrored here: the app shows it from
inside without mirroring (trap 12).

Usage: python -I process_panorama.py <mosaic.jpg> <out.jpg> <horizon row as a fraction of the height, from the top>
"""
import sys

import numpy as np
from PIL import Image

Image.MAX_IMAGE_PIXELS = None
W, H = 4096, 2048
NO_PHOTO = 10            # a pixel whose brightest channel is under this is the mosaic's black
src, out, horizon = sys.argv[1], sys.argv[2], float(sys.argv[3])

im = Image.open(src).convert('RGB')
w0, h0 = im.size
band_h = int(round(h0 * W / w0))
band = np.asarray(im.resize((W, band_h), Image.LANCZOS), dtype=np.float32)
photo = band.max(axis=2) >= NO_PHOTO

edge = max(4, band_h // 20)
hz = int(horizon * band_h)
above = band[:hz]
pale = photo[:hz] & (above.max(axis=2) - above.min(axis=2) < 30) & (above.mean(axis=2) > 140)
sky = np.median(above[pale], axis=0) if pale.sum() > 100 else np.median(band[:edge][photo[:edge]], axis=0)
ground = np.median(band[-edge:][photo[-edge:]], axis=0)

top = int(round(H / 2 - horizon * band_h))
if top < 0 or top + band_h > H:
    raise SystemExit('band does not fit the frame: top %d, height %d' % (top, band_h))
frame = np.empty((H, W, 3), np.float32)
frame[:H // 2] = sky
frame[H // 2:] = ground
rows = np.arange(top, top + band_h)
region = frame[top:top + band_h]
region[photo] = band[photo]
frame[top:top + band_h] = region

Image.fromarray(frame.clip(0, 255).astype(np.uint8)).save(out, quality=88, optimize=True)
print('mosaic %d x %d (%.1f° tall) -> band %d px from row %d, horizon at %.3f of the band; '
      'no photo inside the band: %.1f %%; sky %s, ground %s'
      % (w0, h0, 360.0 * h0 / w0, band_h, top, horizon, 100 * (1 - photo.mean()),
         sky.round().tolist(), ground.round().tolist()))
