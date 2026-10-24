@echo off
chcp 65001 >nul
title نصب سرور درمانگاه سهند
setlocal

net session >nul 2>&1
if errorlevel 1 (
  echo برای نصب سرویس سرور، دسترسی مدیر سیستم لازم است.
  powershell -NoProfile -ExecutionPolicy Bypass -Command "Start-Process -FilePath '%~f0' -Verb RunAs"
  exit /b
)

powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0Install-Server-Package.ps1"
if errorlevel 1 (
  echo.
  echo نصب کامل نشد. پیام خطا و فایل‌های Logs در C:\SahandClinicPro\Server را بررسی کنید.
  pause
  exit /b 1
)
echo.
echo نصب سرور با موفقیت تمام شد.
pause
