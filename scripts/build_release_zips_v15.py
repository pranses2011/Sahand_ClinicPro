#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
📦 ساخت ZIP های رلیز v1.5.0 — درمانگاه سهند
==============================================
خروجی در /home/z/my-project/download/:
  ۱) SahandClinicPro-Setup-v1.5.0.zip
     بسته خودکفای تولید نصب‌کننده: installer + سورس کامل (بدون bin/obj) + راهنماها + فایل‌های ریشه
     سازگار با منطق cd /d "%~dp0.." در BUILD-INSTALLER.bat
  ۲) Sahand-LicenseServer-cPanel-v1.1.zip
     سرور لایسنس PHP خالص برای هاست cPanel — بدون تغییر از v1.3.0 (نسخه بسته حفظ می‌شود)
"""
import zipfile
import pathlib
import sys

REPO = pathlib.Path("/home/z/my-project/Sahand_ClinicPro")
OUT = pathlib.Path("/home/z/my-project/download")

# مسیرهایی که در ZIP نصب نباید باشند (خروجی‌های build و فایل‌های موقت)
EXCLUDE_DIRS = {"bin", "obj", "publish", "dist", ".git", ".vs", "TestResults"}
EXCLUDE_SUFFIXES = (".user", ".suo")


def add_tree(zf: zipfile.ZipFile, base: pathlib.Path, arc_root: str) -> int:
    """افزودن بازگشتی یک پوشه به ZIP با ریشه دلخواه — خروجی: تعداد فایل"""
    count = 0
    for path in sorted(base.rglob("*")):
        if not path.is_file():
            continue
        rel = path.relative_to(base)
        # حذف مسیرهای بن‌بست
        if any(part in EXCLUDE_DIRS for part in rel.parts):
            continue
        if path.suffix in EXCLUDE_SUFFIXES:
            continue
        zf.write(path, f"{arc_root}/{rel.as_posix()}")
        count += 1
    return count


def build_setup_zip():
    """بسته تولید نصب‌کننده — سورس کامل + installer + مستندات"""
    dst = OUT / "SahandClinicPro-Setup-v1.5.0.zip"
    total = 0
    with zipfile.ZipFile(dst, "w", zipfile.ZIP_DEFLATED, compresslevel=9) as zf:
        # ۱) ریشه: فایل‌های ساخت solution
        for fname in [
            "Sahand.ClinicPro.sln",
            "Directory.Packages.props",
            "Directory.Build.props",
            "nuget.config",
            "LICENSE",
        ]:
            f = REPO / fname
            if f.exists():
                zf.write(f, f"SahandClinicPro/{fname}")
                total += 1
        # ۲) کل سورس (شامل LicenseServer.Php هم هست — بونوس)
        total += add_tree(zf, REPO / "src", "SahandClinicPro/src")
        # ۳) بسته نصب‌کننده
        total += add_tree(zf, REPO / "installer", "SahandClinicPro/installer")
        # ۴) راهنماهای HTML (اسکریپت build آن‌ها را داخل نصب‌کننده کپی می‌کند)
        total += add_tree(zf, REPO / "docs" / "guides", "SahandClinicPro/docs/guides")
        # ۵) فونت‌های مستندات
        total += add_tree(zf, REPO / "docs" / "assets" / "fonts", "SahandClinicPro/docs/assets/fonts")
        # ۶) فایل شروع سریع
        quick_start = (
            "سامانه مدیریت درمانگاه سهند — Sahand ClinicPro v1.5.0\n"
            "بسته تولید نصب‌کننده (خودکفا)\n"
            "══════════════════════════════════════════════\n"
            "\n"
            "شروع سریع روی رایانه ویندوز ۱۰/۱۱:\n"
            "  ۱) این بسته را در یک مسیر بدون فاصله استخراج کنید (مثلاً C:\\SahandClinicPro-src)\n"
            "  ۲) .NET 8 SDK را از dotnet.microsoft.com نصب کنید\n"
            "  ۳) برای نصب کلاینت‌ها: installer\\BUILD-INSTALLER.bat را اجرا کنید\n"
            "     → خروجی: dist\\SahandClinicPro-Setup-v1.5.0.exe\n"
            "  ۴) برای نصب سرور API: installer\\INSTALL-SERVER.bat را با دسترسی Administrator اجرا کنید\n"
            "\n"
            "راهنمای کامل گام‌به‌گام: docs\\guides\\install-guide.html\n"
            "پشتیبانی: dev@sahand-clinicpro.ir\n"
        )
        zf.writestr("SahandClinicPro/شروع-سریع-READ-ME.txt", quick_start)
        total += 1
    size_mb = dst.stat().st_size // (1024 * 1024)
    print(f"  ✔ SahandClinicPro-Setup-v1.5.0.zip  ({total} فایل، {size_mb} MB)")
    return dst


def build_license_zip():
    """بسته سرور لایسنس PHP برای cPanel"""
    dst = OUT / "Sahand-LicenseServer-cPanel-v1.1.zip"
    base = REPO / "src" / "LicenseServer.Php"
    total = 0
    with zipfile.ZipFile(dst, "w", zipfile.ZIP_DEFLATED, compresslevel=9) as zf:
        total += add_tree(zf, base, "LicenseServer")
        # راهنمای نصب روی cPanel در ریشه بسته هم قرار بگیرد
        zf.write(base / "README-FA.md", "README-FA.md")
        total += 1
    size_kb = dst.stat().st_size // 1024
    print(f"  ✔ Sahand-LicenseServer-cPanel-v1.1.zip  ({total} فایل، {size_kb} KB)")
    return dst


def main():
    OUT.mkdir(parents=True, exist_ok=True)
    print("📦 ساخت ZIP های رلیز v1.5.0 — آغاز")
    build_setup_zip()
    build_license_zip()
    print("✅ هر دو ZIP آماده شد — خروجی در:", OUT)


if __name__ == "__main__":
    sys.exit(main())
