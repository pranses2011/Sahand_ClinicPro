#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
📦 ساخت ZIP های رلیز v1.6.0 — درمانگاه سهند
==============================================
خروجی در /home/z/my-project/download/:
  ۱) SahandClinicPro-Setup-v1.6.0.zip
     بسته خودکفای تولید نصب‌کننده: installer + سورس کامل (بدون bin/obj) + راهنماها (شامل vs-guide 🆕)
  ۲) SahandClinicPro-LicenseServer-cPanel-v1.2.zip
     سرور لایسنس PHP به‌روزشده — با تنظیمات اطلاعات پشتیبانی + کاتالوگ ۳۰ ماژولی 🆕
"""
import zipfile
import pathlib
import sys

REPO = pathlib.Path("/home/z/my-project/Sahand_ClinicPro")
OUT = pathlib.Path("/home/z/my-project/download")

EXCLUDE_DIRS = {"bin", "obj", "publish", "dist", ".git", ".vs", "TestResults"}
EXCLUDE_SUFFIXES = (".user", ".suo")


def add_tree(zf: zipfile.ZipFile, base: pathlib.Path, arc_root: str) -> int:
    """افزودن بازگشتی یک پوشه به ZIP با ریشه دلخواه — خروجی: تعداد فایل"""
    count = 0
    for path in sorted(base.rglob("*")):
        if not path.is_file():
            continue
        rel = path.relative_to(base)
        if any(part in EXCLUDE_DIRS for part in rel.parts):
            continue
        if path.suffix in EXCLUDE_SUFFIXES:
            continue
        zf.write(path, f"{arc_root}/{rel.as_posix()}")
        count += 1
    return count


def build_setup_zip():
    """بسته تولید نصب‌کننده — سورس کامل + installer + مستندات + راهنمای VS"""
    dst = OUT / "SahandClinicPro-Setup-v1.6.0.zip"
    total = 0
    with zipfile.ZipFile(dst, "w", zipfile.ZIP_DEFLATED, compresslevel=9) as zf:
        # ۱) ریشه: فایل‌های ساخت solution
        for fname in ["Sahand.ClinicPro.sln", "Directory.Packages.props",
                      "Directory.Build.props", "nuget.config", "LICENSE"]:
            f = REPO / fname
            if f.exists():
                zf.write(f, f"SahandClinicPro/{fname}")
                total += 1
        # ۲) کل سورس (شامل LicenseServer.Php و ۵۲ اسکرین‌شات فرم‌ها)
        total += add_tree(zf, REPO / "src", "SahandClinicPro/src")
        # ۳) بسته نصب‌کننده
        total += add_tree(zf, REPO / "installer", "SahandClinicPro/installer")
        # ۴) راهنماهای HTML — شامل vs-guide.html جدید
        total += add_tree(zf, REPO / "docs" / "guides", "SahandClinicPro/docs/guides")
        # ۵) فونت‌ها و دارایی‌های مستندات (لوگو)
        total += add_tree(zf, REPO / "docs" / "assets" / "fonts", "SahandClinicPro/docs/assets/fonts")
        logo = REPO / "docs" / "assets" / "ClinicPro.png"
        if logo.exists():
            zf.write(logo, "SahandClinicPro/docs/assets/ClinicPro.png")
            total += 1
        # ۶) فایل شروع سریع — با اطلاعات پشتیبانی واقعی
        quick_start = (
            "سامانه مدیریت درمانگاه سهند — Sahand ClinicPro v1.6.0\n"
            "بسته تولید نصب‌کننده (خودکفا)\n"
            "════════════════════════════════════════════════════════\n"
            "\n"
            "جدید این نسخه:\n"
            "  • پشتیبانی زنده یکپارچه — اطلاعات تماس از پنل لایسنس قابل ویرایش و\n"
            "    هنگام فعال‌سازی خودکار در سراسر برنامه نمایش داده می‌شود\n"
            "  • کاتالوگ ۳۰ ماژولی لایسنس (۶ ماژول جدید: نظرسنجی، فیش حقوقی،\n"
            "    پاراکلینیک، پرونده پیشرفته، طب مکمل، شعبه‌ها)\n"
            "  • اعتبارسنجی کد ملی در ویرایش بیمار (ضد جعل)\n"
            "  • راهنمای جدید ویژوال استودیو و خروجی exe — docs\\guides\\vs-guide.html\n"
            "\n"
            "شروع سریع روی رایانه ویندوز ۱۰/۱۱:\n"
            "  ۱) این بسته را در مسیری بدون فاصله استخراج کنید (مثلاً C:\\SahandClinicPro-src)\n"
            "  ۲) .NET 8 SDK را از dotnet.microsoft.com نصب کنید\n"
            "  ۳) برای نصب کلاینت‌ها: installer\\BUILD-INSTALLER.bat را اجرا کنید\n"
            "     → خروجی: dist\\SahandClinicPro-Setup-v1.6.0.exe\n"
            "  ۴) برای نصب سرور API: installer\\INSTALL-SERVER.bat را با دسترسی Administrator اجرا کنید\n"
            "\n"
            "راهنماها (هر سه در docs\\guides\\):\n"
            "  • نصب و شبکه (مبتدی‌پسند): install-guide.html\n"
            "  • کار با نرم‌افزار: user-guide.html\n"
            "  • دانلود پروژه و ویژوال استودیو و خروجی exe: vs-guide.html\n"
            "\n"
            "پشتیبانی — سهند سرویس (سعید نجفی):\n"
            "  تلفن: 041-34436141    موبایل: 09144220430\n"
            "  ایمیل: info@ea-fixer.ir    وب‌سایت: https://ea-fixer.ir\n"
        )
        zf.writestr("SahandClinicPro/شروع-سریع-READ-ME.txt", quick_start)
        total += 1
    size_mb = dst.stat().st_size // (1024 * 1024)
    print(f"  ✔ SahandClinicPro-Setup-v1.6.0.zip  ({total} فایل، {size_mb} MB)")
    return dst


def build_license_zip():
    """بسته سرور لایسنس PHP v1.2 برای cPanel — با پشتیبانی پویا و ۳۰ ماژول"""
    dst = OUT / "SahandClinicPro-LicenseServer-cPanel-v1.2.zip"
    base = REPO / "src" / "LicenseServer.Php"
    total = 0
    with zipfile.ZipFile(dst, "w", zipfile.ZIP_DEFLATED, compresslevel=9) as zf:
        total += add_tree(zf, base, "LicenseServer")
        # راهنمای نصب در ریشه بسته هم قرار بگیرد
        zf.write(base / "README-FA.md", "README-FA.md")
        total += 1
        # یادداشت ارتقا از نسخه‌های قبل
        upgrade_note = (
            "ارتقا از نسخه‌های قبل (v1.0/v1.1) به v1.2 — سرور لایسنس درمانگاه سهند\n"
            "════════════════════════════════════════════════════════════════\n"
            "\n"
            "تغییرات v1.2:\n"
            "  ۱) بخش جدید «اطلاعات پشتیبانی محصول» در تنظیمات پنل — نام پشتیبان، شرکت،\n"
            "     تلفن، موبایل، ایمیل و وب‌سایت. این مقادیر هنگام فعال‌سازی/تپ‌قلب به\n"
            "     کلاینت ارسال و در سراسر برنامه نمایش داده می‌شوند (بدون هاردکد).\n"
            "  ۲) ۶ ماژول جدید در کاتالوگ: Survey/Payroll/Paraclinical/EMR/\n"
            "     Complementary/Branches — مجموعاً ۳۰ ماژول قابل فعال‌سازی.\n"
            "\n"
            "روش ارتقا (فقط ۲ فایل):\n"
            "  ۱) فایل‌های inc/functions.php و inc/pages.inc.php و index.php و\n"
            "     install.php این بسته را جایگزین نسخه قبلی روی هاست کنید.\n"
            "  ۲) نیازی به اجرای مجدد install.php یا تغییر دیتابیس نیست — کلیدهای\n"
            "     اطلاعات پشتیبانی در اولین ذخیره از پنل خودکار ساخته می‌شوند.\n"
            "  ۳) از پنل → تنظیمات، اطلاعات پشتیبانی را بررسی/ویرایش کنید.\n"
            "\n"
            "نصب تازه: README-FA.md را ببینید (آپلود → /install.php → ۲ دقیقه).\n"
            "\n"
            "پشتیبانی: سعید نجفی — سهند سرویس — 041-34436141 — info@ea-fixer.ir\n"
        )
        zf.writestr("UPGRADE-FA.txt", upgrade_note)
        total += 1
    size_kb = dst.stat().st_size // 1024
    print(f"  ✔ SahandClinicPro-LicenseServer-cPanel-v1.2.zip  ({total} فایل، {size_kb} KB)")
    return dst


def main():
    OUT.mkdir(parents=True, exist_ok=True)
    print("📦 ساخت ZIP های رلیز v1.6.0 — آغاز")
    build_setup_zip()
    build_license_zip()
    print("✅ هر دو ZIP آماده شد — خروجی در:", OUT)


if __name__ == "__main__":
    sys.exit(main())
