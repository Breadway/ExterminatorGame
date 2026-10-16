#!/usr/bin/env python3
"""Generate arena art into Assets/_Project/Art/Environment/: floor tile, wall tile, crate,
pillar, and a pickup icon set. Pixel art in the Lo-Fi Industrial palette. Requires pillow.
--sheet writes a zoomed contact sheet to /tmp/opencode/env_sheet.png.
"""
import random
import sys
from pathlib import Path

from PIL import Image

OUT = Path(__file__).resolve().parents[1] / "Assets/_Project/Art/Environment"
OUTLINE = (20, 14, 12, 255)


def rgb(r, g, b, a=255):
    return (r, g, b, a)


def noise_tile(size, base, spread, seed, speck=None, speck_chance=0.0):
    rnd = random.Random(seed)
    img = Image.new("RGBA", (size, size))
    for y in range(size):
        for x in range(size):
            d = rnd.randint(-spread, spread)
            c = tuple(max(0, min(255, base[i] + d)) for i in range(3)) + (255,)
            if speck and rnd.random() < speck_chance:
                c = speck
            img.putpixel((x, y), c)
    return img


def floor():
    img = noise_tile(32, (92, 90, 88), 7, 1, rgb(66, 64, 62), 0.04)
    seam = rgb(60, 58, 56)
    for i in range(32):          # expansion joints so the floor reads as slabs
        img.putpixel((i, 0), seam)
        img.putpixel((0, i), seam)
    for x in range(32):
        img.putpixel((x, 1), rgb(104, 102, 100))
    # a stain and a tire scuff for variety
    for (x, y) in ((20, 22), (21, 22), (21, 23), (22, 23), (22, 24), (23, 24), (21, 24)):
        img.putpixel((x, y), rgb(70, 62, 52))
    for x in range(6, 14):
        img.putpixel((x, 12), rgb(70, 70, 72))
    return img


def wall():
    img = Image.new("RGBA", (32, 32), rgb(58, 56, 60))
    rnd = random.Random(2)
    for y in range(32):
        for x in range(32):
            d = rnd.randint(-5, 5)
            img.putpixel((x, y), rgb(58 + d, 56 + d, 60 + d))
    for x in range(32):                       # hazard stripe along the base
        for y in range(24, 32):
            on = ((x + y) // 4) % 2 == 0
            img.putpixel((x, y), rgb(255, 204, 0) if on else rgb(26, 26, 26))
    for x in range(32):
        img.putpixel((x, 23), OUTLINE)
        img.putpixel((x, 0), rgb(110, 108, 114))
    for (x, y) in ((4, 8), (27, 8), (4, 16), (27, 16)):   # rivets
        img.putpixel((x, y), rgb(150, 150, 156))
        img.putpixel((x + 1, y + 1), rgb(30, 30, 34))
    return img


def crate():
    img = Image.new("RGBA", (32, 32), (0, 0, 0, 0))
    wood, light, dark = rgb(150, 100, 52), rgb(192, 138, 78), rgb(96, 60, 30)
    for y in range(2, 30):
        for x in range(2, 30):
            img.putpixel((x, y), wood)
    for i in range(2, 30):                      # frame
        for t in (0, 1, 2):
            img.putpixel((i, 2 + t), light)
            img.putpixel((2 + t, i), light)
            img.putpixel((i, 29 - t), dark)
            img.putpixel((29 - t, i), dark)
    for i in range(4, 28):                      # cross brace
        img.putpixel((i, i), dark)
        img.putpixel((i, i + 1), dark)
        img.putpixel((31 - i, i), dark)
        img.putpixel((31 - i, i + 1), dark)
    for y in range(32):
        for x in range(32):
            if img.getpixel((x, y))[3] == 0:
                continue
            for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                nx, ny = x + dx, y + dy
                if not (0 <= nx < 32 and 0 <= ny < 32) or img.getpixel((nx, ny))[3] == 0:
                    img.putpixel((x, y), OUTLINE)
                    break
    return img


def pillar():
    img = Image.new("RGBA", (32, 32), (0, 0, 0, 0))
    base, light, dark = rgb(110, 108, 112), rgb(160, 158, 164), rgb(70, 68, 74)
    for y in range(32):
        for x in range(32):
            if (x - 15.5) ** 2 + (y - 15.5) ** 2 <= 14.5 ** 2:
                t = ((x - 15.5) + (y - 15.5)) / 20.0
                img.putpixel((x, y), light if t < -0.45 else dark if t > 0.45 else base)
    for y in range(32):
        for x in range(32):
            if (x - 15.5) ** 2 + (y - 15.5) ** 2 <= 5.0 ** 2:
                img.putpixel((x, y), rgb(255, 204, 0) if (x + y) % 4 < 2 else rgb(26, 26, 26))
    for y in range(32):
        for x in range(32):
            if img.getpixel((x, y))[3] == 0:
                continue
            for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                nx, ny = x + dx, y + dy
                if not (0 <= nx < 32 and 0 <= ny < 32) or img.getpixel((nx, ny))[3] == 0:
                    img.putpixel((x, y), OUTLINE)
                    break
    return img


def pickup(color, glow):
    img = Image.new("RGBA", (16, 16), (0, 0, 0, 0))
    for y in range(16):
        for x in range(16):
            d = (x - 7.5) ** 2 + (y - 7.5) ** 2
            if d <= 30:
                img.putpixel((x, y), color)
            elif d <= 49:
                img.putpixel((x, y), glow)
    for (x, y) in ((6, 5), (7, 5), (6, 6)):
        img.putpixel((x, y), rgb(255, 255, 255))
    return img


def flame_frames():
    """Four 16x20 flame frames (a flickering teardrop), brightest at the base, plus a soft smoke puff."""
    frames = []
    for f in range(4):
        img = Image.new("RGBA", (16, 20), (0, 0, 0, 0))
        sway = (-1, 0, 1, 0)[f]
        for y in range(20):
            t = y / 19.0                       # 0 = tip, 1 = base
            half = (1 - abs(t - 0.62) * 1.5) * 6.5 if t > 0.15 else t * 20
            half = max(0.0, half) * (0.9 + 0.1 * ((f + y) % 2))
            cx = 7.5 + sway * (1 - t) * 2.2
            for x in range(16):
                d = abs(x - cx)
                if d <= half:
                    k = d / max(half, 0.01)
                    if t > 0.55 and k < 0.45:
                        c = (255, 240, 170, 255)
                    elif t > 0.35 and k < 0.7:
                        c = (255, 190, 40, 255)
                    elif k < 0.9:
                        c = (255, 110, 20, 255)
                    else:
                        c = (200, 50, 10, 230)
                    img.putpixel((x, y), c)
        frames.append(img)
    return frames


def smoke():
    img = Image.new("RGBA", (12, 12), (0, 0, 0, 0))
    for y in range(12):
        for x in range(12):
            d = ((x - 5.5) ** 2 + (y - 5.5) ** 2) ** 0.5
            if d < 5.4:
                a = int(170 * (1 - d / 5.4))
                img.putpixel((x, y), (60, 58, 56, a))
    return img


def main():
    OUT.mkdir(parents=True, exist_ok=True)
    fr = flame_frames()
    strip = Image.new("RGBA", (16 * 4, 20), (0, 0, 0, 0))
    for i, im in enumerate(fr):
        strip.alpha_composite(im, (i * 16, 0))
    strip.save(OUT / "FlameStrip.png")
    smoke().save(OUT / "Smoke.png")
    art = {
        "Floor": floor(),
        "Wall": wall(),
        "Crate": crate(),
        "Pillar": pillar(),
        "ScrapPickup": pickup(rgb(255, 204, 0), rgb(255, 204, 0, 90)),
        "HealPickup": pickup(rgb(80, 215, 120), rgb(80, 215, 120, 90)),
    }
    for name, img in art.items():
        img.save(OUT / f"{name}.png")
    art["FlameStrip"] = strip
    art["Smoke"] = smoke()
    if "--sheet" in sys.argv:
        scale, pad = 8, 12
        w = sum(i.width * scale + pad for i in art.values()) + pad
        h = max(i.height for i in art.values()) * scale + pad * 2
        sheet = Image.new("RGBA", (w, h), (40, 40, 44, 255))
        x = pad
        for img in art.values():
            sheet.alpha_composite(img.resize((img.width * scale, img.height * scale), Image.NEAREST), (x, pad))
            x += img.width * scale + pad
        Path("/tmp/opencode").mkdir(exist_ok=True)
        sheet.save("/tmp/opencode/env_sheet.png")
    print("wrote", ", ".join(art))


if __name__ == "__main__":
    main()
