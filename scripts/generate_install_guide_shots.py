#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""📸 مولد تصاویر راهنمای نصب — پنجره‌های نصب SQL Server، کنسول سرور و فایروال ویندوز"""
import pathlib
from playwright.sync_api import sync_playwright

REPO = pathlib.Path("/home/z/my-project/Sahand_ClinicPro")
TMP = REPO / "scripts" / "_mockups"
TMP.mkdir(parents=True, exist_ok=True)
OUT = REPO / "docs" / "screenshots"
OUT.mkdir(exist_ok=True)
W, H = 1400, 900

CSS = """
@font-face { font-family:'Vazirmatn'; src:url('../../assets/fonts/Vazirmatn-Regular.ttf') format('truetype'); font-weight:400; }
@font-face { font-family:'Vazirmatn'; src:url('../../assets/fonts/Vazirmatn-Bold.ttf') format('truetype'); font-weight:700; }
* { margin:0; padding:0; box-sizing:border-box; }
html, body { width:1400px; height:900px; overflow:hidden; }
body { font-family:'Vazirmatn','Noto Color Emoji',sans-serif; direction:rtl; }
.win { position:absolute; inset:24px; background:#F3F6FA; border-radius:12px; overflow:hidden;
  box-shadow:0 30px 70px rgba(4,12,28,.5), 0 0 0 1px rgba(0,0,0,.14); display:flex; flex-direction:column; }
.tbar { height:52px; background:#fff; border-bottom:1px solid #E1E8F2; display:flex; align-items:center;
  gap:12px; padding:0 20px; flex:none; }
.tbar .ti { width:26px; height:26px; border-radius:6px; background:#0D47A1; color:#fff; font-size:14px;
  display:flex; align-items:center; justify-content:center; }
.tbar b { font-size:15px; color:#16355E; }
.tbar .sp { flex:1; }
.tbar .wx { width:44px; height:30px; display:flex; align-items:center; justify-content:center;
  color:#7C90AC; font-size:13px; }
.tbar .wx.x { background:#E81123; color:#fff; border-radius:5px; }
.body { flex:1; padding:18px 22px; overflow:hidden; display:flex; flex-direction:column; gap:14px; color:#22334D; }
.steprow { display:flex; align-items:center; gap:8px; background:#fff; border:1px solid #E1E8F2;
  border-radius:10px; padding:13px 16px; }
.steprow .n { width:34px; height:34px; border-radius:50%; background:#EDF3FB; color:#1F5E9E; font-weight:800;
  display:flex; align-items:center; justify-content:center; font-size:15px; flex:none; }
.steprow.done .n { background:#E1F7EE; color:#0B7A55; }
.steprow.now { border:2.5px solid #2E86C1; box-shadow:0 4px 14px rgba(46,134,193,.2); }
.steprow b { font-size:14px; }
.steprow small { display:block; font-size:11px; color:#7C90AC; margin-top:2px; }
.steprow .prog { margin-right:auto; }
.prog { height:8px; width:150px; border-radius:6px; background:#EDF1F6; overflow:hidden; }
.prog > div { height:100%; background:linear-gradient(90deg,#2E86C1,#43C59E); border-radius:6px; }
.btn { display:inline-flex; align-items:center; gap:8px; border-radius:8px; padding:10px 22px;
  font-size:13.5px; font-weight:700; }
.btn.pri { background:linear-gradient(135deg,#1F6FB2,#16355E); color:#fff; box-shadow:0 3px 9px rgba(22,53,94,.3); }
.btn.dis { background:#E5EAF1; color:#9AA8BC; }
textarea.cons { flex:1; width:100%; border-radius:10px; border:1px solid #22334D; background:#101E33;
  color:#8FE3B8; font-family:Consolas,'Courier New',monospace; font-size:13px; line-height:1.8;
  padding:16px 18px; direction:ltr; text-align:left; resize:none; outline:none; }
table.fw { width:100%; border-collapse:collapse; font-size:13px; background:#fff; }
.fw th { background:#16355E; color:#fff; padding:10px 13px; text-align:right; }
.fw td { padding:9.5px 13px; border-bottom:1px solid #EDF1F6; }
.fw tr.sel td { background:#E8F2FC; }
.badge { display:inline-block; border-radius:18px; padding:3px 12px; font-size:11.5px; font-weight:800; }
.b-green { background:#E1F7EE; color:#0B7A55; } .b-amber { background:#FDF1DC; color:#9A6A12; }
.b-blue { background:#E3EFFC; color:#1F5E9E; }
.infobar { background:#E8F2FC; border:1px solid #BFD5EE; border-radius:10px; padding:12px 16px;
  font-size:12.5px; color:#1F5E9E; }
"""

def win(title, icon, body):
    return f"""<!DOCTYPE html><html lang="fa" dir="rtl"><head><meta charset="UTF-8">
<style>{CSS}</style></head><body><div class="win">
<div class="tbar"><div class="ti">{icon}</div><b>{title}</b><div class="sp"></div>
<div class="wx">—</div><div class="wx">▢</div><div class="wx x">✕</div></div>
<div class="body">{body}</div></div></body></html>"""

PAGES = {}

# ─── نصب SQL Server 2022 Express ───
PAGES["install-sql-server.png"] = win("مرکز نصب SQL Server 2022 Express", "🗄",
    """
    <div class="steprow done"><div class="n">✓</div><div><b>بررسی سخت‌افزار و نرم‌افزار رایانه</b>
      <small>پیش‌نیازها با موفقیت بررسی شد</small></div><div class="prog"><div style="width:100%"></div></div></div>
    <div class="steprow now"><div class="n">2</div><div><b>نصب موتور پایگاه‌داده و ابزارها</b>
      <small>در حال کپی فایل‌ها… لطفاً صبر کنید — این مرحله ۳ تا ۸ دقیقه طول می‌کشد</small></div>
      <div class="prog"><div style="width:64%"></div></div></div>
    <div class="steprow"><div class="n">3</div><div><b>پیکربندی نمونه (Instance)</b>
      <small>نام نمونه پیش‌فرض: SQLEXPRESS</small></div><div class="prog"><div style="width:0%"></div></div></div>
    <div class="steprow"><div class="n">4</div><div><b>انتخاب حالت احراز هویت</b>
      <small>حالت ترکیبی (Windows + SQL) و تعیین رمز sa</small></div><div class="prog"><div style="width:0%"></div></div></div>
    <div style="display:flex;gap:10px;justify-content:flex-start;margin-top:auto">
      <button class="btn dis">◀ قبلی</button><button class="btn dis">بعدی ▶</button>
      <button class="btn pri">📄 مشاهده گزارش</button></div>
    """)

# ─── کنسول سرور API در حال اجرا ───
PAGES["install-api-console.png"] = win("Sahand.ClinicPro.API — کنسول سرور", "🖥",
    """
    <textarea class="cons">info: Microsoft.Hosting.Lifetime[0]
      Now listening on: http://0.0.0.0:5000
info: Microsoft.Hosting.Lifetime[0]
      Application started. Press Ctrl+C to shut down.
info: Sahand.ClinicPro.Infrastructure.Persistence.ClinicProDbContext[0]
      Database migration applied successfully (20261005093000_SeedPermissions).
info: Sahand.ClinicPro.API.Services.SeedService[0]
      Default admin user created: admin / (change on first login)
info: Sahand.ClinicPro.API.Hubs.QueueHub[1]
      QueueHub is running — realtime queue enabled.
info: Microsoft.AspNetCore.DataProtection.KeyManagement[0]
      Data protection keys persisted to C:\\ProgramData\\SahandClinicPro\\keys.

  ██████╗ ███████╗ █████╗ ██╗  ██╗ ██████╗██╗  ██╗    ██████╗██╗  ██╗
 ██╔═══██╗██╔════╝██╔══██╗██║ ██╔╝██╔═══██╝██║  ██║   ██╔════╝██║  ██║
 ██║   ██║█████╗  ███████║█████╔╝ ██║   ██║███████║   ██║     ███████║
 ██║   ██║██╔══╝  ██╔══██║██╔═██╗ ██║   ██║╚════██║   ██║     ██╔══██║
 ╚██████╔╝███████╗██║  ██║██║  ██╗╚██████╔╝     ██║██╗╚██████╗██║  ██║
  ╚═════╝ ╚══════╝╚═╝  ╚═╝╚═╝  ╚═╝ ╚═════╝      ╚═╝╚═╝ ╚═════╝╚═╝  ╚═╝

  API Version: v1.6.0   |   Swagger UI:  http://localhost:5000/swagger
  Support: Sahand Service  |  041-34436141</textarea>
    """)

# ─── فایروال ویندوز ───
PAGES["install-firewall.png"] = win("ویندوز دیفندر فایروال — قواعد ورودی", "🧱",
    """
    <div class="infobar">ℹ️ این پورت‌ها برای دسترسی رایانه‌های دیگر درمانگاه به سرور لازم‌اند.
    روی سرور اصلی این قواعد را بسازید — روی رایانه‌های کاربران نیازی نیست.</div>
    <table class="fw">
      <tr><th>نام قاعده</th><th>پورت</th><th>پروتکل</th><th>عملیات</th><th>وضعیت</th></tr>
      <tr class="sel"><td>Sahand ClinicPro — API</td><td dir="ltr">5000</td><td dir="ltr">TCP</td><td>اجازه ورود</td>
        <td><span class="badge b-green">✓ فعال (منتخب)</span></td></tr>
      <tr><td>Sahand ClinicPro — SQL Server</td><td dir="ltr">1433</td><td dir="ltr">TCP</td><td>اجازه ورود</td>
        <td><span class="badge b-green">✓ فعال</span></td></tr>
      <tr><td>Sahand ClinicPro — SQL Browser</td><td dir="ltr">1434</td><td dir="ltr">UDP</td><td>اجازه ورود</td>
        <td><span class="badge b-green">✓ فعال</span></td></tr>
      <tr><td>Sahand ClinicPro — Realtime (SignalR)</td><td dir="ltr">5000</td><td dir="ltr">TCP</td><td>اجازه ورود</td>
        <td><span class="badge b-green">✓ فعال</span></td></tr>
    </table>
    <div style="display:flex;gap:10px;margin-top:auto">
      <button class="btn pri">➕ قاعده جدید…</button>
      <button class="btn dis">✎ ویرایش</button></div>
    """)

# ─── مسیریاب شبکه درمانگاه ───
PAGES["install-network-map.png"] = win("نقشه شبکه پیشنهادی درمانگاه", "🌐",
    """
    <div style="flex:1;display:flex;flex-direction:column;gap:12px">
      <div style="display:flex;justify-content:center">
        <div style="background:#16355E;color:#fff;border-radius:14px;padding:14px 30px;text-align:center">
          <b style="font-size:15px">🌐 مودم/روتر اینترنت</b><br><small style="opacity:.8;direction:ltr">192.168.1.1</small></div>
      </div>
      <div style="display:flex;justify-content:center;font-size:22px;color:#7C90AC">⬍</div>
      <div style="display:flex;justify-content:center">
        <div style="background:linear-gradient(135deg,#1F6FB2,#2E86C1);color:#fff;border-radius:14px;padding:14px 30px;text-align:center;box-shadow:0 8px 20px rgba(31,111,178,.35)">
          <b style="font-size:15px">🖥 رایانه سرور اصلی</b><br>
          <small style="opacity:.85">نصب SQL Server + سرور برنامه</small><br>
          <b style="direction:ltr;font-size:13px">192.168.1.10</b>
          <span style="background:rgba(255,255,255,.2);border-radius:12px;padding:2px 10px;font-size:11px">IP ثابت</span></div>
      </div>
      <div style="display:flex;justify-content:center;font-size:22px;color:#7C90AC">⬍ سوییچ شبکه</div>
      <div style="display:flex;gap:14px;justify-content:center">
        <div style="background:#fff;border:2px solid #E1E8F2;border-radius:12px;padding:11px 22px;text-align:center">
          <b style="font-size:13px">💻 پذیرش ۱</b><br><small style="direction:ltr;color:#7C90AC">192.168.1.21</small></div>
        <div style="background:#fff;border:2px solid #E1E8F2;border-radius:12px;padding:11px 22px;text-align:center">
          <b style="font-size:13px">💻 پذیرش ۲</b><br><small style="direction:ltr;color:#7C90AC">192.168.1.22</small></div>
        <div style="background:#fff;border:2px solid #E1E8F2;border-radius:12px;padding:11px 22px;text-align:center">
          <b style="font-size:13px">💻 اتاق پزشک</b><br><small style="direction:ltr;color:#7C90AC">192.168.1.23</small></div>
        <div style="background:#fff;border:2px solid #E1E8F2;border-radius:12px;padding:11px 22px;text-align:center">
          <b style="font-size:13px">🕐 دستگاه حضورغیاب</b><br><small style="direction:ltr;color:#7C90AC">192.168.1.50</small></div>
      </div>
    </div>
    """)


def main():
    with sync_playwright() as p:
        browser = p.chromium.launch(args=["--force-color-profile=srgb"])
        page = browser.new_page(viewport={"width": W, "height": H}, device_scale_factor=1)
        for name, html in PAGES.items():
            src = TMP / f"ig_{name}.html"
            src.write_text(html, encoding="utf-8")
            page.goto(src.resolve().as_uri(), wait_until="networkidle")
            page.wait_for_timeout(300)
            out = OUT / name
            page.screenshot(path=str(out), clip={"x": 0, "y": 0, "width": W, "height": H})
            print(f"  ✔ {name}  ({out.stat().st_size // 1024} KB)")
        browser.close()
    print("✅ تصاویر راهنمای نصب تولید شد")


if __name__ == "__main__":
    main()
