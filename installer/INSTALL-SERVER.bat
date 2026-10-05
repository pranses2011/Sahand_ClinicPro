@echo off
chcp 65001 >nul
title نصب سرور درمانگاه سهند — سرویس ویندوز
echo ═══════════════════════════════════════════════════
echo   نصب سرور API درمانگاه سهند v1.5.0
echo ═══════════════════════════════════════════════════
echo.

where dotnet >nul 2>&1
if errorlevel 1 ( echo [خطا] .NET 8 SDK/HOST یافت نشد & pause & exit /b 1 )

echo [1/4] انتشار سرور API...
cd /d "%~dp0.."
dotnet publish src\Sahand.ClinicPro.API\Sahand.ClinicPro.API.csproj ^
    -c Release -o publish\server
if errorlevel 1 ( echo [خطا] انتشار ناموفق & pause & exit /b 1 )

echo [2/4] کپی به C:\SahandClinicPro\Server...
robocopy publish\server C:\SahandClinicPro\Server /MIR >nul
if errorlevel 8 ( echo [خطا] کپی ناموفق & pause & exit /b 1 )

echo [3/4] ساخت دیتابیس (EF migrations)...
pushd C:\SahandClinicPro\Server
dotnet Sahand.ClinicPro.API.dll ef database update --assembly Sahand.ClinicPro.Infrastructure.dll
if errorlevel 1 ( echo [خطا] مهاجرت ناموفق — رشته اتصال appsettings.json را بررسی کنید & popd & pause & exit /b 1 )
popd

echo [4/4] ثبت سرویس ویندوز...
sc query SahandClinicPro.API >nul 2>&1
if not errorlevel 1 (
    echo سرویس از قبل موجود است — فقط شروع مجدد...
    net stop SahandClinicPro.API >nul 2>&1
    sc delete SahandClinicPro.API >nul 2>&1
)
sc create SahandClinicPro.API binPath= "dotnet C:\SahandClinicPro\Server\Sahand.ClinicPro.API.dll" start= auto obj= LocalSystem
sc description SahandClinicPro.API "سامانه جامع مدیریت درمانگاه سهند — سرور API"
net start SahandClinicPro.API

echo.
echo ═══════════════════════════════════════════════════
echo   سرور نصب و راه‌اندازی شد
echo   سلامت:  http://localhost:5000/health
echo   Swagger: http://localhost:5000/swagger
echo   ⚠ appsettings.json در C:\SahandClinicPro\Server را بازبینی کنید
echo ═══════════════════════════════════════════════════
pause
