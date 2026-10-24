param([switch]$UpdateMode)
$ErrorActionPreference = 'Stop'
$serviceName = 'SahandClinicPro.API'
$target = 'C:\SahandClinicPro\Server'
$source = Join-Path $PSScriptRoot 'Server'

if (-not (Test-Path (Join-Path $source 'Sahand.ClinicPro.API.exe'))) {
    throw "فایل‌های سرور در پوشه '$source' پیدا نشد. ZIP کامل نسخه را استخراج کنید."
}

Write-Host 'نصب سرور درمانگاه سهند' -ForegroundColor Cyan

# نصب مجدد، تنظیمات و دیتابیس را دست‌نخورده نگه می‌دارد.
if (Get-Service -Name $serviceName -ErrorAction SilentlyContinue) {
    Stop-Service -Name $serviceName -Force -ErrorAction SilentlyContinue
    $svc = Get-Service -Name $serviceName
    $svc.WaitForStatus('Stopped', [TimeSpan]::FromSeconds(30))
    sc.exe delete $serviceName | Out-Null
    Start-Sleep -Seconds 2
}
New-Item -ItemType Directory -Path $target -Force | Out-Null
$settingsPath = Join-Path $target 'appsettings.json'
$hadSettings = Test-Path $settingsPath

# به‌روزرسانی فایل‌های برنامه، بدون حذف تنظیمات/لاگ‌های موجود.
# در حالت به‌روزرسانی (بستهٔ کم‌حجمِ وابسته به فریم‌ورک) نباید آینه‌ای کپی
# شود: فایل‌های Runtime نصب کامل در مقصد می‌مانند و فقط فایل‌های برنامه
# جایگزین می‌شوند؛ در نصب کامل همان رفتار آینه‌ای پیشین برقرار است.
# تشخیص نصب قبلی و انتخاب نوع نصب (درخواست مالک): اگر سرور از قبل نصب است،
# کاربر انتخاب می‌کند «نصب کامل» شود یا «فقط فایل‌های به‌روزرسانی‌شده از نسخهٔ
# نصب‌شده به بعد» کپی شوند. نسخهٔ نصب‌شده از version.txt (که هر نصب می‌نویسد)
# یا از نسخهٔ فایل exe خوانده می‌شود. robocopy در حالت /E فقط فایل‌های
# تغییرکرده/جدید را کپی می‌کند — یعنی دقیقاً همان فایل‌های به‌روزشده.
$installedExe = Join-Path $target 'Sahand.ClinicPro.API.exe'
$installedVersion = $null
if (Test-Path $installedExe) {
    $vf = Join-Path $target 'version.txt'
    if (Test-Path $vf) { $installedVersion = (Get-Content $vf -Raw).Trim() }
    if (-not $installedVersion) {
        try { $installedVersion = (Get-Item $installedExe).VersionInfo.ProductVersion } catch { }
    }
}
$effectiveUpdate = [bool]$UpdateMode
if (-not $UpdateMode -and $installedVersion) {
    Write-Host "نسخهٔ $installedVersion از سرور روی این رایانه نصب است." -ForegroundColor Yellow
    Write-Host '  [1] نصب کامل — همهٔ فایل‌ها از نو کپی می‌شوند (پیش‌فرض Enter)'
    Write-Host "  [2] فقط فایل‌های به‌روزرسانی‌شده از نسخهٔ $installedVersion به بعد — سریع‌تر"
    $choice = Read-Host 'انتخاب شما (1 یا 2)'
    if ($choice -eq '2') { $effectiveUpdate = $true }
}
if ($effectiveUpdate) {
    & robocopy $source $target /E /COPY:DAT /R:2 /W:2 /XF appsettings.json /XD Logs /NFL /NDL /NJH /NJS
} else {
    & robocopy $source $target /MIR /COPY:DAT /R:2 /W:2 /XF appsettings.json /XD Logs /NFL /NDL /NJH /NJS
}
if ($LASTEXITCODE -ge 8) { throw "کپی فایل‌های سرور ناموفق بود (robocopy $LASTEXITCODE)." }

if (-not $hadSettings) {
    Copy-Item -Path (Join-Path $source 'appsettings.json') -Destination $settingsPath -Force
}
if (-not (Test-Path $settingsPath)) {
    throw 'appsettings.json منتشرشده پیدا نشد.'
}
$settings = Get-Content -Raw -Path $settingsPath | ConvertFrom-Json
if (-not $settings.ConnectionStrings) { $settings | Add-Member -NotePropertyName ConnectionStrings -NotePropertyValue ([pscustomobject]@{}) }
if (-not $settings.Jwt) { $settings | Add-Member -NotePropertyName Jwt -NotePropertyValue ([pscustomobject]@{}) }
if (-not $settings.Database) { $settings | Add-Member -NotePropertyName Database -NotePropertyValue ([pscustomobject]@{}) }

$currentConnection = [string]$settings.ConnectionStrings.DefaultConnection
$prompt = if ($hadSettings) { 'رشته اتصال SQL را وارد کنید (Enter = حفظ مقدار قبلی)' } else { 'رشته اتصال SQL Server را وارد کنید (مثال: Server=localhost\SQLEXPRESS;Database=SahandClinicProDb;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true)' }
$connection = Read-Host $prompt
if ($connection) { $settings.ConnectionStrings.DefaultConnection = $connection }
if (-not $hadSettings -and -not $connection) {
    throw 'برای نصب نخست باید رشته اتصال SQL را وارد کنید.'
}
if (-not $settings.ConnectionStrings.DefaultConnection) {
    throw 'رشته اتصال SQL خالی است؛ نصب متوقف شد تا دیتابیس به‌درستی ساخته شود.'
}

# کلید قوی و تصادفی فقط در اولین نصب ساخته می‌شود؛ در به‌روزرسانی حفظ می‌شود.
if (-not $settings.Jwt.SecretKey -or ([string]$settings.Jwt.SecretKey).Length -lt 64) {
    $bytes = New-Object byte[] 64
    $rng = [System.Security.Cryptography.RandomNumberGenerator]::Create()
    $rng.GetBytes($bytes)
    $rng.Dispose()
    $settings.Jwt.SecretKey = [Convert]::ToBase64String($bytes)
}
if ($settings.Database.PSObject.Properties.Name -notcontains 'AutoMigrateOnStartup') {
    $settings.Database | Add-Member -NotePropertyName AutoMigrateOnStartup -NotePropertyValue $false
} else {
    $settings.Database.AutoMigrateOnStartup = $false
}
if ($settings.PSObject.Properties.Name -notcontains 'Urls') {
    $settings | Add-Member -NotePropertyName Urls -NotePropertyValue 'http://0.0.0.0:5000'
} else {
    $settings.Urls = 'http://0.0.0.0:5000'
}
$settings | ConvertTo-Json -Depth 100 | Set-Content -Path $settingsPath -Encoding UTF8

# این مرحله همان تمام تغییرهای schema را خودکار اعمال می‌کند؛ نیازی به نصب SDK،
# ابزار dotnet-ef یا اجرای دستور دستی توسط درمانگاه نیست.
Write-Host 'ساخت دیتابیس و اعمال خودکار تغییرهای لازم (اولین اجرا ممکن است چند دقیقه طول بکشد)...' -ForegroundColor Yellow
$apiExe = Join-Path $target 'Sahand.ClinicPro.API.exe'
Push-Location $target
try {
    & $apiExe --migrate-database=true
    if ($LASTEXITCODE -ne 0) {
        # پیام خطای واقعی (SQL Server / مهاجرت) درست بالای همین خط در کنسول چاپ شده است؛
        # پیام قبلی فقط «رشته اتصال و مجوز CREATE DATABASE» را متهم می‌کرد و برای خطاهای
        # مهاجرت (مثل 15233 «MS_Description already exists» یا 207 «Invalid column name»)
        # گمراه‌کننده بود — گزارش مالک هنگام نصب v2.22.5.
        throw ("اعمال خودکار ساختار دیتابیس شکست خورد (کد $LASTEXITCODE). " +
               "علت دقیق در خطاهای بالا و فایل‌های C:\SahandClinicPro\Server\Logs آمده است. " +
               "اگر خطا درباره «MS_Description»، «Invalid column name» یا «migration» بود، مشکل از " +
               "رشته اتصال یا مجوز CREATE DATABASE نیست؛ بستهٔ سرور v2.23.0 یا بالاتر را نصب کنید.")
    }
}
finally { Pop-Location }

# نصب به‌عنوان Windows Service؛ API از UseWindowsService پشتیبانی می‌کند.
$binaryPath = '"' + $apiExe + '"'
# شمارهٔ نسخه در توضیحات سرویس ویندوز — تا در صفحهٔ Services معلوم باشد
# سرویسِ در حال کار کدام نسخه است (گزارش مالک). نسخه از خود فایل منتشرشدهٔ
# API خوانده می‌شود تا هیچ‌وقت با نسخهٔ واقعیِ نصب‌شده ناهماهنگ نشود.
$apiVersion = $null
try {
    $apiDll = Join-Path $target 'Sahand.ClinicPro.API.dll'
    if (Test-Path $apiDll) { $apiVersion = (Get-Item $apiDll).VersionInfo.ProductVersion }
    if (-not $apiVersion) { $apiVersion = (Get-Item $apiExe).VersionInfo.ProductVersion }
} catch { }
if ($apiVersion) { $apiVersion = ($apiVersion -split '\+')[0].Trim() }
if (-not $apiVersion) { $apiVersion = 'نامشخص' }
$serviceDescription = "سرور سامانه جامع مدیریت درمانگاه سهند — نسخه $apiVersion"
Write-Host "نسخهٔ سرویس API: $apiVersion" -ForegroundColor Cyan
New-Service -Name $serviceName -BinaryPathName $binaryPath -DisplayName 'Sahand ClinicPro API' -Description $serviceDescription -StartupType Automatic | Out-Null
sc.exe description $serviceName $serviceDescription | Out-Null

# بازکردن پورت API برای اتصال رایانه‌های کلاینت در شبکه درمانگاه.
$ruleName = 'Sahand ClinicPro API TCP 5000'
if (-not (Get-NetFirewallRule -DisplayName $ruleName -ErrorAction SilentlyContinue)) {
    New-NetFirewallRule -DisplayName $ruleName -Direction Inbound -Protocol TCP -LocalPort 5000 -Action Allow | Out-Null
}

# انتظار آمادگی API — در نصب نخست، ساخت دیتابیس و مهاجرت‌ها روی SQL Server
# ممکن است چند دقیقه طول بکشد؛ مهلت پیشین (۱۲۰ ثانیه) روی بسیاری سیستم‌ها
# کم بود و نصب در آخرین گام خطا می‌داد. اکنون تا ۵ دقیقه با نمایش پیشرفت
# صبر می‌شود؛ اگر سرویس وسط کار متوقف شده باشد یک بار دیگر روشن می‌شود و
# در پایانِ ناموفق، به‌جای پیام کلی، گزارش تشخیصی واقعی (وضعیت سرویس +
# خطاهای Event Viewer + انتهای فایل‌های لاگ) در فایل گزارش نوشته می‌شود.
Start-Service -Name $serviceName
$deadline = (Get-Date).AddSeconds(300)
$healthy = $false
$restartTried = $false
$waited = 0
do {
    Start-Sleep -Seconds 3
    $waited += 3
    try {
        $svcNow = Get-Service -Name $serviceName -ErrorAction Stop
        if ($svcNow.Status -eq 'Stopped' -and -not $restartTried) {
            $restartTried = $true
            Write-Host 'سرویس در حین راه‌اندازی متوقف شده بود — تلاش دوباره…' -ForegroundColor Yellow
            Start-Service -Name $serviceName -ErrorAction SilentlyContinue
        }
    } catch { }
    try {
        $response = Invoke-RestMethod -Uri 'http://localhost:5000/health/ready' -TimeoutSec 3
        $healthy = $response.status -eq 'Healthy'
    } catch { }
    if (-not $healthy -and ($waited % 30) -eq 0) {
        Write-Host "در انتظار آماده‌شدن API… ($waited ثانیه)" -ForegroundColor DarkGray
    }
} while (-not $healthy -and (Get-Date) -lt $deadline)
if (-not $healthy) {
    $diag = New-Object System.Collections.Generic.List[string]
    $diag.Add("گزارش تشخیصی نصب سرور — $(Get-Date -Format 'yyyy/MM/dd HH:mm:ss')")
    try {
        $svcEnd = Get-Service -Name $serviceName -ErrorAction Stop
        $diag.Add("وضعیت سرویس: $($svcEnd.Status)")
    } catch { $diag.Add("وضعیت سرویس: یافت نشد ($($_.Exception.Message))") }
    $diag.Add('')
    $diag.Add('— آخرین خطاهای Event Viewer (برنامه‌ها) —')
    try {
        $events = Get-WinEvent -FilterHashtable @{ LogName = 'Application'; Level = 1, 2; StartTime = (Get-Date).AddMinutes(-30) } -MaxEvents 15 -ErrorAction Stop
        foreach ($ev in $events) { $diag.Add("[$($ev.TimeCreated)] $($ev.ProviderName): $($ev.Message)"); $diag.Add('') }
    } catch { $diag.Add("خواندن Event Viewer ناموفق بود: $($_.Exception.Message)") }
    $diag.Add('')
    $diag.Add('— انتهای فایل‌های لاگ سرور —')
    try {
        $logDir = Join-Path $target 'Logs'
        $logFiles = @(Get-ChildItem -Path $logDir -Filter '*.log' -ErrorAction Stop |
            Sort-Object LastWriteTime -Descending | Select-Object -First 3)
        foreach ($lf in $logFiles) {
            $diag.Add("### $($lf.Name)")
            foreach ($line in @(Get-Content -Path $lf.FullName -Tail 80 -ErrorAction SilentlyContinue)) { $diag.Add([string]$line) }
            $diag.Add('')
        }
        if ($logFiles.Count -eq 0) { $diag.Add('فایل لاگی در پوشهٔ Logs یافت نشد.') }
    } catch { $diag.Add("خواندن لاگ‌ها ناموفق بود: $($_.Exception.Message)") }
    $diagPath = Join-Path $target 'install-diagnostics.txt'
    try { Set-Content -Path $diagPath -Value $diag -Encoding UTF8 } catch { }
    $diag | ForEach-Object { Write-Host $_ -ForegroundColor DarkYellow }
    throw "سرویس نصب شد اما API در مهلت انتظار (۵ دقیقه) آماده نشد. گزارش تشخیصی در $diagPath ذخیره شد — همان فایل را برای پشتیبانی بفرستید."
}

# ثبت نسخهٔ نصب‌شده تا اجرای بعدی نصب‌کننده نسخهٔ فعلی را بشناسد و انتخاب
# «نصب کامل / فقط فایل‌های به‌روزشده» را پیشنهاد بدهد.
try {
    $newVersion = (Get-Item (Join-Path $target 'Sahand.ClinicPro.API.exe')).VersionInfo.ProductVersion
    if ($newVersion) { Set-Content -Path (Join-Path $target 'version.txt') -Value $newVersion -Encoding ASCII }
} catch { }
Write-Host "سرور آماده است: http://localhost:5000/health/ready" -ForegroundColor Green
Write-Host 'برای کلاینت‌های دیگر، نشانی سرور را در appsettings.json کلاینت به‌صورت http://<IP-سرور>:5000/api/v1 وارد کنید.'
