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
& robocopy $source $target /MIR /COPY:DAT /R:2 /W:2 /XF appsettings.json /XD Logs /NFL /NDL /NJH /NJS
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
    if ($LASTEXITCODE -ne 0) { throw "اعمال خودکار ساختار دیتابیس شکست خورد (کد $LASTEXITCODE). رشته اتصال و مجوز CREATE DATABASE را بررسی کنید." }
}
finally { Pop-Location }

# نصب به‌عنوان Windows Service؛ API از UseWindowsService پشتیبانی می‌کند.
$binaryPath = '"' + $apiExe + '"'
New-Service -Name $serviceName -BinaryPathName $binaryPath -DisplayName 'Sahand ClinicPro API' -Description 'سرور سامانه جامع مدیریت درمانگاه سهند' -StartupType Automatic | Out-Null
sc.exe description $serviceName 'سرور سامانه جامع مدیریت درمانگاه سهند' | Out-Null

# بازکردن پورت API برای اتصال رایانه‌های کلاینت در شبکه درمانگاه.
$ruleName = 'Sahand ClinicPro API TCP 5000'
if (-not (Get-NetFirewallRule -DisplayName $ruleName -ErrorAction SilentlyContinue)) {
    New-NetFirewallRule -DisplayName $ruleName -Direction Inbound -Protocol TCP -LocalPort 5000 -Action Allow | Out-Null
}

Start-Service -Name $serviceName
$deadline = (Get-Date).AddSeconds(120)
$healthy = $false
do {
    Start-Sleep -Seconds 3
    try {
        $response = Invoke-RestMethod -Uri 'http://localhost:5000/health' -TimeoutSec 3
        $healthy = $response.status -eq 'Healthy'
    } catch { }
} while (-not $healthy -and (Get-Date) -lt $deadline)
if (-not $healthy) {
    throw 'سرویس نصب شد اما API در مهلت انتظار آماده نشد. Windows Event Viewer و C:\SahandClinicPro\Server\Logs را بررسی کنید.'
}

Write-Host "سرور آماده است: http://localhost:5000/health" -ForegroundColor Green
Write-Host 'برای کلاینت‌های دیگر، نشانی سرور را در appsettings.json کلاینت به‌صورت http://<IP-سرور>:5000/api/v1 وارد کنید.'
