# 📋 لیست کارهای Sahand_ClinicPro

> فایل مدیریت پروژه — چک‌لیست ۳۱ گام پیاده‌سازی در ۹ فاز
> منبع: `docs/prompts/prompt-part3.html` (بخش ۸: ترتیب پیاده‌سازی)

## 🎯 پیشرفت کلی: 0% (0/31 گام)

---

## 📦 فاز ۱: راه‌اندازی و زیرساخت
- [ ] گام ۱: Solution Setup (ایجاد پروژه‌ها + NuGet Packages + DI Configuration)
- [ ] گام ۲: Domain Layer (BaseEntity + همه Entities + Enums + Value Objects + Interfaces)
- [ ] گام ۳: Infrastructure - Persistence (DbContext + Configurations + Migrations + Seeders)
- [ ] گام ۴: Infrastructure - Identity (Auth Service + JWT + Password Hasher + RBAC)

## 🎯 فاز ۲: ماژول‌های هسته‌ای
- [ ] گام ۵: Application - Common (Behaviors + DTOs + Mapping + Helpers)
- [ ] گام ۶: Application - Auth Module (Login + Refresh + ChangePassword)
- [ ] گام ۷: Application - Patients Module (CQRS Commands + Queries)
- [ ] گام ۸: Application - Doctors Module
- [ ] گام ۹: API Layer (Program.cs + Middleware + Controllers: Auth, Patients, Doctors)

## 🖥️ فاز ۳: Desktop Client
- [ ] گام ۱۰: WPF Setup (App.xaml + MainWindow + Theme + Fonts + Navigation)
- [ ] گام ۱۱: WPF Login + Dashboard
- [ ] گام ۱۲: WPF Patient Module (List + Form + Detail)

## 🏥 فاز ۴: ماژول‌های درمانی
- [ ] گام ۱۳: Appointments Module (Application + API + WPF)
- [ ] گام ۱۴: Reception Module (Application + API + WPF)
- [ ] گام ۱۵: Visit Module (Application + API + WPF)
- [ ] گام ۱۶: Prescription Module (Application + API + WPF)

## 💰 فاز ۵: مالی و حسابداری
- [ ] گام ۱۷: Invoice + Payment Module (Application + API + WPF)
- [ ] گام ۱۸: Accounting Module (Application + API + WPF)
- [ ] گام ۱۹: Insurance Module (Application + API + WPF)

## 🔬 فاز ۶: پاراکلینیک و داروخانه
- [ ] گام ۲۰: Lab Module (Application + API + WPF)
- [ ] گام ۲۱: Pharmacy Module (Application + API + WPF)
- [ ] گام ۲۲: Paraclinical Modules (Imaging, Dental, Eye, Physio)

## 📱 فاز ۷: ارتباطات و یکپارچه‌سازی
- [ ] گام ۲۳: SMS Module (10 providers + Templates + Queue)
- [ ] گام ۲۴: Government Integration (Sepas, Sib, Nab, Sina, E-Prescription)
- [ ] گام ۲۵: License System (Portal + Client + Activation + Heartbeat)

## 📊 فاز ۸: گزارش و اداری
- [ ] گام ۲۶: Reports Module (Stimulsoft + Dashboard Charts)
- [ ] گام ۲۷: HR Module (Employees + Attendance + Salary)
- [ ] گام ۲۸: Backup + Settings + Audit Log

## 🚀 فاز ۹: نهایی‌سازی
- [ ] گام ۲۹: Security Hardening + Performance Optimization
- [ ] گام ۳۰: Unit Tests + Integration Tests
- [ ] گام ۳۱: Documentation + Deployment Package

---

## ✅ خارج از ۳۱ گام (تکمیل شده):
- [x] فاز صفر: ایجاد ریپازیتوری GitHub
- [x] فاز صفر: استخراج مستندات در docs/
- [x] فاز صفر: ایجاد TODO.md و PROGRESS.md و README.md و CHANGELOG.md

---

## 📝 لاگ تغییرات اخیر:
- **2026-10-05**: راه‌اندازی ریپازیتوری، استخراج ۹ فایل مستندات (۶ طراحی + ۳ پرامپت)، ایجاد فایل‌های مدیریتی. آماده شروع گام ۱.

## ⚠️ موارد نیاز به توجه:
- [Open] تعیین نام‌گذاری نهایی پروژه‌های Solution: مستندات داخلی از `ClinicPro.*` استفاده می‌کنند ولی ساختار ریپازیتوری از `Sahand.ClinicPro.*` — پیش از گام ۱ باید نهایی شود.
- [Open] نسخه دقیق NuGet Packageها باید در گام ۱ تعیین شود (در مستندات ذکر نشده است).
- [Info] توکن دسترسی GitHub فقط در Local نگهداری شود و هرگز در فایل‌های ریپازیتوری قرار نگیرد.
