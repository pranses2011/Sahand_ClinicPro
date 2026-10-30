#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
سازندهٔ کلیپ تبلیغاتی «سامانه جامع مدیریت درمانگاه سهند» (Sahand ClinicPro)

خروجی‌ها:
    sahand-clinicpro-promo-vertical.mp4   1080x1920  (ریلز / استوری اینستاگرام)
    sahand-clinicpro-promo-square.mp4     1080x1080  (پست اینستاگرام)

اجرا:
    python3 build_promo.py                  # ساخت هر دو نسخه
    python3 build_promo.py --format vertical
    python3 build_promo.py --stills 0.5,3,6 # فقط چند فریم PNG برای بازبینی

پیش‌نیازها: pillow, numpy, arabic-reshaper, python-bidi, imageio-ffmpeg
"""

from __future__ import annotations

import argparse
import math
import subprocess
import sys
import tempfile
from dataclasses import dataclass, field
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFilter, ImageFont
import arabic_reshaper
from bidi.algorithm import get_display

# ─────────────────────────────── مسیرها ───────────────────────────────
HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[2]
FONTS = ROOT / "docs" / "assets" / "fonts"
SHOTS = ROOT / "docs" / "screenshots"
LOGO = ROOT / "docs" / "assets" / "ClinicPro.png"
OUT = HERE
WORK = Path(tempfile.gettempdir()) / "sahand_promo"
VO_SRC = Path(tempfile.gettempdir()) / "vo"

try:
    import imageio_ffmpeg

    FFMPEG = imageio_ffmpeg.get_ffmpeg_exe()
except Exception:  # pragma: no cover
    FFMPEG = "ffmpeg"

SR = 44100
FPS = 30
FADE = 0.50          # هم‌آمیزی بین صحنه‌ها (ثانیه)
VO_LEAD = 0.45       # مکث ابتدای صحنه پیش از گفتار
VO_TAIL = 0.70       # مکث انتهای صحنه پس از گفتار
OUTRO = 1.40         # نگه‌داشتن صحنهٔ پایانی

C = {
    "bg1": (4, 10, 28), "bg2": (7, 21, 49), "bg3": (10, 42, 82), "bg4": (12, 59, 84),
    "cyan": (56, 189, 248), "sky": (125, 211, 252), "teal": (14, 116, 144),
    "emerald": (16, 185, 129), "blue": (37, 91, 156),
    "white": (255, 255, 255), "soft": (203, 228, 250), "muted": (150, 190, 225),
    "amber": (245, 158, 11), "rose": (244, 114, 182),
}

# ─────────────────────────────── متن فارسی ───────────────────────────────
_RESHAPER = arabic_reshaper.ArabicReshaper(
    configuration={"delete_harakat": False, "support_ligatures": True}
)


def fa(text: str) -> str:
    return get_display(_RESHAPER.reshape(text))


_FONT_CACHE: dict[tuple[str, int], ImageFont.FreeTypeFont] = {}
WEIGHTS = {
    "regular": "Vazirmatn-Regular.ttf", "medium": "Vazirmatn-Medium.ttf",
    "semibold": "Vazirmatn-SemiBold.ttf", "bold": "Vazirmatn-Bold.ttf",
    "extrabold": "Vazirmatn-ExtraBold.ttf", "black": "Vazirmatn-Black.ttf",
}


def font(size: int, weight: str = "bold") -> ImageFont.FreeTypeFont:
    key = (weight, int(size))
    if key not in _FONT_CACHE:
        _FONT_CACHE[key] = ImageFont.truetype(str(FONTS / WEIGHTS[weight]), int(size))
    return _FONT_CACHE[key]


def _rgb(c):
    return c if isinstance(c, tuple) else tuple(int(c[i : i + 2], 16) for i in (1, 3, 5))


def vertical_gradient(size, top, bottom) -> Image.Image:
    w, h = size
    t = np.linspace(0.0, 1.0, h, dtype=np.float32)[:, None]
    a = np.array(_rgb(top), np.float32)
    b = np.array(_rgb(bottom), np.float32)
    col = a * (1 - t) + b * t                    # (h,3)
    arr = np.repeat(col[:, None, :], w, axis=1).astype(np.uint8)
    return Image.fromarray(arr, "RGB")


def text_mask(lines, size, weight, line_h=None, tracking=0, align="right", direction="rtl") -> Image.Image:
    f = font(size, weight)
    line_h = line_h or int(size * 1.34)
    rendered = [fa(l) for l in lines] if direction == "rtl" else list(lines)
    probe = ImageDraw.Draw(Image.new("L", (8, 8)))
    widths = []
    for txt in rendered:
        if tracking:
            widths.append(sum(probe.textlength(ch, font=f) + tracking for ch in txt) - tracking)
        else:
            widths.append(probe.textlength(txt, font=f))

    w = int(math.ceil(max(widths))) + 10
    h = int(round(line_h * (len(rendered) - 1) + size * 1.75)) + 10
    img = Image.new("L", (max(w, 4), max(h, 4)), 0)
    d = ImageDraw.Draw(img)
    for i, txt in enumerate(rendered):
        y = 5 + i * line_h
        if tracking and direction == "ltr":
            x = (w - widths[i]) / 2 if align == "center" else 5
            for ch in txt:
                cw = probe.textlength(ch, font=f)
                d.text((x, y), ch, font=f, fill=255)
                x += cw + tracking
        elif tracking:
            x = w - 5 if align == "right" else (w - widths[i]) / 2
            for ch in txt:
                cw = probe.textlength(ch, font=f)
                d.text((x - cw, y), ch, font=f, fill=255)
                x -= cw + tracking
        elif align == "right":
            d.text((w - 5, y), txt, font=f, fill=255, anchor="ra")
        elif align == "center":
            d.text((w / 2, y), txt, font=f, fill=255, anchor="ma")
        else:
            d.text((5, y), txt, font=f, fill=255, anchor="la")
    return img.crop(img.getbbox())


def text_layer(lines, size, weight="bold", color=C["white"], gradient=None, line_h=None,
               tracking=0, align="right", direction="rtl", shadow=0.55, shadow_blur=12,
               shadow_offset=5) -> Image.Image:
    if isinstance(lines, str):
        lines = [lines]
    mask = text_mask(lines, size, weight, line_h, tracking, align, direction)
    w, h = mask.size
    pad = int(shadow_blur * 2 + shadow_offset + 6) if shadow else 0
    canvas = Image.new("RGBA", (w + pad * 2, h + pad * 2), (0, 0, 0, 0))
    if shadow:
        sh = Image.new("L", canvas.size, 0)
        sh.paste(mask, (pad, pad + shadow_offset))
        sh = sh.filter(ImageFilter.GaussianBlur(shadow_blur)).point(lambda v: int(v * shadow))
        canvas.paste(Image.new("RGBA", canvas.size, (0, 0, 0, 255)), (0, 0), sh)
    if gradient:
        fill = vertical_gradient((w, h), gradient[0], gradient[1]).convert("RGBA")
    else:
        fill = Image.new("RGBA", (w, h), _rgb(color) + (255,))
    canvas.paste(fill, (pad, pad), mask)
    return canvas


# ─────────────────────────────── عناصر گرافیکی ───────────────────────────────
def rounded_mask(size, radius) -> Image.Image:
    m = Image.new("L", size, 0)
    ImageDraw.Draw(m).rounded_rectangle((0, 0, size[0] - 1, size[1] - 1), radius, fill=255)
    return m


def drop_shadow(size, radius, blur=26, offset=(0, 18), alpha=170, spread=8):
    w, h = size
    pad = blur * 2 + max(abs(offset[0]), abs(offset[1])) + spread + 6
    g = Image.new("L", (w + pad * 2, h + pad * 2), 0)
    ImageDraw.Draw(g).rounded_rectangle(
        (pad - spread, pad - spread, pad + w - 1 + spread, pad + h - 1 + spread), radius + spread, fill=alpha)
    g = g.filter(ImageFilter.GaussianBlur(blur))
    if offset[0] or offset[1]:
        g = Image.fromarray(np.roll(np.roll(np.array(g), offset[1], axis=0), offset[0], axis=1), "L")
    return g, pad


def card_layer(shot: Path, width: int, crop_h: int = 1300, radius: int = 24) -> Image.Image:
    src = Image.open(shot).convert("RGB")
    src = src.crop((0, 0, src.width, min(crop_h, src.height)))
    h = int(round(width * src.height / src.width))
    content = src.resize((width, h), Image.LANCZOS)
    content.putalpha(rounded_mask((width, h), radius))

    shadow, pad = drop_shadow((width, h), radius, blur=30, offset=(0, 20), alpha=190, spread=2)
    glow, gpad = drop_shadow((width, h), radius, blur=48, offset=(0, 0), alpha=95, spread=4)
    pad = max(pad, gpad)
    canvas = Image.new("RGBA", (width + pad * 2, h + pad * 2), (0, 0, 0, 0))
    shadow_tint = Image.new("RGBA", shadow.size, (2, 8, 24, 255))
    shadow_tint.putalpha(shadow)
    canvas.alpha_composite(shadow_tint, ((canvas.width - shadow.width) // 2,
                                         (canvas.height - shadow.height) // 2))
    glow_tint = Image.new("RGBA", glow.size, (56, 150, 235, 255))
    glow_tint.putalpha(glow)
    canvas.alpha_composite(glow_tint, ((canvas.width - glow.width) // 2,
                                       (canvas.height - glow.height) // 2))
    canvas.alpha_composite(content, (pad, pad))
    ImageDraw.Draw(canvas).rounded_rectangle((pad, pad, pad + width - 1, pad + h - 1), radius,
                                             outline=(170, 210, 248, 105), width=max(1, width // 400))
    return canvas


def chip_layer(text, size=34, weight="semibold", color=C["white"], dot=C["cyan"],
               pad_x=28, h=76, fill=(255, 255, 255, 18), border=(120, 200, 245, 78)) -> Image.Image:
    label = fa(text)
    f = font(size, weight)
    probe = ImageDraw.Draw(Image.new("L", (8, 8)))
    tw = probe.textlength(label, font=f)
    r = int(size * 0.28)
    w = int(tw + pad_x * 2 + r * 3.0)
    img = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    d.rounded_rectangle((0, 0, w - 1, h - 1), h // 2, fill=fill, outline=border, width=2)
    cy = h / 2
    glow = Image.new("RGBA", img.size, (0, 0, 0, 0))
    ImageDraw.Draw(glow).ellipse((w - pad_x - r * 1.6, cy - r * 1.6, w - pad_x + r * 0.8, cy + r * 0.8),
                                 fill=_rgb(dot) + (95,))
    img.alpha_composite(glow.filter(ImageFilter.GaussianBlur(max(3, r // 2))))
    d.ellipse((w - pad_x - r, cy - r, w - pad_x + r, cy + r), fill=_rgb(dot) + (255,))
    d.text((w - pad_x - r * 3.0, cy), label, font=f, fill=_rgb(color) + (255,), anchor="rm")
    return img


def logo_badge(size=300, radius_ratio=0.24) -> Image.Image:
    r = int(size * radius_ratio)
    badge = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    plate = vertical_gradient((size, size), (241, 250, 255), (183, 222, 249)).convert("RGBA")
    plate.putalpha(rounded_mask((size, size), r))
    badge.alpha_composite(plate)
    logo = Image.open(LOGO).convert("RGBA")
    inner = int(size * 0.84)
    badge.alpha_composite(logo.resize((inner, inner), Image.LANCZOS),
                          ((size - inner) // 2, int(size * 0.08)))
    gloss = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    ImageDraw.Draw(gloss).rounded_rectangle((0, 0, size - 1, int(size * 0.52)), r, fill=(255, 255, 255, 40))
    gloss = gloss.filter(ImageFilter.GaussianBlur(max(2, size // 26)))
    badge.alpha_composite(gloss)

    glow, pad = drop_shadow((size, size), r, blur=int(size * 0.15), offset=(0, 10), alpha=140, spread=2)
    canvas = Image.new("RGBA", (size + pad * 2, size + pad * 2), (0, 0, 0, 0))
    canvas.paste(Image.new("RGBA", canvas.size, (70, 170, 240, 255)), (0, 0), glow)
    canvas.alpha_composite(badge, (pad, pad))
    return canvas


def pill_layer(text, size=44, weight="extrabold", fill=C["cyan"], color=(4, 16, 34),
               pad_x=54, h=None) -> Image.Image:
    label = fa(text)
    f = font(size, weight)
    probe = ImageDraw.Draw(Image.new("L", (8, 8)))
    tw = probe.textlength(label, font=f)
    h = h or int(size * 2.0)
    w = int(tw + pad_x * 2)
    img = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    grad = vertical_gradient((w, h), tuple(min(255, c + 26) for c in fill),
                             tuple(max(0, c - 34) for c in fill)).convert("RGBA")
    grad.putalpha(rounded_mask((w, h), h // 2))
    img.alpha_composite(grad)
    d = ImageDraw.Draw(img)
    d.rounded_rectangle((0, 0, w - 1, h - 1), h // 2, outline=(255, 255, 255, 130), width=2)
    d.text((w / 2, h / 2), label, font=f, fill=_rgb(color) + (255,), anchor="mm")

    glow, pad = drop_shadow((w, h), h // 2, blur=36, offset=(0, 0), alpha=130, spread=2)
    canvas = Image.new("RGBA", (w + pad * 2, h + pad * 2), (0, 0, 0, 0))
    canvas.paste(Image.new("RGBA", canvas.size, _rgb(fill) + (255,)), (0, 0), glow)
    canvas.alpha_composite(img, (pad, pad))
    return canvas


# ─────────────────────────────── پس‌زمینه ───────────────────────────────
BG_ZOOM = 1.16


def _radial(ow, oh, cx, cy, r, strength, falloff=2.0):
    yy = np.arange(oh, dtype=np.float32)[:, None]
    xx = np.arange(ow, dtype=np.float32)[None, :]
    d = np.sqrt((xx - cx) ** 2 + (yy - cy) ** 2) / max(r, 1.0)
    return (np.clip(1.0 - d, 0.0, 1.0) ** falloff * strength).astype(np.float32)


def make_background(W, H, seed=0, warm=False) -> Image.Image:
    ow, oh = int(W * BG_ZOOM), int(H * BG_ZOOM)
    rng = np.random.default_rng(seed)

    t = np.linspace(0.0, 1.0, oh, dtype=np.float32)[:, None]
    stops = [(0.00, C["bg1"]), (0.34, C["bg2"]), (0.72, C["bg3"]), (1.00, C["bg4"])]
    base = np.zeros((oh, ow, 3), np.float32)
    for i in range(len(stops) - 1):
        p0, c0 = stops[i]
        p1, c1 = stops[i + 1]
        lt = np.clip((t - p0) / (p1 - p0), 0, 1)
        col = np.array(_rgb(c0), np.float32) * (1 - lt) + np.array(_rgb(c1), np.float32) * lt
        base += ((t >= p0) & (t <= p1))[:, :, None] * col[:, None, :]

    glows = [
        (0.88, -0.02, 0.72, C["teal"], 0.55),
        (0.08, 0.12, 0.60, C["blue"], 0.50),
        (0.52, 1.12, 0.68, C["emerald"], 0.24),
        (0.28, 0.44, 0.52, (24, 64, 120), 0.40),
    ]
    if warm:
        glows.insert(0, (0.80, 0.28, 0.58, (128, 48, 30), 0.45))
    for gx, gy, gr, col, st in glows:
        m = _radial(ow, oh, gx * ow, gy * oh, gr * max(ow, oh) * 0.72, st)
        base = base * (1 - m[:, :, None]) + np.array(_rgb(col), np.float32) * m[:, :, None]

    step = max(26, int(ow / 18))
    yy = np.arange(oh, dtype=np.float32)[:, None]
    xx = np.arange(ow, dtype=np.float32)[None, :]
    gy = ((yy + step * 0.5) % step) - step * 0.5
    gx = ((xx + step * 0.5) % step) - step * 0.5
    grid = np.clip(1.0 - np.sqrt(gx ** 2 + gy ** 2) / (step * 0.035), 0, 1) ** 1.6
    grid *= _radial(ow, oh, ow * 0.5, oh * 0.34, max(ow, oh) * 0.60, 0.22, 1.4)
    base += grid[:, :, None] * np.array([150, 200, 255], np.float32)

    img = Image.fromarray(np.clip(base, 0, 255).astype(np.uint8), "RGB")

    waves = Image.new("RGBA", (ow, oh), (0, 0, 0, 0))
    wd = ImageDraw.Draw(waves)
    wd.ellipse((-ow * 0.25, oh * 0.80, ow * 1.25, oh * 1.35), fill=(12, 59, 94, 115))
    wd.ellipse((-ow * 0.35, oh * 0.90, ow * 0.95, oh * 1.45), fill=(8, 38, 68, 150))
    img = Image.alpha_composite(img.convert("RGBA"),
                                waves.filter(ImageFilter.GaussianBlur(max(6, ow // 55)))).convert("RGB")

    arr = np.array(img, np.float32) + rng.normal(0, 1.7, (oh, ow, 1)).astype(np.float32)
    return Image.fromarray(np.clip(arr, 0, 255).astype(np.uint8), "RGB")


def bg_frame(bg: Image.Image, W, H, p: float, focus=(0.5, 0.5), zoom=0.07, drift=0.9):
    ow, oh = bg.size
    k = 1.0 + zoom * p
    vw, vh = W / k, H / k
    cx = focus[0] * ow + math.sin(p * 1.9 + 1.0) * drift * (ow - vw) * 0.5
    cy = focus[1] * oh + math.cos(p * 1.6) * 0.6 * (oh - vh) * 0.5
    ox = min(max(cx - vw / 2, 0), ow - vw)
    oy = min(max(cy - vh / 2, 0), oh - vh)
    return bg.transform((W, H), Image.AFFINE, (1 / k, 0, ox, 0, 1 / k, oy), Image.BICUBIC)


# ─────────────────────────────── ترکیب ───────────────────────────────
def _arr(img: Image.Image) -> np.ndarray:
    """کش آرایهٔ numpy روی خود تصویر (کلید id خطرناک است چون id بازاستفاده می‌شود)."""
    a = getattr(img, "_np_arr", None)
    if a is None:
        a = np.array(img.convert("RGBA"))
        img._np_arr = a
    return a


def blend(dst: np.ndarray, src, x: int, y: int, opacity: float = 1.0):
    """ترکیب لایهٔ RGBA روی آرایهٔ RGB با شفافیت."""
    if opacity <= 0.004:
        return
    a_src = _arr(src) if isinstance(src, Image.Image) else src
    sh, sw = a_src.shape[:2]
    x, y = int(round(x)), int(round(y))
    x0, y0 = max(0, x), max(0, y)
    x1, y1 = min(dst.shape[1], x + sw), min(dst.shape[0], y + sh)
    if x1 <= x0 or y1 <= y0:
        return
    s = a_src[y0 - y : y1 - y, x0 - x : x1 - x].astype(np.float32)
    if opacity < 0.996:
        a = s[:, :, 3:4] * (opacity / 255.0)
    else:
        a = s[:, :, 3:4] / 255.0
    region = dst[y0:y1, x0:x1].astype(np.float32)
    dst[y0:y1, x0:x1] = np.clip(region * (1 - a) + s[:, :, :3] * a, 0, 255).astype(np.uint8)


def ease_out(t: float) -> float:
    t = min(max(t, 0.0), 1.0)
    return 1 - (1 - t) ** 3


def smoothstep(t: float) -> float:
    t = min(max(t, 0.0), 1.0)
    return t * t * (3 - 2 * t)


def content_box(img: Image.Image):
    solid = img.getchannel("A").point(lambda v: 255 if v > 26 else 0)
    return solid.getbbox() or (0, 0, img.width, img.height)


def content_h(img: Image.Image) -> int:
    bb = content_box(img)
    return bb[3] - bb[1]


def anchored(img: Image.Image, cx=None, top=None, right=None):
    bb = content_box(img)
    if cx is not None:
        x = int(round(cx - (bb[0] + bb[2]) / 2))
    elif right is not None:
        x = int(right - bb[2])
    else:
        x = -bb[0]
    y = 0 if top is None else int(round(top - bb[1]))
    return x, y


def stack(images, gaps, area, cx, delays, rises=None, durs=None):
    """چیدمان عمودی وسط‌چین با فاصله‌های مشخص؛ خروجی: لیست Item."""
    heights = [content_h(im) for im in images]
    total = sum(heights) + sum(gaps)
    y = area[0] + (area[1] - area[0] - total) / 2
    items = []
    for i, im in enumerate(images):
        x, yy = anchored(im, cx=cx, top=y)
        items.append(Item(im, x, yy, delay=delays[i],
                          dur=(durs[i] if durs else 0.6),
                          rise=(rises[i] if rises else 30)))
        y += heights[i] + (gaps[i] if i < len(gaps) else 0)
    return items


@dataclass
class Item:
    img: Image.Image
    x: int
    y: int
    delay: float = 0.0
    dur: float = 0.6
    rise: int = 30
    bob: float = 0.0
    bob_speed: float = 0.5


@dataclass
class Scene:
    key: str
    vo: str
    bg_seed: int
    items: list = field(default_factory=list)
    dynamic: object = None
    warm: bool = False
    focus: tuple = (0.5, 0.5)
    zoom: float = 0.07
    bg_cache: Image.Image | None = None


# ─────────────────────────────── چیدمان ───────────────────────────────
LAYOUTS = {
    "vertical": dict(
        W=1080, H=1920, pad=64, safe=(210, 1600),
        head_size=72, card_w=1000, hero_logo=330, cta_logo=260,
        chip_size=33, chip_h=74, chip_gap=13, body_size=40, foot_size=27,
        gap_title_card=52, gap_card_chips=54, gap_chip=13,
    ),
    "square": dict(
        W=1080, H=1080, pad=54, safe=(66, 1006),
        head_size=52, card_w=900, hero_logo=240, cta_logo=200,
        chip_size=25, chip_h=56, chip_gap=10, body_size=29, foot_size=21,
        gap_title_card=30, gap_card_chips=30, gap_chip=10,
    ),
}


# ─────────────────────────────── صحنه‌ها ───────────────────────────────
def scene_hook(lay):
    W, H = lay["W"], lay["H"]
    S = lay["head_size"] * 1.16
    lines = [
        text_layer("درمانگاه شلوغ،", S, "black", C["white"], align="center"),
        text_layer("نوبت‌های تلفنی،", S, "black", C["soft"], align="center"),
        text_layer("پرونده‌های کاغذی", S, "black", C["white"], align="center"),
    ]
    kick = text_layer("کار بیمار و کارکنان سخت شده؟", int(lay["body_size"] * 1.08), "semibold",
                      C["amber"], align="center")
    gap = int(S * 0.34)
    items = stack(lines + [kick], [gap, gap, int(gap * 1.8)],
                  area=(int(H * 0.27), int(H * 0.86)), cx=W // 2,
                  delays=[0.30, 1.05, 1.80, 3.05], rises=[40, 40, 40, 26])

    line3 = items[2]
    bb3 = content_box(line3.img)
    bar_w = int((bb3[2] - bb3[0]) * 1.04)
    strike = Image.new("RGBA", (bar_w, 12), (0, 0, 0, 0))
    ImageDraw.Draw(strike).rounded_rectangle((0, 0, bar_w - 1, 11), 6, fill=(245, 158, 11, 235))
    bar_y = line3.y + bb3[3] + int(S * 0.06)
    mark = text_layer("؟", int(H * 0.30), "black", (56, 189, 248), align="center", shadow=0)

    def dynamic(dst, t, dur, lay):
        blend(dst, mark, (W - mark.width) // 2, int(H * 0.10), 0.055 * smoothstep(t / 1.2))
        p = smoothstep((t - 2.35) / 0.5)
        if p <= 0:
            return
        w = max(3, int(bar_w * p))
        blend(dst, strike.crop((0, 0, w, strike.height)), (W - bar_w) // 2, bar_y, 1.0)

    return Scene("hook", "s1", bg_seed=11, items=items, dynamic=dynamic, warm=True,
                 focus=(0.55, 0.45), zoom=0.09)


def scene_brand(lay):
    W, H = lay["W"], lay["H"]
    badge = logo_badge(size=int(lay["hero_logo"]))
    title = text_layer(["سامانه جامع مدیریت", "درمانگاه سهند"], int(lay["head_size"] * 1.10), "black",
                       gradient=((255, 255, 255), (152, 212, 255)), align="center",
                       line_h=int(lay["head_size"] * 1.44))
    latin = text_layer(["SAHAND CLINICPRO"], int(lay["head_size"] * 0.38), "semibold", C["sky"],
                       align="center", tracking=int(lay["head_size"] * 0.10), direction="ltr")
    tag = text_layer("از نوبت‌دهی تا حسابداری — یک سامانه", int(lay["body_size"] * 0.92), "medium",
                     C["muted"], align="center")
    chips = [chip_layer(t, lay["chip_size"], h=lay["chip_h"], pad_x=int(lay["chip_size"] * 0.85))
             for t in ["فارسی و راست‌به‌چپ", "تقویم شمسی", "بیمه‌های ایرانی"]]
    g = lay["chip_gap"]
    images = [badge, title, latin, tag] + chips
    gaps = [int(lay["gap_title_card"] * 0.9), int(H * 0.018), int(H * 0.022), int(H * 0.045)] + [g, g]
    items = stack(images, gaps, area=lay["safe"], cx=W // 2,
                  delays=[0.05, 0.45, 0.72, 0.95, 1.30, 1.48, 1.66],
                  rises=[46, 34, 20, 20, 22, 22, 22],
                  durs=[0.85, 0.7, 0.6, 0.6, 0.5, 0.5, 0.5])
    items[0].bob, items[0].bob_speed = 6, 0.65
    return Scene("brand", "s2", bg_seed=3, items=items, focus=(0.5, 0.38), zoom=0.05)


def scene_feature(lay, key, vo, seed, shots, title, chips, focus=(0.5, 0.5), cross_at=3.2,
                  crop_h=1300):
    W = lay["W"]
    title_img = text_layer(title, lay["head_size"], "black", C["white"], align="center")
    card = card_layer(SHOTS / shots[0], lay["card_w"], crop_h=crop_h)
    card2 = card_layer(SHOTS / shots[1], lay["card_w"], crop_h=crop_h) if len(shots) > 1 else None
    chip_imgs = [chip_layer(t, lay["chip_size"], h=lay["chip_h"], pad_x=int(lay["chip_size"] * 0.85))
                 for t in chips]
    g = lay["chip_gap"]
    images = [title_img, card] + chip_imgs
    gaps = [lay["gap_title_card"], lay["gap_card_chips"]] + [g] * (len(chip_imgs) - 1)
    items = stack(images, gaps, area=lay["safe"], cx=W // 2,
                  delays=[0.08, 0.26, 0.85, 1.02, 1.19],
                  rises=[30, 54, 22, 22, 22],
                  durs=[0.6, 0.75, 0.5, 0.5, 0.5])
    items[1].bob, items[1].bob_speed = 5, 0.45

    cxy = (items[1].x, items[1].y)

    def dynamic(dst, t, dur, lay):
        if card2 is None:
            return
        p = smoothstep((t - cross_at) / 0.9)
        if p <= 0:
            return
        mixed = card2 if p >= 1 else Image.blend(card, card2, p)
        blend(dst, mixed, cxy[0], cxy[1], 1.0)

    return Scene(key, vo, bg_seed=seed, items=items, dynamic=dynamic, focus=focus, zoom=0.06)


def scene_cta(lay):
    W, H = lay["W"], lay["H"]
    badge = logo_badge(size=int(lay["cta_logo"]))
    title = text_layer(["سامانه سهند؛", "نسل تازهٔ مدیریت درمانگاه"], int(lay["head_size"] * 1.02),
                       "black", gradient=((255, 255, 255), (150, 212, 255)), align="center",
                       line_h=int(lay["head_size"] * 1.42))
    cta = pill_layer("همین امروز شروع کنید", size=int(lay["head_size"] * 0.56))
    note = text_layer("دمو و مشاوره: به ما پیام بدهید", int(lay["body_size"] * 0.90), "semibold",
                      C["white"], align="center")
    chips = [chip_layer(t, lay["chip_size"], h=lay["chip_h"], pad_x=int(lay["chip_size"] * 0.85))
             for t in ["فارسی و راست‌به‌چپ", "تقویم شمسی", "پشتیبانی زنده"]]
    url = text_layer(["github.com/pranses2011/Sahand_ClinicPro"], int(lay["body_size"] * 0.62), "medium",
                     C["muted"], align="center", direction="ltr", shadow=0.35, shadow_blur=6)
    g = lay["chip_gap"]
    images = [badge, title, cta, note] + chips + [url]
    gaps = [int(lay["gap_title_card"] * 0.75), int(H * 0.028), int(H * 0.020), int(H * 0.030)] + [g, g] + [int(H * 0.028)]
    items = stack(images, gaps, area=lay["safe"], cx=W // 2,
                  delays=[0.05, 0.35, 1.35, 1.70, 2.00, 2.15, 2.30, 2.55],
                  rises=[40, 32, 26, 20, 20, 20, 20, 14],
                  durs=[0.8, 0.7, 0.7, 0.6, 0.5, 0.5, 0.5, 0.6])
    items[0].bob, items[0].bob_speed = 6, 0.6
    items[2].bob, items[2].bob_speed = 4, 0.8
    return Scene("cta", "s7", bg_seed=21, items=items, focus=(0.5, 0.4), zoom=0.05)


def build_scenes(lay):
    return [
        scene_hook(lay),
        scene_brand(lay),
        scene_feature(lay, "booking", "s3", 5, ["04-appointments.png", "05-receptions.png"],
                      ["نوبت‌دهی و پذیرش"],
                      ["تقویم شمسی پزشک", "پذیرش چهار مرحله‌ای", "بیمه‌های ایرانی"], focus=(0.5, 0.45),
                      crop_h=1160),
        scene_feature(lay, "clinical", "s4", 7, ["06-prescriptions.png", "07-emr-advanced.png"],
                      ["پروندهٔ بالینی و نسخه"],
                      ["پروندهٔ الکترونیک", "نسخه‌نویسی", "آزمایشگاه و تصویربرداری"], focus=(0.45, 0.5),
                      crop_h=1180),
        scene_feature(lay, "finance", "s5", 9, ["08-billing.png", "10-attendance-today.png"],
                      ["مالی، حضور و غیاب"],
                      ["صندوق و حسابداری", "حقوق و دستمزد", "انبار و اقلام مصرفی"], focus=(0.5, 0.55),
                      crop_h=1320),
        scene_feature(lay, "report", "s6", 13, ["02-dashboard.png", "13-reports.png"],
                      ["داشبورد مدیریتی"],
                      ["گزارش‌های لحظه‌ای", "پشتیبان‌گیری خودکار", "پشتیبانی زنده"], focus=(0.5, 0.42),
                      crop_h=1465),
        scene_cta(lay),
    ]


# ─────────────────────────────── صدا ───────────────────────────────
def run(cmd, **kw):
    res = subprocess.run(cmd, capture_output=True, text=True, **kw)
    if res.returncode != 0:
        sys.stderr.write(res.stderr[-3000:])
        raise SystemExit(f"command failed: {' '.join(map(str, cmd))}")
    return res


def wav_duration(path: Path) -> float:
    out = subprocess.run([FFMPEG, "-hide_banner", "-i", str(path)], capture_output=True, text=True).stderr
    for line in out.splitlines():
        if "Duration:" in line:
            h, m, s = line.split("Duration:")[1].split(",")[0].strip().split(":")
            return int(h) * 3600 + int(m) * 60 + float(s)
    raise RuntimeError(f"no duration: {path}")


def prep_voice(src: Path, dst: Path, tempo=1.12) -> float:
    af = (
        "highpass=f=95,"
        "silenceremove=start_periods=1:start_duration=0.03:start_threshold=-45dB,"
        "areverse,"
        "silenceremove=start_periods=1:start_duration=0.03:start_threshold=-45dB,"
        "areverse,"
        "acompressor=threshold=-18dB:ratio=3:attack=8:release=180,"
        f"atempo={tempo},"
        "loudnorm=I=-15.5:TP=-2:LRA=9"
    )
    run([FFMPEG, "-y", "-i", str(src), "-ac", "1", "-ar", str(SR), "-af", af, str(dst)])
    return wav_duration(dst)


def make_music(total: float, sr=SR) -> np.ndarray:
    n = int(total * sr) + 1
    t = np.arange(n, dtype=np.float32) / sr
    out = np.zeros(n, np.float32)
    roots = [110.0, 87.31, 130.81, 98.0]
    ratios = [1.0, 1.5, 2.0, 2.5198]
    seg = 4.6
    idx = np.floor(t / seg).astype(int)
    for i, root in enumerate(roots):
        sel = (idx % len(roots)) == i
        if not sel.any():
            continue
        local = (t - i * seg) % (seg * len(roots))
        env = np.minimum(1.0, local / 1.6) * np.minimum(1.0, np.maximum(0.0, (seg * 1.05 - local) / 1.2))
        env = np.where(sel, np.clip(env, 0, 1), 0).astype(np.float32)
        chord = np.zeros(n, np.float32)
        for k, r in enumerate(ratios):
            amp = [0.42, 0.22, 0.12, 0.10][k]
            f0 = root * r
            chord += amp * np.sin(2 * np.pi * f0 * t + k * 0.7)
            chord += amp * 0.55 * np.sin(2 * np.pi * f0 * 1.003 * t + 1.1)
        out += chord * env

    out += 0.06 * np.sin(2 * np.pi * 880.0 * t) * (0.5 + 0.5 * np.sin(2 * np.pi * 0.07 * t))
    out += 0.10 * np.sin(2 * np.pi * 55.0 * t) * (0.5 + 0.5 * np.sin(2 * np.pi * 0.45 * t - 1.2))
    out *= 1.0 + 0.08 * np.sin(2 * np.pi * 0.11 * t)
    out *= np.clip(t / 1.4, 0, 1) * np.clip((total - t) / 2.8, 0, 1)
    out /= max(1e-6, np.max(np.abs(out)))
    return out.astype(np.float32)


def write_wav(path: Path, stereo: np.ndarray, sr=SR):
    import wave

    pcm = (np.clip(stereo, -1, 1) * 32767).astype("<i2")
    with wave.open(str(path), "wb") as w:
        w.setnchannels(2)
        w.setsampwidth(2)
        w.setframerate(sr)
        w.writeframes(pcm.tobytes())


# ─────────────────────────────── رندر ───────────────────────────────
def compose_scene(sc: Scene, t_local: float, dur: float, lay, item_fade: float = 1.0) -> np.ndarray:
    W, H = lay["W"], lay["H"]
    p = min(max(t_local / max(dur, 0.01), 0.0), 1.0)
    frame = np.array(bg_frame(sc.bg_cache, W, H, p, focus=sc.focus, zoom=sc.zoom))

    if sc.dynamic:
        sc.dynamic(frame, t_local, dur, lay)

    for it in sc.items:
        prog = ease_out((t_local - it.delay) / it.dur) * item_fade
        if prog <= 0:
            continue
        dy = int((1 - prog) * it.rise)
        if it.bob:
            dy += int(math.sin(t_local * it.bob_speed * 2 * math.pi) * it.bob)
        blend(frame, it.img, it.x, it.y + dy, prog)
    return frame


def render_at(t, scenes, starts, durs, lay, total, foot=None) -> np.ndarray:
    W, H = lay["W"], lay["H"]
    idx = 0
    for i in range(len(scenes)):
        if t >= starts[i] - 1e-6:
            idx = i
    frame = compose_scene(scenes[idx], t - starts[idx], durs[idx], lay)

    if idx > 0 and t < starts[idx] + FADE:
        a = smoothstep((t - starts[idx]) / FADE)
        prev = compose_scene(scenes[idx - 1], t - starts[idx - 1], durs[idx - 1], lay,
                             item_fade=max(0.0, 1.0 - a * 1.15))
        blended = np.array(Image.blend(Image.fromarray(prev), Image.fromarray(frame), a))
        dip = 1.0 - 0.16 * math.sin(math.pi * a)
        frame = (blended.astype(np.float32) * dip).astype(np.uint8)

    if foot is not None:
        blend(frame, foot[0], (W - foot[0].width) // 2, H - foot[1] - int(H * 0.024), 0.65)

    if t > total - 0.9:
        frame = (frame.astype(np.float32) * max(0.0, (total - t) / 0.9)).astype(np.uint8)
    return frame


def build(fmt: str, tempo=1.12, stills=None):
    lay = LAYOUTS[fmt]
    W, H = lay["W"], lay["H"]
    WORK.mkdir(parents=True, exist_ok=True)

    print("• آماده‌سازی گفتار…")
    vo_dur = {}
    for key in ["s1", "s2", "s3", "s4", "s5", "s6", "s7"]:
        dst = WORK / f"{key}_{tempo:.2f}.wav"
        if not dst.exists():
            vo_dur[key] = prep_voice(VO_SRC / f"{key}.mp3", dst, tempo)
        else:
            vo_dur[key] = wav_duration(dst)

    scenes = build_scenes(lay)
    starts, durs = [], []
    cur = 0.0
    for i, sc in enumerate(scenes):
        d = VO_LEAD + vo_dur[sc.vo] + VO_TAIL + (OUTRO if i == len(scenes) - 1 else 0.0)
        starts.append(cur)
        durs.append(d)
        cur += d - FADE
    total = starts[-1] + durs[-1]
    print(f"• مدت کلیپ: {total:.1f} ثانیه ({fmt})")

    print("• ساخت پس‌زمینه‌ها…")
    for sc in scenes:
        sc.bg_cache = make_background(W, H, seed=sc.bg_seed, warm=sc.warm)

    if stills:
        for ts in stills:
            t = float(ts)
            frame = render_at(t, scenes, starts, durs, lay, total)
            p = WORK / f"still_{fmt}_{t:05.2f}.png"
            Image.fromarray(frame).save(p)
            print("  ↳", p)
        return

    foot = text_layer("Sahand ClinicPro  ·  نسخهٔ ۲.۲۸", lay["foot_size"], "medium", C["muted"],
                      align="center", shadow=0.4, shadow_blur=6)
    foot_h = content_h(foot)

    # ── صدا
    print("• میکس صدا…")
    n = int((total + 0.6) * SR)
    voice = np.zeros(n, np.float32)
    for i, sc in enumerate(scenes):
        raw = subprocess.run([FFMPEG, "-hide_banner", "-loglevel", "error", "-i", str(WORK / f"{sc.vo}.wav"),
                              "-f", "f32le", "-ac", "1", "-ar", str(SR), "-"], capture_output=True)
        arr = np.frombuffer(raw.stdout, dtype=np.float32)
        off = int((starts[i] + VO_LEAD) * SR)
        end = min(n, off + len(arr))
        voice[off:end] += arr[: end - off]

    pad = make_music(total + 0.6)
    music = np.pad(pad, (0, max(0, n - len(pad))))[:n] * 0.16
    mix = voice + music
    mix = np.tanh(mix * 1.15) / np.tanh(1.15)
    mix = mix / max(1e-6, np.max(np.abs(mix))) * 0.90
    wav_path = WORK / f"mix_{fmt}.wav"
    write_wav(wav_path, np.stack([mix, mix], axis=1))

    # ── تصویر
    print("• رندر تصویر…")
    out_path = OUT / f"sahand-clinicpro-promo-{fmt}.mp4"
    n_frames = int(total * FPS)
    cmd = [FFMPEG, "-y", "-f", "rawvideo", "-pix_fmt", "rgb24", "-s", f"{W}x{H}", "-r", str(FPS),
           "-i", "-", "-i", str(wav_path),
           "-c:v", "libx264", "-preset", "medium", "-crf", "19", "-pix_fmt", "yuv420p",
           "-profile:v", "high", "-level", "4.2", "-g", str(FPS * 2),
           "-c:a", "aac", "-b:a", "192k", "-ar", "44100",
           "-movflags", "+faststart", "-shortest", str(out_path)]
    proc = subprocess.Popen(cmd, stdin=subprocess.PIPE, stderr=subprocess.DEVNULL)
    try:
        for f in range(n_frames):
            t = f / FPS
            proc.stdin.write(render_at(t, scenes, starts, durs, lay, total,
                                       foot=(foot, foot_h)).tobytes())
            if f % (FPS * 3) == 0:
                print(f"    {t:5.1f}s / {total:.1f}s", flush=True)
    finally:
        proc.stdin.close()
        proc.wait()
    print("✓", out_path, f"({out_path.stat().st_size / 1e6:.1f} MB)")


if __name__ == "__main__":
    ap = argparse.ArgumentParser()
    ap.add_argument("--format", default="both", choices=["vertical", "square", "both"])
    ap.add_argument("--stills", default="")
    ap.add_argument("--tempo", type=float, default=1.12)
    args = ap.parse_args()
    fmts = ["vertical", "square"] if args.format == "both" else [args.format]
    st = [s.strip() for s in args.stills.split(",") if s.strip()] or None
    for f in fmts:
        build(f, tempo=args.tempo, stills=st)
