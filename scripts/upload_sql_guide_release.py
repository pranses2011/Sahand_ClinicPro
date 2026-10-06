#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
📗 انتشار راهنمای «اتصال به SQL Server و ساخت دیتابیس» در رلیزها — پس از v2.0.1
==============================================================================
۱) آپلود PDF به هر دو رلیز v2.0.0 (خانه مجموعه راهنماها) و v2.0.1 (آخرین رلیز)
۲) به‌روزرسانی متن هر دو رلیز: افزودن ردیف/سطر راهنمای SQL به فهرست فایل‌های راهنما
⚠️ توکن از متغیر محیطی GH_TOKEN خوانده می‌شود — هرگز در سورس هاردکد نمی‌شود
"""
import json
import os
import pathlib
import sys
import urllib.parse
import urllib.request

REPO_API = "https://api.github.com/repos/pranses2011/Sahand_ClinicPro"
PDF_PATH = pathlib.Path("/home/z/my-project/download/SahandClinicPro-SQL-Setup-Guide-v2.0.pdf")
ASSET_NAME = "SahandClinicPro-SQL-Setup-Guide-v2.0.pdf"

TOKEN = os.environ.get("GH_TOKEN") or ""
if not TOKEN:
    raise SystemExit("متغیر محیطی GH_TOKEN تنظیم نشده است")


def api(url: str, method: str = "GET", data: bytes | None = None,
        content_type: str = "application/json", raw: bool = False):
    req = urllib.request.Request(url, data=data, method=method)
    req.add_header("Authorization", f"token {TOKEN}")
    req.add_header("Accept", "application/vnd.github+json")
    req.add_header("Content-Type", content_type)
    req.add_header("User-Agent", "SahandClinicPro-SQLGuide-Publisher")
    with urllib.request.urlopen(req) as resp:
        payload = resp.read()
        return payload if raw else json.loads(payload.decode())


def upload_asset(release: dict) -> None:
    # idempotent — حذف دارایی هم‌نام قبلی
    for asset in release.get("assets", []):
        if asset["name"] == ASSET_NAME:
            api(asset["url"], method="DELETE")
            print(f"  ↻ دارایی قبلی {ASSET_NAME} در {release['tag_name']} حذف شد")
            break
    upload_base = release["upload_url"].split("{")[0]
    url = f"{upload_base}?name={urllib.parse.quote(ASSET_NAME)}"
    data = PDF_PATH.read_bytes()
    api(url, method="POST", data=data, content_type="application/pdf", raw=True)
    print(f"  ✔ {ASSET_NAME} به {release['tag_name']} آپلود شد ({len(data)/1024:.0f} KB)")


def update_body_v200(body: str) -> str:
    marker_row = "| `SahandClinicPro-VS-Guide-v2.0.pdf` | دانلود پروژه + Visual Studio + خروجی exe |"
    new_row = ("| `SahandClinicPro-SQL-Setup-Guide-v2.0.pdf` | راهنمای مبتدی‌پسند اتصال به SQL Server "
               "و ساخت دیتابیس در اولین نصب و اجرا — ۱۸ صفحه با دو جدول رفع اشکال ۱۶ خطایی |")
    if new_row in body:
        print("  ✔ متن v2.0.0 از قبل به‌روز است")
        return body
    if marker_row not in body:
        raise SystemExit("✖ نشانه جدول پیوست‌های v2.0.0 یافت نشد")
    return body.replace(marker_row, marker_row + "\n" + new_row, 1)


def update_body_v201(body: str) -> str:
    # نشانه کوتاه — متن زنده رلیز «همچنان» با الف است؛ رشته بلند اسکریپت قدیمی «همچنین» داشت
    short_marker = "راهنمای VS، بروشور"
    if "راهنمای SQL" in body:
        print("  ✔ متن v2.0.1 از قبل به‌روز است")
        return body
    if short_marker not in body:
        raise SystemExit("✖ نشانه فهرست دارایی‌های v2.0.1 یافت نشد")
    add_note = ("\n\n> 📗 **راهنمای جدید (پس از انتشار وصله):** «اتصال به SQL Server و ساخت دیتابیس در اولین نصب و اجرا» "
                "— مبتدی‌پسند، ۱۸ صفحه، دو جدول رفع اشکال — به پیوست‌های همین رلیز و رلیز v2.0.0 اضافه شد.")
    body = body.replace(short_marker, "راهنمای VS، راهنمای SQL، بروشور", 1)
    body = body.replace("</div>", add_note + "\n</div>", 1)
    return body


def main() -> int:
    if not PDF_PATH.exists():
        raise SystemExit(f"✖ فایل PDF یافت نشد: {PDF_PATH}")

    print("📗 انتشار راهنمای SQL در رلیزها")
    v200 = api(f"{REPO_API}/releases/tags/v2.0.0")
    v201 = api(f"{REPO_API}/releases/tags/v2.0.1")

    upload_asset(v200)
    upload_asset(v201)

    api(f"{REPO_API}/releases/{v200['id']}", method="PATCH",
        data=json.dumps({"body": update_body_v200(v200["body"])}).encode())
    print("  ✅ متن رلیز v2.0.0 به‌روزرسانی شد (ردیف جدید در جدول پیوست‌ها)")

    api(f"{REPO_API}/releases/{v201['id']}", method="PATCH",
        data=json.dumps({"body": update_body_v201(v201["body"])}).encode())
    print("  ✅ متن رلیز v2.0.1 به‌روزرسانی شد (افزودن به فهرست راهنماها + یادداشت)")

    # تأیید نهایی
    for tag in ("v2.0.0", "v2.0.1"):
        rel = api(f"{REPO_API}/releases/tags/{tag}")
        names = [a["name"] for a in rel["assets"]]
        ok = ASSET_NAME in names
        print(f"  🔎 {tag}: {len(names)} دارایی — {'✓ راهنمای SQL حاضر' if ok else '✖ غایب!'}")
        if tag == "v2.0.0":
            print("      پیوست‌ها:", "، ".join(sorted(names)))
    print("🎉 انتشار کامل شد")
    return 0


if __name__ == "__main__":
    sys.exit(main())
