#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
📦 استخراج کلیدهای منبع پکیج MaterialDesignThemes از DLL روی NuGet
اجرا از ریشه مخزن:  python3 scripts/extract_materialdesign_keys.py <نسخه>
پیش‌نیاز: دانلود nupkg از NuGet و استخراج DLL — سپس strings (ASCII و UTF-16)
نکته: کلیدهای BAML در DLL به‌صورت رشته ذخیره می‌شوند؛ فهرست حاصل شامل همه
کلیدهای تعریف‌شده/ارجاع‌شده منبع در پکیج است (برای مانیفست ممیزی کافی و امن است).
پس از ارتقای نسخه پکیج، مانیفست جدید بسازید و scripts/materialdesign-keys-<ver>.txt را جایگزین کنید.
"""
import pathlib
import re
import subprocess
import sys
import tempfile
import urllib.request
import zipfile

VERSION = sys.argv[1] if len(sys.argv) > 1 else "5.3.2"
URL = f"https://api.nuget.org/v3-flatcontainer/materialdesignthemes/{VERSION}/materialdesignthemes.{VERSION}.nupkg"
TOKEN = re.compile(rb'MaterialDesign[A-Za-z0-9._-]*')

with tempfile.TemporaryDirectory() as tmp:
    pkg = pathlib.Path(tmp) / "md.nupkg"
    print(f"⬇ دانلود {URL}")
    urllib.request.urlretrieve(URL, pkg)  # noqa: S310 — منبع معتبر NuGet
    z = zipfile.ZipFile(pkg)
    dll_name = next(n for n in z.namelist()
                    if n.startswith("lib/net8") and n.endswith("MaterialDesignThemes.Wpf.dll"))
    dll = pathlib.Path(tmp) / "MaterialDesignThemes.Wpf.dll"
    dll.write_bytes(z.read(dll_name))
    # استخراج رشته‌های ASCII و UTF-16LE از DLL (کلیدهای BAML)
    keys: set[bytes] = set(TOKEN.findall(dll.read_bytes()))
    for m in re.finditer(rb'(?:M\x00a\x00t\x00e\x00r\x00i\x00a\x00l\x00D\x00e\x00s\x00i\x00g\x00n\x00)+[^\x00]*',
                         dll.read_bytes()):
        keys.update(TOKEN.findall(m.group(0)))

out = pathlib.Path(__file__).resolve().parent / f"materialdesign-keys-{VERSION}.txt"
lines = sorted({k.decode("ascii", "ignore") for k in keys if len(k) > 8})
header = [f"# کلیدهای منبع واقعی پکیج MaterialDesignThemes {VERSION}",
          "# استخراج‌شده از MaterialDesignThemes.Wpf.dll با strings",
          f"# منبع: https://www.nuget.org/packages/MaterialDesignThemes/{VERSION}",
          "# بازتولید: scripts/extract_materialdesign_keys.py " + VERSION]
out.write_text("\n".join(header + lines) + "\n", encoding="utf-8")
print(f"✅ {len(lines)} کلید → {out}")
