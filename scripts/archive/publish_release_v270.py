#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""چرخهٔ چهل‌وسوم — انتشار رلیز v2.7.0 + بارگذاری زیپ آپدیت (الگوی چرخهٔ ۳۹/۴۱/۴۲)"""
import json
import os
import urllib.request

TOKEN = os.environ['GITHUB_TOKEN']
REPO = 'pranses2011/Sahand_ClinicPro'
TAG = 'v2.7.0'
ZIP = '/home/z/my-project/download/SahandClinicPro-Update-2.6.1-to-2.7.0.zip'
COMMIT_SHA = '61e00ff'  # کامیت نسخه — پس از آن فقط اسکریپت بسته‌بندی آمده

NOTES = '''# درمانگاه سهند — نسخهٔ 2.7.0 (چرخهٔ چهل‌وسوم)

**چهارده گزارش مالک در یک بسته** — بزرگ‌ترین چرخه از زمان 2.5.0.

## 🩺 رفع‌ها
- **هشدار EF Core سرور** (MedicationItemKind Sentinel) — برای همیشه خاموش؛ درس: Sentinel باید «متفاوت از پیش‌فرض CLR و معنادار» باشد
- **تکراری کاذب اقلام** — رونوشت خودکار نام ژنریک فقط حرف اول را کپی می‌کرد؛ اکنون کامل کپی می‌شود تا ویرایش دستی + مقایسهٔ نرمال‌شدهٔ فارسی سمت سرور
- **ورود کالا** — گارد وجود/نوع قلم + اتصال ناوبری تراکنش + پیام گویا در خطاهای ۵۰۰ (نوع + شرح کوتاه)
- **لیست اقلام مصرفی و داروها** — فهرست از «بچ‌محور» به «قلم‌محور» تبدیل شد؛ قلم بدون ورود کالا هم دیده می‌شود
- **اتاق‌های پذیرش** — نام واقعی اتاق‌ها (خطای DTO)
- **میانبرهای داشبورد** — دکمهٔ مرده برای بار سوم: رویداد NavigationRequested مشترک شد

## ✨ بهبودها
- **جستجوی تایپی همهٔ کمبوباکس‌ها** — پالایش زندهٔ «شامل‌بودن» با نرمال‌سازی فارسی؛ تایپ «پانسمان» گزینهٔ «تزریقات و پانسمان سرپایی» را هم می‌آورد
- **عنوان شناور همهٔ فیلدها** — استایل خط‌دار سراسری (مانند کد ملی) + ۱۲ فیلد بی‌عنوان عنوان گرفتند
- **کاشی‌های میانبر و توست‌ها** — بازطراحی؛ دکمهٔ «میانبر جدید» فقط-آیکون؛ توست بالاتر از استاتوس‌بار با نوار رنگی راست
- **بستن شیفت کامل** — مبالغ با واحد پول (ریال/تومان) + پنجرهٔ بزرگ با دکمهٔ پین‌شده + پیش‌نمایش رسید با دکمهٔ چاپ + ستون‌های کامل تاریخچه

## 🏗 مستقل‌سازی
- **مصرفی‌ها و داروخانه** — کمبوی «نوع قلم» حذف؛ نوع از بافت صفحه قفل است (سمت سرور هم)
- **انبارگردانی دامنه‌دار** — کل انبار / فقط داروها / فقط لوازم مصرفی
- **چهار صفحهٔ مستقل پاراکلینیک** — دندانپزشکی، چشم‌پزشکی، فیزیوتراپی، تصویربرداری

## 📦 نصب
زیپ دلتا (۶۵ فایل) روی نصب 2.6.1 — **سه فایل قدیمی پاراکلینیک باید حذف شوند** (در مانیفست و راهنما). هر دو پروژه Rebuild + سرور ری‌استارت؛ مهاجرت دیتابیس خودکار. جزئیات و سنجه‌های موفقیت: `Update-Guide-2.7.0.html` داخل بسته.
'''

api = f'https://api.github.com/repos/{REPO}/releases'
body = json.dumps({
    'tag_name': TAG,
    'target_commitish': COMMIT_SHA,
    'name': f'درمانگاه سهند {TAG} — مصرفی‌های مستقل + جستجوی تایپی کمبوباکس‌ها + پاراکلینیک مستقل + بستن شیفت کامل',
    'body': NOTES,
    'draft': False,
    'prerelease': False,
}).encode()

req = urllib.request.Request(api, data=body, method='POST', headers={
    'Authorization': f'Bearer {TOKEN}',
    'Accept': 'application/vnd.github+json',
    'Content-Type': 'application/json',
})
with urllib.request.urlopen(req) as r:
    release = json.load(r)
    print(f"✅ رلیز ساخته شد: {release['html_url']} (id: {release['id']})")

# بارگذاری زیپ
import mimetypes
upload_url = release['upload_url'].split('{')[0]
filename = os.path.basename(ZIP)
data = open(ZIP, 'rb').read()
req = urllib.request.Request(
    f'{upload_url}?name={filename}',
    data=data, method='POST', headers={
        'Authorization': f'Bearer {TOKEN}',
        'Accept': 'application/vnd.github+json',
        'Content-Type': 'application/zip',
    })
with urllib.request.urlopen(req) as r:
    asset = json.load(r)
    print(f"✅ زیپ بارگذاری شد: {asset['browser_download_url']} ({asset['size']:,} بایت)")
