"""Tiny reader for Unity 6 serialized files (format 22+, type trees stripped).

Resolves Resources/ paths through the ResourceManager in globalgamemanagers and saves the
Texture2D behind each path as a PNG. Standard library only.

Usage:
    python -I unity_extract.py <RowHero_Data dir> <out dir> <path prefix>...
    e.g. python -I unity_extract.py ".../RowHero_Data" out heads/ playericons/ achievements/
"""
import os, struct, sys, zlib

CLASS_TEXTURE2D, CLASS_RESOURCE_MANAGER = 28, 147


def write_png(path, w, h, rgba):
    def chunk(tag, data):
        return struct.pack(">I", len(data)) + tag + data + struct.pack(">I", zlib.crc32(tag + data) & 0xFFFFFFFF)
    rows = b"".join(b"\x00" + rgba[y * w * 4:(y + 1) * w * 4] for y in range(h))
    with open(path, "wb") as f:
        f.write(b"\x89PNG\r\n\x1a\n" + chunk(b"IHDR", struct.pack(">IIBBBBB", w, h, 8, 6, 0, 0, 0))
                + chunk(b"IDAT", zlib.compress(rows, 9)) + chunk(b"IEND", b""))


class Reader:
    def __init__(self, buf, pos=0, little=True):
        self.b, self.p, self.e = buf, pos, "<" if little else ">"
    def _u(self, fmt):
        v = struct.unpack_from(self.e + fmt, self.b, self.p)[0]
        self.p += struct.calcsize(fmt)
        return v
    def i16(self): return self._u("h")
    def i32(self): return self._u("i")
    def u32(self): return self._u("I")
    def i64(self): return self._u("q")
    def u64(self): return self._u("Q")
    def u8(self): return self._u("B")
    def align(self): self.p = (self.p + 3) & ~3
    def cstr(self):
        end = self.b.index(b"\0", self.p)
        s = self.b[self.p:end].decode()
        self.p = end + 1
        return s
    def astr(self):
        n = self.i32()
        s = self.b[self.p:self.p + n].decode("utf-8", "replace")
        self.p += n
        self.align()
        return s


class SerializedFile:
    def __init__(self, path):
        self.b = open(path, "rb").read()
        h = Reader(self.b, 0, little=False)
        h.u32(); h.u32(); version = h.u32(); h.u32(); endian = h.u8(); h.p += 3
        if version < 22:
            raise SystemExit(f"{path}: serialized format {version} is older than this reader expects")
        h.u32(); h.u64(); data_offset = h.u64(); h.u64()
        self.little = endian == 0
        r = Reader(self.b, h.p, self.little)
        r.cstr(); r.i32()  # Unity version, target platform
        if r.u8():
            raise SystemExit(f"{path}: has type trees; this reader only handles stripped builds")
        types = []
        for _ in range(r.i32()):
            class_id = r.i32(); r.u8(); r.i16()
            if class_id == 114:
                r.p += 16  # script hash
            r.p += 16  # type hash
            types.append(class_id)
        self.objects = {}
        for _ in range(r.i32()):
            r.align(); path_id = r.i64(); start = r.u64(); size = r.u32(); type_index = r.i32()
            self.objects[path_id] = (types[type_index], data_offset + start, size)
        for _ in range(r.i32()):
            r.i32(); r.align(); r.i64()  # script types
        self.externals = []
        for _ in range(r.i32()):
            r.cstr(); r.p += 16; r.i32(); self.externals.append(r.cstr())

    def object(self, path_id):
        class_id, off, size = self.objects[path_id]
        return class_id, self.b[off:off + size]


def resource_paths(ggm):
    """(path, file id, path id) for everything under Assets/Resources."""
    for path_id, (class_id, _, _) in ggm.objects.items():
        if class_id == CLASS_RESOURCE_MANAGER:
            r = Reader(ggm.object(path_id)[1], 0, ggm.little)
            return [(r.astr(), r.i32(), r.i64()) for _ in range(r.i32())]
    raise SystemExit("no ResourceManager in globalgamemanagers")


def read_texture(raw, data_dir):
    """Texture2D as laid out by Unity 6000.4: name, one 4-byte field, then width, height, image size,
    mips stripped, format, mip count ... and the pixel data streamed from a .resS file at the end."""
    r = Reader(raw)
    r.astr(); r.i32()
    w, h, size = r.i32(), r.i32(), r.i32()
    r.i32(); fmt = r.i32()
    end = raw.rfind(b".resS")
    if end == -1:
        raise ValueError("texture data is not streamed; not handled")
    for start in range(end, max(end - 256, 4), -1):
        n = struct.unpack_from("<i", raw, start - 4)[0]
        if 0 < n and start + n == end + 5:
            stream_size = struct.unpack_from("<I", raw, start - 8)[0]
            offset = struct.unpack_from("<Q", raw, start - 16)[0]
            name = raw[start:start + n].decode().split("/")[-1]
            with open(os.path.join(data_dir, name), "rb") as f:
                f.seek(offset)
                return w, h, fmt, f.read(stream_size)
    raise ValueError("could not find the stream data reference")


def to_rgba(w, h, fmt, data):
    n = w * h
    if fmt == 4:  # RGBA32
        px = data[:n * 4]
    elif fmt == 3:  # RGB24
        px = b"".join(data[i:i + 3] + b"\xff" for i in range(0, n * 3, 3))
    elif fmt == 5:  # ARGB32
        px = b"".join(data[i + 1:i + 4] + data[i:i + 1] for i in range(0, n * 4, 4))
    elif fmt == 14:  # BGRA32
        px = b"".join(data[i + 2:i + 3] + data[i + 1:i + 2] + data[i:i + 1] + data[i + 3:i + 4] for i in range(0, n * 4, 4))
    else:
        raise ValueError(f"texture format {fmt} not handled (compressed?)")
    rows = [px[y * w * 4:(y + 1) * w * 4] for y in range(h)]
    return b"".join(reversed(rows))  # Unity stores rows bottom-up


def extract(data_dir, out_dir, prefixes):
    """Save every texture whose Resources path starts with one of prefixes. Returns {path: png file}."""
    os.makedirs(out_dir, exist_ok=True)
    ggm = SerializedFile(os.path.join(data_dir, "globalgamemanagers"))
    files, saved = {}, {}
    for path, file_id, path_id in resource_paths(ggm):
        if not any(path.lower().startswith(p.lower()) for p in prefixes):
            continue
        name = "globalgamemanagers" if file_id == 0 else os.path.basename(ggm.externals[file_id - 1])
        sf = files.get(name) or files.setdefault(name, SerializedFile(os.path.join(data_dir, name)))
        class_id, raw = sf.object(path_id)
        if class_id != CLASS_TEXTURE2D:
            continue  # the matching Sprite; its texture is what we want
        w, h, fmt, data = read_texture(raw, data_dir)
        png = os.path.join(out_dir, path.replace("/", "__") + ".png")
        write_png(png, w, h, to_rgba(w, h, fmt, data))
        saved[path.lower()] = png
        print(f"{path}: {w}x{h}")
    return saved


if __name__ == "__main__":
    if len(sys.argv) < 4:
        raise SystemExit(__doc__)
    extract(sys.argv[1], sys.argv[2], sys.argv[3:])
