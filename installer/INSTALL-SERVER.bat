@echo off
chcp 65001 >nul
setlocal
title ساخت و نصب سرور درمانگاه سهند
set "APP_VERSION=2.22.6"

where dotnet >nul 2>&1
if errorlevel 1 (
  echo [خطا] برای ساخت سرور از سورس، .NET 8 SDK لازم است.
  pause
  exit /b 1
)

cd /d "%~dp0.."
echo انتشار خودبسنده API برای Windows x64...
dotnet publish src\Sahand.ClinicPro.API\Sahand.ClinicPro.API.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=false -o installer\Server
if errorlevel 1 ( echo [خطا] انتشار سرور ناموفق بود & pause & exit /b 1 )

call installer\Install-Server-Package.bat
exit /b %errorlevel%
