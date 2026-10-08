@echo off
chcp 65001 >nul
title پاک‌سازی کش ویژوال استودیو — درمانگاه سهند (چرخه ۲۵)
echo ═══════════════════════════════════════════════════
echo   پاک‌سازی کامل کش بیلد و کش طراح XAML ویژوال استودیو
echo   رفع هشدارهای MC3072 / MC3074 — چرخه ۲۵
echo ═══════════════════════════════════════════════════
echo.
echo   این اسکریپت سه کار می‌کند:
echo     ۱) حذف پوشهٔ مخفی ‎.vs‎ (کش قدیمی طراح XAML)
echo     ۲) حذف پوشه‌های bin و obj همهٔ پروژه‌ها
echo     ۳) ساخت مجدد راه‌حل برای تأیید نهایی
echo.

REM ── پیش‌نیاز: ویژوال استودیو باید کاملاً بسته باشد ──
tasklist /fi "imagename eq devenv.exe" 2>nul | find /i "devenv.exe" >nul
if not errorlevel 1 (
    echo [خطا] ویژوال استودیو باز است — ابتدا آن را کاملاً ببندید و دوباره اجرا کنید
    pause & exit /b 1
)

cd /d "%~dp0"

echo [1/3] حذف پوشهٔ کش مخفی .vs ...
if exist ".vs" rmdir /s /q ".vs"

echo [2/3] حذف پوشه‌های bin و obj در همهٔ پروژه‌ها...
for /d /r "%~dp0src" %%D in (bin obj) do (
    if exist "%%D" rmdir /s /q "%%D"
)
for /d /r "%~dp0tests" %%D in (bin obj) do (
    if exist "%%D" rmdir /s /q "%%D"
)

echo [3/3] ساخت مجدد راه‌حل...
where dotnet >nul 2>&1
if errorlevel 1 (
    echo [خطا] دستور dotnet یافت نشد — .NET 8 SDK نصب نیست
    pause & exit /b 1
)
dotnet build Sahand.ClinicPro.sln -c Debug
if errorlevel 1 ( echo [خطا] بیلد ناموفق & pause & exit /b 1 )

echo.
echo ═══════════════════════════════════════════════════
echo   پاک‌سازی کامل شد.
echo   اکنون ویژوال استودیو را باز کنید و Rebuild بگیرید —
echo   هشدارهای MC3072/MC3074 دیگر نباید ظاهر شوند.
echo ═══════════════════════════════════════════════════
echo.
pause
