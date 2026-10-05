<?php
/**
 * هسته مشترک سرور لایسنس Sahand ClinicPro — نسخه cPanel / هاست اشتراکی
 * PHP خالص بدون هیچ وابستگی خارجی — فقط افزونه‌های استاندارد (pdo_mysql، openssl، json)
 * همه توابع با کامنت فارسی مستند شده‌اند.
 *
 * @license MIT
 */

declare(strict_types=1);

if (!defined('LS_ROOT')) {
    define('LS_ROOT', dirname(__DIR__));
}
if (!defined('LS_DATA_DIR')) {
    define('LS_DATA_DIR', LS_ROOT . '/data');
}
if (!defined('LS_CONFIG_FILE')) {
    define('LS_CONFIG_FILE', LS_DATA_DIR . '/config.php');
}
if (!defined('LS_INSTALL_LOCK')) {
    define('LS_INSTALL_LOCK', LS_DATA_DIR . '/install.lock');
}
if (!defined('LS_VERSION')) {
    define('LS_VERSION', '1.0.0');
}

date_default_timezone_set('Asia/Tehran');

/* جایگزین سبک در صورت نبود افزونه mbstring (هاست‌های بسیار قدیمی) */
if (!function_exists('mb_substr')) {
    function mb_substr($s, $start, $length = null)
    {
        return $length === null
            ? substr((string)$s, (int)$start)
            : substr((string)$s, (int)$start, (int)$length);
    }
    function mb_strlen($s)
    {
        return strlen((string)$s);
    }
}

/* ──────────────────────────  بارگذاری پیکربندی  ────────────────────────── */

/**
 * بارگذاری config.php تولیدشده توسط نصب‌کننده — اگر نبود یعنی نصب نشده
 */
function ls_load_config(): void
{
    if (!is_file(LS_CONFIG_FILE)) {
        http_response_code(503);
        header('Content-Type: text/html; charset=utf-8');
        echo '<!doctype html><html lang="fa" dir="rtl"><meta charset="utf-8">';
        echo '<body style="font-family:Tahoma;text-align:center;padding:60px">';
        echo '<h2>نصب انجام نشده است</h2><p>ابتدا <a href="install.php">install.php</a> را اجرا کنید.</p>';
        echo '</body></html>';
        exit;
    }
    require LS_CONFIG_FILE;
}

/* ──────────────────────────  اتصال دیتابیس  ────────────────────────── */

/**
 * دریافت اتصال PDO تکی (utf8mb4 + حالت خطای استثنا)
 */
function ls_db(): PDO
{
    static $pdo = null;
    if ($pdo instanceof PDO) {
        return $pdo;
    }
    $pdo = new PDO(
        'mysql:host=' . LS_DB_HOST . ';port=' . (defined('LS_DB_PORT') ? LS_DB_PORT : 3306)
        . ';dbname=' . LS_DB_NAME . ';charset=utf8mb4',
        LS_DB_USER,
        LS_DB_PASS,
        [
            PDO::ATTR_ERRMODE            => PDO::ERRMODE_EXCEPTION,
            PDO::ATTR_DEFAULT_FETCH_MODE => PDO::FETCH_ASSOC,
            PDO::ATTR_EMULATE_PREPARES   => false,
        ]
    );
    return $pdo;
}

/* ──────────────────────────  پاسخ JSON  ────────────────────────── */

/**
 * خروجی JSON استاندارد + توقف
 */
function ls_json_out(array $data, int $status = 200): void
{
    http_response_code($status);
    header('Content-Type: application/json; charset=utf-8');
    header('X-Content-Type-Options: nosniff');
    echo json_encode($data, JSON_UNESCAPED_UNICODE | JSON_UNESCAPED_SLASHES);
    exit;
}

/* ──────────────────────────  تاریخ شمسی  ────────────────────────── */

/** تبدیل میلادی به جلالی — الگوریتم استاندارد تقویم هجری شمسی */
function ls_g2j(int $gy, int $gm, int $gd): array
{
    $gdm = [0, 31, 59, 90, 120, 151, 181, 212, 243, 273, 304, 334];
    $jy = ($gy <= 1600) ? 0 : 979;
    $gy -= ($gy <= 1600) ? 621 : 1600;
    $gy2 = ($gm > 2) ? ($gy + 1) : $gy;
    $days = (365 * $gy) + intdiv($gy2 + 3, 4) - intdiv($gy2 + 99, 100)
        + intdiv($gy2 + 399, 400) - 80 + $gdm[$gm - 1] + $gd;
    $jy += 33 * intdiv($days, 12053);
    $days %= 12053;
    $jy += 4 * intdiv($days, 1461);
    $days %= 1461;
    if ($days > 365) {
        $jy += intdiv($days - 1, 365);
        $days = ($days - 1) % 365;
    }
    $jm = ($days < 186) ? 1 + intdiv($days, 31) : 7 + intdiv($days - 186, 30);
    $jd = ($days < 186) ? ($days % 31) + 1 : (($days - 186) % 30) + 1;
    return [$jy, $jm, $jd];
}

/** تبدیل جلالی به میلادی */
function ls_j2g(int $jy, int $jm, int $jd): array
{
    $gy = ($jy <= 979) ? 621 : 1600;
    $jy -= ($jy <= 979) ? 0 : 979;
    $days = (365 * $jy) + (intdiv($jy, 33) * 8) + intdiv(($jy % 33) + 3, 4)
        + 78 + $jd + (($jm < 7) ? ($jm - 1) * 31 : (($jm - 7) * 30) + 186);
    $gy += 400 * intdiv($days, 146097);
    $days %= 146097;
    if ($days > 36524) {
        $gy += 100 * intdiv(--$days, 36524);
        $days %= 36524;
        if ($days >= 365) {
            $days++;
        }
    }
    $gy += 4 * intdiv($days, 1461);
    $days %= 1461;
    if ($days > 365) {
        $gy += intdiv($days - 1, 365);
        $days = ($days - 1) % 365;
    }
    $gd = $days + 1;
    $months = [31, ((($gy % 4 == 0) && ($gy % 100 != 0)) || ($gy % 400 == 0)) ? 29 : 28,
        31, 30, 31, 30, 31, 31, 30, 31, 30, 31];
    $gm = 0;
    while ($gm < 12 && $gd > $months[$gm]) {
        $gd -= $months[$gm];
        $gm++;
    }
    return [$gy, $gm + 1, $gd];
}

/** نام ماه‌های شمسی */
function ls_j_month(int $m): string
{
    $names = ['', 'فروردین', 'اردیبهشت', 'خرداد', 'تیر', 'مرداد', 'شهریور',
        'مهر', 'آبان', 'آذر', 'دی', 'بهمن', 'اسفند'];
    return $names[$m] ?? '';
}

/** نمایش تاریخ به شمسی — ورودی 'Y-m-d' یا DateTime */
function ls_shamsi(?string $mysqlDate, bool $withTime = false): string
{
    if (empty($mysqlDate)) {
        return '—';
    }
    $ts = strtotime($mysqlDate);
    if ($ts === false) {
        return '—';
    }
    [$jy, $jm, $jd] = ls_g2j((int)date('Y', $ts), (int)date('n', $ts), (int)date('j', $ts));
    $out = sprintf('%04d/%02d/%02d', $jy, $jm, $jd);
    if ($withTime) {
        $out .= ' ' . date('H:i', $ts);
    }
    return $out;
}

/** تاریخ امروز به شمسی */
function ls_today_shamsi(): string
{
    [$jy, $jm, $jd] = ls_g2j((int)date('Y'), (int)date('n'), (int)date('j'));
    return sprintf('%04d/%02d/%02d', $jy, $jm, $jd);
}

/** نمایش اعداد فارسی */
function ls_fa_num(string $s): string
{
    return str_replace(
        ['0', '1', '2', '3', '4', '5', '6', '7', '8', '9'],
        ['۰', '۱', '۲', '۳', '۴', '۵', '۶', '۷', '۸', '۹'], $s);
}

/* ──────────────────────────  امنیت فرم‌ها (CSRF)  ────────────────────────── */

/** تولید یا بازیابی توکن CSRF نشست */
function ls_csrf_token(): string
{
    if (session_status() !== PHP_SESSION_ACTIVE) {
        session_start();
    }
    if (empty($_SESSION['csrf'])) {
        $_SESSION['csrf'] = bin2hex(random_bytes(32));
    }
    return $_SESSION['csrf'];
}

/** بررسی توکن CSRF — در صورت عدم تطابق توقف */
function ls_csrf_check(): void
{
    if (session_status() !== PHP_SESSION_ACTIVE) {
        session_start();
    }
    $sent = $_POST['csrf'] ?? '';
    if (empty($_SESSION['csrf']) || !hash_equals($_SESSION['csrf'], (string)$sent)) {
        http_response_code(419);
        exit('توکن امنیتی نامعتبر است — صفحه را دوباره بارگذاری کنید.');
    }
}

/** فیلد مخفی CSRF برای درج در فرم‌ها */
function ls_csrf_field(): string
{
    return '<input type="hidden" name="csrf" value="' . ls_csrf_token() . '">';
}

/* ──────────────────────────  تنظیمات  ────────────────────────── */

/** خواندن یک تنظیم با مقدار پیش‌فرض */
function ls_setting(string $key, string $default = ''): string
{
    $stmt = ls_db()->prepare('SELECT svalue FROM ls_settings WHERE skey = ?');
    $stmt->execute([$key]);
    $row = $stmt->fetch();
    return $row === false ? $default : (string)$row['svalue'];
}

/** ذخیره تنظیم (upsert) */
function ls_setting_set(string $key, string $value): void
{
    ls_db()->prepare(
        'INSERT INTO ls_settings (skey, svalue) VALUES (?, ?)
         ON DUPLICATE KEY UPDATE svalue = VALUES(svalue)'
    )->execute([$key, $value]);
}

/* ──────────────────────────  احراز هویت مدیر  ────────────────────────── */

/** شروع نشست امن */
function ls_session_start(): void
{
    if (session_status() === PHP_SESSION_ACTIVE) {
        return;
    }
    session_set_cookie_params([
        'lifetime' => 0,
        'path'     => '/',
        'httponly' => true,
        'samesite' => 'Lax',
        'secure'   => (!empty($_SERVER['HTTPS']) && $_SERVER['HTTPS'] !== 'off'),
    ]);
    session_name('LSSESS');
    session_start();
}

/** آیا مدیر وارد شده است */
function ls_admin(): ?array
{
    ls_session_start();
    if (empty($_SESSION['admin_id'])) {
        return null;
    }
    return [
        'id'       => (int)$_SESSION['admin_id'],
        'username' => (string)$_SESSION['admin_username'],
        'name'     => (string)($_SESSION['admin_name'] ?? 'مدیر'),
    ];
}

/** الزام ورود مدیر — در غیر این صورت هدایت به صفحه ورود */
function ls_require_admin(): array
{
    $admin = ls_admin();
    if ($admin === null) {
        header('Location: index.php?page=login');
        exit;
    }
    return $admin;
}

/** تلاش ورود مدیر — قفل ۳۰ دقیقه‌ای بعد از ۵ خطا */
function ls_admin_login(string $username, string $password): array
{
    $db = ls_db();
    $stmt = $db->prepare('SELECT * FROM ls_admins WHERE username = ? AND is_active = 1');
    $stmt->execute([$username]);
    $admin = $stmt->fetch();

    if ($admin === false) {
        return [false, 'نام کاربری یا رمز عبور اشتباه است.'];
    }
    // بررسی قفل اکانت
    if (!empty($admin['locked_until']) && strtotime((string)$admin['locked_until']) > time()) {
        return [false, 'به دلیل تلاش‌های ناموفق، حساب تا ۳۰ دقیقه قفل شده است.'];
    }

    if (!password_verify($password, (string)$admin['password_hash'])) {
        $fails = (int)$admin['failed_attempts'] + 1;
        $lock = $fails >= 5 ? date('Y-m-d H:i:s', time() + 1800) : null;
        $db->prepare('UPDATE ls_admins SET failed_attempts = ?, locked_until = ? WHERE id = ?')
            ->execute([$fails, $lock, $admin['id']]);
        return [false, $lock !== null
            ? 'تعداد تلاش‌ها بیش از حد مجاز بود — حساب ۳۰ دقیقه قفل شد.'
            : 'نام کاربری یا رمز عبور اشتباه است.'];
    }

    // ورود موفق — بازنشانی شمارنده و شناسه نشست تازه
    $db->prepare('UPDATE ls_admins SET failed_attempts = 0, locked_until = NULL,
        last_login_at = NOW() WHERE id = ?')->execute([$admin['id']]);
    session_regenerate_id(true);
    $_SESSION['admin_id'] = (int)$admin['id'];
    $_SESSION['admin_username'] = (string)$admin['username'];
    $_SESSION['admin_name'] = (string)$admin['full_name'];
    return [true, 'خوش آمدید.'];
}

/** ثبت رویداد در لاگ حسابرسی مدیران */
function ls_audit(int $adminId, string $action, string $entity = '', ?int $entityId = null, string $details = ''): void
{
    ls_db()->prepare(
        'INSERT INTO ls_admin_audit (admin_id, action, entity, entity_id, details, ip_address, created_at)
         VALUES (?, ?, ?, ?, ?, ?, NOW())'
    )->execute([
        $adminId, $action, $entity, $entityId, mb_substr($details, 0, 500),
        substr((string)($_SERVER['REMOTE_ADDR'] ?? ''), 0, 45),
    ]);
}

/* ──────────────────────────  محدودسازی نرخ (Rate Limit)  ────────────────────────── */

/**
 * محدودسازی نرخ درخواست بر اساس شناسه + پنجره زمانی
 * @return bool true = مجاز | false = از حد گذشته
 */
function ls_rate_allow(string $identifier, string $action, int $maxHits, int $windowSec): bool
{
    $db = ls_db();
    $windowStart = (int)(time() / $windowSec) * $windowSec;
    $bucket = hash('sha256', $identifier . '|' . $action . '|' . $windowStart);

    try {
        $stmt = $db->prepare(
            'INSERT INTO ls_rate_limits (bucket, hits, window_start) VALUES (?, 1, ?)
             ON DUPLICATE KEY UPDATE hits = hits + 1');
        $stmt->execute([$bucket, $windowStart]);
    } catch (PDOException $e) {
        return true; // خطای جدول محدودیت نباید سرویس را از کار بیندازد
    }

    $stmt = $db->prepare('SELECT hits FROM ls_rate_limits WHERE bucket = ?');
    $stmt->execute([$bucket]);
    $row = $stmt->fetch();
    return $row === false || (int)$row['hits'] <= $maxHits;
}

/* ──────────────────────────  موتور لایسنس  ────────────────────────── */

/** کاتالوگ ماژول‌ها — یکسان با LicenseAdminService سمت دات‌نت */
function ls_module_catalog(): array
{
    return [
        'Dashboard'     => 'داشبورد',
        'Patients'      => 'بیماران',
        'Appointments'  => 'نوبت‌دهی',
        'Receptions'    => 'پذیرش',
        'Visits'        => 'ویزیت',
        'Prescriptions' => 'نسخه‌نویسی',
        'Billing'       => 'صندوق و فاکتور',
        'Lab'           => 'آزمایشگاه',
        'Pharmacy'      => 'داروخانه',
        'Imaging'       => 'تصویربرداری',
        'Dental'        => 'دندانپزشکی',
        'Eye'           => 'چشم‌پزشکی',
        'Physiotherapy' => 'فیزیوتراپی',
        'Accounting'    => 'حسابداری',
        'HR'            => 'پرسنل',
        'Attendance'    => 'حضور و غیاب',
        'Reports'       => 'گزارش‌ها',
        'Sms'           => 'پیامک',
        'Insurance'     => 'بیمه',
        'Government'    => 'سامانه‌های دولتی',
        'Settings'      => 'تنظیمات',
        'Backup'        => 'بکاپ',
        'Audit'         => 'لاگ عملیات',
        'Users'         => 'کاربران',
    ];
}

/** تولید کلید لایسنس با قالب SCPRO-XXXXX-XXXXX-XXXXX (الفبای بدون ابهام) */
function ls_generate_key(): string
{
    $alphabet = 'ABCDEFGHJKLMNPQRSTUVWXYZ23456789';
    $key = 'SCPRO-';
    for ($group = 0; $group < 3; $group++) {
        for ($i = 0; $i < 5; $i++) {
            $key .= $alphabet[random_int(0, strlen($alphabet) - 1)];
        }
        if ($group < 2) {
            $key .= '-';
        }
    }
    return $key;
}

/** بارگذاری کلید خصوصی RSA از پوشه data */
function ls_private_key(): string
{
    $pem = (string)@file_get_contents(LS_DATA_DIR . '/rsa_private.pem');
    if ($pem === '') {
        throw new RuntimeException('کلید خصوصی RSA یافت نشد.');
    }
    return $pem;
}

/**
 * امضای دیجیتال RSA-2048/SHA256/PKCS1 — سازگار با سمت دات‌نت:
 * C#: rsa.SignData(payload, SHA256, Pkcs1) == PHP: openssl_sign(SHA256)
 * قالب داده امضا شدهده دقیقاً «کلید|انقضای yyyy-MM-dd|حداکثر کاربر» است
 * (تاریخ تهی در لایسنس دائمی به صورت رشته خالی می‌آید).
 */
function ls_sign_license(string $licenseKey, ?string $expiryDate, int $maxUsers): string
{
    $payload = $licenseKey . '|' . ($expiryDate ?? '') . '|' . $maxUsers;
    $sig = '';
    if (!openssl_sign($payload, $sig, ls_private_key(), OPENSSL_ALGO_SHA256)) {
        throw new RuntimeException('خطا در امضای دیجیتال.');
    }
    return base64_encode($sig);
}

/** کلید عمومی به قالب PEM */
function ls_public_key_pem(): string
{
    $pem = (string)@file_get_contents(LS_DATA_DIR . '/rsa_public.pem');
    if ($pem === '') {
        throw new RuntimeException('کلید عمومی یافت نشد.');
    }
    return $pem;
}

/** کلید عمومی به قالب XML سازگار با RSA.FromXmlString در دات‌نت */
function ls_public_key_xml(): string
{
    $res = openssl_pkey_get_public(ls_public_key_pem());
    if ($res === false) {
        throw new RuntimeException('کلید عمومی نامعتبر است.');
    }
    $d = openssl_pkey_get_details($res);
    if ($d === false || empty($d['rsa']['n']) || empty($d['rsa']['e'])) {
        throw new RuntimeException('استخراج مؤلفه‌های کلید ممکن نیست.');
    }
    return '<RSAKeyValue><Modulus>' . base64_encode($d['rsa']['n'])
        . '</Modulus><Exponent>' . base64_encode($d['rsa']['e'])
        . '</Exponent></RSAKeyValue>';
}

/** وضعیت لایسنس را محاسبه می‌کند — بدون تغییر در دیتابیس */
function ls_license_status(array $l): array
{
    // وضعیت متن + کد + رنگ
    if ((int)$l['is_revoked'] === 1) {
        return ['revoked', 'ابطال‌شده', '#e53935'];
    }
    if (!empty($l['expiry_date']) && $l['expiry_date'] < date('Y-m-d')) {
        return ['expired', 'منقضی‌شده', '#fb8c00'];
    }
    if (empty($l['hardware_fingerprint'])) {
        return ['pending', 'در انتظار فعال‌سازی', '#fdd835'];
    }
    return ['active', 'فعال', '#43a047'];
}

/** روزهای باقیمانده انقضا — تهی برای دائمی */
function ls_days_remaining(array $l): ?int
{
    if (empty($l['expiry_date'])) {
        return null;
    }
    $diff = (strtotime((string)$l['expiry_date']) - strtotime(date('Y-m-d'))) / 86400;
    return max(0, (int)$diff);
}

/** ساخت بدنه پاسخ API برای یک لایسنس معتبر (شامل امضا) */
function ls_license_payload(array $l): array
{
    $modules = json_decode((string)$l['enabled_modules'], true);
    return [
        'licenseKey'     => (string)$l['license_key'],
        'licenseType'    => (string)$l['license_type'],
        'issuedDate'     => (string)$l['issued_date'],
        'expiryDate'     => $l['expiry_date'] !== null ? (string)$l['expiry_date'] : null,
        'maxUsers'       => (int)$l['max_users'],
        'maxDoctors'     => (int)$l['max_doctors'],
        'maxPatients'    => (int)$l['max_patients'],
        'maxBranches'    => (int)$l['max_branches'],
        'enabledModules' => is_array($modules) ? array_values($modules) : [],
        'daysRemaining'  => ls_days_remaining($l),
    ];
}

/** ثبت لاگ اعتبارسنجی */
function ls_log_check(?int $licenseId, ?string $licenseKey, string $result, string $source, string $message = ''): void
{
    ls_db()->prepare(
        'INSERT INTO ls_check_logs (license_id, license_key, result, source, message, ip_address, created_at)
         VALUES (?, ?, ?, ?, ?, ?, NOW())'
    )->execute([
        $licenseId, $licenseKey, $result, $source, mb_substr($message, 0, 500),
        substr((string)($_SERVER['REMOTE_ADDR'] ?? ''), 0, 45),
    ]);
}

/**
 * هسته اعتبارسنجی — مشترک بین activate و heartbeat
 * @param bool $bind  true = در اولین تماس اثر انگشت سخت‌افزار متصل می‌شود
 */
function ls_validate_license(string $licenseKey, string $fingerprint, bool $bind): array
{
    $db = ls_db();

    // ۱) جستجوی کلید
    $stmt = $db->prepare('SELECT * FROM ls_licenses WHERE license_key = ? AND is_revoked = 0');
    $stmt->execute([$licenseKey]);
    $l = $stmt->fetch();

    if ($l === false) {
        // شاید ابطال شده باشد — پیام دقیق‌تر
        $stmt2 = $db->prepare('SELECT * FROM ls_licenses WHERE license_key = ? AND is_revoked = 1');
        $stmt2->execute([$licenseKey]);
        $revoked = $stmt2->fetch();
        ls_log_check(null, $licenseKey, $revoked !== false ? 'revoked' : 'not_found', 'online',
            $revoked !== false ? 'لایسنس ابطال شده است' : 'کلید لایسنس یافت نشد');
        return ['isValid' => false, 'status' => $revoked !== false ? 'revoked' : 'not_found',
            'message' => $revoked !== false ? 'این لایسنس ابطال شده است.' : 'کلید لایسنس یافت نشد.'];
    }

    // ۲) انقضا
    if (!empty($l['expiry_date']) && $l['expiry_date'] < date('Y-m-d')) {
        ls_log_check((int)$l['id'], $licenseKey, 'expired', 'online', 'تاریخ انقضای لایسنس گذشته است');
        return ['isValid' => false, 'status' => 'expired', 'message' => 'اعتبار لایسنس به پایان رسیده است.'];
    }

    // ۳) اثر انگشت سخت‌افزار
    $bound = (string)($l['hardware_fingerprint'] ?? '');
    if ($bound === '' && $bind && $fingerprint !== '') {
        // اولین فعال‌سازی — اتصال اثر انگشت به این کلید
        $actCode = 'ACT-' . strtoupper(bin2hex(random_bytes(5)));
        $db->prepare(
            'UPDATE ls_licenses SET hardware_fingerprint = ?, activation_code = ?,
             activation_count = activation_count + 1, last_validated_at = NOW(),
             last_heartbeat_at = NOW() WHERE id = ?'
        )->execute([$fingerprint, $actCode, $l['id']]);
        $db->prepare(
            'INSERT INTO ls_activations (license_id, hardware_fingerprint, clinic_name, app_version, ip_address, activated_at, last_seen_at, is_active)
             VALUES (?, ?, ?, ?, ?, NOW(), NOW(), 1)
             ON DUPLICATE KEY UPDATE last_seen_at = NOW(), is_active = 1'
        )->execute([
            $l['id'], $fingerprint,
            mb_substr((string)($_REQUEST['clinicName'] ?? ''), 0, 200),
            mb_substr((string)($_REQUEST['appVersion'] ?? ''), 0, 20),
            substr((string)($_SERVER['REMOTE_ADDR'] ?? ''), 0, 45),
        ]);
        $l['hardware_fingerprint'] = $fingerprint;
        $l['activation_code'] = $actCode;
        ls_log_check((int)$l['id'], $licenseKey, 'valid', 'online', 'فعال‌سازی موفق روی دستگاه جدید');
    } elseif ($bound !== '' && !hash_equals($bound, $fingerprint)) {
        // اثر انگشت متفاوت — لایسنس به دستگاه دیگری تعلق دارد
        ls_log_check((int)$l['id'], $licenseKey, 'fingerprint_mismatch', 'online',
            'اثر انگشت نامطبق — ورودی: ' . substr($fingerprint, 0, 16) . '…');
        return ['isValid' => false, 'status' => 'fingerprint_mismatch',
            'message' => 'این لایسنس به دستگاه دیگری تعلق دارد.'];
    } else {
        // تپ قلب یا اعتبارسنجی مجدد
        $db->prepare('UPDATE ls_licenses SET last_validated_at = NOW(), last_heartbeat_at = NOW() WHERE id = ?')
            ->execute([$l['id']]);
        $db->prepare('UPDATE ls_activations SET last_seen_at = NOW(), is_active = 1
            WHERE license_id = ? AND hardware_fingerprint = ?')->execute([$l['id'], $fingerprint]);
        ls_log_check((int)$l['id'], $licenseKey, 'valid', 'online', $bind ? 'فعال‌سازی مجدد روی دستگاه ثبت‌شده' : 'تپ قلب موفق');
    }

    // ۴) پاسخ موفق با امضای دیجیتال
    return [
        'isValid'    => true,
        'status'     => 'valid',
        'message'    => 'لایسنس معتبر است.',
        'serverTime' => gmdate('Y-m-d\TH:i:s\Z'),
        'license'    => ls_license_payload($l),
        'signature'  => ls_sign_license(
            (string)$l['license_key'],
            $l['expiry_date'] !== null ? (string)$l['expiry_date'] : null,
            (int)$l['max_users']
        ),
    ];
}

/** تولید محتوای فایل .lic برای فعال‌سازی آفلاین — JSON امضاشده */
function ls_build_lic_file(array $l, string $fingerprint): string
{
    $payload = ls_license_payload($l);
    $lic = $payload;
    $lic['formatVersion'] = 1;
    $lic['hardwareFingerprint'] = $fingerprint;
    $lic['signatureAlgorithm'] = 'SHA256withRSA';
    $lic['signature'] = ls_sign_license(
        (string)$l['license_key'],
        $l['expiry_date'] !== null ? (string)$l['expiry_date'] : null,
        (int)$l['max_users']
    );
    return json_encode($lic, JSON_PRETTY_PRINT | JSON_UNESCAPED_UNICODE | JSON_UNESCAPED_SLASHES);
}

/* ──────────────────────────  ابزارهای UI  ────────────────────────── */

/** پاکسازی خروجی HTML */
function ls_e(?string $s): string
{
    return htmlspecialchars((string)$s, ENT_QUOTES, 'UTF-8');
}

/** آدرس IP کاربر */
function ls_ip(): string
{
    return substr((string)($_SERVER['REMOTE_ADDR'] ?? '-'), 0, 45);
}

/** نمایش مبلغ به ریال با جداکننده */
function ls_rial($n): string
{
    return number_format((float)$n);
}
