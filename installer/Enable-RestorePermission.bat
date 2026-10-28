@echo off
chcp 65001 >nul
title برقراری خودکار مجوز بازیابی پشتیبان — سهند کلینیکپرو
setlocal

net session >nul 2>&1
if errorlevel 1 (
  echo برای این کار دسترسی مدیر سیستم لازم است. در حال درخواست دسترسی...
  powershell -NoProfile -ExecutionPolicy Bypass -Command "Start-Process -FilePath '%~f0' -Verb RunAs"
  exit /b
)

echo.
echo اين ابزار نقش dbcreator را به لاگين سرور API ميدهد تا بازيابي از فايل پشتيبان
echo بدون دخالت دستي در SQL Server انجام شود. در صورت نياز SQL Server محلي براي
echo چند ثانيه رياستارت موقت ميشود و سپس به حالت عادي برميگردد.
echo.

powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0Enable-RestorePermission.ps1" %*
set code=%errorlevel%
echo.
if "%code%"=="0" (
  echo عمليات با موفقيت انجام شد. سرويس SahandClinicPro.API را يکبار رياستارت کنيد.
) else (
  echo نتيجه در فایل C:\SahandClinicPro\Server\restore-permission.status.txt ثبت شد.
)
echo.
pause
exit /b %code%
