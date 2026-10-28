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
#  این نسخه چهار لایه دارد و تا جایی که ممکن است بدون دخالت انسان جلو میرود:
#    ۱) اتصال با همان شناسههای رشتهٔ اتصال API (اغلب همان sa/حساب مدیر)؛
#    ۲) اتصال با احراز ویندوزی حساب جاری (اغلب نصبکنندهٔ SQL Express)؛
#    ۳) پرسش نام/رمز یک حساب مدیر SQL (مثلاً sa) — فقط اگر لازم شد؛
#    ۴) بالا آوردن موقت SQL Server محلی در «حالت تککاربره» (/m). مایکروسافت
#       در این حالت هر عضو گروه Administrators محلی را sysadmin میداند؛
#       بنابراین حتی وقتی رمز sa و حساب مدیر SQL در دسترس نیست، نصاب
#       خودش نقش dbcreator را به لاگین API میدهد و سپس SQL را به حالت
#       عادی برمیگرداند. پارامتر /m فقط برای همان استارت است و در رجیستری
#       ذخیره نمیشود؛ با یک ریاستارت ساده همیشه وضع عادی برمیگردد.
#
#  در تمام لایهها لاگین دقیق، نتیجه و آخرین خطا در
#  restore-permission.status.txt ثبت میشود. خطای این گام نصب را متوقف نمیکند.
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

function Get-CpLocalSqlServiceName {
    param([string]$DataSource)
    if ([string]::IsNullOrWhiteSpace($DataSource)) { return $null }
    $ds = $DataSource.Trim()
    foreach ($prefix in @('tcp:', 'np:', 'lpc:')) {
        if ($ds.StartsWith($prefix, [StringComparison]::OrdinalIgnoreCase)) {
            if ($prefix -ne 'tcp:') { return $null } # لولهٔ نامدار — سرویس استانداردی نیست
            $ds = $ds.Substring($prefix.Length)
            break
        }
    }
    $comma = $ds.IndexOf(',')
    if ($comma -gt 0) { $ds = $ds.Substring(0, $comma) }
    $parts = $ds -split '\\'
    $hostName = $parts[0]
    $instance = if ($parts.Count -gt 1) { $parts[1] } else { '' }
    if ($hostName -ieq '(localdb)') { return $null }
    $localNames = @('', '.', '(local)', 'localhost', '127.0.0.1', '::1',
                    [string]$env:COMPUTERNAME, "$env:COMPUTERNAME.$env:USERDNSDOMAIN")
    if ($localNames -notcontains $hostName) {
        Write-Host "  ! SQL Server ($DataSource) روی رایانهٔ دیگری است؛ ریاستارت تککاربره فقط محلی مجاز است." -ForegroundColor DarkYellow
        return $null
    }
    $svc = if ([string]::IsNullOrEmpty($instance)) { 'MSSQLSERVER' } else { 'MSSQL$' + $instance }
    if (-not (Get-Service -Name $svc -ErrorAction SilentlyContinue)) {
        $candidate = Get-CimInstance Win32_Service -ErrorAction SilentlyContinue |
            Where-Object { $_.Name -like 'MSSQL*' -and $_.PathName -like '*sqlservr.exe*' } |
            Select-Object -First 1
        if ($candidate) { $svc = [string]$candidate.Name }
        else { return $null }
    }
    return $svc
}

function Invoke-CpNet {
    param([string[]]$Arguments)
    $winDir = if ($env:WINDIR) { $env:WINDIR } else { $env:SystemRoot }
    $netExe = Join-Path $winDir 'System32\net.exe'
    $tag = [Guid]::NewGuid().ToString('N').Substring(0, 8)
    $out = Join-Path $env:TEMP "clinicpro_net_$tag.out"
    $err = Join-Path $env:TEMP "clinicpro_net_$tag.err"
    try {
        $proc = Start-Process -FilePath $netExe -ArgumentList $Arguments -Wait -PassThru `
            -RedirectStandardOutput $out -RedirectStandardError $err
        $code = $proc.ExitCode
    } catch {
        return [pscustomobject]@{ ExitCode = -1; Output = $_.Exception.Message }
    }
    $text = ''
    try { $text = ((Get-Content $out -Raw -ErrorAction SilentlyContinue) + (Get-Content $err -Raw -ErrorAction SilentlyContinue)) } catch { }
    try { Remove-Item $out, $err -Force -ErrorAction SilentlyContinue } catch { }
    return [pscustomobject]@{ ExitCode = $code; Output = [string]$text }
}

function Wait-CpServiceStatus {
    param([string]$Name, [string]$Status, [int]$Seconds = 60)
    $sw = [System.Diagnostics.Stopwatch]::StartNew()
    while ($sw.Elapsed.TotalSeconds -lt $Seconds) {
        try {
            $svc = Get-Service -Name $Name -ErrorAction Stop
            if ([string]$svc.Status -eq $Status) { return $true }
        } catch { }
        Start-Sleep -Milliseconds 500
    }
    return $false
}

function Invoke-CpSingleUserElevation {
    param([string]$DataSource, [string]$Encrypt, [string[]]$Targets,
          [string]$Primary, [string]$Sql, [string]$ReportDir, [object]$Report)

    $svc = Get-CpLocalSqlServiceName -DataSource $DataSource
    if (-not $svc) { return $false }

    $agent = if ($svc -eq 'MSSQLSERVER') { 'SQLSERVERAGENT' }
             else { 'SQLAgent$' + $svc.Substring(6) }
    $agentWasRunning = $false
    $recovery = Join-Path $ReportDir 'SQL-RECOVERY-Restart-Normal.bat'
    $granted = $false

    Write-Host '' 
    Write-Host '── لایهٔ ۴: بالا آوردن موقت SQL Server در حالت تککاربره ──' -ForegroundColor Cyan
    Write-Host 'هیچ حساب مدیر SQLای در دسترس نبود. SQL Server حداکثر حدود ۳۰ ثانیه ریاستارت موقت میشود تا نصاب' -ForegroundColor Yellow
    Write-Host 'خودش نقش dbcreator را بدهد؛ سپس بدون استثنا به حالت عادی چندکاربره برگردانده میشود.' -ForegroundColor Yellow
    if ($null -ne $Report) { $Report.Add("لایهٔ ۴: ریاستارت موقت سرویس $svc در حالت تککاربره") }

    try {
        # فایل نجات: اگر برق/پردازش وسط کار قطع شد، یک ریاستارت ساده SQL را عادی میکند
        try {
            $lines = @('@echo off', 'chcp 65001 >nul',
                       ('net stop "' + $svc + '" /y'),
                       ('net start "' + $svc + '"'),
                       'echo SQL Server به حالت عادی برگشت.')
            Set-Content -Path $recovery -Value $lines -Encoding UTF8
        } catch { }

        try {
            $ag = Get-Service -Name $agent -ErrorAction SilentlyContinue
            if ($ag -and [string]$ag.Status -eq 'Running') {
                $agentWasRunning = $true
                Stop-Service -Name $agent -Force -ErrorAction SilentlyContinue
                Write-Host "  • عامل SQL Server ($agent) موقتاً متوقف شد تا تنها اتصال را نگیرد." -ForegroundColor DarkGray
            }
        } catch { }

        Stop-Service -Name $svc -Force -ErrorAction Stop
        if (-not (Wait-CpServiceStatus -Name $svc -Status 'Stopped' -Seconds 60)) {
            throw "سرویس $svc در مهلت مقرر متوقف نشد."
        }

        $start = Invoke-CpNet -Arguments @('start', $svc, '/m')
        if ($start.ExitCode -ne 0) {
            throw "اجرای net start $svc /m ناموفق بود (کد $($start.ExitCode)): $($start.Output)"
        }
        Write-Host "  • SQL Server در حالت تککاربره بالا آمد ($svc)." -ForegroundColor DarkGray

        $cs = New-CpMasterConnectionString -DataSource $DataSource -Encrypt $Encrypt -ConnectTimeout 8
        for ($i = 0; $i -lt 15 -and -not $granted; $i++) {
            try {
                $granted = Invoke-CpRestoreGrant -ConnectionString $cs -Targets $Targets `
                    -Primary $Primary -Sql $Sql -Report $Report
            } catch {
                if ($null -ne $Report) { $Report.Add("تلاش $($i + 1) ناموفق: $($_.Exception.Message)") }
            }
            if (-not $granted) { Start-Sleep -Seconds 2 }
        }
        return $granted
    } catch {
        $m = $_.Exception.Message
        Write-Host "  ! ریاستارت تککاربره ناموفق: $m" -ForegroundColor DarkYellow
        if ($null -ne $Report) { $Report.Add("خطای لایهٔ ۴: $m") }
        return $false
    } finally {
        # بدون استثنا: بازگرداندن SQL Server به حالت عادی چندکاربره
        try {
            Stop-Service -Name $svc -Force -ErrorAction SilentlyContinue
            [void](Wait-CpServiceStatus -Name $svc -Status 'Stopped' -Seconds 60)
            $back = Invoke-CpNet -Arguments @('start', $svc)
            if ($back.ExitCode -ne 0) {
                Start-Service -Name $svc -ErrorAction SilentlyContinue
            }
            [void](Wait-CpServiceStatus -Name $svc -Status 'Running' -Seconds 60)
            # راستیآزمایی:SERVERPROPERTY('IsSingleUser') باید ۰ باشد
            try {
                $check = Invoke-CpSqlRow -ConnectionString (
                    New-CpMasterConnectionString -DataSource $DataSource -Encrypt $Encrypt -ConnectTimeout 8) `
                    -Sql "SELECT ISNULL(CAST(SERVERPROPERTY('IsSingleUser') AS int), 0) AS IsSingleUser" -Timeout 10
                if ($check -and [int]$check['IsSingleUser'] -eq 1) {
                    Stop-Service -Name $svc -Force -ErrorAction SilentlyContinue
                    Start-Service -Name $svc -ErrorAction SilentlyContinue
                }
            } catch { }
            Write-Host '  • SQL Server به حالت عادی (چندکاربره) برگشت.' -ForegroundColor DarkGreen
            if ($null -ne $Report) { $Report.Add('SQL Server به حالت عادی چندکاربره برگشت') }
        } catch {
            $m = $_.Exception.Message
            Write-Host "  ! هشدار: بازگرداندن حالت عادی SQL نیازمند بررسی است: $m" -ForegroundColor Red
            if ($null -ne $Report) { $Report.Add("هشدار بازگشت حالت عادی: $m — فایل SQL-RECOVERY-Restart-Normal.bat را اجرا کنید") }
        }
        if ($agentWasRunning) {
            try { Start-Service -Name $agent -ErrorAction SilentlyContinue } catch { }
        }
        try {
            if ($granted) { Remove-Item $recovery -Force -ErrorAction SilentlyContinue }
        } catch { }
    }
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
            $targets = @('NT AUTHORITY\SYSTEM')
            try {
                $me = [System.Security.Principal.WindowsIdentity]::GetCurrent().Name
                if ($me) { $targets += $me }
            } catch { }
            if ($env:USERDOMAIN -and $env:COMPUTERNAME) {
                $targets += ('{0}\{1}$' -f $env:USERDOMAIN, $env:COMPUTERNAME)
            }
            $targets += 'NT SERVICE\SahandClinicPro.API'
        }
        foreach ($extra in $ExtraTargets) {
            if (-not [string]::IsNullOrWhiteSpace($extra)) { $targets += $extra }
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
        if ($sqlUser -and $sqlPassword) {
            $attempts.Add([pscustomobject]@{
                Label = 'sa با همان رمز رشتهٔ اتصال'
                ConnectionString = (New-CpMasterConnectionString -DataSource $dataSource -User 'sa' -Password $sqlPassword -Encrypt $encrypt)
            })
        }
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
            Write-Host 'اگر رمز را نمیدانید Enter بزنید — نصاب خودش از مسیر بعدی (ریاستارت موقت SQL) استفاده میکند.' -ForegroundColor Yellow
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

    # لایهٔ ۴ — ریاستارت موقت SQL Server محلی در حالت تککاربره
    if (-not $granted -and $AllowSingleUserElevation -and $dataSource) {
        $granted = Invoke-CpSingleUserElevation -DataSource $dataSource -Encrypt $encrypt `
            -Targets $targets -Primary $primary -Sql $script:CpGrantSql `
            -ReportDir $ReportDir -Report $report
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
    Write-Host "رشتهٔ اتصال: $ConnectionString" -ForegroundColor DarkGray
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
