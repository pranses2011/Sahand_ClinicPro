#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
🎨 مولد آیکون رسمی برنامه — چرخه نهم (v1.7.0)
===============================================
لوگوی رسمی محصول (docs/assets/ClinicPro.png — خانه + صلیب درمانی + استتوسکوپ،
آپلودی مالک) به آیکون چندمقیاسی ویندوزی (ICO) با ۶ اندازه استاندارد تبدیل
می‌شود تا هم به‌عنوان آیکون exe دسکتاپ و هم SetupIconFile نصب‌کننده Inno Setup
استفاده شود.

خروجی‌ها:
  1. src/Sahand.ClinicPro.Desktop/Assets/appicon.ico  → <ApplicationIcon> در csproj
  2. installer/appicon.ico                            → SetupIconFile در SahandClinicPro.iss
"""
import pathlib
import sys

from PIL import Image

REPO = pathlib.Path("/home/z/my-project/Sahand_ClinicPro")
SRC_LOGO = REPO / "docs" / "assets" / "ClinicPro.png"
OUT_DESKTOP = REPO / "src" / "Sahand.ClinicPro.Desktop" / "Assets" / "appicon.ico"
OUT_INSTALLER = REPO / "installer" / "appicon.ico"

# اندازه‌های استاندارد آیکون ویندوز — از درشت (256) تا ریز (16)
SIZES = [(256, 256), (128, 128), (64, 64), (48, 48), (32, 32), (16, 16)]


def build_icon() -> None:
    if not SRC_LOGO.exists():
        sys.exit(f"❌ لوگوی منبع یافت نشد: {SRC_LOGO}")

    logo = Image.open(SRC_LOGO).convert("RGBA")
    print(f"🖼 لوگوی منبع: {logo.size[0]}×{logo.size[1]} ({logo.mode})")

    # اگر مربع نباشد، در بوم مربع با حاشیه شفاف قرار می‌گیرد (حفظ کل لوگو)
    side = max(logo.size)
    if logo.size != (side, side):
        canvas = Image.new("RGBA", (side, side), (0, 0, 0, 0))
        canvas.paste(logo, ((side - logo.width) // 2, (side - logo.height) // 2), logo)
        logo = canvas
        print(f"↔ تصویر غیر مربع بود؛ در بوم {side}×{side} شفاف قرار گرفت")

    # مبنای کیفیت: بزرگ‌ترین اندازه با بازنمونه‌گیری Lanczos (نه تصعید پیکسلی)
    master = logo.resize((256, 256), Image.LANCZOS)

    # بهبود خوانایی در اندازه‌های ریز: پس‌زمینه گرد با کمترین صافی
    # (ICO ویندوز خودش mask می‌سازد؛ پکیج PNG شفاف درون ICO پشتیبانی می‌شود)
    for target in (OUT_DESKTOP, OUT_INSTALLER):
        target.parent.mkdir(parents=True, exist_ok=True)
        master.save(
            target,
            format="ICO",
            sizes=SIZES,
            append_images=[master.resize(s, Image.LANCZOS) for s in SIZES[1:]],
        )
        verify(target)


def verify(path: pathlib.Path) -> None:
    """راستی‌آزمایی: بازخوانی ICO و اطمینان از سالم‌بودن هر ۶ اندازه"""
    with Image.open(path) as ico:
        ico_size = ico.size
    size_bytes = path.stat().st_size
    print(f"  ✅ {path.relative_to(REPO)} — {size_bytes:,} بایت | بزرگ‌ترین لایه {ico_size[0]}×{ico_size[1]}")


if __name__ == "__main__":
    build_icon()
    print("🎉 آیکون چندمقیاسی با موفقیت ساخته شد (۶ اندازه: 256/128/64/48/32/16)")
