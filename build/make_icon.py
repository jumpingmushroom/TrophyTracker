"""Thunderstore icon: 256x256 PNG. The same family look as the sibling mods (dark plate, golden
border): a stylised trophy head silhouette, off-white, centred, with a thin progress bar
underneath (dark track, gold fill about two-thirds full) — the panel's own visual language.
Written without PIL, which the build box lacks.
Run from the repo root: python3 build/make_icon.py"""
import struct
import zlib

S = 256
SS = 3                       # supersample
W = S * SS

px = bytearray(W * W * 4)

def blend(x, y, r, g, b, a):
    if x < 0 or y < 0 or x >= W or y >= W:
        return
    i = (y * W + x) * 4
    ia = 1.0 - a
    px[i] = int(r * a + px[i] * ia)
    px[i + 1] = int(g * a + px[i + 1] * ia)
    px[i + 2] = int(b * a + px[i + 2] * ia)
    px[i + 3] = int(min(255, 255 * a + px[i + 3] * ia))

def rounded_rect(x0, y0, x1, y1, rad, col, alpha=1.0):
    for y in range(int(y0), int(y1)):
        for x in range(int(x0), int(x1)):
            dx = max(x0 + rad - x, 0, x - (x1 - 1 - rad))
            dy = max(y0 + rad - y, 0, y - (y1 - 1 - rad))
            if dx * dx + dy * dy <= rad * rad:
                blend(x, y, *col, alpha)

def polygon(points, col, alpha=1.0):
    """Even-odd fill of a polygon given in supersampled pixel coordinates (y down)."""
    ys = [p[1] for p in points]
    for y in range(int(min(ys)), int(max(ys)) + 1):
        for x in range(int(min(p[0] for p in points)), int(max(p[0] for p in points)) + 1):
            inside = False
            j = len(points) - 1
            for i in range(len(points)):
                xi, yi = points[i]
                xj, yj = points[j]
                if (yi > y) != (yj > y) and x < (xj - xi) * (y - yi) / (yj - yi) + xi:
                    inside = not inside
                j = i
            if inside:
                blend(x, y, *col, alpha)

PLATE = (0x1c, 0x1a, 0x17)
BORDER = (0xc8, 0xa0, 0x50)
TRACK = (0x12, 0x11, 0x0f)
HEAD = (0xe8, 0xe4, 0xda)

border_w = 10 * SS
rounded_rect(0, 0, W, W, 34 * SS, BORDER)
rounded_rect(border_w, border_w, W - border_w, W - border_w, 26 * SS, PLATE)

# Trophy head: a simplified mounted-head silhouette — two ear/horn tips at the top, a notch
# between them, bulging cheeks, tapering to a snout point at the bottom.
HEAD_UNIT = [
    (-0.55, -1.00),   # left ear tip
    (-0.20, -0.55),   # notch between ears
    (0.55, -1.00),    # right ear tip
    (0.65, -0.10),    # right cheek
    (0.30, 0.55),     # right jaw
    (0.00, 0.90),     # snout tip
    (-0.30, 0.55),    # left jaw
    (-0.65, -0.10),   # left cheek
]
HEAD_CX = 128 * SS
HEAD_CY = 100 * SS
HEAD_SCALE = 66 * SS
head_pts = [(HEAD_CX + fx * HEAD_SCALE, HEAD_CY + fy * HEAD_SCALE) for fx, fy in HEAD_UNIT]
polygon(head_pts, HEAD)

# Progress bar: dark track, gold fill at about two-thirds.
BAR_X0, BAR_X1 = 42 * SS, 214 * SS
BAR_Y0, BAR_Y1 = 190 * SS, 206 * SS
BAR_RAD = 8 * SS
rounded_rect(BAR_X0, BAR_Y0, BAR_X1, BAR_Y1, BAR_RAD, TRACK)
fill_x1 = BAR_X0 + (BAR_X1 - BAR_X0) * 0.67
rounded_rect(BAR_X0, BAR_Y0, fill_x1, BAR_Y1, BAR_RAD, BORDER)

# downsample
out = bytearray()
for y in range(S):
    row = bytearray([0])
    for x in range(S):
        r = g = b = a = 0
        for sy in range(SS):
            for sx in range(SS):
                i = ((y * SS + sy) * W + (x * SS + sx)) * 4
                r += px[i]; g += px[i + 1]; b += px[i + 2]; a += px[i + 3]
        n = SS * SS
        row += bytes((r // n, g // n, b // n, a // n))
    out += row

def chunk(tag, data):
    c = tag + data
    return struct.pack(">I", len(data)) + c + struct.pack(">I", zlib.crc32(c) & 0xffffffff)

png = b"\x89PNG\r\n\x1a\n"
png += chunk(b"IHDR", struct.pack(">IIBBBBB", S, S, 8, 6, 0, 0, 0))
png += chunk(b"IDAT", zlib.compress(bytes(out), 9))
png += chunk(b"IEND", b"")
open("thunderstore/icon.png", "wb").write(png)
print("wrote thunderstore/icon.png", S, "x", S, len(png), "bytes")
