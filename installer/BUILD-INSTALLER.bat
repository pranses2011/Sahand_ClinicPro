@echo off
chcp 65001 >nul
setlocal
title ساخت بسته نصب درمانگاه سهند
set "APP_VERSION=2.28.2"
if not "%CI%"=="" set "CI_MODE=1"

echo ===========================================================
echo   ساخت بسته نصب درمانگاه سهند v%APP_VERSION%
echo ===========================================================

where dotnet >nul 2>&1
if errorlevel 1 (
  echo [خطا] .NET 8 SDK یافت نشد.
  goto :fail
)

cd /d "%~dp0.."
if not exist dist mkdir dist

echo [1/6] انتشار خودبسنده کلاینت WPF برای Windows x64...
dotnet publish src\Sahand.ClinicPro.Desktop\Sahand.ClinicPro.Desktop.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=false -o publish\desktop
if errorlevel 1 goto :fail
if not exist publish\desktop\docs mkdir publish\desktop\docs
copy /y docs\guides\*.html publish\desktop\docs\ >nul
mkdir publish\desktop\assets 2>nul
copy /y docs\assets\ClinicPro.png publish\desktop\assets\ >nul
mkdir publish\desktop\assets\fonts 2>nul
copy /y docs\assets\fonts\* publish\desktop\assets\fonts\ >nul
mkdir publish\desktop\screenshots 2>nul
for %%F in (01-login.png 11-attendance-devices.png 14-license.png install-api-console.png install-firewall.png install-network-map.png install-sql-server.png) do copy /y docs\screenshots\%%F publish\desktop\screenshots\ >nul

echo [2/6] ساخت EXE نصب کلاینت...
set "ISCC=%ProgramFiles(x86)%\Inno Setup 6\ISCC.exe"
if not exist "%ISCC%" set "ISCC=%ProgramFiles%\Inno Setup 6\ISCC.exe"
if not exist "%ISCC%" (
  echo [خطا] Inno Setup 6 نصب نیست.
  goto :fail
)
"%ISCC%" /DMyAppVersion=%APP_VERSION% installer\SahandClinicPro.iss
if errorlevel 1 goto :fail

echo [3/6] انتشار خودبسنده API برای سرور Windows x64...
dotnet publish src\Sahand.ClinicPro.API\Sahand.ClinicPro.API.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=false -o publish\server
if errorlevel 1 goto :fail

echo [4/6] بسته‌بندی سرور و ابزار نصب خودکار...
if exist dist\SahandClinicPro-Server-v%APP_VERSION% rmdir /s /q dist\SahandClinicPro-Server-v%APP_VERSION%
mkdir dist\SahandClinicPro-Server-v%APP_VERSION%\Server
robocopy publish\server dist\SahandClinicPro-Server-v%APP_VERSION%\Server /E >nul
if errorlevel 8 goto :fail
copy /y installer\Install-Server-Package.bat dist\SahandClinicPro-Server-v%APP_VERSION%\ >nul
copy /y installer\Install-Server-Package.ps1 dist\SahandClinicPro-Server-v%APP_VERSION%\ >nul
powershell -NoProfile -Command "$guide = (Get-Content 'installer\RELEASE-INSTALL-GUIDE.txt' -Raw).Replace('{VERSION}', '%APP_VERSION%'); Set-Content 'dist\SahandClinicPro-Server-v%APP_VERSION%\RELEASE-INSTALL-GUIDE.txt' $guide -Encoding UTF8"
if errorlevel 1 goto :fail
powershell -NoProfile -Command "Compress-Archive -Path 'dist\SahandClinicPro-Server-v%APP_VERSION%\*' -DestinationPath 'dist\SahandClinicPro-Server-v%APP_VERSION%.zip' -Force"
if errorlevel 1 goto :fail

echo [5/6] انتشار وابسته به فریم‌ورک کلاینت و ساخت EXE به‌روزرسانی کم‌حجم...
dotnet publish src\Sahand.ClinicPro.Desktop\Sahand.ClinicPro.Desktop.csproj -c Release -r win-x64 --self-contained false -p:PublishSingleFile=false -o publish\desktop-update
if errorlevel 1 goto :fail
"%ISCC%" /DMyAppVersion=%APP_VERSION% installer\SahandClinicPro-Update.iss
if errorlevel 1 goto :fail

echo [6/6] انتشار وابسته به فریم‌ورک API و بسته‌بندی به‌روزرسانی کم‌حجم سرور...
dotnet publish src\Sahand.ClinicPro.API\Sahand.ClinicPro.API.csproj -c Release -r win-x64 --self-contained false -p:PublishSingleFile=false -o publish\server-update
if errorlevel 1 goto :fail
if exist dist\SahandClinicPro-ServerUpdate-v%APP_VERSION% rmdir /s /q dist\SahandClinicPro-ServerUpdate-v%APP_VERSION%
mkdir dist\SahandClinicPro-ServerUpdate-v%APP_VERSION%\Server
robocopy publish\server-update dist\SahandClinicPro-ServerUpdate-v%APP_VERSION%\Server /E >nul
if errorlevel 8 goto :fail
copy /y installer\Install-Server-Package.ps1 dist\SahandClinicPro-ServerUpdate-v%APP_VERSION%\ >nul
copy /y installer\Update-Server-Package.bat dist\SahandClinicPro-ServerUpdate-v%APP_VERSION%\ >nul
powershell -NoProfile -Command "Compress-Archive -Path 'dist\SahandClinicPro-ServerUpdate-v%APP_VERSION%\*' -DestinationPath 'dist\SahandClinicPro-Server-Update-v%APP_VERSION%.zip' -Force"
if errorlevel 1 goto :fail

echo.
echo ===========================================================
echo   کلاینت: dist\SahandClinicPro-Setup-v%APP_VERSION%.exe
echo   به‌روزرسانی کلاینت: dist\SahandClinicPro-Update-v%APP_VERSION%.exe
echo   سرور:   dist\SahandClinicPro-Server-v%APP_VERSION%.zip
echo   به‌روزرسانی سرور:   dist\SahandClinicPro-Server-Update-v%APP_VERSION%.zip
echo ===========================================================
if not defined CI_MODE pause
exit /b 0

:fail
echo.
echo [خطا] ساخت بسته ناموفق بود. پیام‌های بالا را بررسی کنید.
if not defined CI_MODE pause
exit /b 1
