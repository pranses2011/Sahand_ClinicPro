#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""🧰 ClinicPro Release CLI — ابزار واحد انتشار (چرخهٔ ۵۳ · BUG-05).

جایگزین ۱۲۳ اسکریپت تاریخی بایگانی‌شده در scripts/archive/ — یک ابزار،
سه دستور، همهٔ گاردهای انسجام ۴۸ چرخهٔ گذشته:

  doctor     — گارد انسجام نسخه/CHANGELOG + ممیزی XAML/usings + اسکنر امنیتی
  update-zip — بستهٔ آپدیت فقط با فایل‌های تغییرکرده از تگ --from + راهنمای HTML فارسی
  release    — doctor + update-zip + تگ + رلیز گیت‌هاب + بارگذاری زیپ

نمونه:
  python scripts/clinicpro_release.py doctor
  python scripts/clinicpro_release.py update-zip --from v2.13.0 --to v2.14.0
  python scripts/clinicpro_release.py release   --from v2.13.0 --to v2.14.0
"""
from __future__ import annotations

import argparse
import json
import pathlib
import re
import subprocess
import sys
import urllib.request
import zipfile

REPO = pathlib.Path(__file__).resolve().parents[1]
PROPS = REPO / "Directory.Build.props"
CHANGELOG = REPO / "CHANGELOG.md"
OLD_JWT_KEY = "SahandClinicPro-Development-Secret-Key-Change-In-Production-2026"
GITHUB_API = "https://api.github.com/repos/pranses2011/Sahand_ClinicPro"

# مستثنی‌های دیف بستهٔ آپدیت — مستندات داخلی/آرشیو/زیرساخت مخزن به ایستگاه مالک ربط ندارند
ZIP_EXCLUDE_PREFIXES = (
    "docs/analysis/", "docs/screenshots/", "docs/prompts/",
    "docs/marketing/", "docs/design/", "docs/assets/",
    ".github/", "scripts/archive/",
)
# از scripts فقط امنیتی می‌رود؛ ابزارهای ممیزی و خودِ CLI نیستند
ZIP_SCRIPTS_ROOT_KEEP = pathlib.PurePosixPath("scripts/security")


def die(msg: str) -> None:
    print(f"⛔ {msg}")
    sys.exit(1)


def ok(msg: str) -> None:
    print(f"✅ {msg}")


def run(cmd: list[str] | str, *, shell: bool = False, check: bool = True) -> str:
    r = subprocess.run(cmd, shell=shell, capture_output=True, text=True, cwd=REPO)
    if check and r.returncode != 0:
        die(f"فرمان شکست خورد: {cmd}\n{r.stdout[-800:]}\n{r.stderr[-800:]}")
    return r.stdout


def read_props_version() -> str:
    m = re.search(r"<Version>([\d.]+)</Version>", PROPS.read_text(encoding="utf-8"))
    return m.group(1) if m else die("نسخه در Directory.Build.props پیدا نشد")


# ═══════════════════════ دستور ۱: doctor ═══════════════════════

def cmd_doctor(args: argparse.Namespace) -> None:
    """گارد انسجام پیش از هر انتشار — همه باید سبز باشد."""
    print("🩺 doctor — گارد انسجام انتشار ClinicPro")
    print("═" * 60)

    # ۱) نسخهٔ Build.props
    version = read_props_version()
    ok(f"نسخهٔ Directory.Build.props: {version}")

    # ۲) سرچشمهٔ CHANGELOG با نسخهٔ فعلی همخوان است؟
    head = CHANGELOG.read_text(encoding="utf-8")[:4000]
    if f"## [v{version}]" in head:
        ok(f"سرچشمهٔ CHANGELOG با v{version} همخوان است")
    else:
        die(f"CHANGELOG سرچشمهٔ «## [v{version}]» ندارد — ابتدا سرچشمهٔ فارسی بنویسید")

    # ۳) انطباق اختیاری با نسخهٔ موردانتظار (پرچم --expect)
    if args.expect and args.expect != version:
        die(f"نسخهٔ موردانتظار {args.expect} != نسخهٔ فایل {version}")

    # ۴) ممیزی XAML — همهٔ فایل‌ها باید XML معتبر باشند
    out = run([sys.executable, "scripts/validate_xaml.py"])
    if "نامعتبر: 0" not in out:
        die(f"XAML نامعتبر: {out[-500:]}")
    ok("اعتبارسنجی XAML — همه سبز")

    # ۵) ممیزی using های رسانه‌ای
    out = run([sys.executable, "scripts/audit_csharp_usings.py"])
    if "سالم‌اند" not in out:
        die(f"ممیزی using: {out[-500:]}")
    ok("ممیزی using های رسانه‌ای — همه سبز")

    # ۶) اسکنر الگوهای ممنوعه (کلید/انسداد رشته‌ای/async void)
    out = run(["bash", "scripts/security/scan_secrets.sh"], check=False)
    if "همه گیت‌ها سبز" not in out and "گیت‌ها سبز" not in out:
        die(f"اسکنر الگوهای ممنوعه قرمز است:\n{out[-800:]}")
    ok("اسکنر امنیتی ۶ گیته — همه سبز")

    # ۷) آمار بهداشت مخزن
    active = [p for p in (REPO / "scripts").iterdir() if p.is_file()]
    archived = list((REPO / "scripts" / "archive").glob("*"))
    ok(f"اسکریپت فعال: {len(active)} (CLI + ممیزی‌ها + E2E) — آرشیو تاریخی: {len(archived)} فایل")

    # ۸) درخت کاری تمیز؟ (هشدار — نه خطا)
    status = run("git status --porcelain", shell=True)
    if status.strip():
        print(f"⚠️ درخت کاری کثیف است ({len(status.splitlines())} فایل) — پیش از تگ/رلیز کامیت کنید")
    else:
        ok("درخت کاری تمیز")

    print("═" * 60)
    print(f"🎉 doctor سبز است — آمادهٔ انتشار v{version}")


# ═══════════════════ دستور ۲: update-zip ═══════════════════

def build_zip_file_list(tag_from: str) -> list[str]:
    """دیف واقعی تگ --from تا HEAD با مستثنی‌های بسته."""
    raw = run(f'git diff --name-only {tag_from}..HEAD', shell=True).split()
    files = []
    for path in raw:
        p = pathlib.PurePosixPath(path)
        if any(path.startswith(prefix) for prefix in ZIP_EXCLUDE_PREFIXES):
            continue
        if len(p.parts) >= 2 and p.parts[0] == "scripts" and p.parts[1] != "security":
            continue  # اسکریپت‌های داخلی مخزن — به ایستگاه مالک نمی‌روند
        if not (REPO / path).is_file():
            continue
        files.append(path)
    return sorted(set(files))


GUIDE_TEMPLATE = """<!DOCTYPE html>
<html lang="fa" dir="rtl">
<head><meta charset="UTF-8"><title>راهنمای آپدیت {TO_VER}</title>
<style>
 body {{ font-family: Vazirmatn, Tahoma, sans-serif; background:#f8fafc; color:#0f172a; margin:0; padding:24px; line-height:1.9; }}
 .wrap {{ max-width:860px; margin:0 auto; }}
 h1 {{ color:#0d9488; border-bottom:3px solid #0d9488; padding-bottom:12px; font-size:1.4rem; }}
 h2 {{ color:#be123c; margin-top:28px; font-size:1.1rem; }}
 .warn {{ background:#fff7ed; border:1px solid #fdba74; border-right:5px solid #ea580c; border-radius:8px; padding:14px 18px; margin:14px 0; }}
 code {{ background:#e2e8f0; border-radius:4px; padding:2px 8px; direction:ltr; display:inline-block; }}
 table {{ border-collapse:collapse; width:100%; margin:12px 0; }}
 th, td {{ border:1px solid #cbd5e1; padding:6px 10px; text-align:right; font-size:0.9em; }}
 th {{ background:#f1f5f9; }}
 ol li {{ margin:6px 0; }}
</style></head>
<body><div class="wrap">
<h1>📦 آپدیت {FROM_VER} → {TO_VER}</h1>
<p>این بسته فقط <b>فایل‌های تغییرکرده</b> را دارد — روی سورس پروژه جایگزین کنید (ساختار مسیرها حفظ شده)، سپس <b>Clean + Rebuild</b> و در صورت تغییر سرور API، ری‌استارت آن.</p>
<h2>📋 فایل‌های این بسته ({COUNT} فایل)</h2>
<table><tr><th>مسیر</th><th>لایه</th></tr>{ROWS}</table>
<h2>🧯 اگر پس از Rebuild خطا یا رفتار کهنه دیدید</h2>
<div class="warn"><code>Clean-VS-Cache.bat</code> را در ریشهٔ سولوشن اجرا و دوباره Rebuild کنید — کش BAML/انجماد Visual Studio علت شناخته‌شدهٔ «رفع‌ها بی‌اثر» است.</div>
<p>جزئیات کامل کاربر-محور در <code>CHANGELOG.md</code> سرچشمهٔ v{TO_VER}.</p>
</div></body></html>"""


def cmd_update_zip(args: argparse.Namespace) -> None:
    """ساخت بستهٔ آپدیت فقط با فایل‌های تغییرکرده + گاردهای انسجام."""
    to_version = args.to.lstrip("v")
    from_tag = args.from_tag

    # گارد ۱: نسخهٔ فایل == نسخهٔ هدف
    if read_props_version() != to_version:
        die(f"نسخهٔ Directory.Build.props ({read_props_version()}) != نسخهٔ هدف ({to_version})")

    # گارد ۲: تگ مبدأ موجود است
    run(f'git rev-parse --verify {from_tag}', shell=True)

    # فهرست دیف
    files = build_zip_file_list(from_tag)
    if not files:
        die(f"دیف {from_tag}..HEAD پس از مستثنی‌ها خالی است — یا تگ غلط است یا تغییری نیست")
    # گارد ۳: تغییرات API/دیتابیس باید در بسته باشند (الگوی مالک)
    has_api = any(f.startswith("src/Sahand.ClinicPro.API/") or f.startswith("src/Sahand.ClinicPro.Infrastructure/") for f in files)
    has_desktop = any(f.startswith("src/Sahand.ClinicPro.Desktop/") for f in files)

    # گارد ۴: منفی — کلید افشاشدهٔ تاریخی در هیچ فایل بسته (خارج از گارد/تست/اسکنر)
    negative_whitelist = {
        "src/Sahand.ClinicPro.Infrastructure/Identity/IdentitySetup.cs",
        "tests/Sahand.ClinicPro.Infrastructure.IntegrationTests/JwtKeyGuardTests.cs",
        "scripts/security/scan_secrets.sh",
    }
    for path in files:
        if path in negative_whitelist:
            continue
        if OLD_JWT_KEY in (REPO / path).read_text(encoding="utf-8", errors="ignore"):
            die(f"گارد منفی: کلید افشاشدهٔ تاریخی در {path} — نباید در بسته باشد")

    # خروجی
    out = pathlib.Path(args.out) if args.out else (
        REPO.parent / "download" / f"SahandClinicPro-Update-{from_tag.lstrip('v')}-to-{to_version}.zip")
    out.parent.mkdir(parents=True, exist_ok=True)

    rows = "\n".join(
        f"<tr><td><code>{p}</code></td><td>{'دسکتاپ' if '/Desktop/' in p else ('سرور API' if ('/API/' in p or '/Infrastructure/' in p or '/Application/' in p or '/Domain/' in p) else 'عمومی')}</td></tr>"
        for p in files)

    guide = GUIDE_TEMPLATE.format(
        FROM_VER=from_tag.lstrip("v"), TO_VER=to_version, COUNT=len(files), ROWS=rows)
    manifest = (
        f"فایل‌های تغییرکرده {from_tag} → v{to_version} (ClinicPro Release CLI — چرخهٔ ۵۳)\n"
        + "═" * 60 + "\n" + "\n".join(files)
        + f"\n\nلایه‌ها: سرور API={'دارد' if has_api else 'ندارد'} · دسکتاپ={'دارد' if has_desktop else 'ندارد'}\n")

    with zipfile.ZipFile(out, "w", zipfile.ZIP_DEFLATED) as zf:
        for path in files:
            zf.write(REPO / path, path)
        zf.writestr(f"Update-Guide-{to_version}.html", guide)
        zf.writestr("changed-files.txt", manifest)

    ok(f"بستهٔ آپدیت ساخته شد: {out}")
    ok(f"تعداد فایل‌ها: {len(files)} + راهنما + مانیفست — حجم: {out.stat().st_size:,} بایت")
    print(f"   سرور API در بسته: {'✅' if has_api else '—'} · دسکتاپ در بسته: {'✅' if has_desktop else '—'}")
    for p in files:
        print("   •", p)


# ═══════════════════ دستور ۳: release ═══════════════════

def cmd_release(args: argparse.Namespace) -> None:
    """doctor سبز → زیپ → تگ → رلیز گیت‌هاب → بارگذاری زیپ."""
    to_tag = args.to if args.to.startswith("v") else "v" + args.to
    to_version = to_tag.lstrip("v")

    # ۱) doctor سبز (بدون پرچم انتظار — نسخهٔ فایل ملاک است)
    doctor_ns = argparse.Namespace(expect=to_version)
    cmd_doctor(doctor_ns)

    # ۲) بستهٔ آپدیت
    zip_ns = argparse.Namespace(from_tag=args.from_tag, to=to_version, out=args.zip_out)
    if not args.skip_zip:
        cmd_update_zip(zip_ns)
    zip_path = (pathlib.Path(args.zip_out) if args.zip_out else (
        REPO.parent / "download" / f"SahandClinicPro-Update-{args.from_tag.lstrip('v')}-to-{to_version}.zip"))

    # ۳) توکن از ریموت
    remote = run("git config --get remote.origin.url", shell=True)
    token = remote.split("//")[1].split("@")[0].strip() if "//" in remote and "@" in remote else ""
    if not token:
        die("توکن گیت‌هاب از remote.origin.url استخراج نشد")

    # ۴) تگ روی HEAD
    run(f'git tag -f {to_tag} HEAD', shell=True)
    run(f'git push origin -f {to_tag}', shell=True)
    ok(f"تگ {to_tag} پوش شد")

    # ۵) بدنهٔ یادداشت از سرچشمهٔ CHANGELOG (بخش همین نسخه)
    text = CHANGELOG.read_text(encoding="utf-8")
    m = re.search(rf"## \[v{re.escape(to_version)}\](.*?)(?=\n## \[|\Z)", text, re.S)
    if not m:
        die(f"بخش CHANGELOG نسخهٔ v{to_version} برای یادداشت رلیز پیدا نشد")
    body = m.group(1).strip()

    # ۶) ساخت رلیز
    payload = {
        "tag_name": to_tag,
        "name": f"{to_tag} — " + (re.search(rf"## \[v{re.escape(to_version)}\][^(]*\(([^)]+)\)", text).group(1)
                 if re.search(rf"## \[v{re.escape(to_version)}\][^(]*\(([^)]+)\)", text) else to_tag),
        "body": body,
        "draft": False,
        "prerelease": False,
    }
    req = urllib.request.Request(
        f"{GITHUB_API}/releases", data=json.dumps(payload).encode(),
        headers={"Authorization": f"token {token}", "Accept": "application/vnd.github+json",
                 "Content-Type": "application/json"}, method="POST")
    with urllib.request.urlopen(req) as r:
        release = json.load(r)
    ok(f"رلیز ساخته شد: {release['html_url']}")

    # ۷) بارگذاری زیپ
    if not args.skip_zip and zip_path.exists():
        upload_url = release["upload_url"].replace("{?name,label}", f"?name={zip_path.name}")
        req = urllib.request.Request(
            upload_url, data=zip_path.read_bytes(),
            headers={"Authorization": f"token {token}", "Accept": "application/vnd.github+json",
                     "Content-Type": "application/zip"}, method="POST")
        with urllib.request.urlopen(req) as r:
            asset = json.load(r)
        ok(f"زیپ بارگذاری شد: {asset['name']} — {asset['size'] // 1024} KB")

    print(f"\n🎉 رلیز {to_tag} کامل شد: {release['html_url']}")


# ═══════════════════════ ورودی اصلی ═══════════════════════

def main() -> None:
    parser = argparse.ArgumentParser(
        prog="clinicpro_release",
        description="ابزار واحد انتشار ClinicPro — چرخهٔ ۵۳ (جایگزین ۱۲۳ اسکریپت تاریخی)")
    sub = parser.add_subparsers(dest="command", required=True)

    p_doctor = sub.add_parser("doctor", help="گارد انسجام نسخه/فایل/ممیزی‌ها پیش از انتشار")
    p_doctor.add_argument("--expect", help="نسخهٔ موردانتظار (مثال 2.14.0) — انطباق با Build.props چک می‌شود")

    p_zip = sub.add_parser("update-zip", help="ساخت بستهٔ آپدیت فقط با فایل‌های تغییرکرده از تگ مبدأ")
    p_zip.add_argument("--from", dest="from_tag", required=True, help="تگ مبدأ (مثال v2.13.0)")
    p_zip.add_argument("--to", required=True, help="نسخهٔ هدف (مثال 2.14.0)")
    p_zip.add_argument("--out", help="مسیر خروجی زیپ (پیش‌فرض: download کنار مخزن)")

    p_rel = sub.add_parser("release", help="doctor + زیپ + تگ + رلیز گیت‌هاب + بارگذاری")
    p_rel.add_argument("--from", dest="from_tag", required=True, help="تگ مبدأ (مثال v2.13.0)")
    p_rel.add_argument("--to", required=True, help="نسخه/تگ هدف (مثال 2.14.0)")
    p_rel.add_argument("--zip-out", help="مسیر خروجی زیپ")
    p_rel.add_argument("--skip-zip", action="store_true", help="بدون ساخت/بارگذاری زیپ (فقط تگ+رلیز)")

    args = parser.parse_args()
    if args.command == "doctor":
        cmd_doctor(args)
    elif args.command == "update-zip":
        cmd_update_zip(args)
    else:
        cmd_release(args)


if __name__ == "__main__":
    main()
