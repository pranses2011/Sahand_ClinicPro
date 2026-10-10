#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""🔍 ممیزی خودکار using های رسانه‌ای C# — چرخهٔ سی‌وششم (قانون هم‌کلاسی‌ها).

پیش‌زمینهٔ تولد: بیلد‌شکنندهٔ v2.4.8 — مبدل HexBrushConverter از انواع
فضای‌نام System.Windows.Media استفاده می‌کرد ولی using آن جا افتاده بود و
Rebuild دسکتاپ با CS0103/CS0246 متوقف شد. این اسکریپت از این پس در بسته‌بندی
هر چرخه اجرا می‌شود تا هیچ فایل .cs دیگری نتواند چنین فراموشی‌ای داشته باشد.

قاعدهٔ ممیزی:
  اگر فایلی از انواعِ اختصاصی WPF-Media (Brushes / SolidColorBrush /
  ColorConverter / LinearGradientBrush / RadialGradientBrush) با نامِ
  «غیرآدرس‌دار» استفاده کند، باید «using System.Windows.Media» در فایل باشد.

استثناهای مجاز (خطا نمی‌گیرند):
  • آدرس کامل: «System.Windows.Media.Brushes.Black» — شناسه پشت نقطه است
    (الگوی lookbehind نقطه را رد می‌کند) — مثل ThemeService و AppointmentTicketPrinter
  • using های تخصصی‌تر مثل «using System.Windows.Media.Animation» هم
    به‌عنوان وجود using پذیرفته می‌شوند (بافت رشتهٔ جستجو)

نکته: انواع عمومی‌تر مثل «Color» یا «FontFamily» عمداً در ممیزی نیستند چون
در لایه‌های غیر WPF هم نام‌همند دارند (مشخصهٔ رشته‌ای Color دامنه، FontFamily
چاپ‌سازها) و خطای کاذب می‌دادند؛ پنج شناسهٔ بالا در این سورس فقط از
System.Windows.Media می‌آیند.
"""
import pathlib
import re
import sys

REPO = pathlib.Path(__file__).resolve().parent.parent

# انواع فقط-و-فقط System.Windows.Media — در این سورس منبع دیگری ندارند؛
# lookbehind «(?<![\\w.])» نام‌های پشت نقطه (آدرس کامل) و چسبیده به کلمه را رد می‌کند
MEDIA_IDENTIFIERS = re.compile(
    r'(?<![\w.])(Brushes|SolidColorBrush|ColorConverter|LinearGradientBrush|RadialGradientBrush)\b')

failures = []
scanned = 0
for cs in sorted(REPO.glob('src/**/*.cs')):
    scanned += 1
    try:
        txt = cs.read_text(encoding='utf-8-sig')
    except Exception as exc:  # خواندن ناموفق = خودش خطای ممیزی است
        failures.append((str(cs.relative_to(REPO)), f'ناموفق در خواندن فایل: {exc}'))
        continue
    if 'using System.Windows.Media' in txt:
        continue  # using رسانه‌ای موجود است — مجاز
    # پیمایش همهٔ مطابقت‌ها؛ خطوطِ کامنتیِ خالص (// و ///) نادیده گرفته می‌شوند
    # چون ذکرِ توضیحیِ نام نوع در کامنت «مصرف» نیست (مثل کامنت رفع این چرخه که
    # خودش نام Brushes/ColorConverter را برای توضیح علت درج using آورده است)
    hit = None
    for m in MEDIA_IDENTIFIERS.finditer(txt):
        line_start = txt.rfind('\n', 0, m.start()) + 1
        line_end = txt.find('\n', m.end())
        line_txt = txt[line_start:line_end if line_end != -1 else len(txt)]
        if line_txt.lstrip().startswith('//'):
            continue  # اشارهٔ صرفاً توضیحی در کامنت — مصرف واقعی نیست
        hit = m
        break
    if hit:
        line = txt.count('\n', 0, hit.start()) + 1
        failures.append((str(cs.relative_to(REPO)),
                         f'خط {line}: استفادهٔ غیرآدرس‌دار از «{hit.group(1)}» بدون using System.Windows.Media'))

if failures:
    print(f'❌ ممیزی رسانه‌ای: {len(failures)} فایل مشکل دارد:')
    for path, msg in failures:
        print(f'  • {path}: {msg}')
    sys.exit(1)

print(f'✅ ممیزی using های رسانه‌ای: همهٔ {scanned} فایل .cs سالم‌اند')
