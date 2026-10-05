#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
⬆️ آپلود asset های رلیز v1.5.0 به گیت‌هاب
============================================
۹ پیوست با GitHub Upload API به رلیز v1.5.0 اضافه می‌شود:
  ۲ ZIP (نصب خودکفا + سرور لایسنس cPanel) + ۳ PDF (راهنماها و بروشور) + ۴ PNG (پوسترها و بنرها)
"""
import json
import pathlib
import sys
import time
import urllib.parse
import urllib.request

TOKEN = "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"
REPO = "pranses2011/Sahand_ClinicPro"
TAG = "v1.5.0"
OUT = pathlib.Path("/home/z/my-project/download")

# ترتیب آپلود: از کوچک به بزرگ (تست زودهنگام اتصال)
ASSETS = [
    "Sahand-LicenseServer-cPanel-v1.1.zip",
    "SahandClinicPro-Setup-v1.5.0.zip",
    "SahandClinicPro-Install-Network-Guide-v1.5.pdf",
    "SahandClinicPro-Brochure-v1.5.pdf",
    "SahandClinicPro-User-Guide-v1.5.pdf",
    "SahandClinicPro-Poster-Vertical-50x70-150dpi.png",
    "SahandClinicPro-Poster-Horizontal-70x50-150dpi.png",
    "SahandClinicPro-Banner-Vertical-1.5x3m.png",
    "SahandClinicPro-Banner-Billboard-Horizontal-3x1.5m.png",
]


def api(url: str, method: str = "GET", data: bytes | None = None,
        content_type: str = "application/json") -> dict:
    req = urllib.request.Request(url, data=data, method=method)
    req.add_header("Authorization", f"token {TOKEN}")
    req.add_header("Accept", "application/vnd.github+json")
    req.add_header("Content-Type", content_type)
    req.add_header("User-Agent", "SahandClinicPro-Release-Uploader")
    with urllib.request.urlopen(req) as resp:
        return json.loads(resp.read().decode())


def get_release_id() -> int:
    """یافتن شناسه رلیز v1.5.0"""
    rel = api(f"https://api.github.com/repos/{REPO}/releases/tags/{TAG}")
    return rel["id"]


def delete_existing(release_id: int, name: str):
    """حذف asset هم‌نام قبلی (برای idempotency)"""
    assets = api(f"https://api.github.com/repos/{REPO}/releases/{release_id}/assets")
    for a in assets:
        if a["name"] == name:
            api(f"https://api.github.com/repos/{REPO}/releases/assets/{a['id']}", method="DELETE")
            print(f"    ↻ asset قبلی «{name}» حذف شد")


def upload_asset(release_id: int, path: pathlib.Path):
    """آپلود یک asset با Upload API"""
    quoted = urllib.parse.quote(path.name)
    url = (
        f"https://uploads.github.com/repos/{REPO}/releases/{release_id}/assets"
        f"?name={quoted}"
    )
    data = path.read_bytes()
    result = api(url, method="POST", data=data, content_type="application/octet-stream")
    return result


def main():
    print(f"⬆️ آپلود ۹ asset به رلیز {TAG} — آغاز")
    release_id = get_release_id()
    print(f"  ✔ شناسه رلیز: {release_id}")

    ok, failed = [], []
    for name in ASSETS:
        path = OUT / name
        if not path.exists():
            print(f"  ✖ فایل یافت نشد: {path}")
            failed.append(name)
            continue
        size_mb = path.stat().st_size / (1024 * 1024)
        print(f"  … در حال آپلود {name} ({size_mb:.1f} MB)")
        try:
            delete_existing(release_id, name)
            result = upload_asset(release_id, path)
            print(f"  ✔ {result['name']} → {result['state']}")
            ok.append(name)
        except Exception as exc:  # noqa: BLE001
            print(f"  ✖ خطا در {name}: {exc}")
            failed.append(name)
        time.sleep(1.0)  # فاصله برای احترام به محدودیت نرخ

    print(f"════════════════════════════════════")
    print(f"✅ موفق: {len(ok)}/{len(ASSETS)}")
    if failed:
        print(f"❌ ناموفق: {failed}")
        return 1
    return 0


if __name__ == "__main__":
    sys.exit(main())
