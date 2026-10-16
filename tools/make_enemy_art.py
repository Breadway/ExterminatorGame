#!/usr/bin/env python3
"""Generate the enemy roster sprites into Assets/_Project/Art/Enemies/.

Every sprite faces up and is drawn left/right symmetric: draw calls take an x offset
from the centre line and are mirrored automatically. Edit and rerun to retune the art.
Requires pillow.  --sheet writes a zoomed contact sheet to /tmp/opencode/enemy_sheet.png.
"""
import sys
from pathlib import Path

from PIL import Image

OUT = Path(__file__).resolve().parents[1] / "Assets/_Project/Art/Enemies"
OUTLINE = (20, 12, 12, 255)


def rgb(r, g, b):
    return (r, g, b, 255)


class Sprite:
    def __init__(self, w, h):
        self.w, self.h = w, h
        self.img = Image.new("RGBA", (w, h), (0, 0, 0, 0))
        self.cx = (w - 1) / 2.0

    def put(self, x, y, c):
        if 0 <= x < self.w and 0 <= y < self.h:
            self.img.putpixel((int(x), int(y)), c)

    def sym_put(self, dx, y, c):
        """dx = distance from the centre line (0 = centre)."""
        self.put(round(self.cx - dx), y, c)
        self.put(round(self.cx + dx), y, c)

    def ellipse(self, cy, rx, ry, c, dx=0.0):
        """Filled ellipse centred dx from the centre line (mirrored when dx != 0)."""
        for y in range(self.h):
            for x in range(self.w):
                for sx in ((-1, 1) if dx else (0,)):
                    ex = self.cx + sx * dx
                    if ((x - ex) / rx) ** 2 + ((y - cy) / ry) ** 2 <= 1.0:
                        self.put(x, y, c)

    def line(self, x0, y0, x1, y1, c, mirror=True):
        """Line from (x0, y0) to (x1, y1) where x are offsets from the centre line."""
        n = max(abs(x1 - x0), abs(y1 - y0), 1)
        for i in range(int(n * 2) + 1):
            t = i / (n * 2)
            x = x0 + (x1 - x0) * t
            y = y0 + (y1 - y0) * t
            if mirror:
                self.sym_put(x, round(y), c)
            else:
                self.put(round(self.cx + x), round(y), c)

    def rect(self, y0, y1, dx0, dx1, c):
        for y in range(y0, y1 + 1):
            for dx in range(dx0, dx1 + 1):
                self.sym_put(dx, y, c)

    def finish(self, name):
        base = self.img.copy()
        for y in range(self.h):
            for x in range(self.w):
                if base.getpixel((x, y))[3]:
                    continue
                for ox, oy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                    nx, ny = x + ox, y + oy
                    if 0 <= nx < self.w and 0 <= ny < self.h and base.getpixel((nx, ny))[3]:
                        self.img.putpixel((x, y), OUTLINE)
                        break
        OUT.mkdir(parents=True, exist_ok=True)
        self.img.save(OUT / f"{name}.png")
        return self.img


def shade(s, y0, y1, dx0, dx1, light, dark):
    """Cheap lighting on already-drawn body pixels: highlight top-left, shadow bottom-right."""
    mid = (y0 + y1) / 2
    base = s.img.copy()
    for y in range(y0, y1 + 1):
        for x in range(s.w):
            px = base.getpixel((x, y))
            if px[3] == 0 or px in (light, dark):
                continue
            off = x - s.cx
            if abs(off) < dx0 or abs(off) > dx1 + 0.5:
                continue
            # light from the upper left; a diagonal band keeps the shape readable
            tone = (off / max(dx1, 1)) * 0.6 + (y - mid) / max(mid - y0, 1) * 0.4
            if tone < -0.45:
                s.put(x, y, light)
            elif tone > 0.45:
                s.put(x, y, dark)


def roach():
    s = Sprite(30, 34)
    body, light, dark, wing = rgb(122, 74, 36), rgb(168, 108, 56), rgb(76, 44, 24), rgb(96, 56, 28)
    for y0, reach in ((12, 8), (17, 9), (22, 8)):
        s.line(3, y0, reach, y0 - 3, rgb(150, 96, 52))
        s.line(reach, y0 - 3, reach + 2, y0 + 3, rgb(150, 96, 52))
    s.line(1, 8, 6, 1, rgb(200, 150, 90))   # antennae
    s.line(1, 8, 4, 0, rgb(200, 150, 90))
    s.ellipse(25, 4.0, 5.0, dark)           # abdomen tip
    s.ellipse(17, 7.0, 10.0, body)          # thorax and wing case
    s.ellipse(8, 4.5, 4.0, rgb(92, 54, 28)) # head
    s.rect(12, 27, 0, 0, dark)              # wing case seam
    shade(s, 8, 26, 1, 6, light, dark)
    s.put(round(s.cx - 2), 7, rgb(255, 204, 0))
    s.put(round(s.cx + 2), 7, rgb(255, 204, 0))
    return s.finish("Roach")


def spider():
    s = Sprite(40, 34)
    body, light, dark = rgb(70, 40, 96), rgb(110, 70, 150), rgb(40, 22, 60)
    for i, y in enumerate((12, 15, 18, 21)):  # four leg pairs, two joints each
        out = 8 + (2 if i in (0, 3) else 0)
        s.line(3, y, out - 2, y - 5 + i, rgb(60, 34, 80))
        s.line(out - 2, y - 5 + i, out + 6, y + 4 + i, rgb(60, 34, 80))
    s.ellipse(22, 7.0, 8.0, body)
    s.ellipse(12, 5.0, 4.5, rgb(86, 50, 118))
    shade(s, 14, 29, 1, 7, light, dark)
    s.rect(18, 20, 0, 0, rgb(180, 50, 60))       # warning mark
    for dx, y in ((1, 10), (3, 10), (2, 12), (4, 12)):
        s.sym_put(dx, y, rgb(255, 70, 70))
    s.rect(15, 16, 1, 2, rgb(150, 150, 160))     # fangs
    return s.finish("Spider")


def wasp():
    s = Sprite(34, 36)
    yellow, black = rgb(240, 196, 40), rgb(36, 30, 24)
    wing = (190, 220, 240, 150)
    s.ellipse(15, 8.0, 5.0, wing, dx=8)
    s.ellipse(21, 5.0, 3.5, wing, dx=7)
    s.ellipse(12, 3.8, 3.6, rgb(64, 52, 36))       # head
    s.ellipse(18, 4.0, 3.6, yellow)                # thorax
    for i, y in enumerate(range(22, 33)):          # striped abdomen
        w = 3.6 - abs(y - 26) * 0.18
        c = black if (y // 2) % 2 == 0 else yellow
        s.rect(y, y, 0, int(max(1, w)), c)
    s.rect(33, 34, 0, 0, rgb(120, 235, 130))       # stinger
    s.line(1, 9, 5, 4, black)
    s.sym_put(2, 11, rgb(255, 60, 60))
    return s.finish("Wasp")


def beetle():
    s = Sprite(36, 36)
    shell, light, dark = rgb(48, 112, 120), rgb(96, 176, 184), rgb(28, 64, 76)
    for y0 in (14, 19, 24):
        s.line(4, y0, 11, y0 + 2, rgb(30, 70, 76))
    s.ellipse(21, 11.0, 13.0, shell)               # dome
    s.rect(12, 33, 0, 0, dark)                     # shell seam
    s.ellipse(8, 5.5, 4.5, rgb(40, 90, 98))        # head
    shade(s, 9, 33, 1, 11, light, dark)
    s.line(2, 6, 4, 0, rgb(180, 180, 190))         # horn pair
    s.line(3, 7, 7, 2, rgb(180, 180, 190))
    s.sym_put(2, 8, rgb(255, 204, 0))
    for y in (16, 22, 28):                         # armor rivets
        s.sym_put(6, y, rgb(190, 200, 205))
    return s.finish("Beetle")


def larva():
    s = Sprite(20, 26)
    body, light, dark = rgb(222, 214, 150), rgb(246, 240, 190), rgb(170, 160, 100)
    for i, y in enumerate((20, 15, 10)):
        s.ellipse(y, 5.5 - i * 0.5, 3.6, body)
        s.rect(y + 3, y + 3, 0, 4 - i, dark)
    s.ellipse(5, 4.0, 3.6, rgb(150, 120, 70))
    shade(s, 7, 23, 1, 5, light, dark)
    s.sym_put(1, 4, rgb(30, 30, 30))
    return s.finish("Larva")


def spit():
    s = Sprite(10, 10)
    s.ellipse(4.5, 3.6, 3.6, rgb(150, 230, 90))
    s.ellipse(4.5, 2.0, 2.0, rgb(220, 255, 140))
    return s.finish("Spit")


def brood_mother():
    s = Sprite(56, 60)
    body, light, dark = rgb(112, 60, 36), rgb(160, 96, 58), rgb(66, 34, 22)
    for y0, reach in ((20, 14), (28, 16), (36, 14)):
        s.line(6, y0, reach, y0 - 5, rgb(140, 84, 50))
        s.line(reach, y0 - 5, reach + 3, y0 + 5, rgb(140, 84, 50))
    s.ellipse(44, 9.0, 11.0, rgb(224, 150, 120))    # egg sac
    for dx, y in ((4, 40), (-4, 46), (3, 50)):
        s.put(round(s.cx + dx), y, rgb(255, 214, 190))
    s.ellipse(30, 13.0, 17.0, body)
    s.ellipse(12, 8.0, 7.0, rgb(86, 46, 28))
    s.rect(20, 44, 0, 0, dark)
    shade(s, 13, 46, 1, 13, light, dark)
    s.line(2, 8, 10, 0, rgb(200, 150, 90))
    s.line(2, 8, 7, 0, rgb(200, 150, 90))
    for dx in (3, 5):
        s.sym_put(dx, 11, rgb(255, 80, 40))
    return s.finish("BroodMother")


def queen():
    s = Sprite(64, 66)
    body, light, dark = rgb(96, 28, 34), rgb(150, 54, 56), rgb(52, 14, 20)
    wing = (255, 120, 100, 140)
    s.ellipse(30, 11.0, 17.0, wing, dx=20)
    for y0, reach in ((24, 16), (32, 18), (40, 16)):
        s.line(6, y0, reach, y0 - 5, rgb(130, 50, 46))
        s.line(reach, y0 - 5, reach + 3, y0 + 5, rgb(130, 50, 46))
    s.ellipse(33, 13.0, 19.0, body)
    s.ellipse(48, 9.0, 12.0, rgb(70, 18, 26))
    s.ellipse(15, 8.5, 7.5, rgb(80, 22, 28))
    s.rect(22, 54, 0, 0, dark)
    shade(s, 16, 56, 1, 13, light, dark)
    gold = rgb(255, 204, 0)
    for dx, top in ((0, 1), (3, 3), (6, 5), (9, 7)):  # crown
        s.sym_put(dx, top, gold)
        s.rect(top + 1, 8, dx, dx, gold)
    s.rect(9, 9, 0, 9, gold)
    for dx in (3, 5):
        s.sym_put(dx, 13, rgb(255, 230, 120))
    return s.finish("Queen")


def main():
    sprites = {f.__name__: f() for f in (roach, spider, wasp, beetle, larva, spit, brood_mother, queen)}
    if "--sheet" in sys.argv:
        scale, pad = 8, 16
        names = list(sprites)
        width = sum(im.width * scale + pad for im in sprites.values()) + pad
        height = max(im.height for im in sprites.values()) * scale + pad * 2
        sheet = Image.new("RGBA", (width, height), (66, 48, 38, 255))
        x = pad
        for n in names:
            im = sprites[n]
            sheet.alpha_composite(im.resize((im.width * scale, im.height * scale), Image.NEAREST), (x, pad))
            x += im.width * scale + pad
        Path("/tmp/opencode").mkdir(exist_ok=True)
        sheet.save("/tmp/opencode/enemy_sheet.png")
    print("wrote", ", ".join(f"{k}({v.width}x{v.height})" for k, v in sprites.items()))


if __name__ == "__main__":
    main()
