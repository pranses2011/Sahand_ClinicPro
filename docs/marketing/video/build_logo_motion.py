#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
لوگوموشن (Logo Motion) سامانه جامع مدیریت درمانگاه سهند — Sahand ClinicPro

خروجی‌ها:
    logo-motion-square.mp4     1080×1080 · 60fps · با صدا (فید/اینستاگرام)
    logo-motion-vertical.mp4   1080×1920 · 60fps · با صدا (ریلز/استوری)
    logo-motion-alpha.mov      1080×1080 · 30fps · QuickTime با کانال آلفا (تدوین)
    logo-motion-alpha.webm     1080×1080 · 30fps · VP9 با آلفا (وب/OBS)

اجرا:
    python3 build_logo_motion.py
    python3 build_logo_motion.py --only square
    python3 build_logo_motion.py --stills 0.6,1.2,2.0,3.0,4.5

پیش‌نیازها: pillow, numpy, arabic-reshaper, python-bidi, imageio-ffmpeg
"""

from __future__ import annotations

import argparse
import math
import subprocess
import sys
import tempfile
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageEnhance, ImageFilter

sys.path.insert(0, str(Path(__file__).resolve().parent))
import build_promo as bp  # noqa: E402

HERE = Path(__file__).resolve().parent
WORK = Path(tempfile.gettempdir()) / "sahand_logo"
FFMPEG = bp.FFMPEG
FPS = 60
ALPHA_FPS = 30
ALPHA_FORMAT = "webm"
SR = 48000
T_TOTAL = 6.8
C = bp.C

# ───────────────────────────── تایم‌لاین ─────────────────────────────
T_BG_IN = 0.50
T_SPARK = (0.30, 0.80)
T_STREAK = (0.34, 1.02)
T_BADGE = 0.68
T_BADGE_LEN = 1.10
T_RINGS = (1.02, 1.26, 1.52)
T_RING_LEN = 1.45
T_SHINE = (1.45, 2.30)
T_TITLE = (2.10, 2.95)
T_LATIN = (2.50, 3.25)
T_DIVIDER = (2.80, 3.30)
T_SUBTITLE = (3.00, 3.65)
T_FADE_OUT = 6.20
T_LAND = 0.92         # لحظهٔ فرود لوگو (هماهنگ با صدا)

LAYOUTS = {
    "square": dict(W=1080, H=1080, badge=0.34, badge_cy=0.355, title_size=0.088, title_cy=0.600,
                   latin_size=0.026, latin_cy=0.676, div_y=0.714, sub_size=0.034, sub_cy=0.757),
    "vertical": dict(W=1080, H=1920, badge=0.40, badge_cy=0.368, title_size=0.090, title_cy=0.590,
                     latin_size=0.026, latin_cy=0.652, div_y=0.683, sub_size=0.034, sub_cy=0.718),
}


# ───────────────────────────── ابزار ─────────────────────────────
def clamp01(v):
    return 0.0 if v < 0 else (1.0 if v > 1 else v)


def seg(t, a, b):
    return clamp01((t - a) / max(1e-6, b - a))


def ease_out(t):
    t = clamp01(t)
    return 1 - (1 - t) ** 3


def ease_in_out(t):
    t = clamp01(t)
    return t * t * (3 - 2 * t)


def spring(t):
    """فنر میرا: از ۰ آغاز و با فراگذشت ملایم به ۱ می‌رسد."""
    if t <= 0:
        return 0.0
    if t >= 1:
        return 1.0
    return 1 - math.exp(-6.2 * t) * math.cos(9.4 * t)


def blend(dst: Image.Image, src: Image.Image, x, y, op=1.0):
    """ترکیب یک لایهٔ RGBA روی تصویر مقصد با شفافیت."""
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


def gaussian_band(w, h, center, width, angle_deg=0.0, color=(255, 255, 255)):
    yy = np.arange(h, dtype=np.float32)[:, None]
    xx = np.arange(w, dtype=np.float32)[None, :]
    a = math.radians(angle_deg)
    proj = xx * math.cos(a) + yy * math.sin(a)
    alpha = np.exp(-0.5 * ((proj - center) / max(width, 1e-3)) ** 2)
    rgb = np.empty((h, w, 3), np.float32)
    rgb[..., 0], rgb[..., 1], rgb[..., 2] = color
    return Image.fromarray(np.dstack([rgb, alpha * 255.0]).astype(np.uint8), "RGBA")


def ellipse_layer(box, radius, alpha, width, col, pad=10):
    """حلقهٔ نازک روی بوم کوچک (سریع) و برگرداندن تصویر + موقعیت."""
    side = int(2 * (radius + pad))
    img = Image.new("RGBA", (side, side), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    c = side / 2
    d.ellipse((c - radius, c - radius, c + radius, c + radius),
              outline=tuple(col) + (int(alpha),), width=max(1, width))
    return img


# ───────────────────────────── اجزا ─────────────────────────────
def plate(size: int, radius_ratio=0.235) -> Image.Image:
    r = int(size * radius_ratio)
    img = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    grad = bp.vertical_gradient((size, size), (247, 252, 255), (176, 219, 249)).convert("RGBA")
    grad.putalpha(bp.rounded_mask((size, size), r))
    img.alpha_composite(grad)

    logo = Image.open(bp.LOGO).convert("RGBA")
    inner = int(size * 0.86)
    img.alpha_composite(logo.resize((inner, inner), Image.LANCZOS),
                        ((size - inner) // 2, int(size * 0.075)))

    gloss = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    ImageDraw.Draw(gloss).rounded_rectangle((0, 0, size - 1, int(size * 0.50)), r,
                                            fill=(255, 255, 255, 46))
    img.alpha_composite(gloss.filter(ImageFilter.GaussianBlur(max(2, size // 24))))
    ImageDraw.Draw(img).rounded_rectangle((0, 0, size - 1, size - 1), r,
                                          outline=(255, 255, 255, 125), width=max(1, size // 170))
    return img


def badge_group(size: int):
    """سایه + هاله + پلاک روی بوم مربع بزرگ‌تر (تا دوران بدون برش انجام شود)."""
    canvas = int(size * 1.9)
    shadow, _ = bp.drop_shadow((size, size), int(size * 0.235), blur=int(size * 0.085),
                               offset=(0, int(size * 0.05)), alpha=200, spread=int(size * 0.012))
    glow, _ = bp.drop_shadow((size, size), int(size * 0.235), blur=int(size * 0.12),
                             offset=(0, 0), alpha=115, spread=int(size * 0.025))

    group = Image.new("RGBA", (canvas, canvas), (0, 0, 0, 0))
    for mask, color in ((glow, (72, 168, 245)), (shadow, (1, 5, 16))):
        tint = Image.new("RGBA", mask.size, color + (255,))
        tint.putalpha(mask)
        group.alpha_composite(tint, ((canvas - mask.width) // 2, (canvas - mask.height) // 2))
    p = plate(size)
    group.alpha_composite(p, ((canvas - size) // 2, (canvas - size) // 2))
    return group, p


def background(W, H, seed=7) -> Image.Image:
    rng = np.random.default_rng(seed)
    yy = np.arange(H, dtype=np.float32)[:, None]
    xx = np.arange(W, dtype=np.float32)[None, :]

    top, bottom = np.array([4, 9, 24], np.float32), np.array([10, 34, 62], np.float32)
    t = (yy / H)
    base = top[None, None, :] * (1 - t)[:, :, None] + bottom[None, None, :] * t[:, :, None]
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
    rad = rng.uniform(0.20, 0.60, n) * W
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


def wipe(layer: Image.Image, p: float, feather=70, mode="rtl"):
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


def centered(img: Image.Image, cx, cy):
    bb = bp.content_box(img)
    return int(round(cx - (bb[0] + bb[2]) / 2)), int(round(cy - (bb[1] + bb[3]) / 2))


# ───────────────────────────── صحنه ─────────────────────────────
class LogoMotion:
    def __init__(self, name: str):
        self.lay = LAYOUTS[name]
        self.name = name
        W, H = self.lay["W"], self.lay["H"]
        self.W, self.H = W, H
        self.bg_img = background(W, H)
        self.particles = make_particles(W, H)
        self.badge_size = int(W * self.lay["badge"])
        self.group, self.plate_img = badge_group(self.badge_size)
        self.badge_center = (W // 2, int(H * self.lay["badge_cy"]))
        self.shine_full = gaussian_band(2000, 2000, center=0, width=46, angle_deg=52)

        title_size = int(W * self.lay["title_size"])
        self.title = bp.text_layer("درمانگاه سهند", title_size, "black",
                                   gradient=((255, 255, 255), (156, 214, 255)), align="center",
                                   shadow=0.55, shadow_blur=int(title_size * 0.15))
        self.subtitle = bp.text_layer("سامانه جامع مدیریت درمانگاه", int(W * self.lay["sub_size"]),
                                      "medium", (176, 208, 236), align="center", shadow=0.4, shadow_blur=10)
        latin_size = int(W * self.lay["latin_size"])
        self.latin_frames = [
            bp.text_layer("SAHAND CLINICPRO", latin_size, "semibold", C["sky"], align="center",
                          tracking=int(latin_size * (0.05 + 0.34 * i / 23)), direction="ltr",
                          shadow=0.45, shadow_blur=8)
            for i in range(24)
        ]

    # ── پس‌زمینه
    def render_bg(self, t) -> Image.Image:
        k = 0.22 + 0.78 * ease_out(seg(t, 0.0, T_BG_IN))
        img = ImageEnhance.Brightness(self.bg_img).enhance(k).convert("RGBA")
        img.alpha_composite(draw_particles((self.W, self.H), self.particles, t,
                                           intensity=clamp01(t / 0.7)))
        return img

    # ── جرقه و شعاع
    def render_spark(self, dst, t):
        cx, cy = self.badge_center
        p = seg(t, *T_SPARK)
        if 0 < p < 1:
            a = math.sin(math.pi * p) ** 1.4
            r = 3 + 24 * ease_out(p)
            side = int(r * 8)
            glow = Image.new("RGBA", (side, side), (0, 0, 0, 0))
            d = ImageDraw.Draw(glow)
            c = side / 2
            for rr, aa in ((r * 3.0, 55), (r * 1.5, 110), (r * 0.6, 235)):
                d.ellipse((c - rr, c - rr, c + rr, c + rr), fill=(205, 240, 255, int(aa * a)))
            glow = glow.filter(ImageFilter.GaussianBlur(side // 12))
            blend(dst, glow, cx - side // 2, cy - side // 2, 1.0)

        p = seg(t, *T_STREAK)
        if 0 < p < 1:
            a = math.sin(math.pi * min(1.0, p * 1.12)) ** 1.2
            w = int(self.W * (0.12 + 1.02 * ease_out(p)))
            band = gaussian_band(w, 30, center=15, width=5.0, angle_deg=0)
            img = Image.new("RGBA", (w, 30), (0, 0, 0, 0))
            img.alpha_composite(band)
            img = img.filter(ImageFilter.GaussianBlur(2.0))
            blend(dst, img, cx - w // 2, cy - 15, 0.9 * a)

    # ── فلاش فرود لوگو
    def render_flash(self, dst, t):
        d = (t - T_LAND) / 0.16
        if abs(d) > 3.2:
            return
        a = math.exp(-0.5 * d * d) * 0.60
        cx, cy = self.badge_center
        r = self.badge_size * (0.62 + 0.30 * min(1.0, abs(d) / 3.0))
        side = int(r * 4.0)
        img = Image.new("RGBA", (side, side), (0, 0, 0, 0))
        dd = ImageDraw.Draw(img)
        c = side / 2
        for rr, aa in ((r, 70), (r * 0.62, 130), (r * 0.24, 220)):
            dd.ellipse((c - rr, c - rr, c + rr, c + rr), fill=(225, 246, 255, int(aa * a)))
        img = img.filter(ImageFilter.GaussianBlur(side // 9))
        blend(dst, img, cx - side // 2, cy - side // 2, 1.0)

    # ── حلقه‌های موج
    def render_rings(self, dst, t):
        cx, cy = self.badge_center
        for i, t0 in enumerate(T_RINGS):
            p = seg(t, t0, t0 + T_RING_LEN)
            if not 0 < p < 1:
                continue
            e = ease_out(p)
            r = self.badge_size * (0.50 + 1.05 * e + 0.08 * i)
            a = (1 - e) ** 1.7 * (0.60 - 0.10 * i)
            col = (110, 205, 255) if i % 2 == 0 else (90, 225, 185)
            layer = ellipse_layer((self.W, self.H), r, 255 * a, 2 + (i == 0), col)
            layer = layer.filter(ImageFilter.GaussianBlur(1.3))
            blend(dst, layer, cx - layer.width // 2, cy - layer.height // 2, 1.0)

    # ── لوگو
    def render_badge(self, dst, t):
        p = seg(t, T_BADGE, T_BADGE + T_BADGE_LEN)
        if p <= 0:
            return
        sp = spring(p)
        scale = 0.50 + 0.50 * sp
        rot = -16.0 * (1 - ease_out(seg(t, T_BADGE, T_BADGE + 0.85)))
        alpha = clamp01((t - T_BADGE) / 0.30)
        blur = 7.0 * (1 - ease_out(seg(t, T_BADGE, T_BADGE + 0.50)))
        float_y = math.sin((t - 2.2) * 1.05) * 4.0 * clamp01((t - 2.2) / 1.2)

        size = max(8, int(round(self.group.width * scale)))
        g = self.group.resize((size, size), Image.LANCZOS)
        if abs(rot) > 0.2:
            g = g.rotate(rot, resample=Image.BICUBIC, expand=False)
        if blur > 0.3:
            g = g.filter(ImageFilter.GaussianBlur(blur))

        sh_p = seg(t, *T_SHINE)
        if 0 < sh_p < 1:
            g = self._shine(g, size, sh_p)

        cx, cy = self.badge_center
        blend(dst, g, cx - size // 2, cy - size // 2 + int(float_y), alpha)

        if t > T_BADGE + 0.55:
            breathe = 0.5 + 0.5 * math.sin((t - 1.2) * 1.4)
            r = self.badge_size * (0.98 + 0.05 * breathe)
            ring = ellipse_layer((self.W, self.H), r, 26 + 22 * breathe, 2, (110, 205, 255), pad=14)
            blend(dst, ring, cx - ring.width // 2, cy - ring.height // 2 + int(float_y * 0.4), 1.0)

    def _shine(self, g: Image.Image, size: int, p: float) -> Image.Image:
        """نوار نور مورب که روی سطح پلاک می‌گذرد."""
        ratio = self.badge_size / self.group.width
        psize = max(6, int(round(size * ratio)))
        off = int((self.group.width - size) // 2) + (size - psize) // 2

        plate = self.plate_img.resize((psize, psize), Image.LANCZOS)
        mask = np.array(plate.getchannel("A"), np.float32) / 255.0

        src_w = self.shine_full.width
        travel = int(-psize * 0.9 + p * (psize * 2.6))
        x0 = int((src_w - psize) / 2 + travel * 0.72)
        x0 = max(0, min(src_w - psize, x0))
        band = np.array(self.shine_full.crop((x0, (src_w - psize) // 2, x0 + psize,
                                             (src_w - psize) // 2 + psize)), np.float32)
        band[..., 3] *= mask * 0.60
        shine = Image.fromarray(band.astype(np.uint8), "RGBA")

        out = g.copy()
        out.alpha_composite(shine, (off, off))
        return out

    # ── تایپوگرافی
    def render_type(self, dst, t):
        W, H = self.W, self.H
        cy = int(H * self.lay["title_cy"])

        p = seg(t, *T_TITLE)
        if p > 0:
            layer = wipe(self.title, ease_in_out(p), feather=int(W * 0.075), mode="rtl")
            if layer is not None:
                x, y = centered(layer, W // 2, cy + int((1 - ease_out(p)) * 18))
                blend(dst, layer, x, y, clamp01(p * 2.2))

        p = seg(t, *T_LATIN)
        if p > 0:
            idx = min(len(self.latin_frames) - 1, int(ease_out(p) * (len(self.latin_frames) - 1)))
            layer = self.latin_frames[idx]
            x, y = centered(layer, W // 2, int(H * self.lay["latin_cy"]))
            blend(dst, layer, x, y, clamp01(p * 2.0))

        p = seg(t, *T_DIVIDER)
        if p > 0:
            w = max(4, int(W * 0.20 * ease_out(p)))
            line = Image.new("RGBA", (w, 3), (120, 200, 245, int(185 * clamp01(p * 1.6))))
            line = line.filter(ImageFilter.GaussianBlur(1.1))
            blend(dst, line, W // 2 - w // 2, int(H * self.lay["div_y"]) - 1, 1.0)

        p = seg(t, *T_SUBTITLE)
        if p > 0:
            layer = wipe(self.subtitle, ease_in_out(p), feather=int(W * 0.06), mode="rtl")
            if layer is not None:
                x, y = centered(layer, W // 2, int(H * self.lay["sub_cy"]))
                blend(dst, layer, x, y, clamp01(p * 2.0))

    # ── فریم
    def frame(self, t: float, alpha=False):
        if alpha:
            dst = Image.new("RGBA", (self.W, self.H), (0, 0, 0, 0))
        else:
            dst = self.render_bg(t)
        self.render_rings(dst, t)
        self.render_spark(dst, t)
        self.render_flash(dst, t)
        self.render_badge(dst, t)
        self.render_type(dst, t)
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

    # رایزر نرم ورود
    riser_env = (np.clip(t / 0.85, 0, 1) ** 2 * np.clip((1.10 - t) / 0.28, 0, 1)).astype(np.float32)
    mix += _lowpass(noise, 2400) * riser_env * 0.15

    # ووش فرود لوگو
    whoosh_env = (np.clip((t - 0.42) / 0.20, 0, 1) * np.clip((1.35 - t) / 0.40, 0, 1)).astype(np.float32)
    mix += _lowpass(noise, 820) * whoosh_env * 0.26

    # ضربهٔ نرم فرود
    tp = t - 0.94
    thump = np.sin(2 * np.pi * 62.0 * tp) * np.exp(-np.clip(tp, 0, None) * 6.0)
    thump = np.where(tp >= 0, thump, 0).astype(np.float32)
    click = _lowpass(rng.normal(0, 1, n).astype(np.float32), 5200) * np.exp(-np.clip(tp, 0, None) * 45)
    click = np.where(tp >= 0, click, 0).astype(np.float32)
    mix += thump * 0.42 + click * 0.09

    # زنگ‌های ملایم روی تیتر و زیرنویس
    for t0, f0, dec, amp in ((2.22, 1046.5, 0.90, 0.098), (2.56, 1568.0, 0.70, 0.055),
                             (3.12, 1318.5, 0.78, 0.046)):
        tt = t - t0
        bell = np.zeros(n, np.float32)
        for h, ha in ((1, 1.0), (2.01, 0.40), (3.02, 0.16), (4.07, 0.07)):
            bell += ha * np.sin(2 * np.pi * f0 * h * tt)
        bell = np.where(tt >= 0, bell * np.exp(-np.clip(tt, 0, None) / dec), 0).astype(np.float32)
        mix += bell * amp

    # شیمِر ملایم هنگام عبور نور روی لوگو
    shimmer_env = (np.clip((t - 1.42) / 0.35, 0, 1) * np.clip((2.30 - t) / 0.45, 0, 1)).astype(np.float32)
    hi_noise = noise - _lowpass(noise, 5200)
    mix += hi_noise * shimmer_env * 0.05

    # پد عمق
    pad = (np.sin(2 * np.pi * 82.4 * t) * 0.10 + np.sin(2 * np.pi * 123.5 * t) * 0.06
           + np.sin(2 * np.pi * 164.8 * t) * 0.035)
    pad *= np.clip(t / 1.3, 0, 1) * np.clip((total - 0.15 - t) / 1.5, 0, 1)
    mix += (pad * 0.5).astype(np.float32)

    mix *= np.clip((total - t) / 0.6, 0, 1)
    mix = np.tanh(mix * 1.85) / np.tanh(1.85)
    mix /= max(1e-6, np.max(np.abs(mix))) * 1.05
    right = np.roll(mix, int(SR * 0.006)) * 0.99
    return np.stack([mix, right], axis=1).astype(np.float32)


# ───────────────────────────── ساخت ─────────────────────────────
def save_still(lm: LogoMotion, t: float, path: Path, alpha=False):
    fr = lm.frame(t, alpha=alpha)
    img = fr if isinstance(fr, Image.Image) else Image.fromarray(fr)
    img.save(path)


def render_video(name: str, alpha=False, stills=None):
    WORK.mkdir(parents=True, exist_ok=True)
    lm = LogoMotion(name)
    W, H = lm.W, lm.H

    if stills:
        for ts in stills:
            p = WORK / f"still_{name}_{float(ts):05.2f}.png"
            save_still(lm, float(ts), p, alpha=alpha)
            print("  ↳", p)
        return

    fps = ALPHA_FPS if alpha else FPS
    frames = int(T_TOTAL * fps)

    if alpha:
        out_path = HERE / f"logo-motion-alpha.{ALPHA_FORMAT}"
        if ALPHA_FORMAT == "webm":
            cmd = [FFMPEG, "-y", "-f", "rawvideo", "-pix_fmt", "rgba", "-s", f"{W}x{H}",
                   "-r", str(fps), "-i", "-", "-c:v", "libvpx-vp9", "-pix_fmt", "yuva420p",
                   "-b:v", "0", "-crf", "26", "-row-mt", "1", "-auto-alt-ref", "0",
                   "-metadata:s:v:0", "alpha_mode=1", str(out_path)]
        else:
            cmd = [FFMPEG, "-y", "-f", "rawvideo", "-pix_fmt", "rgba", "-s", f"{W}x{H}", "-r", str(fps),
                   "-i", "-", "-c:v", "prores_ks", "-profile:v", "4444", "-pix_fmt", "yuva444p10le",
                   "-vendor", "apl0", "-qscale:v", "18", str(out_path)]
    else:
        wav = WORK / f"logo_sound_{name}.wav"
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
            data = np.array(fr, dtype=np.uint8).tobytes() if isinstance(fr, Image.Image) else fr.tobytes()
            proc.stdin.write(data)
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
    ap.add_argument("--alpha-format", default="webm", choices=["webm", "mov"])
    args = ap.parse_args()
    ALPHA_FORMAT = args.alpha_format
    st = [s.strip() for s in args.stills.split(",") if s.strip()] or None
    if st:
        for nm in (["square", "vertical"] if args.only == "all" else [args.only if args.only != "alpha" else "square"]):
            render_video(nm, stills=st)
        sys.exit(0)
    if args.only in ("all", "square"):
        render_video("square")
    if args.only in ("all", "vertical"):
        render_video("vertical")
    if args.only in ("all", "alpha"):
        render_video("square", alpha=True)
