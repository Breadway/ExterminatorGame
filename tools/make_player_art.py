#!/usr/bin/env python3
"""Draw the player (a front-facing hazmat exterminator) and every animation frame from scratch.

A recreation of the hand-drawn Art/Player/Player.png: same palette and silhouette idea, but built
from parts (head, torso, two arms, two legs) so each frame can pose them. The originals are left
alone. Output:
  Art/Player/PlayerSheet.png   8 columns x 4 rows of 32x32 cells
  Art/Player/Weapons/*.png     held weapon sprites
Frame index = row * 8 + column:
  0-3 idle   4-5 dash   6-7 hurt   8-13 walk   14 land   15 step   16-20 death
Requires pillow.  --sheet writes a zoomed contact sheet to /tmp/opencode/player_sheet.png.
"""
import sys
from pathlib import Path

from PIL import Image

ART = Path(__file__).resolve().parents[1] / "Assets/_Project/Art"
OUT = ART / "Player/PlayerSheet.png"
CELL = 32
COLS = 8

OUTLINE = (16, 12, 10, 255)
SUIT = (249, 194, 43, 255)
SUIT_LIGHT = (255, 224, 110, 255)
SUIT_SHADE = (190, 140, 24, 255)
SUIT_DARK = (120, 86, 14, 255)
RUBBER = (46, 46, 50, 255)
RUBBER_LIGHT = (84, 84, 92, 255)
EYE = (232, 59, 59, 255)
GLASS = (30, 20, 24, 255)
STRAP = (70, 52, 12, 255)


class Canvas:
    def __init__(self, size=CELL):
        self.size = size
        self.img = Image.new("RGBA", (size, size), (0, 0, 0, 0))

    def px(self, x, y, c):
        if 0 <= x < self.size and 0 <= y < self.size:
            self.img.putpixel((int(x), int(y)), c)

    def rect(self, x0, y0, x1, y1, c):
        for y in range(int(y0), int(y1) + 1):
            for x in range(int(x0), int(x1) + 1):
                self.px(x, y, c)

    def ellipse(self, cx, cy, rx, ry, c):
        for y in range(self.size):
            for x in range(self.size):
                if ((x - cx) / rx) ** 2 + ((y - cy) / ry) ** 2 <= 1.0:
                    self.px(x, y, c)

    def outline(self):
        base = self.img.copy()
        for y in range(self.size):
            for x in range(self.size):
                if base.getpixel((x, y))[3]:
                    continue
                for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                    nx, ny = x + dx, y + dy
                    if 0 <= nx < self.size and 0 <= ny < self.size and base.getpixel((nx, ny))[3]:
                        self.img.putpixel((x, y), OUTLINE)
                        break
        return self.img


def limb(c, x, y_top, y_bottom, width, glove=0, light_side=-1):
    """A vertical limb: yellow suit with a darker edge, an optional rubber glove/boot at the bottom."""
    for y in range(int(y_top), int(y_bottom) + 1):
        for i in range(width):
            col = SUIT
            if i == (0 if light_side < 0 else width - 1):
                col = SUIT_LIGHT
            elif i == (width - 1 if light_side < 0 else 0):
                col = SUIT_SHADE
            c.px(x + i, y, col)
    for y in range(int(y_bottom) - glove + 1, int(y_bottom) + 1):
        for i in range(width):
            c.px(x + i, y, RUBBER_LIGHT if (i == 0 and light_side < 0) else RUBBER)


def draw_player(bob=0, l_arm=0, r_arm=0, l_leg=0, r_leg=0, dx=0, lean=0, squash=1.0):
    """One pose. bob moves the upper body (negative = up); arm/leg values shift the hands/feet
    vertically (negative = raised). lean shifts the head and shoulders sideways."""
    c = Canvas()
    cx = 15 + dx
    hip = 21 + bob

    # legs (drawn first, torso overlaps them)
    left_foot = 31 + l_leg
    right_foot = 31 + r_leg
    limb(c, cx - 5, hip - 1, left_foot, 4, glove=3, light_side=-1)
    limb(c, cx + 2, hip - 1, right_foot, 4, glove=3, light_side=-1)

    # torso
    top = 11 + bob
    c.rect(cx - 6, top, cx + 6, hip, SUIT)
    c.rect(cx - 6, top, cx - 5, hip, SUIT_LIGHT)
    c.rect(cx + 5, top, cx + 6, hip, SUIT_SHADE)
    c.rect(cx - 6, top, cx + 6, top, SUIT_LIGHT)
    c.rect(cx - 6, hip - 2, cx + 6, hip - 1, STRAP)           # belt
    c.rect(cx - 1, hip - 2, cx + 1, hip - 1, RUBBER_LIGHT)    # buckle
    c.rect(cx - 3, top + 3, cx + 3, top + 6, SUIT_SHADE)      # chest pocket
    c.rect(cx - 2, top + 4, cx + 2, top + 5, SUIT_DARK)

    # arms: a shoulder pad, then the arm hanging down with a glove
    arm_top = top
    limb(c, cx - 9, arm_top, hip - 3 + l_arm, 3, glove=3, light_side=-1)
    limb(c, cx + 7, arm_top, hip - 3 + r_arm, 3, glove=3, light_side=1)

    # head: hood, mask, eyes, respirator
    hx = cx + lean
    hy = 6 + bob
    c.ellipse(hx, hy, 6.2, 6.0, SUIT)
    c.ellipse(hx - 1.5, hy - 2, 3.5, 2.5, SUIT_LIGHT)
    c.rect(hx - 4, hy + 4, hx + 4, hy + 5, SUIT_SHADE)
    c.rect(hx - 4, hy - 1, hx + 4, hy + 3, GLASS)             # face mask
    c.px(hx - 3, hy + 1, EYE)
    c.px(hx - 2, hy + 1, EYE)
    c.px(hx + 2, hy + 1, EYE)
    c.px(hx + 3, hy + 1, EYE)
    c.rect(hx - 1, hy + 3, hx + 1, hy + 6, RUBBER)            # respirator
    c.px(hx, hy + 5, RUBBER_LIGHT)

    img = c.outline()
    if squash != 1.0:
        img = stretch(img, 1.0 / squash, squash)
    return img


def stretch(img, sx, sy):
    """Scale about the feet so squash and stretch keeps the character planted."""
    bbox = img.getbbox()
    sub = img.crop(bbox)
    sub = sub.resize((max(1, round(sub.width * sx)), max(1, round(sub.height * sy))), Image.NEAREST)
    out = Image.new("RGBA", (CELL, CELL), (0, 0, 0, 0))
    cx = (bbox[0] + bbox[2]) // 2
    out.alpha_composite(sub, (max(0, min(CELL - sub.width, cx - sub.width // 2)), CELL - sub.height))
    return out


def tint(img, color, amount):
    out = img.copy()
    px = out.load()
    for y in range(out.height):
        for x in range(out.width):
            r, g, b, a = px[x, y]
            if a:
                px[x, y] = tuple(int(v * (1 - amount) + t * amount) for v, t in zip((r, g, b), color)) + (a,)
    return out


def fall(img, degrees, shift):
    """Rotate the body about the feet on a roomy canvas, then fit it back into the cell."""
    big = Image.new("RGBA", (CELL * 3, CELL * 3), (0, 0, 0, 0))
    big.alpha_composite(img, (CELL, CELL))
    rotated = big.rotate(degrees, resample=Image.NEAREST, center=(CELL + CELL // 2, CELL * 2 - 1))
    bbox = rotated.getbbox()
    sub = rotated.crop(bbox)
    out = Image.new("RGBA", (CELL, CELL), (0, 0, 0, 0))
    ox = max(0, min(CELL - sub.width, (CELL - sub.width) // 2 + shift))
    out.alpha_composite(sub, (ox, CELL - sub.height))
    return out


def build_frames():
    f = {}
    f[0] = draw_player()
    f[1] = draw_player(bob=0, l_arm=1, r_arm=1)
    f[2] = draw_player(bob=1)
    f[3] = draw_player(bob=1, l_arm=-1, r_arm=-1)

    base = draw_player()
    f[4] = stretch(draw_player(lean=1, l_arm=-3, r_arm=-3), 0.8, 1.14)
    f[5] = stretch(draw_player(lean=1, l_arm=-4, r_arm=-4, l_leg=-1), 0.86, 1.08)

    f[6] = tint(draw_player(dx=-1, lean=-1, l_arm=-2, r_arm=-2), (255, 255, 255), 1.0)
    f[7] = tint(draw_player(dx=1, lean=1, bob=1, l_arm=-2, r_arm=-2), (232, 59, 59), 0.5)

    walk = [
        dict(bob=-1, l_leg=-4, r_leg=0, l_arm=2, r_arm=-2),
        dict(bob=0, l_leg=-2, r_leg=0, l_arm=1, r_arm=-1),
        dict(bob=1, l_leg=0, r_leg=0, l_arm=0, r_arm=0),
        dict(bob=-1, l_leg=0, r_leg=-4, l_arm=-2, r_arm=2),
        dict(bob=0, l_leg=0, r_leg=-2, l_arm=-1, r_arm=1),
        dict(bob=1, l_leg=0, r_leg=0, l_arm=0, r_arm=0),
    ]
    for i, pose in enumerate(walk):
        f[8 + i] = draw_player(**pose)

    f[14] = draw_player(bob=2, l_arm=1, r_arm=1, squash=0.9)
    f[15] = draw_player(bob=-2, l_arm=-2, r_arm=-2)

    f[16] = tint(draw_player(dx=-1, lean=-1, l_arm=-3, r_arm=-3, bob=1), (255, 255, 255), 0.7)
    f[17] = fall(draw_player(bob=1, l_arm=-2, r_arm=-2), -20, 0)
    f[18] = fall(draw_player(bob=2, l_arm=-3, r_arm=-3), -55, 1)
    f[19] = fall(draw_player(bob=2, l_arm=-3, r_arm=-3), -88, 2)
    f[20] = tint(fall(draw_player(bob=2, l_arm=-3, r_arm=-3), -90, 2), (36, 32, 26), 0.45)
    return f


# ---------- held weapons (barrel pointing up, grip near the bottom) ----------
def draw_rects(width, height, rects):
    c = Canvas(max(width, height))
    for (x0, y0, x1, y1, color) in rects:
        c.rect(x0, y0, x1, y1, color)
    img = c.outline().crop((0, 0, width, height))
    return img


def rgb(r, g, b):
    return (r, g, b, 255)


def build_misc(name):
    if name == "Bee":
        # fist-sized mutant bee: striped abdomen, head, wings, stinger
        return draw_rects(10, 10, [
            (2, 4, 7, 8, rgb(240, 196, 40)), (3, 4, 4, 8, rgb(36, 30, 24)), (6, 4, 6, 8, rgb(36, 30, 24)),
            (1, 7, 3, 9, rgb(54, 46, 36)), (1, 8, 2, 8, rgb(232, 59, 59)),
            (7, 7, 8, 9, rgb(54, 46, 36)), (8, 8, 8, 8, rgb(232, 59, 59)),
            (2, 0, 3, 3, rgb(190, 220, 240)), (6, 0, 7, 3, rgb(190, 220, 240)),
            (4, 9, 5, 9, rgb(54, 46, 36))])
    raise ValueError(name)


def build_weapons():
    steel, dark, red = rgb(150, 154, 160), rgb(70, 72, 78), rgb(200, 60, 50)
    yellow, orange = SUIT, rgb(255, 120, 0)
    return {
        "Flamethrower": draw_rects(14, 22, [
            (5, 6, 8, 20, dark), (4, 2, 9, 7, steel), (5, 1, 8, 2, dark),
            (2, 10, 3, 19, red), (10, 10, 11, 19, red), (6, 1, 7, 1, orange)]),
        "Mop": draw_rects(10, 34, [
            (4, 8, 5, 32, rgb(150, 110, 60)), (1, 1, 8, 7, rgb(230, 230, 220)), (1, 6, 8, 7, rgb(190, 190, 180)),
            (3, 8, 6, 9, steel)]),
        "Foam": draw_rects(14, 22, [
            (4, 8, 9, 20, yellow), (5, 4, 8, 8, steel), (6, 1, 7, 4, dark),
            (3, 12, 10, 13, SUIT_SHADE), (5, 15, 8, 17, rgb(240, 240, 240))]),
        "Cables": draw_rects(14, 22, [
            (6, 4, 7, 20, dark), (3, 1, 5, 6, red), (8, 1, 10, 6, rgb(60, 60, 66)),
            (4, 7, 9, 10, steel), (5, 11, 8, 13, yellow)]),
        "Beam": draw_rects(14, 26, [
            (5, 8, 8, 24, steel), (4, 4, 9, 9, dark), (6, 1, 7, 4, rgb(120, 235, 220)),
            (3, 12, 4, 20, rgb(60, 120, 140)), (9, 12, 10, 20, rgb(60, 120, 140)),
            (5, 14, 8, 15, rgb(120, 235, 220))]),
    }


def main():
    frames = build_frames()
    rows = (max(frames) // COLS) + 1
    sheet = Image.new("RGBA", (CELL * COLS, CELL * rows), (0, 0, 0, 0))
    for index, img in frames.items():
        sheet.alpha_composite(img, ((index % COLS) * CELL, (index // COLS) * CELL))
    sheet.save(OUT)

    wdir = ART / "Player/Weapons"
    wdir.mkdir(parents=True, exist_ok=True)
    weapons = build_weapons()
    for name, img in weapons.items():
        img.save(wdir / f"{name}.png")
    build_misc("Bee").save(ART / "Enemies/Bee.png")

    if "--sheet" in sys.argv:
        scale = 5
        bg = Image.new("RGBA", sheet.size, (74, 70, 66, 255))
        bg.alpha_composite(sheet)
        big = bg.resize((bg.width * scale, bg.height * scale), Image.NEAREST)
        Path("/tmp/opencode").mkdir(exist_ok=True)
        big.save("/tmp/opencode/player_sheet.png")
    print(f"wrote {OUT.name} {sheet.size}: {len(frames)} frames, {len(weapons)} weapon sprites")


if __name__ == "__main__":
    main()
