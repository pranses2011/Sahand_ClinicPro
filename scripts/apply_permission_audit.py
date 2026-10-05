#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""🛠 اعمال نتایج ممیزی مجوز چرخه دوازدهم (A3) — افزودن HasPermission به موارد حساس
و کامنت عمدت «عمومی برای کاربران واردشده» بر خودخدمت‌ها"""
import pathlib
import re

ROOT = pathlib.Path("/home/z/my-project/Sahand_ClinicPro/src/Sahand.ClinicPro.API/Controllers")

def patch(fname, anchor, add_above):
    """درج خط add_above دقیقاً بالای خط anchor در فایل — یک‌بار"""
    p = ROOT / fname
    src = p.read_text(encoding="utf-8")
    if add_above.strip() in src:
        print(f"  ⏭ {fname}: از قبل هست — {add_above.strip()[:60]}")
        return
    idx = src.find(anchor)
    if idx < 0:
        print(f"  ❌ {fname}: anchor پیدا نشد — {anchor[:60]}")
        return
    # درج قبل از anchor با همان تورفتگی
    indent = re.match(r'\s*', src[idx:]).group(0)
    src = src[:idx] + indent + add_above.strip() + "\n" + src[idx:]
    p.write_text(src, encoding="utf-8")
    print(f"  ✔ {fname}: {anchor.strip()[:52]} ← {add_above.strip()[:44]}")

# ─── Doctors — غایبات/برنامه/عملکرد ───
patch("DoctorsController.cs", 'public async Task<IActionResult> GetAbsences',
      '[HasPermission("doctors.view")] // ممیزی چرخه ۱۲ — داده حساس برنامه پزشک')
patch("DoctorsController.cs", 'public async Task<IActionResult> CreateAbsence',
      '[HasPermission("doctors.edit")] // ممیزی چرخه ۱۲')
patch("DoctorsController.cs", 'public async Task<IActionResult> DeleteAbsence',
      '[HasPermission("doctors.edit")] // ممیزی چرخه ۱۲')
patch("DoctorsController.cs", 'public async Task<IActionResult> SaveSchedule',
      '[HasPermission("doctors.edit")] // ممیزی چرخه ۱۲')
patch("DoctorsController.cs", 'public async Task<IActionResult> GetSchedule',
      '[HasPermission("doctors.view")] // ممیزی چرخه ۱۲')
patch("DoctorsController.cs", 'public async Task<IActionResult> GetPerformance',
      '[HasPermission("doctors.view")] // ممیزی چرخه ۱۲')
patch("DoctorsController.cs", 'public async Task<IActionResult> GetCalendarSummary',
      '[HasPermission("doctors.view")] // ممیزی چرخه ۱۲')

# ─── Payments — تراکنش‌های امروز ───
patch("PaymentsController.cs", 'public async Task<IActionResult> GetTodayTransactions',
      '[HasPermission("payments.view")] // ممیزی چرخه ۱۲')
patch("PaymentsController.cs", 'public async Task<IActionResult> GetTodaySummary',
      '[HasPermission("payments.view")] // ممیزی چرخه ۱۲')

# ─── Prescriptions — قالب‌ها/تداخل/ارسال الکترونیک ───
patch("PrescriptionsController.cs", 'public async Task<IActionResult> GetTemplates',
      '[HasPermission("prescriptions.view")] // ممیزی چرخه ۱۲')
patch("PrescriptionsController.cs", 'public async Task<IActionResult> SaveTemplate',
      '[HasPermission("prescriptions.edit")] // ممیزی چرخه ۱۲')
patch("PrescriptionsController.cs", 'public async Task<IActionResult> DeleteTemplate',
      '[HasPermission("prescriptions.edit")] // ممیزی چرخه ۱۲')
patch("PrescriptionsController.cs", 'public async Task<IActionResult> CheckInteractions',
      '[HasPermission("prescriptions.view")] // ممیزی چرخه ۱۲ — لازمِ نسخه‌نویس')
patch("PrescriptionsController.cs", 'public async Task<IActionResult> SubmitElectronic',
      '[HasPermission("prescriptions.edit")] // ممیزی چرخه ۱۲ — ارسال دولتی')

# ─── Receptions — آمار صف ───
patch("ReceptionsController.cs", 'public async Task<IActionResult> GetQueueStats',
      '[HasPermission("receptions.view")] // ممیزی چرخه ۱۲')

# ─── Surveys — ثبت پاسخ ───
patch("SurveysController.cs", 'public async Task<IActionResult> SubmitResponse',
      '[HasPermission("surveys.view")] // ممیزی چرخه ۱۲ — ثبت پاسخ توسط پرسنل/کیوسک')

# ─── خودخدمت‌ها — کامنت عمدت «عمومی برای کاربران واردشده» ───
PUBLIC_NOTE = '// عمدتاً عمومی برای همه کاربران واردشده (ممیزی چرخه ۱۲):'
patch("AuthController.cs", 'public async Task<IActionResult> ChangePassword',
      PUBLIC_NOTE + ' تغییر رمزِ خودِ کاربر — نیازمند مجوز جدا نیست')
patch("LicenseController.cs", 'public async Task<IActionResult> GetStatus',
      PUBLIC_NOTE + ' وضعیت لایسنس برای همه کاربران برنامه نمایش داده می‌شود')
patch("LicenseController.cs", 'public async Task<IActionResult> GetSupportMessages',
      PUBLIC_NOTE + ' پیام‌های پشتیبانی روی داشبورد همه کاربران')
patch("LookupsController.cs", 'public class LookupsController',
      PUBLIC_NOTE + ' فهرست‌های مرجع فقط-خواندنی (تخصص/بیمه/دارو/خدمت) — خواندن برای همه فرم‌ها لازم است')
patch("NotificationsController.cs", 'public class NotificationsController',
      PUBLIC_NOTE + ' اعلان‌های خودِ کاربر واردشده — خودخدمت')
patch("PrescriptionsController.cs", 'public async Task<IActionResult> SearchMedications',
      PUBLIC_NOTE + ' جستجوی مرجع دارو — فقط-خواندنی برای همه فرم‌ها')

print("\nDONE — ممیزی اعمال شد")
