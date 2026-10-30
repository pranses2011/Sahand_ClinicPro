#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
لوگوموشن «مونتاژ المان‌به‌المان» سامانه جامع مدیریت درمانگاه سهند — Sahand ClinicPro

هر جزء لوگو (نوار سبز، هلال سبز، خانهٔ آبی، صلیب، لوله‌های گوشی و صفحهٔ گوشی)
جدا می‌شود و با انیمیشن مخصوص خودش می‌آید تا در پایان لوگوی کامل ساخته شود.

خروجی‌ها:
    logo-motion-square.mp4     1080×1080 · 60fps · با صدا (فید/اینستاگرام)
    logo-motion-vertical.mp4   1080×1920 · 60fps · با صدا (ریلز/استوری)
    logo-motion-alpha.mov      1080×1080 · 30fps · ProRes 4444 با کانال آلفا (تدوین)

اجرا:
    python3 build_logo_motion.py
    python3 build_logo_motion.py --only square
    python3 build_logo_motion.py --preview        # تصویر المان‌های جدا‌شده
    python3 build_logo_motion.py --stills 0.7,1.2,1.8,2.4,4

پیش‌نیازها: pillow, numpy, arabic-reshaper, python-bidi, imageio-ffmpeg
"""

from __future__ import annotations

import argparse
import math
import subprocess
import sys
import tempfile
from collections import deque
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageEnhance, ImageFilter

sys.path.insert(0, str(Path(__file__).resolve().parent))
import build_promo as bp  # noqa: E402

HERE = Path(__file__).resolve().parent
WORK = Path(tempfile.gettempdir()) / "sahand_logobuild"
FFMPEG = bp.FFMPEG
FPS = 60
ALPHA_FPS = 30
SR = 48000
C = bp.C
ALPHA_FORMAT = "mov"   # VP9/WebM در این نسخهٔ ffmpeg آلفا را دور می‌ریزد؛ ProRes 4444 مطمئن است

# ───────────────────────────── زمان‌بندی (ثانیه) ─────────────────────────────
T_TOTAL = 7.0
T_FADE_OUT = 6.42

# (شروع، مدت) ورود هر المان — ترتیب: نوار، هلال، خانه، گوشی، صفحه، صلیب
T_BAR = (0.30, 0.48)
T_ARC = (0.52, 0.56)
T_HOUSE = (0.80, 0.55)
T_TUBES = (1.24, 0.48)
T_CHEST = (1.52, 0.42)
T_CROSS = (1.84, 0.42)
T_SHINE = (2.06, 0.85)
T_TITLE = (2.58, 0.80)
T_LATIN = (2.90, 0.70)
T_DIVIDER = (3.16, 0.45)
T_SUBTITLE = (3.34, 0.60)

LAND_BAR, LAND_ARC, LAND_HOUSE = 0.71, 1.04, 1.33
LAND_TUBES, LAND_CHEST, LAND_CROSS = 1.66, 1.90, 2.22

LAYOUTS = {
    "square": dict(W=1080, H=1080, logo=0.470, logo_cy=0.394, title_size=0.086, title_cy=0.647,
                   latin_size=0.025, latin_cy=0.713, div_y=0.749, sub_size=0.035, sub_cy=0.789),
    "vertical": dict(W=1080, H=1920, logo=0.520, logo_cy=0.425, title_size=0.088, title_cy=0.610,
                     latin_size=0.026, latin_cy=0.670, div_y=0.702, sub_size=0.035, sub_cy=0.738),
}

LOGO_SRC = bp.LOGO


# ───────────────────────────── ابزار ریاضی ─────────────────────────────
def clamp01(v):
    return 0.0 if v < 0 else (1.0 if v > 1 else v)


def seg(t, ab):
    a, b = ab
    return clamp01((t - a) / max(1e-6, b - a))


def ease_out(t):
    t = clamp01(t)
    return 1 - (1 - t) ** 3


def ease_in_out(t):
    t = clamp01(t)
    return t * t * (3 - 2 * t)


def ease_out_back(t, k=2.2):
    t = clamp01(t) - 1.0
    return 1 + t * t * ((k + 1) * t + k)


def spring(t, damp=6.4, freq=9.6):
    if t <= 0:
        return 0.0
    if t >= 1:
        return 1.0
    return 1 - math.exp(-damp * t) * math.cos(freq * t)


def blend(dst: Image.Image, src: Image.Image, x, y, op=1.0):
    if op <= 0.004:
        return
    x, y = int(round(x)), int(round(y))
    x0, y0 = max(0, x), max(0, y)
    x1, y1 = min(dst.width, x + src.width), min(dst.height, y + src.height)
    if x1 <= x0 or y1 <= y0:
        return
    if (x0, y0, x1, y1) != (x, y, x + src.width, y + src.height):
        src = src.crop((x0 - x, y0 - y, x1 - x, y1 - y))
    if op < 0.996:
        src = src.copy()
        src.putalpha(src.getchannel("A").point(lambda v: int(v * op)))
    dst.alpha_composite(src, (x0, y0))


# ───────────────────────────── جداسازی المان‌های لوگو ─────────────────────────────
def _label(mask: np.ndarray):
    H, W = mask.shape
    lab = np.zeros((H, W), np.int32)
    cur = 0
    for y in range(H):
        row = mask[y]
        for x in range(W):
            if row[x] and lab[y, x] == 0:
                cur += 1
                q = deque([(y, x)])
                lab[y, x] = cur
                while q:
                    cy, cx = q.popleft()
                    for dy, dx in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                        ny, nx = cy + dy, cx + dx
                        if 0 <= ny < H and 0 <= nx < W and mask[ny, nx] and lab[ny, nx] == 0:
                            lab[ny, nx] = cur
                            q.append((ny, nx))
    return lab, cur


def _dilate(mask: np.ndarray, r: int = 2) -> np.ndarray:
    out = mask.copy()
    for _ in range(r):
        out = out | np.roll(out, 1, 0) | np.roll(out, -1, 0) | np.roll(out, 1, 1) | np.roll(out, -1, 1)
    return out


def _largest(mask: np.ndarray) -> np.ndarray:
    lab, n = _label(mask)
    if n == 0:
        return mask
    sizes = [(int((lab == i).sum()), i) for i in range(1, n + 1)]
    return lab == max(sizes)[1]


def _keep_big(mask: np.ndarray, min_px=900) -> np.ndarray:
    lab, n = _label(mask)
    out = np.zeros_like(mask)
    for i in range(1, n + 1):
        m = lab == i
        if m.sum() >= min_px:
            out |= m
    return out


def segment_logo(path=LOGO_SRC):
    """لوگو را به شش المان سازنده تقسیم می‌کند: bar, arc, house, cross, tubes, chest."""
    im = Image.open(path).convert("RGBA")
    a = np.array(im).astype(np.float32)
    r, g, b, al = a[..., 0], a[..., 1], a[..., 2], a[..., 3]
    H, W = al.shape
    yy, xx = np.mgrid[0:H, 0:W]

    # صلیب: تنها مؤلفهٔ متصل جدا از بدنهٔ لوگو
    lab, n = _label(al > 200)
    sizes = {i: int((lab == i).sum()) for i in range(1, n + 1)}
    cross_core = lab == min(sizes, key=sizes.get)
    cross = _largest(_dilate(cross_core, 3))

    # سبزها (نوار بالا و هلال پایین)
    green = ((g - np.maximum(r, b)) > 6) & (al > 12)
    green = _dilate(green, 1)
    bar_zone = (yy < 362) & (xx > 880)
    green_bar = _largest(green & bar_zone)
    green_arc = _largest(green & ~bar_zone)

    # گوشی پزشکی: ناحیهٔ هندسی دقیق (لوله‌ها + حلقهٔ گوشی) و صفحهٔ گوشی
    chest_disk = ((xx - 940) ** 2 + (yy - 952) ** 2) <= 160 ** 2
    region = (((yy >= 482) & (yy <= 730) & (xx >= 893)) |
              ((yy > 730) & (yy <= 878) & (xx >= 925)) |
              chest_disk)
    steth = region & (al > 12) & ~green & ~cross
    chest = _largest(steth & chest_disk)
    tubes = _largest(steth & ~chest_disk)
    # حذف هالهٔ خاکستری سایه از لوله‌ها (فقط پیکسل‌های آبی‌رنگ گوشی می‌مانند)
    chroma = np.clip(((b - r) - 18.0) / 46.0, 0, 1)
    floor = np.clip((al - 30.0) / 40.0, 0, 1)
    tubes_alpha_w = chroma * floor
    chest_alpha_w = floor

    house = (al > 12) & ~green & ~cross & ~steth
    house = _keep_big(house, 2500)

    masks = dict(bar=green_bar, arc=green_arc, house=house, cross=cross, tubes=tubes, chest=chest)
    soft = {"tubes": tubes_alpha_w, "chest": chest_alpha_w}
    layers = {}
    for name, m in masks.items():
        arr = a.copy()
        arr[..., 3] = a[..., 3] * m * (soft.get(name, 1.0))
        img = Image.fromarray(arr.astype(np.uint8), "RGBA")
        bb = img.getchannel("A").point(lambda v: 255 if v > 12 else 0).getbbox()
        layers[name] = (img.crop(bb), bb)  # (تصویر برش‌خورده، جعبه در مختصات لوگو)
    return layers, (W, H)


# ───────────────────────────── المان متحرک ─────────────────────────────
class Element:
    """یک المان لوگو با انیمیشن ورود اختصاصی."""

    def __init__(self, name, img, box, logo_px, origin, order):
        self.name = name
        self.box = box
        self.origin = origin
        self.order = order
        # مقیاس‌دهی به اندازهٔ نهایی روی بوم
        self.s = logo_px / 1254.0
        w = max(2, int(round(img.width * self.s)))
        h = max(2, int(round(img.height * self.s)))
        self.base = img.resize((w, h), Image.LANCZOS)
        # مرکز المان روی بوم
        cx = origin[0] + (box[0] + box[2]) / 2 * self.s
        cy = origin[1] + (box[1] + box[3]) / 2 * self.s
        self.center = (cx, cy)

    def draw(self, dst, t, angle_pivot=None):
        st = self.state(t)
        if st is None:
            return
        s, ang, dx, dy, op, blur = st
        img = self.base
        if abs(s - 1.0) > 0.002:
            img = img.resize((max(2, int(img.width * s)), max(2, int(img.height * s))), Image.BICUBIC)
        if abs(ang) > 0.15:
            img = img.rotate(ang, resample=Image.BICUBIC, expand=True)
        if blur > 0.4:
            img = img.filter(ImageFilter.GaussianBlur(blur))
        px = self.center[0] + dx - img.width / 2
        py = self.center[1] + dy - img.height / 2
        if op < 0.999:
            img = img.copy()
            img.putalpha(img.getchannel("A").point(lambda v: int(v * op)))
        dst.alpha_composite(img, (int(round(px)), int(round(py))))

    # هر المان حالت خودش را تعریف می‌کند
    def state(self, t):
        return None


# ───────────────────────────── پس‌زمینه و ذرات ─────────────────────────────
def background(W, H, seed=7) -> Image.Image:
    rng = np.random.default_rng(seed)
    yy = np.arange(H, dtype=np.float32)[:, None]
    xx = np.arange(W, dtype=np.float32)[None, :]
    top, bottom = np.array([4, 9, 24], np.float32), np.array([10, 34, 62], np.float32)
    tr = (yy / H)
    base = top[None, None, :] * (1 - tr)[:, :, None] + bottom[None, None, :] * tr[:, :, None]
    base = np.repeat(base, W, axis=1)

    for gx, gy, gr, col, st, fall in [
        (0.50, 0.36, 0.62, (20, 74, 132), 0.90, 2.2),
        (0.86, 0.10, 0.42, (14, 116, 144), 0.45, 2.0),
        (0.12, 0.86, 0.45, (16, 118, 96), 0.30, 2.0),
    ]:
        d = np.sqrt(((xx - gx * W) / (gr * W)) ** 2 + ((yy - gy * H) / (gr * W)) ** 2)
        m = np.clip(1 - d, 0, 1) ** fall * st
        base = base * (1 - m[:, :, None]) + np.array(col, np.float32) * m[:, :, None]

    step = max(30, int(W / 24))
    gy_ = ((yy + step * 0.5) % step) - step * 0.5
    gx_ = ((xx + step * 0.5) % step) - step * 0.5
    grid = np.clip(1 - np.sqrt(gx_ ** 2 + gy_ ** 2) / (step * 0.030), 0, 1) ** 1.8
    dm = np.sqrt(((xx - W * 0.5) / (W * 0.75)) ** 2 + ((yy - H * 0.42) / (W * 0.75)) ** 2)
    grid *= np.clip(1 - dm, 0, 1) ** 1.6
    base += grid[:, :, None] * np.array([120, 170, 235], np.float32) * 0.35

    dv = np.sqrt(((xx - W / 2) / (W * 0.80)) ** 2 + ((yy - H / 2) / (H * 0.74)) ** 2)
    vig = np.clip(1.0 - 0.62 * np.clip(dv - 0.32, 0, 2) ** 1.5, 0.12, 1.0)
    base *= vig[:, :, None]
    base += rng.normal(0, 1.5, (H, W, 1)).astype(np.float32)
    return Image.fromarray(np.clip(base, 0, 255).astype(np.uint8), "RGB")


def make_particles(W, H, n=80, seed=3):
    rng = np.random.default_rng(seed)
    ang = rng.uniform(0, 2 * np.pi, n)
    rad = rng.uniform(0.20, 0.62, n) * W
    return dict(ang=ang, rad=rad, size=rng.uniform(1.1, 3.2, n),
                phase=rng.uniform(0, 2 * np.pi, n), speed=rng.uniform(0.6, 2.2, n),
                drift=rng.uniform(-0.4, 0.4, n), hue=rng.uniform(0, 1, n))


def draw_particles(size, P, t, intensity=1.0) -> Image.Image:
    layer = Image.new("RGBA", size, (0, 0, 0, 0))
    d = ImageDraw.Draw(layer)
    W, H = size
    ready = clamp01((t - 0.15) / 0.9)
    for i in range(len(P["rad"])):
        r = P["rad"][i] + P["speed"][i] * 14 * max(0.0, t - 0.5)
        ang = P["ang"][i] + P["drift"][i] * t * 0.05
        x = W / 2 + math.cos(ang) * r
        y = H * 0.40 + math.sin(ang) * r * 0.90
        if not (-20 < x < W + 20 and -20 < y < H + 20):
            continue
        tw = 0.40 + 0.60 * (0.5 + 0.5 * math.sin(P["phase"][i] + t * 2.1))
        a = int(135 * tw * intensity * ready)
        if a <= 3:
            continue
        s = P["size"][i]
        col = (150, 210, 255) if P["hue"][i] < 0.74 else (120, 235, 190)
        d.ellipse((x - s * 2.8, y - s * 2.8, x + s * 2.8, y + s * 2.8), fill=col + (a // 7,))
        d.ellipse((x - s, y - s, x + s, y + s), fill=col + (a,))
    return layer


def gaussian_band(w, h, center, width, angle_deg=0.0, color=(255, 255, 255)) -> Image.Image:
    yy = np.arange(h, dtype=np.float32)[:, None]
    xx = np.arange(w, dtype=np.float32)[None, :]
    a = math.radians(angle_deg)
    proj = xx * math.cos(a) + yy * math.sin(a)
    alpha = np.exp(-0.5 * ((proj - center) / max(width, 1e-3)) ** 2)
    rgb = np.empty((h, w, 3), np.float32)
    rgb[..., 0], rgb[..., 1], rgb[..., 2] = color
    return Image.fromarray(np.dstack([rgb, alpha * 255.0]).astype(np.uint8), "RGBA")


def ellipse_layer(radius, alpha, width, col, pad=12) -> Image.Image:
    side = int(2 * (radius + pad))
    img = Image.new("RGBA", (side, side), (0, 0, 0, 0))
    c = side / 2
    ImageDraw.Draw(img).ellipse((c - radius, c - radius, c + radius, c + radius),
                                outline=tuple(col) + (int(alpha),), width=max(1, width))
    return img


def wipe(layer, p, feather=70, mode="rtl"):
    if p >= 1:
        return layer
    if p <= 0:
        return None
    arr = np.array(layer)
    h, w = arr.shape[:2]
    xs = np.arange(w, dtype=np.float32)[None, :]
    if mode == "rtl":
        edge = w - p * (w + feather)
        m = np.clip((xs - edge) / feather, 0, 1)
    else:
        edge = p * (w + feather) - feather
        m = np.clip((edge - xs) / feather, 0, 1)
    arr[..., 3] = (arr[..., 3].astype(np.float32) * m).astype(np.uint8)
    return Image.fromarray(arr, "RGBA")


def angle_sweep(layer, p, center, a0, a1, feather=0.15):
    """نمایش تدریجی بر اساس زاویه حول یک مرکز (برای کشیده‌شدن هلال)."""
    if p >= 1:
        return layer
    if p <= 0:
        return None
    arr = np.array(layer)
    h, w = arr.shape[:2]
    yy = np.arange(h, dtype=np.float32)[:, None] + 0
    xx = np.arange(w, dtype=np.float32)[None, :] + 0
    ang = np.degrees(np.arctan2(yy - center[1], xx - center[0])) % 360
    start = a0 % 360
    span = (a1 - a0) % 360
    rel = (ang - start) % 360
    m = np.clip((span - rel) / max(1e-3, span * feather), 0, 1)
    m = np.where(rel <= span, 1.0, m)
    # پنجرهٔ نرم در جهت پیشرفت
    reveal = span * p
    m = np.clip((reveal - rel) / max(1.0, span * feather * 0.4), 0, 1)
    arr[..., 3] = (arr[..., 3].astype(np.float32) * m).astype(np.uint8)
    return Image.fromarray(arr, "RGBA")


# ───────────────────────────── صحنه ─────────────────────────────
class LogoBuild:
    def __init__(self, name: str):
        self.lay = LAYOUTS[name]
        self.name = name
        W, H = self.lay["W"], self.lay["H"]
        self.W, self.H = W, H
        self.bg_img = background(W, H)
        self.particles = make_particles(W, H)

        self.logo_px = int(W * self.lay["logo"])
        self.s = self.logo_px / 1254.0
        logo_cy = int(H * self.lay["logo_cy"])
        self.origin = ((W - self.logo_px) / 2, logo_cy - self.logo_px / 2)
        self.mid = (W / 2, logo_cy)

        layers, _ = segment_logo()
        self.parts = {}
        for nm, (img, box) in layers.items():
            w = max(2, int(round(img.width * self.s)))
            h = max(2, int(round(img.height * self.s)))
            self.parts[nm] = (img.resize((w, h), Image.LANCZOS), box, self._center(box))

        self.shine_full = gaussian_band(2400, 2400, center=0, width=70, angle_deg=48)

        title_size = int(W * self.lay["title_size"])
        self.title = bp.text_layer("درمانگاه سهند", title_size, "black",
                                   gradient=((255, 255, 255), (156, 214, 255)), align="center",
                                   shadow=0.55, shadow_blur=int(title_size * 0.15))
        self.subtitle = bp.text_layer("سامانه جامع مدیریت درمانگاه", int(W * self.lay["sub_size"]),
                                      "medium", (176, 208, 236), align="center",
                                      shadow=0.4, shadow_blur=10)
        latin_size = int(W * self.lay["latin_size"])
        self.latin_frames = [
            bp.text_layer("SAHAND CLINICPRO", latin_size, "semibold", C["sky"], align="center",
                          tracking=int(latin_size * (0.05 + 0.34 * i / 23)), direction="ltr",
                          shadow=0.45, shadow_blur=8)
            for i in range(24)
        ]

    def _center(self, box):
        return (self.origin[0] + (box[0] + box[2]) / 2 * self.s,
                self.origin[1] + (box[1] + box[3]) / 2 * self.s)

    # ── ابزار ترسیم المان
    def zoom_at(self, t):
        """پالس کوچک کل نشان هنگام نشستن صفحهٔ گوشی."""
        if t < LAND_CHEST:
            return 1.0
        return 1.0 + 0.018 * math.exp(-(t - LAND_CHEST) / 0.16)

    def _put(self, dst, name, *, scale=1.0, angle=0.0, dx=0.0, dy=0.0, op=1.0, blur=0.0,
             alpha_hook=None, zoom=1.0):
        img, box, center = self.parts[name]
        center = (self.mid[0] + (center[0] - self.mid[0]) * zoom,
                  self.mid[1] + (center[1] - self.mid[1]) * zoom)
        scale *= zoom
        base = img
        if abs(scale - 1.0) > 0.002:
            base = base.resize((max(2, int(base.width * scale)), max(2, int(base.height * scale))),
                               Image.BICUBIC)
        if alpha_hook is not None:
            base = alpha_hook(base, (center[0] - base.width / 2, center[1] - base.height / 2))
            if base is None:
                return
        if abs(angle) > 0.15:
            base = base.rotate(angle, resample=Image.BICUBIC, expand=True)
        if blur > 0.35:
            base = base.filter(ImageFilter.GaussianBlur(blur))
        if op < 0.999:
            base = base.copy()
            base.putalpha(base.getchannel("A").point(lambda v: int(v * op)))
        dst.alpha_composite(base, (int(round(center[0] + dx - base.width / 2)),
                                   int(round(center[1] + dy - base.height / 2))))

    # ── اجزا
    def draw_bar(self, dst, t):
        p = seg(t, T_BAR)
        if p <= 0:
            return
        e = spring(p)
        self._put(dst, "bar", zoom=self.zoom_at(t), scale=0.82 + 0.18 * e, dx=0, dy=-96 * (1 - e),
                  op=clamp01(p * 3), blur=2.2 * (1 - ease_out(p)))

    def draw_arc(self, dst, t):
        p = seg(t, T_ARC)
        if p <= 0:
            return
        e = ease_in_out(p)
        # مرکز چرخش هلال (نزدیک مرکز لوگو) برای sweep
        cx_logo, cy_logo = 620.0, 700.0
        def hook(base, pos):
            cxl = cx_logo * self.s + self.origin[0] - pos[0]
            cyl = cy_logo * self.s + self.origin[1] - pos[1]
            return angle_sweep(base, e, (cxl, cyl), 196.0, 52.0, feather=0.35)
        self._put(dst, "arc", zoom=self.zoom_at(t), angle=-4.0 * (1 - e), dy=14 * (1 - e), op=clamp01(p * 2.5),
                  alpha_hook=hook)

    def draw_house(self, dst, t):
        p = seg(t, T_HOUSE)
        if p <= 0:
            return
        e = ease_in_out(p)
        def hook(base, pos):
            arr = np.array(base)
            h = arr.shape[0]
            ynorm = (np.arange(h, dtype=np.float32) / max(1, h - 1))[:, None]
            front = -0.12 + 1.30 * e
            m = np.clip((front - ynorm) / 0.14, 0, 1)
            arr[..., 3] = (arr[..., 3].astype(np.float32) * m).astype(np.uint8)
            return Image.fromarray(arr, "RGBA")
        self._put(dst, "house", zoom=self.zoom_at(t), scale=0.975 + 0.025 * e,
                  blur=2.6 * (1 - ease_out(p)), alpha_hook=hook)

    def draw_cross(self, dst, t):
        p = seg(t, T_CROSS)
        if p <= 0:
            return
        e = ease_out_back(p, 2.6)
        self._put(dst, "cross", zoom=self.zoom_at(t), scale=0.35 + 0.65 * e, angle=-14 * (1 - ease_out(p)),
                  op=clamp01(p * 3.2), blur=2.4 * (1 - ease_out(p)))
        # فلاش کوتاه هنگام نشستن صلیب
        d = (t - LAND_CROSS) / 0.13
        if abs(d) < 3:
            a = math.exp(-0.5 * d * d) * 0.5
            r = self.logo_px * 0.20
            side = int(r * 5)
            img = Image.new("RGBA", (side, side), (0, 0, 0, 0))
            dd = ImageDraw.Draw(img)
            c = side / 2
            for rr, aa in ((r * 1.5, 60), (r * 0.8, 120), (r * 0.3, 200)):
                dd.ellipse((c - rr, c - rr, c + rr, c + rr), fill=(210, 245, 255, int(aa * a)))
            img = img.filter(ImageFilter.GaussianBlur(side // 10))
            blend(dst, img, self.mid[0] - side / 2, self.mid[1] - side / 2, 1.0)

    def draw_tubes(self, dst, t):
        p = seg(t, T_TUBES)
        if p <= 0:
            return
        e = spring(p)
        self._put(dst, "tubes", zoom=self.zoom_at(t), angle=26 * (1 - e), dx=78 * (1 - e), dy=-38 * (1 - e),
                  scale=0.96 + 0.04 * e, op=clamp01(p * 3), blur=2.2 * (1 - ease_out(p)))

    def draw_chest(self, dst, t):
        p = seg(t, T_CHEST)
        if p <= 0:
            return
        e = ease_out_back(p, 2.4)
        self._put(dst, "chest", zoom=self.zoom_at(t), scale=0.38 + 0.62 * e, angle=-20 * (1 - ease_out(p)),
                  op=clamp01(p * 3.2), blur=2.6 * (1 - ease_out(p)))
        # موج ضربه
        q = seg(t, (LAND_CHEST, LAND_CHEST + 0.95))
        if 0 < q < 1:
            r = self.logo_px * (0.10 + 0.36 * ease_out(q))
            ring = ellipse_layer(r, 190 * (1 - q) ** 1.6, 2, (255, 255, 255), pad=14)
            blend(dst, ring, self.parts["chest"][2][0] - ring.width / 2,
                  self.parts["chest"][2][1] - ring.height / 2, 1.0)

    def draw_shine(self, dst, t):
        p = seg(t, T_SHINE)
        if not 0 < p < 1:
            return
        # نوار نور روی کل نشان
        size = int(self.logo_px * 1.5)
        travel = int(-size * 0.5 + p * size * 2.0)
        crop = self.shine_full.crop((int(self.shine_full.width / 2 - size / 2 + travel * 0.6),
                                     int(self.shine_full.height / 2 - size / 2),
                                     int(self.shine_full.width / 2 + size / 2 + travel * 0.6),
                                     int(self.shine_full.height / 2 + size / 2)))
        # ماسک با آلفای ترکیب فعلی
        mask = dst.getchannel("A").crop((int(self.mid[0] - size / 2), int(self.mid[1] - size / 2),
                                         int(self.mid[0] + size / 2), int(self.mid[1] + size / 2)))
        arr = np.array(crop.convert("RGBA"), np.float32)
        ma = np.array(mask, np.float32) / 255.0
        if arr.shape[:2] != ma.shape:
            return
        arr[..., 3] *= ma * 0.55 * math.sin(math.pi * p) ** 0.6
        blend(dst, Image.fromarray(arr.astype(np.uint8), "RGBA"),
              self.mid[0] - size / 2, self.mid[1] - size / 2, 1.0)

    # ── تایپوگرافی
    def draw_type(self, dst, t):
        W, H = self.W, self.H
        p = seg(t, T_TITLE)
        if p > 0:
            layer = wipe(self.title, ease_in_out(p), feather=int(W * 0.075), mode="rtl")
            if layer is not None:
                x = int(W / 2 - layer.width / 2)
                y = int(H * self.lay["title_cy"] - layer.height / 2 + (1 - ease_out(p)) * 16)
                blend(dst, layer, x, y, clamp01(p * 2.2))

        p = seg(t, T_LATIN)
        if p > 0:
            idx = min(len(self.latin_frames) - 1, int(ease_out(p) * (len(self.latin_frames) - 1)))
            layer = self.latin_frames[idx]
            blend(dst, layer, int(W / 2 - layer.width / 2),
                  int(H * self.lay["latin_cy"] - layer.height / 2), clamp01(p * 2.0))

        p = seg(t, T_DIVIDER)
        if p > 0:
            w = max(4, int(W * 0.19 * ease_out(p)))
            line = Image.new("RGBA", (w, 3), (120, 200, 245, int(185 * clamp01(p * 1.6))))
            line = line.filter(ImageFilter.GaussianBlur(1.1))
            blend(dst, line, W // 2 - w // 2, int(H * self.lay["div_y"]) - 1, 1.0)

        p = seg(t, T_SUBTITLE)
        if p > 0:
            layer = wipe(self.subtitle, ease_in_out(p), feather=int(W * 0.06), mode="rtl")
            if layer is not None:
                blend(dst, layer, int(W / 2 - layer.width / 2),
                      int(H * self.lay["sub_cy"] - layer.height / 2), clamp01(p * 2.0))

    # ── فریم
    def frame(self, t: float, alpha=False):
        if alpha:
            dst = Image.new("RGBA", (self.W, self.H), (0, 0, 0, 0))
        else:
            k = 0.22 + 0.78 * ease_out(seg(t, (0.0, 0.5)))
            dst = ImageEnhance.Brightness(self.bg_img).enhance(k).convert("RGBA")
            dst.alpha_composite(draw_particles((self.W, self.H), self.particles, t,
                                               intensity=clamp01(t / 0.7)))

        # گردآمدن المان‌ها به ترتیب عمق
        self.draw_bar(dst, t)
        self.draw_arc(dst, t)
        self.draw_house(dst, t)
        self.draw_tubes(dst, t)
        self.draw_chest(dst, t)
        self.draw_cross(dst, t)
        self.draw_shine(dst, t)
        self.draw_type(dst, t)

        if alpha:
            return dst
        arr = np.array(dst.convert("RGB"), np.float32)
        if t > T_FADE_OUT:
            arr *= max(0.0, 1.0 - (t - T_FADE_OUT) / (T_TOTAL - T_FADE_OUT))
        return np.clip(arr, 0, 255).astype(np.uint8)


# ───────────────────────────── صدا ─────────────────────────────
def _lowpass(x, cutoff_hz, sr=SR):
    a = math.exp(-2 * math.pi * cutoff_hz / sr)
    y = np.empty_like(x)
    acc = 0.0
    for i in range(len(x)):
        acc = a * acc + (1 - a) * x[i]
        y[i] = acc
    return y


def make_sound(total=T_TOTAL) -> np.ndarray:
    n = int(total * SR)
    t = np.arange(n, dtype=np.float32) / SR
    rng = np.random.default_rng(11)
    mix = np.zeros(n, np.float32)
    noise = rng.normal(0, 1, n).astype(np.float32)

    def bell(t0, f0, dec, amp, parts=((1, 1.0), (2.01, 0.35), (3.02, 0.14))):
        tt = t - t0
        sig = np.zeros(n, np.float32)
        for h, ha in parts:
            sig += ha * np.sin(2 * np.pi * f0 * h * tt)
        return np.where(tt >= 0, sig * np.exp(-np.clip(tt, 0, None) / dec), 0).astype(np.float32) * amp

    def tick(t0, f0, dec=0.055, amp=0.10):
        tt = t - t0
        s = np.sin(2 * np.pi * f0 * tt) * np.exp(-np.clip(tt, 0, None) / dec)
        return np.where(tt >= 0, s, 0).astype(np.float32) * amp

    # رایزر و بستر
    riser_env = (np.clip(t / 0.9, 0, 1) ** 2 * np.clip((1.2 - t) / 0.3, 0, 1)).astype(np.float32)
    mix += _lowpass(noise, 2400) * riser_env * 0.10

    # ووش هنگام کشیده‌شدن هلال و ورود خانه
    for a, b, amp in ((0.44, 1.10, 0.20), (0.70, 1.42, 0.16), (1.18, 1.90, 0.12)):
        env = (np.clip((t - a) / 0.18, 0, 1) * np.clip((b - t) / 0.35, 0, 1)).astype(np.float32)
        mix += _lowpass(noise, 700 + 600 * (a > 1.5)) * env * amp

    # تیک‌های ورود هر المان (زیر و بم به ترتیب)
    mix += tick(LAND_BAR, 620, 0.045, 0.085)
    mix += tick(LAND_ARC, 500, 0.06, 0.075)
    mix += tick(LAND_HOUSE, 300, 0.09, 0.11)
    mix += bell(LAND_CROSS, 1180, 0.16, 0.075, parts=((1, 1.0), (2.0, 0.3)))
    mix += tick(LAND_TUBES, 860, 0.05, 0.065)

    # ضربهٔ اصلی هنگام نشستن صفحهٔ گوشی
    tp = t - LAND_CHEST
    thump = np.sin(2 * np.pi * 62.0 * tp) * np.exp(-np.clip(tp, 0, None) * 6.0)
    thump = np.where(tp >= 0, thump, 0).astype(np.float32)
    click = _lowpass(rng.normal(0, 1, n).astype(np.float32), 5200) * np.exp(-np.clip(tp, 0, None) * 45)
    click = np.where(tp >= 0, click, 0).astype(np.float32)
    mix += thump * 0.46 + click * 0.10

    # شیمِر نور
    shimmer_env = (np.clip((t - 2.06) / 0.30, 0, 1) * np.clip((2.82 - t) / 0.45, 0, 1)).astype(np.float32)
    mix += (noise - _lowpass(noise, 5200)) * shimmer_env * 0.045

    # زنگ‌های تایپوگرافی
    mix += bell(2.64, 1046.5, 0.90, 0.095)
    mix += bell(2.96, 1568.0, 0.70, 0.052)
    mix += bell(3.40, 1318.5, 0.80, 0.046)

    # پد پایه
    pad = (np.sin(2 * np.pi * 82.4 * t) * 0.10 + np.sin(2 * np.pi * 123.5 * t) * 0.06
           + np.sin(2 * np.pi * 164.8 * t) * 0.035)
    pad *= np.clip(t / 1.4, 0, 1) * np.clip((total - 0.2 - t) / 1.6, 0, 1)
    mix += (pad * 0.5).astype(np.float32)

    mix *= np.clip((total - t) / 0.65, 0, 1)
    mix = np.tanh(mix * 1.9) / np.tanh(1.9)
    mix /= max(1e-6, np.max(np.abs(mix))) * 1.05
    right = np.roll(mix, int(SR * 0.006)) * 0.99
    return np.stack([mix, right], axis=1).astype(np.float32)


# ───────────────────────────── ساخت ─────────────────────────────
def preview_elements():
    layers, (W, H) = segment_logo()
    names = list(layers)
    tints = dict(bar=(70, 230, 90), arc=(190, 255, 120), house=(255, 200, 60),
                 cross=(255, 80, 200), tubes=(0, 235, 255), chest=(255, 255, 255))
    tiles = []
    for nm in names:
        img, box = layers[nm]
        tile = Image.new("RGB", (520, 520), (12, 18, 34))
        sc = min(460 / img.width, 460 / img.height, 1.0)
        prev = img.resize((max(1, int(img.width * sc)), max(1, int(img.height * sc))), Image.LANCZOS)
        tile.paste(prev, ((520 - prev.width) // 2, (520 - prev.height) // 2), prev)
        ImageDraw.Draw(tile).text((12, 12), nm, fill=tints[nm])
        tiles.append(tile)
    sheet = Image.new("RGB", (520 * 3, 520 * 2), (12, 18, 34))
    for i, tl in enumerate(tiles):
        sheet.paste(tl, ((i % 3) * 520, (i // 3) * 520))
    p = WORK / "elements.png"
    WORK.mkdir(exist_ok=True, parents=True)
    sheet.save(p)
    print("✓", p)


def render_video(name: str, alpha=False, stills=None):
    WORK.mkdir(parents=True, exist_ok=True)
    lm = LogoBuild(name)
    W, H = lm.W, lm.H

    if stills:
        for ts in stills:
            out = WORK / f"still_{name}_{float(ts):05.2f}.png"
            fr = lm.frame(float(ts), alpha=alpha)
            (fr if isinstance(fr, Image.Image) else Image.fromarray(fr)).save(out)
            print("  ↳", out)
        return

    fps = ALPHA_FPS if alpha else FPS
    frames = int(T_TOTAL * fps)

    if alpha:
        out_path = HERE / f"logo-motion-alpha.{ALPHA_FORMAT}"
        if ALPHA_FORMAT == "webm":
            cmd = [FFMPEG, "-y", "-f", "rawvideo", "-pix_fmt", "rgba", "-s", f"{W}x{H}", "-r", str(fps),
                   "-i", "-", "-c:v", "libvpx-vp9", "-pix_fmt", "yuva420p", "-b:v", "0", "-crf", "26",
                   "-row-mt", "1", "-auto-alt-ref", "0", "-metadata:s:v:0", "alpha_mode=1", str(out_path)]
        else:
            cmd = [FFMPEG, "-y", "-f", "rawvideo", "-pix_fmt", "rgba", "-s", f"{W}x{H}", "-r", str(fps),
                   "-i", "-", "-c:v", "prores_ks", "-profile:v", "4444", "-pix_fmt", "yuva444p10le",
                   "-vendor", "apl0", "-alpha_bits", "8", "-qscale:v", "26", str(out_path)]
    else:
        wav = WORK / f"sound_{name}.wav"
        bp.write_wav(wav, make_sound(T_TOTAL), sr=SR)
        out_path = HERE / f"logo-motion-{name}.mp4"
        cmd = [FFMPEG, "-y", "-f", "rawvideo", "-pix_fmt", "rgb24", "-s", f"{W}x{H}", "-r", str(fps),
               "-i", "-", "-i", str(wav),
               "-c:v", "libx264", "-preset", "slow", "-crf", "17", "-pix_fmt", "yuv420p",
               "-profile:v", "high", "-level", "4.2", "-g", str(fps * 2),
               "-c:a", "aac", "-b:a", "256k", "-ar", str(SR),
               "-movflags", "+faststart", "-shortest", str(out_path)]

    proc = subprocess.Popen(cmd, stdin=subprocess.PIPE, stderr=subprocess.DEVNULL)
    try:
        for f in range(frames):
            t = f / fps
            fr = lm.frame(t, alpha=alpha)
            proc.stdin.write((np.array(fr, np.uint8) if isinstance(fr, Image.Image) else fr).tobytes())
            if f % (fps * 2) == 0:
                print(f"    {t:4.1f}s / {T_TOTAL}s", flush=True)
    finally:
        proc.stdin.close()
        proc.wait()
    print("✓", out_path, f"({out_path.stat().st_size / 1e6:.1f} MB)")


if __name__ == "__main__":
    ap = argparse.ArgumentParser()
    ap.add_argument("--only", default="all", choices=["all", "square", "vertical", "alpha"])
    ap.add_argument("--stills", default="")
    ap.add_argument("--preview", action="store_true")
    ap.add_argument("--alpha-format", default="mov", choices=["webm", "mov"])
    args = ap.parse_args()
    ALPHA_FORMAT = args.alpha_format
    if args.preview:
        preview_elements()
        sys.exit(0)
    st = [s.strip() for s in args.stills.split(",") if s.strip()] or None
    if st:
        for nm in (["square", "vertical"] if args.only == "all" else
                   ([args.only] if args.only != "alpha" else ["square"])):
            render_video(nm, stills=st)
        sys.exit(0)
    if args.only in ("all", "square"):
        render_video("square")
    if args.only in ("all", "vertical"):
        render_video("vertical")
    if args.only in ("all", "alpha"):
        render_video("square", alpha=True)
