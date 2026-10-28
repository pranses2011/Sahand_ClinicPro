param(
    [switch]$UpdateMode,
    [switch]$NoSqlSingleUserElevation,
    [switch]$NoPrompt,
    [string]$SqlAdminUser,
    [string]$SqlAdminPassword
)
# امکان نصب بیتعامل (اسکریپت/راه دور) با حساب مدیر SQL از متغیرهای محیطی
if (-not $SqlAdminUser -and $env:CLINICPRO_SQL_ADMIN_USER) { $SqlAdminUser = $env:CLINICPRO_SQL_ADMIN_USER }
if (-not $SqlAdminPassword -and $env:CLINICPRO_SQL_ADMIN_PASSWORD) { $SqlAdminPassword = $env:CLINICPRO_SQL_ADMIN_PASSWORD }
$ErrorActionPreference = 'Stop'
$serviceName = 'SahandClinicPro.API'
$target = 'C:\SahandClinicPro\Server'
$source = Join-Path $PSScriptRoot 'Server'

if (-not (Test-Path (Join-Path $source 'Sahand.ClinicPro.API.exe'))) {
    throw "فایل‌های سرور در پوشه '$source' پیدا نشد. ZIP کامل نسخه را استخراج کنید."
}

Write-Host 'نصب سرور درمانگاه سهند' -ForegroundColor Cyan

# نصب مجدد، تنظیمات و دیتابیس را دست‌نخورده نگه می‌دارد.
# ═══ چرخهٔ جاری: حساب سرویس نصب قبلی پیش از حذف ثبت میشود تا مجوز
# dbcreator دقیقاً به همان لاگینی داده شود که API با آن به SQL وصل میشود.
$existingServiceAccounts = @()
try {
    $existingSvc = Get-CimInstance Win32_Service -Filter "Name='$serviceName'" -ErrorAction SilentlyContinue
    if ($existingSvc -and $existingSvc.StartName) {
        $existingServiceAccounts = @([string]$existingSvc.StartName)
    }
} catch { }

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
# پیش‌شرط به‌روزرسانی کم‌حجم: Runtime نصب موجود باید سر جایش باشد؛ اگر نیست،
# این بسته به‌تنهایی کافی نیست و نصب کامل لازم است (پیام روشن به‌جای خرابی).
if ($UpdateMode) {
    $hostFxr = Join-Path $target 'hostfxr.dll'
    $coreClr = Join-Path $target 'coreclr.dll'
    if (-not (Test-Path $hostFxr) -or -not (Test-Path $coreClr)) {
        throw 'Runtime نصب سرور (hostfxr.dll/coreclr.dll) در C:\SahandClinicPro\Server پیدا نشد. این بسته «به‌روزرسانی کم‌حجم» است و به نصب کاملِ موجود نیاز دارد؛ لطفاً بستهٔ کامل سرور همین نسخه را اجرا کنید.'
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
    # ═══ قانون طلایی به‌روزرسانی کم‌حجم ═══
    # runtimeconfig.json و deps.json نصب خودبسنده هرگز بازنویسی نمی‌شوند؛
    # بازنویسی runtimeconfig با نسخهٔ وابسته به فریم‌ورک همان چیزی بود که در
    # آپدیت ۲.۲۸.۲ سرور را با خطای «No frameworks were found» از کار انداخت.
    # Runtime موجود (hostfxr/coreclr و همراهانش) میزبان فایل‌های تازه است.
    & robocopy $source $target /E /COPY:DAT /R:2 /W:2 /XF appsettings.json Sahand.ClinicPro.API.runtimeconfig.json Sahand.ClinicPro.API.deps.json /XD Logs /NFL /NDL /NJH /NJS

    # ترمیم نصب‌هایی که با آپدیت خراب ۲.۲۸.۲ آسیب دیده‌اند: اگر runtimeconfig
    # فعلی «وابسته به فریم‌ورک» شده باشد، نسخهٔ خودبسندهٔ حداقلی برگردانده
    # می‌شود تا API دوباره با Runtime کنار خودش بالا بیاید.
    $rcPath = Join-Path $target 'Sahand.ClinicPro.API.runtimeconfig.json'
    if (Test-Path $rcPath) {
        try {
            $rcText = Get-Content $rcPath -Raw
            if ($rcText -match '"framework"\s*:\s*\{' -and $rcText -match 'Microsoft\.NETCore\.App') {
                Copy-Item $rcPath "$rcPath.bak" -Force -ErrorAction SilentlyContinue
                $selfContainedRc = @'
{
  "runtimeOptions": {
    "tfm": "net8.0",
    "rollForward": "LatestMinor",
    "configProperties": {
      "System.Reflection.Metadata.MetadataUpdater.IsSupported": false,
      "System.Runtime.Serialization.EnableUnsafeBinaryFormatterSerialization": false
    }
  }
}
'@
                Set-Content -Path $rcPath -Value $selfContainedRc -Encoding UTF8
                Write-Host 'runtimeconfig خراب‌شدهٔ آپدیت قبلی ترمیم شد (نسخهٔ پشتیبان: runtimeconfig.json.bak)' -ForegroundColor Yellow
            }
        } catch { }
    }
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

# ═══════════════════════════════════════════════════════════════════════════
#  اعطای خودکار نقش سروری dbcreator — «بازیابی اطلاعات از فایل پشتیبان»
# ═══════════════════════════════════════════════════════════════════════════
#  برای اعطای نقش از اتصال API، حساب ویندوزی یا حساب مدیر SQL استفاده می‌شود.
#  در صورت نداشتن مجوز مدیریتی SQL، وضعیت ناموفق گزارش می‌شود.
$restoreModule = Join-Path $PSScriptRoot 'Enable-RestorePermission.ps1'
if (-not (Test-Path $restoreModule)) {
    throw 'فایل Enable-RestorePermission.ps1 کنار نصاب پیدا نشد. بستهٔ کامل سرور همنسخه را دوباره استخراج کنید.'
}
# پارامترهای کتابخانه هنگام dot-source در همین scope مقداردهی می‌شوند.
$savedAdminUser = $SqlAdminUser
$savedAdminPassword = $SqlAdminPassword
$savedNoPrompt = $NoPrompt
. $restoreModule -AsLibrary
$SqlAdminUser = $savedAdminUser
$SqlAdminPassword = $savedAdminPassword
$NoPrompt = $savedNoPrompt

$promptAllowed = -not $NoPrompt
$elevationAllowed = -not $NoSqlSingleUserElevation
$restoreResult = $null
try {
    $restoreResult = Invoke-ClinicProRestorePermission `
        -ConnectionString ([string]$settings.ConnectionStrings.DefaultConnection) `
        -ReportDir $target -AdminUser $SqlAdminUser -AdminPassword $SqlAdminPassword `
        -ExtraTargets $existingServiceAccounts -AllowPrompt:$promptAllowed `
        -AllowSingleUserElevation:$elevationAllowed
} catch {
    Write-Host "هشدار: پردازش خودکار مجوز بازیابی (dbcreator) با خطا روبهرو شد: $($_.Exception.Message)" -ForegroundColor Yellow
    Write-Host 'میتوانید بعداً فایل Enable-RestorePermission.bat را کنار سرور با Run as administrator اجرا کنید.' -ForegroundColor Yellow
}
$restoreGranted = $false
$restoreTargets = @()
$primaryRestoreTarget = ''
$lastGrantError = ''
if ($restoreResult) {
    $restoreGranted = [bool]$restoreResult.Granted
    $restoreTargets = @($restoreResult.Targets)
    $primaryRestoreTarget = [string]$restoreResult.Primary
    $lastGrantError = [string]$restoreResult.LastError
}

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
               "رشته اتصال یا مجوز CREATE DATABASE نیست؛ آخرین بستهٔ سرور همین نسخه را نصب کنید.")
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
# ═══ معیار موفقیت نصب: زنده‌بودن API (/health/live) ═══
# پیش‌تر موفقیت فقط با Healthy شدن /health/ready سنجیده می‌شد؛ اما آن نقطه
# فضای دیسک پشتیبان‌گیری و Hangfire را هم می‌سنجد و روی ماشینی که API کاملاً
# سالم بالا آمده بود (لاگ‌ها و تپ‌قلب موفق) نصب «ناموفق» گزارش شد. اکنون:
# زنده‌بودن فرآیند = نصب موفق؛ نتیجهٔ readiness فقط برای هشدار/گزارش خوانده
# می‌شود و هرگز نصب سالم را شکست نمی‌دهد.
$deadline = (Get-Date).AddSeconds(300)
$alive = $false
$readyNote = ''
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
        $live = Invoke-RestMethod -Uri 'http://localhost:5000/health/live' -TimeoutSec 3
        if ($live.status) { $alive = $true }
    } catch { }
    if ($alive) {
        try {
            $ready = Invoke-RestMethod -Uri 'http://localhost:5000/health/ready' -TimeoutSec 3
            $readyNote = "readiness: $($ready.status)"
            if ($ready.status -ne 'Healthy') {
                Write-Host "توجه: API بالا است اما آمادگی کامل گزارش نشد ($($ready.status)) — جزئیات در install-diagnostics.txt" -ForegroundColor Yellow
            }
        } catch {
            $body = ''
            try {
                $resp = $_.Exception.Response
                if ($resp) {
                    $sr = New-Object System.IO.StreamReader($resp.GetResponseStream())
                    $body = $sr.ReadToEnd()
                }
            } catch { }
            $readyNote = "readiness پاسخ کامل نداد: $($_.Exception.Message) $body"
            Write-Host 'توجه: API بالا است اما نقطهٔ آمادگی پاسخ کامل نداد — نصب ادامه پیدا می‌کند؛ جزئیات در install-diagnostics.txt' -ForegroundColor Yellow
        }
    }
    if (-not $alive -and ($waited % 30) -eq 0) {
        Write-Host "در انتظار آماده‌شدن API… ($waited ثانیه)" -ForegroundColor DarkGray
    }
} while (-not $alive -and (Get-Date) -lt $deadline)
if (-not $alive) {
    $diag = New-Object System.Collections.Generic.List[string]
    $diag.Add("گزارش تشخیصی نصب سرور — $(Get-Date -Format 'yyyy/MM/dd HH:mm:ss')")
    $diag.Add($readyNote)
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
    throw "سرویس نصب شد اما API در مهلت انتظار (۵ دقیقه) پاسخ نداد. گزارش تشخیصی در $diagPath ذخیره شد — همان فایل را برای پشتیبانی بفرستید."
}
# گزارش آمادگی (حتی در نصب موفق) برای پشتیبانی نگه داشته می‌شود.
try {
    $diagOk = @("نصب سرور موفق — $(Get-Date -Format 'yyyy/MM/dd HH:mm:ss')", $readyNote)
    Set-Content -Path (Join-Path $target 'install-diagnostics.txt') -Value $diagOk -Encoding UTF8
} catch { }

# ثبت نسخهٔ نصب‌شده تا اجرای بعدی نصب‌کننده نسخهٔ فعلی را بشناسد و انتخاب
# «نصب کامل / فقط فایل‌های به‌روزشده» را پیشنهاد بدهد.
try {
    $newVersion = (Get-Item (Join-Path $target 'Sahand.ClinicPro.API.exe')).VersionInfo.ProductVersion
    if ($newVersion) { Set-Content -Path (Join-Path $target 'version.txt') -Value $newVersion -Encoding ASCII }
} catch { }
# ═══ تأیید نهایی مجوز بازیابی پس از نصب و بالا آمدن سرویس ═══
# اگر SQL Server هنگام نصب در حال راهاندازی بوده یا حساب سرویس تازه ساخته
# شده باشد، این بار دوم همان اعطا را دوباره امتحان میکند؛ نتیجهٔ نهایی
# در فایل وضعیت نوشته میشود.
if (-not $restoreGranted -and $restoreResult -and $restoreResult.Attempts.Count -gt 0 -and $restoreTargets.Count -gt 0) {
    try {
        foreach ($attempt in $restoreResult.Attempts) {
            try {
                if (Invoke-CpRestoreGrant -ConnectionString $attempt.ConnectionString `
                        -Targets $restoreTargets -Primary $primaryRestoreTarget `
                        -Sql $script:CpGrantSql -Report $null) {
                    $restoreGranted = $true
                    Write-Host 'مجوز بازیابی پشتیبان (dbcreator) در تأیید نهایی پس از نصب سرویس برقرار شد.' -ForegroundColor Green
                    break
                }
            } catch { }
        }
        $permReport = @(
            "وضعیت مجوز بازیابی (dbcreator) — تأیید نهایی — $(Get-Date -Format 'yyyy/MM/dd HH:mm:ss')",
            "نتیجه: $(if ($restoreGranted) { 'اعطای خودکار موفق' } else { 'نیازمند اجرای دستی Grant-RestorePermission.sql' })",
            "لاگین هدف اصلی: $primaryRestoreTarget",
            "همهٔ لاگینهای هدف: $($restoreTargets -join '، ')",
            "آخرین خطا: $lastGrantError"
        )
        Set-Content -Path (Join-Path $target 'restore-permission.status.txt') -Value $permReport -Encoding UTF8
    } catch { }
}

Write-Host "سرور آماده است: http://localhost:5000/health/ready" -ForegroundColor Green
Write-Host 'برای کلاینت‌های دیگر، نشانی سرور را در appsettings.json کلاینت به‌صورت http://<IP-سرور>:5000/api/v1 وارد کنید.'
