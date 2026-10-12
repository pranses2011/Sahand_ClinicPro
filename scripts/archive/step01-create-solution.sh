#!/usr/bin/env bash
# ═══════════════════════════════════════════════════════════════════
# گام ۱ (Solution Setup) — اسکریپت ایجاد ساختار اولیه پروژه
# پروژه: Sahand ClinicPro - سامانه جامع مدیریت درمانگاه سهند
#
# نکته مهم: این اسکریپت فقط پروژه‌های قالبی (Template) را می‌سازد.
# فایل‌های csproj پس از اجرای این اسکریپت به‌صورت دستی بازنویسی
# می‌شوند تا نسخه پکیج‌ها از Directory.Packages.props مدیریت شود.
#
# نکته دوم: پروژه WPF (Sahand.ClinicPro.Desktop) به‌دلیل نبود قالب
# WPF روی لینوکس، به‌صورت دستی ساخته می‌شود و در این اسکریپت نیست.
# ═══════════════════════════════════════════════════════════════════
set -euo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$REPO_ROOT"

echo "📁 ریشه ریپازیتوری: $REPO_ROOT"
echo "──────────────────────────────────────────────"

# ── ۱) ایجاد فایل Solution اصلی ──
dotnet new sln -n Sahand.ClinicPro

# ── ۲) پروژه‌های لایه‌های اصلی (پوشه src) ──

# لایه دامنه — بدون هیچ وابستگی خارجی (قانون معماری تمیز)
dotnet new classlib -n Sahand.ClinicPro.Domain \
  -o src/Sahand.ClinicPro.Domain -f net8.0

# لایه کاربرد — فقط به دامنه وابسته است
dotnet new classlib -n Sahand.ClinicPro.Application \
  -o src/Sahand.ClinicPro.Application -f net8.0

# لایه زیرساخت — پیاده‌سازی فنی (EF Core، سرویس‌ها، امنیت)
dotnet new classlib -n Sahand.ClinicPro.Infrastructure \
  -o src/Sahand.ClinicPro.Infrastructure -f net8.0

# مدل‌های مشترک بین کلاینت و سرور
dotnet new classlib -n Sahand.ClinicPro.Shared \
  -o src/Sahand.ClinicPro.Shared -f net8.0

# لایه API — کنترلرمحور (نه Minimal API چون ۲۴+ کنترلر خواهیم داشت)
dotnet new webapi -n Sahand.ClinicPro.API \
  -o src/Sahand.ClinicPro.API -f net8.0 --use-controllers

# پنل مدیریت لایسنس — Blazor Server با تعامل سراسری (بدون صفحات نمونه)
dotnet new blazor -n Sahand.ClinicPro.LicensePortal \
  -o src/Sahand.ClinicPro.LicensePortal -f net8.0 \
  --interactivity Server --all-interactive --empty

# ── ۳) پروژه‌های تست (پوشه tests) ──
dotnet new xunit -n Sahand.ClinicPro.Domain.UnitTests \
  -o tests/Sahand.ClinicPro.Domain.UnitTests -f net8.0

dotnet new xunit -n Sahand.ClinicPro.Application.UnitTests \
  -o tests/Sahand.ClinicPro.Application.UnitTests -f net8.0

dotnet new xunit -n Sahand.ClinicPro.Infrastructure.IntegrationTests \
  -o tests/Sahand.ClinicPro.Infrastructure.IntegrationTests -f net8.0

dotnet new xunit -n Sahand.ClinicPro.API.IntegrationTests \
  -o tests/Sahand.ClinicPro.API.IntegrationTests -f net8.0

# ── ۴) افزودن پروژه‌های قالبی به Solution ──
# (پروژه Desktop پس از ساخت دستی، جداگانه اضافه می‌شود)
dotnet sln add \
  "src/Sahand.ClinicPro.Domain/Sahand.ClinicPro.Domain.csproj" \
  "src/Sahand.ClinicPro.Application/Sahand.ClinicPro.Application.csproj" \
  "src/Sahand.ClinicPro.Infrastructure/Sahand.ClinicPro.Infrastructure.csproj" \
  "src/Sahand.ClinicPro.Shared/Sahand.ClinicPro.Shared.csproj" \
  "src/Sahand.ClinicPro.API/Sahand.ClinicPro.API.csproj" \
  "src/Sahand.ClinicPro.LicensePortal/Sahand.ClinicPro.LicensePortal.csproj" \
  "tests/Sahand.ClinicPro.Domain.UnitTests/Sahand.ClinicPro.Domain.UnitTests.csproj" \
  "tests/Sahand.ClinicPro.Application.UnitTests/Sahand.ClinicPro.Application.UnitTests.csproj" \
  "tests/Sahand.ClinicPro.Infrastructure.IntegrationTests/Sahand.ClinicPro.Infrastructure.IntegrationTests.csproj" \
  "tests/Sahand.ClinicPro.API.IntegrationTests/Sahand.ClinicPro.API.IntegrationTests.csproj"

echo "──────────────────────────────────────────────"
echo "✅ اسکلت قالبی ۱۰ پروژه ساخته شد. (پروژه Desktop دستی ساخته می‌شود)"
