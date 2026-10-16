#!/usr/bin/env python3
"""Generate the scorpion sprite (28x28, faces up) at Assets/_Project/Art/Enemies/Scorpion.png.

The left half of the body is an explicit pixel map, mirrored for symmetry. Legs and
the tail arched over the back are drawn on top, then everything gets a 1px outline.
Edit the constants/map and rerun. Requires pillow.  Pass --preview for a zoomed copy.
"""
import sys
from pathlib import Path

from PIL import Image

PAD = 5
HALF_W = 12
W = HALF_W * 2 + PAD * 2
H = 28
OUT = Path(__file__).resolve().parents[1] / "Assets/_Project/Art/Enemies/Scorpion.png"

OUTLINE = (22, 12, 10, 255)
COL = {
    "b": (160, 66, 40, 255),     # carapace
    "l": (206, 104, 60, 255),    # highlight
    "d": (104, 40, 28, 255),     # shadow / segment gaps
    "c": (188, 84, 48, 255),     # claws
    "k": (132, 52, 34, 255),     # claw underside
    "e": (255, 204, 0, 255),     # eyes
}
LEG = (176, 76, 46, 255)
TAIL = (126, 50, 34, 255)
TAIL_BEAD = (184, 82, 48, 255)
STING = (120, 235, 130, 255)
STING_DARK = (52, 150, 80, 255)

# Left half, columns 0..11 (11 is the centre line). Rows top to bottom.
HALF = [
    ".cc..cc.....",  # 0  claw fingertips
    ".ccckccc....",  # 1
    ".cccccc.....",  # 2
    ".kcccck.....",  # 3
    "..kcck......",  # 4
    "...ccc......",  # 5  arm
    "...ccc......",  # 6
    "....ccb.....",  # 7
    "....cbbbbbbb",  # 8  head
    "...bbbbbebbb",  # 9
    "..bbbbbbbbbb",  # 10 carapace
    ".bbllbbbbbbb",  # 11
    ".bbllbbbbbbb",  # 12
    ".bbbbbbbbbbb",  # 13
    ".bbbbbbbbbbb",  # 14
    "..bbbbbbbbbb",  # 15
    "..dddddddddd",  # 16 abdomen segments
    "..bbbbbbbbbb",  # 17
    "...dddddddd.",  # 18
    "...bbbbbbbbb",  # 19
    "....dddddddd",  # 20
    "....bbbbbbbb",  # 21
    ".....ddddddd",  # 22
    ".....bbbbbbb",  # 23
    "......dddddd",  # 24
    "............",  # 25
    "............",  # 26
    "............",  # 27
]

img = Image.new("RGBA", (W, H), (0, 0, 0, 0))


def put(x, y, c):
    if 0 <= x < W and 0 <= y < H:
        img.putpixel((x, y), c)


def line(x0, y0, x1, y1, c):
    n = max(abs(x1 - x0), abs(y1 - y0), 1)
    for i in range(n + 1):
        put(round(x0 + (x1 - x0) * i / n), round(y0 + (y1 - y0) * i / n), c)


def mirrored(x):
    return W - 1 - x


# body: mirror the half map
for y, row in enumerate(HALF):
    for hx, ch in enumerate(row):
        if ch in COL:
            c = COL[ch]
            put(PAD + hx, y, c)
            put(mirrored(PAD + hx), y, c)

# legs: four pairs, each bends outward then down
for y0, x0 in ((10, PAD + 1), (12, PAD + 1), (14, PAD + 1), (16, PAD + 2)):
    for side in (1, -1):
        xs = (lambda x: x) if side == 1 else mirrored
        line(xs(x0), y0, xs(x0 - 2), y0 - 1, LEG)
        line(xs(x0 - 2), y0 - 1, xs(x0 - 4), y0 + 2, LEG)

# tail: arches over the back from the rear to a stinger above the head
cx = W // 2 - 1  # left of the two centre columns
for i, y in enumerate(range(24, 5, -1)):
    c = TAIL_BEAD if i % 3 == 1 else TAIL
    put(cx, y, c)
    put(cx + 1, y, c)
# curl at the rear so the root reads as a joint
line(cx - 1, 24, cx - 1, 25, TAIL)
line(cx + 2, 24, cx + 2, 25, TAIL)
# stinger bulb and barb
for dx in (-1, 0, 1, 2):
    for dy in (3, 4, 5):
        put(cx + dx, dy, STING)
put(cx - 1, 5, STING_DARK)
put(cx + 2, 5, STING_DARK)
put(cx, 2, STING)
put(cx + 1, 2, STING)
put(cx, 1, STING_DARK)
put(cx + 1, 1, STING_DARK)

# 1px outline around every opaque pixel
base = img.copy()
for y in range(H):
    for x in range(W):
        if base.getpixel((x, y))[3]:
            continue
        for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
            nx, ny = x + dx, y + dy
            if 0 <= nx < W and 0 <= ny < H and base.getpixel((nx, ny))[3]:
                img.putpixel((x, y), OUTLINE)
                break

OUT.parent.mkdir(parents=True, exist_ok=True)
img.save(OUT)
if "--preview" in sys.argv:
    bg = Image.new("RGBA", img.size, (74, 52, 40, 255))
    bg.alpha_composite(img)
    bg.resize((W * 14, H * 14), Image.NEAREST).save("/tmp/opencode/scorpion_preview.png")
print(f"wrote {OUT} ({W}x{H})")
