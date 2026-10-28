#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
════════════════════════════════════════════════════════════════════════════
 نگهبانهای رگرسیون — «تپش پیوستهٔ قلب» + «اعطای خودکار نقش dbcreator»
════════════════════════════════════════════════════════════════════════════

چرا این فایل وجود دارد؟
  ۱) در پوشش بارگذاری داشبورد، تپش قلب فقط «یکبار» اجرا میشد چون فقط خط
     نوار قلب RepeatBehavior=Forever داشت؛ مدت طبیعی استوریبورد از همان
     فرزند بینهایت میشد و ۱۲ انیمیشن حفرهها هرگز چرخهٔ بعدی را نمیگرفتند.
  ۲) نصاب سرور برای «بازیابی اطلاعات از فایل پشتیبان» نقش dbcreator را فقط
     در یک حالت باریک و با Integrated Security اعطا میکرد؛ لاگین نبود،
     IS_SRVROLEMEMBERNULL میداد و مالک نمیتوانست نقش کاربر را روی SQL
     اعمال کند. نگهبان دوم، مؤلفههای حیاتیِ رفع خودکار را قفل میکند.

روش اجرا:  python3 scripts/validate_ui_guards.py
کد خروجی: ۰ = همهٔ نگهبانها سبز؛ ۱ = حداقل یک رگرسیون (پیام فارسی چاپ میشود).
"""
from __future__ import annotations

import re
import sys
import xml.etree.ElementTree as ET
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
XAML = ROOT / "src" / "Sahand.ClinicPro.Desktop" / "MainWindow.xaml"
PS1 = ROOT / "installer" / "Install-Server-Package.ps1"

errors: list[str] = []


def check(condition: bool, message: str) -> None:
    if not condition:
        errors.append(message)


# ─────────────────────────────────────────────────────────────────────────────
# ۱) انیمیشن بارگذاری — XAML معتبر + تکرار پیوسته و همگام
# ─────────────────────────────────────────────────────────────────────────────
xaml_text = XAML.read_text(encoding="utf-8")
try:
    ET.fromstring(xaml_text)
except ET.ParseError as exc:  # پروندهٔ XAML نباید شکسته شود
    errors.append(f"MainWindow.xaml دیگر XML معتبر نیست: {exc}")

story_line = next(
    (line for line in xaml_text.splitlines() if 'x:Key="HeartbeatStoryboard"' in line),
    "",
)
check(bool(story_line), "استوریبورد HeartbeatStoryboard در MainWindow.xaml پیدا نشد.")
check(
    'Duration="0:0:2.4"' in story_line and 'RepeatBehavior="Forever"' in story_line,
    "استوریبورد قلب باید مدت صریح ۲٫۴ ثانیه و RepeatBehavior=Forever داشته باشد "
    "(در غیر این صورت مدت طبیعیاش بینهایت شده و تپش فقط یکبار اجرا میشود).",
)

keyframes = re.findall(r"<DoubleAnimationUsingKeyFrames\b[^>]*>", story_line)
check(len(keyframes) >= 12, f"انتظار حداقل ۱۲ انیمیشن حفرهٔ قلب؛ پیدا شد {len(keyframes)}.")
for tag in keyframes:
    check(
        'RepeatBehavior="Forever"' in tag,
        f"یک انیمیشن حفرهٔ قلب بدون RepeatBehavior=Forever است: {tag[:100]}",
    )
# بدنهٔ کیفریمها باید سرانجامشان ۲٫۴ ثانیه باشد تا با نوار قلب همفاز بمانند
keyframe_bodies = re.findall(
    r"<DoubleAnimationUsingKeyFrames\b[^>]*>(.*?)</DoubleAnimationUsingKeyFrames>",
    story_line,
)
check(len(keyframe_bodies) == len(keyframes),
      "تعداد بدنههای کیفریم با تعداد انیمیشنهای حفرهها همخوان نیست.")
check(
    all('KeyTime="0:0:2.4"' in body for body in keyframe_bodies),
    "یکی از انیمیشنهای حفرهها کلید پایانی ۲٫۴ ثانیه ندارد — چرخهها از هم جدا میافتند.",
)

ecg = re.findall(r"<DoubleAnimation\b[^>]*>", story_line)
check(len(ecg) >= 1, "انیمیشن حرکت نوار قلب پیدا نشد.")
for tag in ecg:
    check(
        'RepeatBehavior="Forever"' in tag and 'Duration="0:0:2.4"' in tag,
        f"خط نوار قلب باید پیوسته و با دورهٔ ۲٫۴ ثانیه حرکت کند: {tag[:120]}",
    )

# ─────────────────────────────────────────────────────────────────────────────
# ۲) نصاب سرور — اعطای خودکار و چهارلایهٔ نقش dbcreator
# ─────────────────────────────────────────────────────────────────────────────
PS1 = ROOT / "installer" / "Install-Server-Package.ps1"
PS_LIB = ROOT / "installer" / "Enable-RestorePermission.ps1"
PS_BAT = ROOT / "installer" / "Enable-RestorePermission.bat"


def read_ps(path: Path, label: str) -> str:
    raw = path.read_bytes()
    check(raw.startswith(b"\xef\xbb\xbf"),
          f"{label} باید UTF-8 همراه BOM باشد (Windows PowerShell 5.1).")
    return raw.decode("utf-8-sig")


ps = read_ps(PS1, "installer/Install-Server-Package.ps1")
lib = read_ps(PS_LIB, "installer/Enable-RestorePermission.ps1")
check(PS_BAT.is_file(), "installer/Enable-RestorePermission.bat وجود ندارد.")

required_markers = {
    "ساخت لاگین ویندوزی در صورت نبود": "CREATE LOGIN [",
    "افزودن بایدمنت به نقش سروری": "ALTER SERVER ROLE [dbcreator] ADD MEMBER",
    "بررسی عضویت/وجود لاگین": "sys.server_principals",
    "جلوگیری از خطای sa/حساب مدیر": "IS_SRVROLEMEMBER(N'sysadmin'",
    "مسیر جایگزین SQL Server قدیمی": "sp_addsrvrolemember",
    "حساب پیشفرض سرویس ویندوزی": "NT AUTHORITY\\SYSTEM",
    "اتصال با شناسههای خود رشته اتصال": "همان شناسههای رشتهٔ اتصال",
    "پشتیبان ارائهدهندهٔ SQL در PowerShell 7": "Microsoft.Data.SqlClient.SqlConnection",
    "فرمان پارامتری (جلوگیری از تزریق SQL)": "@Principal",
    "پرسش تعاملی حساب مدیر SQL": "Read-Host 'رمز آن لاگین' -AsSecureString",
    "راستیآزمایی نتیجه": "AS Granted",
    "فایل SQL آمادهٔ اجرا در SSMS": "Grant-RestorePermission.sql",
    "گزارش وضعیت برای پشتیبانی": "restore-permission.status.txt",
    "تشخیص لاگین واقعی API": "SUSER_SNAME()",
    "استفاده از ISNULL بهجای NULL<>1": "ISNULL(IS_SRVROLEMEMBER",
    "سوئیچ غیرفعالسازی ریاستارت": "NoSqlSingleUserElevation",
    "کتابخانه قابل اجرای مستقل": "-not $AsLibrary",
}
for label, needle in required_markers.items():
    check(needle in lib, f"نگهبان نصاب سرور شکست — {label}: نشانهٔ «{needle}» پیدا نشد.")

installer_markers = {
    "ثبت حساب سرویس نصب قبلی": "existingServiceAccounts",
    "بارگذاری کتابخانه مشترک": ". $restoreModule -AsLibrary",
    "فراخوانی تابع چهارلایه": "Invoke-ClinicProRestorePermission",
    "گزارش وضعیت": "restore-permission.status.txt",
    "تلاش دوباره پس از نصب سرویس": "تأیید نهایی مجوز بازیابی پس از نصب",
    "فایل SQL دستی": "Grant-RestorePermission.sql",
}
for label, needle in installer_markers.items():
    check(needle in ps, f"نگهبان نصاب سرور شکست — {label}: نشانهٔ «{needle}» پیدا نشد.")

# بایدمنت نباید دوباره به «IS_SRVROLEMEMBER <> 1» تنها متکی باشد
check("IS_SRVROLEMEMBER(N'dbcreator', N'$loginLiteral') <> 1" not in lib,
      "الگوی قدیمی IS_SRVROLEMEMBER <> 1 (که با NULL بیصدا رد میشد) هنوز در نصاب هست.")


def check_ps_balance(text: str, label: str) -> None:
    """تعادل ساختاری سادهای از اسکریپت PowerShell (کروشهها/پرانتزها بیرون از رشتهها)."""
    in_here = False
    braces = 0
    parens = 0
    for line in text.splitlines():
        if line.rstrip().endswith("@'"):
            in_here = True
        if in_here:
            if line.strip() == "'@":
                in_here = False
            continue
        stripped = re.sub(r"'([^']|'')*'", "''", line)
        stripped = re.sub(r'"([^"]|"")*"', '""', stripped)
        stripped = re.sub(r"#.*$", "", stripped)
        braces += stripped.count("{") - stripped.count("}")
        parens += stripped.count("(") - stripped.count(")")
    check(braces == 0, f"کروشههای {label} نامتعادلاند (اختلاف {braces}).")
    check(parens == 0, f"پرانتزهای {label} نامتعادلاند (اختلاف {parens}).")
    here_opens = sum(1 for line in text.splitlines() if line.rstrip().endswith("@'"))
    here_closes = sum(1 for line in text.splitlines() if line.strip() == "'@")
    check(here_opens == here_closes,
          f"چینش رشتههای چندخطی {label} نامتعادل است ({here_opens} باز / {here_closes} بسته).")


check_ps_balance(ps, "Install-Server-Package.ps1")
check_ps_balance(lib, "Enable-RestorePermission.ps1")

# ─────────────────────────────────────────────────────────────────────────────
# ۳) فرم پرسنل — «سمت دستیتایپشده ذخیره نمیشود»
# ─────────────────────────────────────────────────────────────────────────────
DESKTOP = ROOT / "src" / "Sahand.ClinicPro.Desktop"
APP = ROOT / "src" / "Sahand.ClinicPro.Application"
vm = (DESKTOP / "ViewModels" / "EmployeeFormViewModel.cs").read_text(encoding="utf-8")
lookup_helper = (DESKTOP / "Helpers" / "LookupName.cs").read_text(encoding="utf-8")
hr = (APP / "Features" / "HR" / "HRFeatures.cs").read_text(encoding="utf-8")
resolver = (APP / "Features" / "HR" / "EmployeeLookupResolver.cs").read_text(encoding="utf-8")

check("positionName" in vm and "departmentName" in vm,
      "بدنهٔ درخواست ذخیرهٔ پرسنل باید positionName/departmentName بفرستد.")
check("public static (long? Id, string? Name) ResolveLookup(" in vm,
      "تابع ResolveLookup (متنمحور) در EmployeeFormViewModel پیدا نشد.")
check("ResolveLookupIdAsync" not in vm,
      "تابع قدیمی ResolveLookupIdAsync (ایندکسمحور) هنوز در ViewModel هست.")
check("public static string? Normalize(" in lookup_helper,
      "کمککنندهٔ نرمالسازی نام مرجع LookupName پیدا نشد.")
check("public enum Resolution" in resolver and "Resolution.FindOrCreate" in resolver,
      "قانون تقدم نام بر شناسه در EmployeeLookupResolver پیدا نشد.")
check("string? PositionName = null" in hr and "string? DepartmentName = null" in hr,
      "CreateEmployeeDto باید فیلدهای PositionName/DepartmentName داشته باشد.")
check(hr.count("EmployeeLookupResolver.ResolvePositionIdAsync") == 2,
      "هر دو گردانندهٔ ثبت و ویرایش پرسنل باید از حلکنندهٔ سروری استفاده کنند.")
check(hr.count("EmployeeLookupResolver.ResolveDepartmentIdAsync") == 2,
      "هر دو گردانندهٔ ثبت و ویرایش پرسنل باید دپارتمان را حل کنند.")
check("selectedIndex >= 0" in vm and "IsEmptyMarker(name)" in vm,
      "نگهبان ترتیب حل مرجع شکست — متن باید پیش از ایندکس بررسی شود.")

# ─────────────────────────────────────────────────────────────────────────────
if errors:
    print("❌ نگهبانهای رگرسیون قرمز شدند:")
    for e in errors:
        print("   •", e)
    sys.exit(1)
print("✅ نگهبانهای رگرسیون سبزند: تپش پیوستهٔ قلب + dbcreator چهارلایه + سمت پرسنل.")
