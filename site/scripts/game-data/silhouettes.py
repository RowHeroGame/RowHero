"""Trace the wiki home's section silhouettes from the game's sprites into src/lib/silhouettes.json.

Each sprite's opaque pixels become one SVG path (one rect per run of pixels), cropped to the
sprite's bounds, so the site can draw it in any single colour at any whole-pixel scale.

Usage (from the repo root):
    python -I scripts/game-data/silhouettes.py
"""
import json, os, struct, zlib

ROOT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..")

# section -> (sprite, pixels to leave out). The trophy's (11, 0) is a glint above the rim.
SPRITES = {
    "characters": ("public/img/wiki/rower-alex.png", set()),
    "events": ("public/img/icon-career.png", {(11, 0)}),
    "achievements": ("public/img/wiki/ach-star.png", set()),
}

# The game has no music-note sprite (its free play icon is a solid disc), so this one is drawn
# here at the same 16px scale as the trophy and the star.
NOTE = """
.....###########
.....###########
.....##.......##
.....##.......##
.....##.......##
.....##.......##
.....##.......##
.....##.......##
.....##.......##
..#####....#####
.######...######
.######...######
..####.....####.
"""


def read_alpha(path):
    """Opaque-pixel grid of an 8-bit RGBA PNG."""
    b = open(path, "rb").read()
    pos, idat = 8, b""
    while pos < len(b):
        n, tag = struct.unpack(">I4s", b[pos:pos + 8])
        data = b[pos + 8:pos + 8 + n]
        pos += 12 + n
        if tag == b"IHDR":
            w, h, depth, colour = struct.unpack(">IIBB", data[:10])
            if (depth, colour) != (8, 6):
                raise SystemExit(f"{path}: expected 8-bit RGBA")
        elif tag == b"IDAT":
            idat += data
    raw, stride = zlib.decompress(idat), w * 4
    grid, prev, i = [], bytearray(stride), 0
    for _ in range(h):
        kind, line = raw[i], bytearray(raw[i + 1:i + 1 + stride])
        i += 1 + stride
        for x in range(stride):
            a = line[x - 4] if x >= 4 else 0
            up, ul = prev[x], (prev[x - 4] if x >= 4 else 0)
            if kind == 1:
                line[x] = (line[x] + a) & 255
            elif kind == 2:
                line[x] = (line[x] + up) & 255
            elif kind == 3:
                line[x] = (line[x] + (a + up) // 2) & 255
            elif kind == 4:
                pa, pb, pc = abs(up - ul), abs(a - ul), abs(a + up - 2 * ul)
                line[x] = (line[x] + (a if pa <= pb and pa <= pc else up if pb <= pc else ul)) & 255
        grid.append([line[x * 4 + 3] > 127 for x in range(w)])
        prev = line
    return grid


def trace(grid, drop=frozenset()):
    """Crop to the opaque pixels and return {w, h, d}."""
    cells = {(x, y) for y, row in enumerate(grid) for x, on in enumerate(row) if on} - drop
    x0, y0 = min(x for x, _ in cells), min(y for _, y in cells)
    w, h = max(x for x, _ in cells) - x0 + 1, max(y for _, y in cells) - y0 + 1
    d = []
    for y in range(h):
        x = 0
        while x < w:
            if (x + x0, y + y0) in cells:
                start = x
                while x < w and (x + x0, y + y0) in cells:
                    x += 1
                d.append(f"M{start} {y}h{x - start}v1h-{x - start}z")
            else:
                x += 1
    return {"w": w, "h": h, "d": "".join(d)}


def main():
    shapes = {key: trace(read_alpha(os.path.join(ROOT, path)), drop) for key, (path, drop) in SPRITES.items()}
    shapes["songs"] = trace([[c == "#" for c in row] for row in NOTE.strip().splitlines()])
    out = os.path.join(ROOT, "src", "lib", "silhouettes.json")
    with open(out, "w", encoding="utf-8", newline="\n") as f:
        json.dump(shapes, f, indent=2)
        f.write("\n")
    for key, s in shapes.items():
        print(f"{key}: {s['w']}x{s['h']}")
    print(f"wrote {os.path.normpath(out)}")


if __name__ == "__main__":
    main()
