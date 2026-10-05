@echo off
chcp 65001 >nul
title ساخت نصب‌کننده درمانگاه سهند v1.6.0
echo ═══════════════════════════════════════════════════
echo   ساخت نصب‌کننده کلاینت — درمانگاه سهند v1.6.0
echo ═══════════════════════════════════════════════════
echo.

REM ── پیش‌نیاز: .NET 8 SDK روی همین رایانه ──
where dotnet >nul 2>&1
if errorlevel 1 (
    echo [خطا] .NET 8 SDK یافت نشد — از dotnet.microsoft.com نصب کنید
    pause & exit /b 1
)

echo [1/3] انتشار کلاینت WPF...
cd /d "%~dp0.."
dotnet publish src\Sahand.ClinicPro.Desktop\Sahand.ClinicPro.Desktop.csproj ^
    -c Release -r win-x64 --self-contained false ^
    -o publish\desktop
if errorlevel 1 ( echo [خطا] انتشار ناموفق & pause & exit /b 1 )

echo [2/3] کپی مستندات داخل نصب‌کننده...
if not exist publish\desktop\docs mkdir publish\desktop\docs
copy /y docs\guides\*.html publish\desktop\docs\ >nul 2>&1

echo [3/3] ساخت EXE نصب‌کننده با Inno Setup...
set "ISCC=%ProgramFiles(x86)%\Inno Setup 6\ISCC.exe"
if not exist "%ISCC%" set "ISCC=%ProgramFiles%\Inno Setup 6\ISCC.exe"
if exist "%ISCC%" (
    "%ISCC%" installer\SahandClinicPro.iss
    if errorlevel 1 ( echo [خطا] کامپایل Inno Setup ناموفق & pause & exit /b 1 )
    echo.
    echo ═══════════════════════════════════════════════════
    echo   نصب‌کننده آماده: dist\SahandClinicPro-Setup-v1.6.0.exe
    echo ═══════════════════════════════════════════════════
) else (
    echo [توجه] Inno Setup 6 یافت نشد — فایل‌های منتشرشده در publish\desktop آماده‌اند
    echo         برای EXE نهایی، Inno Setup 6 را از jrsoftware.org/isdl نصب کنید و دوباره اجرا کنید
)
echo.
pause
