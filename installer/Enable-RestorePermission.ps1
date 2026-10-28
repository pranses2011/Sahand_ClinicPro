param(
    [string]$TargetDir = 'C:\SahandClinicPro\Server',
    [string]$ConnectionString = '',
    [string]$SqlAdminUser = '',
    [string]$SqlAdminPassword = '',
    [switch]$NoSqlSingleUserElevation,
    [switch]$NoPrompt,
    [switch]$AsLibrary
)

$ErrorActionPreference = 'Stop'

# ═══════════════════════════════════════════════════════════════════════════
#  سهند کلینیکپرو — برقراری خودکار مجوز «بازیابی اطلاعات از فایل پشتیبان»
# ═══════════════════════════════════════════════════════════════════════════
#  RESTORE DATABASE مجوز «سطح سرور» میخواهد (نقش dbcreator یا sysadmin)؛
#  db_owner بودن روی خود دیتابیس کافی نیست. نسخههای پیشین نصاب فقط با
#  Integrated Security به master وصل میشدند و اگر حساب ویندوزی نصاب روی SQL
#  مدیر نبود، اعطای نقش بیصدا رد میشد — همان «من نتونستم نقش کاربر رو روی
#  SQL اعمال بکنم» که مالک گزارش کرد.
#
#  نصب با حساب اتصال API، حساب ویندوزی جاری یا حساب مدیر SQL تلاش می‌کند.
#  اعطای نقش نیازمند مجوز مدیریتی SQL است؛ بدون آن موفقیت گزارش نمی‌شود.
#  سرور SQL به طور خودکار متوقف یا در حالت تک‌کاربره اجرا نمی‌شود.
#  نتیجه در restore-permission.status.txt ثبت می‌شود.
#  فایل هم بهصورت مستقل (دابلکلیک Enable-RestorePermission.bat) و هم بهصورت
#  dot-source از Install-Server-Package.ps1 قابل اجراست.
# ═══════════════════════════════════════════════════════════════════════════

$script:CpGrantSql = @'
SET NOCOUNT ON;
DECLARE @p nvarchar(128) = @Principal;
DECLARE @cmd nvarchar(max);
DECLARE @msg nvarchar(4000) = N'';
IF ISNULL(IS_SRVROLEMEMBER(N'sysadmin', @p), 0) = 1
BEGIN
    SELECT CAST(1 AS int) AS Granted, CAST(N'عضو sysadmin' AS nvarchar(4000)) AS Reason;
    RETURN;
END
IF NOT EXISTS (SELECT 1 FROM sys.server_principals WHERE name = @p)
BEGIN
    IF CHARINDEX(N'\', @p) > 0
    BEGIN
        SET @cmd = N'CREATE LOGIN [' + REPLACE(@p, N']', N']]') + N'] FROM WINDOWS';
        BEGIN TRY
            EXEC(@cmd);
        END TRY
        BEGIN CATCH
            SET @msg = ERROR_MESSAGE();
            SELECT CAST(0 AS int) AS Granted, @msg AS Reason;
            RETURN;
        END CATCH
    END
END
IF EXISTS (SELECT 1 FROM sys.server_principals WHERE name = @p)
   AND ISNULL(IS_SRVROLEMEMBER(N'dbcreator', @p), 0) = 0
BEGIN
    SET @cmd = N'ALTER SERVER ROLE [dbcreator] ADD MEMBER [' + REPLACE(@p, N']', N']]') + N']';
    BEGIN TRY
        EXEC(@cmd);
    END TRY
    BEGIN CATCH
        -- SQL Server 2008 R2 و قدیمیتر: ALTER SERVER ROLE وجود ندارد
        BEGIN TRY
            EXEC sp_addsrvrolemember @loginame = @p, @rolename = N'dbcreator';
        END TRY
        BEGIN CATCH
            SET @msg = ERROR_MESSAGE();
        END CATCH
    END CATCH
END
SELECT CASE WHEN ISNULL(IS_SRVROLEMEMBER(N'dbcreator', @p), 0) = 1
              OR ISNULL(IS_SRVROLEMEMBER(N'sysadmin', @p), 0) = 1
            THEN CAST(1 AS int) ELSE CAST(0 AS int) END AS Granted,
       @msg AS Reason;
'@

$script:CpVerifySql = @'
SELECT SUSER_SNAME() AS LoginName,
       ORIGINAL_LOGIN() AS OriginalLogin,
       ISNULL(IS_SRVROLEMEMBER(N'sysadmin'), 0) AS IsSysadmin,
       ISNULL(IS_SRVROLEMEMBER(N'dbcreator'), 0) AS IsDbcreator;
'@

function Get-CpCsValue {
    param($Builder, [string[]]$Names)
    foreach ($n in $Names) {
        foreach ($k in @($Builder.Keys)) {
            if ([string]$k -ieq $n) {
                $v = [string]$Builder[$k]
                if (-not [string]::IsNullOrWhiteSpace($v)) { return $v }
            }
        }
    }
    return ''
}

function New-CpMasterConnectionString {
    param([string]$DataSource, [string]$User, [string]$Password,
          [string]$Encrypt, [int]$ConnectTimeout = 15)
    $b = New-Object System.Data.Common.DbConnectionStringBuilder
    $b['Data Source'] = $DataSource
    $b['Initial Catalog'] = 'master'
    $b['Connect Timeout'] = [string]$ConnectTimeout
    $b['TrustServerCertificate'] = 'True'
    if ($Encrypt) { $b['Encrypt'] = $Encrypt }
    if ($User) {
        $b['User ID'] = $User
        $b['Password'] = $Password
    } else {
        $b['Integrated Security'] = 'True'
    }
    return $b.ConnectionString
}

function New-CpSqlConnection {
    param([string]$ConnectionString)
    try { return New-Object System.Data.SqlClient.SqlConnection($ConnectionString) }
    catch {
        try {
            $dll = Join-Path $TargetDir 'Microsoft.Data.SqlClient.dll'
            if (Test-Path $dll) {
                try { Add-Type -Path $dll -ErrorAction SilentlyContinue } catch { }
                return New-Object Microsoft.Data.SqlClient.SqlConnection($ConnectionString)
            }
        } catch { }
        throw 'هیچ ارائهدهندهٔ SQLای (System.Data.SqlClient / Microsoft.Data.SqlClient) در این PowerShell در دسترس نیست.'
    }
}

function Invoke-CpSqlRow {
    param([string]$ConnectionString, [string]$Sql, [int]$Timeout = 20)
    $conn = New-CpSqlConnection $ConnectionString
    try {
        $conn.Open()
        $cmd = $conn.CreateCommand()
        $cmd.CommandText = $Sql
        $cmd.CommandTimeout = $Timeout
        $reader = $cmd.ExecuteReader()
        try {
            if (-not $reader.Read()) { return $null }
            $row = @{}
            for ($i = 0; $i -lt $reader.FieldCount; $i++) {
                $row[$reader.GetName($i)] = $reader.GetValue($i)
            }
            return $row
        } finally {
            try { $reader.Close() } catch { }
            $cmd.Dispose()
        }
    } finally {
        try { $conn.Close() } catch { }
        $conn.Dispose()
    }
}

function Invoke-CpRestoreGrant {
    param([string]$ConnectionString, [string[]]$Targets, [string]$Primary,
          [string]$Sql, [object]$Report)
    $ok = $false
    $conn = New-CpSqlConnection $ConnectionString
    try {
        $conn.Open()
        foreach ($t in $Targets) {
            $cmd = $null
            try {
                $cmd = $conn.CreateCommand()
                $cmd.CommandText = $Sql
                $cmd.CommandTimeout = 30
                $p = $cmd.CreateParameter()
                $p.ParameterName = '@Principal'
                $p.Value = $t
                [void]$cmd.Parameters.Add($p)
                $r = $cmd.ExecuteScalar()
                if ($null -ne $r -and [int]$r -eq 1) {
                    Write-Host "  ✓ نقش dbcreator برای «$t» برقرار/تأیید شد." -ForegroundColor DarkGreen
                    if ($null -ne $Report) { $Report.Add("موفق: $t") }
                    if ($t -eq $Primary) { $ok = $true }
                } else {
                    if ($null -ne $Report) { $Report.Add("ناموفق: $t") }
                }
            } catch {
                $m = $_.Exception.Message
                Write-Host "  ! «$t»: $m" -ForegroundColor DarkYellow
                if ($null -ne $Report) { $Report.Add("خطا: $t — $m") }
            } finally {
                if ($cmd) { $cmd.Dispose() }
            }
        }
    } finally {
        try { $conn.Close() } catch { }
        $conn.Dispose()
    }
    return $ok
}

function Write-CpManualSql {
    param([string[]]$Targets, [string]$ReportDir)
    $sql = New-Object System.Collections.Generic.List[string]
    $sql.Add('/* سهند کلینیکپرو — فعالسازی بازیابی پشتیبان (نقش dbcreator).')
    $sql.Add('   این فایل را در SQL Server Management Studio با حساب sysadmin اجرا کنید. */')
    $sql.Add('SET NOCOUNT ON;')
    $idx = 0
    foreach ($t in $Targets) {
        $idx++
        $v = "@p$idx"
        $lit = $t.Replace("'", "''")
        $sql.Add("DECLARE $v nvarchar(128) = N'$lit';")
        $sql.Add("IF NOT EXISTS (SELECT 1 FROM sys.server_principals WHERE name = $v)")
        $sql.Add("    AND CHARINDEX(N'\', $v) > 0")
        $sql.Add("    EXEC(N'CREATE LOGIN [' + REPLACE($v, N']', N']]') + N'] FROM WINDOWS');")
        $sql.Add("IF EXISTS (SELECT 1 FROM sys.server_principals WHERE name = $v)")
        $sql.Add("    AND ISNULL(IS_SRVROLEMEMBER(N'dbcreator', $v), 0) = 0")
        $sql.Add("    AND ISNULL(IS_SRVROLEMEMBER(N'sysadmin',  $v), 0) = 0")
        $sql.Add("    EXEC(N'ALTER SERVER ROLE [dbcreator] ADD MEMBER [' + REPLACE($v, N']', N']]') + N']');")
        $sql.Add('')
    }
    $path = Join-Path $ReportDir 'Grant-RestorePermission.sql'
    Set-Content -Path $path -Value ($sql -join [Environment]::NewLine) -Encoding UTF8
    return $path
}

function Invoke-ClinicProRestorePermission {
    param(
        [Parameter(Mandatory = $true)][string]$ConnectionString,
        [Parameter(Mandatory = $true)][string]$ReportDir,
        [string]$AdminUser = '',
        [string]$AdminPassword = '',
        [string[]]$ExtraTargets = @(),
        [switch]$AllowPrompt,
        [switch]$AllowSingleUserElevation
    )

    $report = New-Object System.Collections.Generic.List[string]
    $report.Add("وضعیت مجوز بازیابی (dbcreator) — $(Get-Date -Format 'yyyy/MM/dd HH:mm:ss')")
    $granted = $false
    $lastError = ''
    $targets = @()
    $primary = ''
    $attempts = New-Object System.Collections.Generic.List[object]
    $dataSource = ''
    $encrypt = ''
    $apiLogin = ''

    try {
        $parsed = New-Object System.Data.Common.DbConnectionStringBuilder
        $parsed.ConnectionString = $ConnectionString
        $dataSource = Get-CpCsValue $parsed @('Data Source', 'Server', 'Address', 'Addr', 'Network Address')
        $sqlUser = Get-CpCsValue $parsed @('User ID', 'UID', 'User')
        $sqlPassword = Get-CpCsValue $parsed @('Password', 'PWD')
        $encrypt = Get-CpCsValue $parsed @('Encrypt')
        if (-not $dataSource) { throw 'در رشتهٔ اتصال، آدرس SQL Server (Data Source) پیدا نشد.' }

        # لاگین دقیق API — اگر رشته اتصال SQL Login داشته باشد مستقیم قابل پرسش است
        if ($sqlUser) {
            try {
                $csApi = New-CpMasterConnectionString -DataSource $dataSource -User $sqlUser -Password $sqlPassword -Encrypt $encrypt
                $row = Invoke-CpSqlRow -ConnectionString $csApi -Sql $script:CpVerifySql
                if ($row) { $apiLogin = [string]$row['LoginName'] }
            } catch { }
            $targets = @($sqlUser)
        } else {
            # احراز ویندوزی: سرویس API بهصورت پیشفرض LocalSystem است؛ برای SQL
            # راهدور حساب ماشین و برای حساب مجازی سرویس هم پوشش داده میشود.
            $hostPart = ($dataSource -replace '^(?i:tcp:)', '' -split '[\\,]')[0]
            if (@('.', '(local)', 'localhost', '127.0.0.1', $env:COMPUTERNAME) -notcontains $hostPart) {
                throw 'SQL روی رایانهٔ دیگر است؛ حساب واقعی سرویس باید با مدیر SQL بررسی شود.'
            }
            # نصاب سرویس جدید را با LocalSystem ایجاد می‌کند. ابزار مستقل
            # فقط در صورت اجرای سرویس با همان حساب این فرض را معتبر می‌داند.
            if (-not $AsLibrary) {
                $service = Get-CimInstance Win32_Service -Filter "Name='SahandClinicPro.API'" -ErrorAction Stop
                if (-not $service -or @('LocalSystem', 'NT AUTHORITY\SYSTEM') -notcontains $service.StartName) {
                    throw 'حساب سرویس API متفاوت است یا سرویس یافت نشد؛ هویت واقعی را با مدیر SQL بررسی کنید.'
                }
            }
            $targets = @('NT AUTHORITY\SYSTEM')
        }
        $targets = @($targets | Where-Object { $_ } | Select-Object -Unique)
        $primary = [string]$targets[0]


        if ($apiLogin -and ($targets -notcontains $apiLogin)) {
            $targets = @($apiLogin) + $targets
            $primary = $apiLogin
        }
        $report.Add("لاگین تشخیصدادهشدهٔ API: $(if ($apiLogin) { $apiLogin } else { 'ویندوزی — از روی حساب سرویس حدس زده شد' })")
        $report.Add("لاگین هدف اصلی: $primary")
        $report.Add("همهٔ لاگینهای هدف: $($targets -join '، ')")

        # مسیرهای اتصال مدیریتی — تکراریها حذف میشوند
        if ($AdminUser) {
            $attempts.Add([pscustomobject]@{
                Label = "حساب مدیر ارسالشده ($AdminUser)"
                ConnectionString = (New-CpMasterConnectionString -DataSource $dataSource -User $AdminUser -Password $AdminPassword -Encrypt $encrypt)
            })
        }
        $attempts.Add([pscustomobject]@{
            Label = 'همان شناسههای رشتهٔ اتصال'
            ConnectionString = (New-CpMasterConnectionString -DataSource $dataSource -User $sqlUser -Password $sqlPassword -Encrypt $encrypt)
        })
        $attempts.Add([pscustomobject]@{
            Label = 'احراز هویت ویندوزی حساب جاری'
            ConnectionString = (New-CpMasterConnectionString -DataSource $dataSource -Encrypt $encrypt)
        })
        # حذف تکراریها بدون بههمزدن ترتیب اولویت (اولین مسیر موفق، کار را تمام میکند)
        $seen = @{}
        $ordered = New-Object System.Collections.Generic.List[object]
        foreach ($a in $attempts) {
            if (-not $seen.ContainsKey($a.ConnectionString)) {
                $seen[$a.ConnectionString] = $true
                $ordered.Add($a)
            }
        }
        $attempts = $ordered
    } catch {
        $lastError = $_.Exception.Message
        $report.Add("خطای آمادهسازی: $lastError")
        $targets = @()
    }

    if ($targets.Count -gt 0) {
        foreach ($attempt in $attempts) {
            try {
                if (Invoke-CpRestoreGrant -ConnectionString $attempt.ConnectionString -Targets $targets `
                        -Primary $primary -Sql $script:CpGrantSql -Report $report) {
                    $granted = $true
                    $report.Add("مسیر موفق: $($attempt.Label)")
                    Write-Host "مجوز بازیابی پشتیبان (نقش سروری dbcreator) بهصورت خودکار برقرار شد — با «$($attempt.Label)»." -ForegroundColor Green
                    break
                }
                $lastError = "اتصال «$($attempt.Label)» برقرار شد ولی اجازهٔ اعطای نقش نداشت."
                $report.Add($lastError)
            } catch {
                $lastError = $_.Exception.Message
                $report.Add("اتصال «$($attempt.Label)» ناموفق: $lastError")
                Write-Host "  ! اتصال «$($attempt.Label)» ناموفق: $lastError" -ForegroundColor DarkYellow
            }
        }
    }

    # لایهٔ ۳ — پرسش تعاملی حساب مدیر SQL (مثلاً sa)
    $canPrompt = $false
    try { $canPrompt = [Environment]::UserInteractive } catch { }
    if (-not $granted -and $AllowPrompt -and $canPrompt) {
        try {
            Write-Host ''
            Write-Host 'برای اعطای خودکار نقش، نام و رمز یک حساب مدیر SQL (مثلاً sa) را وارد کنید.' -ForegroundColor Yellow
            Write-Host 'اگر رمز را نمیدانید Enter بزنید؛ بدون دسترسی مدیر SQL اعطای نقش امکان‌پذیر نیست.' -ForegroundColor Yellow
            $adminLogin = Read-Host 'نام لاگین مدیر SQL (خالی = رد کردن)'
            if ($adminLogin) {
                $adminSecure = Read-Host 'رمز آن لاگین' -AsSecureString
                $bstr = [System.Runtime.InteropServices.Marshal]::SecureStringToBSTR($adminSecure)
                try { $adminPwd = [System.Runtime.InteropServices.Marshal]::PtrToStringAuto($bstr) }
                finally { [System.Runtime.InteropServices.Marshal]::ZeroFreeBSTR($bstr) }
                $cs = New-CpMasterConnectionString -DataSource $dataSource -User $adminLogin -Password $adminPwd -Encrypt $encrypt
                if (Invoke-CpRestoreGrant -ConnectionString $cs -Targets $targets -Primary $primary `
                        -Sql $script:CpGrantSql -Report $report) {
                    $granted = $true
                    $report.Add("مسیر موفق: حساب مدیر $adminLogin")
                    Write-Host "مجوز بازیابی پشتیبان (dbcreator) با حساب مدیر «$adminLogin» برقرار شد." -ForegroundColor Green
                }
            }
        } catch {
            $lastError = $_.Exception.Message
            $report.Add("دریافت حساب مدیر ناموفق: $lastError")
        }
    }

    if (-not $granted) {
        try {
            $manual = Write-CpManualSql -Targets $targets -ReportDir $ReportDir
            Write-Host "هشدار: اعطای خودکار نقش dbcreator موفق نشد ($lastError)." -ForegroundColor Yellow
            Write-Host "فایل آمادهٔ اجرا ساخته شد: $manual" -ForegroundColor Yellow
            Write-Host 'آن را یکبار در SSMS با حساب مدیر اجرا کنید؛ سپس سرویس API را ریاستارت کنید.' -ForegroundColor Yellow
            $report.Add("فایل دستی ساخته شد: $manual")
        } catch { }
    }

    $report.Insert(1, "نتیجه: $(if ($granted) { 'اعطای خودکار موفق' } else { 'نیازمند اجرای دستی Grant-RestorePermission.sql' })")
    $report.Add("آخرین خطا: $lastError")
    try {
        if (-not (Test-Path $ReportDir)) { New-Item -ItemType Directory -Path $ReportDir -Force | Out-Null }
        Set-Content -Path (Join-Path $ReportDir 'restore-permission.status.txt') -Value $report -Encoding UTF8
    } catch { }

    return [pscustomobject]@{
        Granted = $granted
        Primary = $primary
        Targets = $targets
        Attempts = $attempts
        LastError = $lastError
        Report = $report
    }
}

# ─── اجرای مستقل (دابلکلیک Enable-RestorePermission.bat) ───
if (-not $AsLibrary) {
    Write-Host '════════ برقراری مجوز بازیابی پشتیبان — سهند کلینیکپرو ════════' -ForegroundColor Cyan
    $settingsPath = Join-Path $TargetDir 'appsettings.json'
    if (-not $ConnectionString) {
        if (-not (Test-Path $settingsPath)) {
            Write-Host "فایل تنظیمات سرور پیدا نشد: $settingsPath" -ForegroundColor Red
            Write-Host 'ابتدا بستهٔ کامل سرور همنسخه را نصب کنید.' -ForegroundColor Red
            exit 1
        }
        try {
            $settings = Get-Content -Raw -Path $settingsPath | ConvertFrom-Json
            $ConnectionString = [string]$settings.ConnectionStrings.DefaultConnection
        } catch {
            Write-Host "خواندن تنظیمات سرور ناموفق بود: $($_.Exception.Message)" -ForegroundColor Red
            exit 1
        }
    }
    Write-Host "بررسی مجوز SQL برای سرویس API" -ForegroundColor DarkGray
    $promptAllowed = -not $NoPrompt
    $elevationAllowed = -not $NoSqlSingleUserElevation
    $result = Invoke-ClinicProRestorePermission -ConnectionString $ConnectionString `
        -ReportDir $TargetDir -AdminUser $SqlAdminUser -AdminPassword $SqlAdminPassword `
        -AllowPrompt:$promptAllowed -AllowSingleUserElevation:$elevationAllowed
    Write-Host ''
    if ($result.Granted) {
        Write-Host '✅ مجوز بازیابی پشتیبان برقرار است. سرویس API را یکبار ریاستارت کنید.' -ForegroundColor Green
        exit 0
    }
    Write-Host '⚠️ اعطای خودکار ممکن نشد. جزئیات در restore-permission.status.txt کنار سرور است.' -ForegroundColor Yellow
    exit 2
}
