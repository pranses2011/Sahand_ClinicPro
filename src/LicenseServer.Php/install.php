<?php
/**
 * نصب‌کننده وب سرور لایسنس Sahand ClinicPro — مخصوص هاست اشتراکی cPanel
 * نصب ۳ مرحله‌ای: بررسی پیش‌نیازها ← اطلاعات دیتابیس ← حساب مدیر + ساخت کلیدها
 * پس از نصب، فایل install.php را حذف کنید (نصب‌کننده خودش هم قفل می‌شود).
 *
 * @license MIT
 */

declare(strict_types=1);

error_reporting(E_ALL);
ini_set('display_errors', '1');

define('LS_ROOT', __DIR__);
define('LS_DATA_DIR', __DIR__ . '/data');
define('LS_CONFIG_FILE', LS_DATA_DIR . '/config.php');
define('LS_INSTALL_LOCK', LS_DATA_DIR . '/install.lock');
define('LS_VERSION', '1.0.0');

date_default_timezone_set('Asia/Tehran');

/** فرار خروجی HTML — مستقل از functions.php چون در مرحله نصب هنوز پیکربندی وجود ندارد */
function ls_e2(?string $s): string
{
    return htmlspecialchars((string)$s, ENT_QUOTES, 'UTF-8');
}

/* ─────────  قفل نصب — پس از نصب موفق دیگر اجرا نمی‌شود  ───────── */
if (is_file(LS_INSTALL_LOCK)) {
    http_response_code(403);
    header('Content-Type: text/html; charset=utf-8');
    echo '<!doctype html><html lang="fa" dir="rtl"><meta charset="utf-8">';
    echo '<body style="font-family:Tahoma;text-align:center;padding:60px;background:#f5f5f5">';
    echo '<h2 style="color:#43a047">✅ نصب قبلاً کامل شده است</h2>';
    echo '<p>برای امنیت، این فایل را از هاست حذف کنید: <code>install.php</code></p>';
    echo '<p><a href="index.php">ورود به پنل مدیریت ←</a></p></body></html>';
    exit;
}

/* ─────────  ابزارهای نصب  ───────── */

/** بررسی پیش‌نیازهای میزبانی */
function inst_requirements(): array
{
    $checks = [];
    $checks[] = ['نسخه PHP (حداقل ۷٫۴)', version_compare(PHP_VERSION, '7.4.0', '>='), PHP_VERSION];
    $checks[] = ['افزونه PDO MySQL', extension_loaded('pdo_mysql'), extension_loaded('pdo_mysql') ? 'فعال' : 'غیرفعال'];
    $checks[] = ['افزونه OpenSSL (تولید کلید RSA)', extension_loaded('openssl'), extension_loaded('openssl') ? 'فعال' : 'غیرفعال'];
    $checks[] = ['افزونه JSON', extension_loaded('json'), extension_loaded('json') ? 'فعال' : 'غیرفعال'];
    $checks[] = ['افزونه mbstring (پشتیبانی فارسی)', extension_loaded('mbstring'), extension_loaded('mbstring') ? 'فعال' : 'اختیاری — توصیه می‌شود'];
    $checks[] = ['افزونه session', extension_loaded('session'), extension_loaded('session') ? 'فعال' : 'غیرفعال'];
    $writableData = is_writable(__DIR__) || (is_dir(LS_DATA_DIR) && is_writable(LS_DATA_DIR));
    $checks[] = ['قابلیت نوشتن در پوشه data', $writableData, $writableData ? 'قابل نوشتن' : 'پوشه data بسازید و مجوز ۷۵۵ بدهید'];

    $allOk = true;
    foreach ($checks as $c) {
        // mbstring اختیاری است — strpos بایتی برای یافتن واژه ASCII امن است
        if (!$c[1] && strpos($c[0], 'mbstring') === false) {
            $allOk = false;
        }
    }
    return ['checks' => $checks, 'allOk' => $allOk];
}

/** اجرای فایل اسکیما روی اتصال داده‌شده */
function inst_run_schema(PDO $pdo): void
{
    $sql = (string)file_get_contents(__DIR__ . '/inc/schema.sql');
    // حذف کامنت‌ها و اجرای دستورها یکی‌یکی
    $sql = preg_replace('/^\s*--.*$/m', '', $sql);
    $statements = array_filter(array_map('trim', explode(';', $sql)));
    foreach ($statements as $stmt) {
        if ($stmt !== '') {
            $pdo->exec($stmt);
        }
    }
}

/** نوشتن فایل پیکربندی */
function inst_write_config(string $host, int $port, string $name, string $user, string $pass, string $cronSecret): void
{
    if (!is_dir(LS_DATA_DIR)) {
        mkdir(LS_DATA_DIR, 0755, true);
    }
    $tpl = '<?php
/**
 * پیکربندی سرور لایسنس — توسط install.php تولید شده (' . date('Y-m-d H:i') . ')
 * این فایل محرمانه است — هرگز آن را به اشتراک نگذارید.
 */
declare(strict_types=1);
define("LS_DB_HOST", ' . var_export($host, true) . ');
define("LS_DB_PORT", ' . $port . ');
define("LS_DB_NAME", ' . var_export($name, true) . ');
define("LS_DB_USER", ' . var_export($user, true) . ');
define("LS_DB_PASS", ' . var_export($pass, true) . ');
define("LS_CRON_SECRET", ' . var_export($cronSecret, true) . ');
';
    file_put_contents(LS_CONFIG_FILE, $tpl, LOCK_EX);
    @chmod(LS_CONFIG_FILE, 0640);
}

/** نوشتن فایل‌های محافظ htaccess */
function inst_write_protection(): void
{
    $deny = "# دسترسی مستقیم وب به این پوشه ممنوع است\n"
        . "<IfModule mod_authz_core.c>\n  Require all denied\n</IfModule>\n"
        . "<IfModule !mod_authz_core.c>\n  Order allow,deny\n  Deny from all\n</IfModule>\n";
    if (!is_dir(LS_DATA_DIR)) {
        mkdir(LS_DATA_DIR, 0755, true);
    }
    file_put_contents(LS_DATA_DIR . '/.htaccess', $deny);
    if (is_dir(__DIR__ . '/inc')) {
        file_put_contents(__DIR__ . '/inc/.htaccess', $deny);
    }
}

/** تولید و ذخیره جفت‌کلید RSA-2048 */
function inst_generate_keys(): void
{
    $res = openssl_pkey_new([
        'private_key_bits' => 2048,
        'private_key_type' => OPENSSL_KEYTYPE_RSA,
    ]);
    if ($res === false) {
        throw new RuntimeException('تولید کلید RSA ممکن نیست — OpenSSL را بررسی کنید.');
    }
    $private = '';
    openssl_pkey_export($res, $private);
    $details = openssl_pkey_get_details($res);
    file_put_contents(LS_DATA_DIR . '/rsa_private.pem', $private, LOCK_EX);
    file_put_contents(LS_DATA_DIR . '/rsa_public.pem', $details['key'], LOCK_EX);
    @chmod(LS_DATA_DIR . '/rsa_private.pem', 0640);
}

/** نمایش HTML رابط نصب */
function inst_page(string $title, string $body, string $footer = ''): void
{
    echo '<!doctype html><html lang="fa" dir="rtl"><head><meta charset="utf-8">';
    echo '<meta name="viewport" content="width=device-width, initial-scale=1">';
    echo '<title>نصب سرور لایسنس — ' . ls_e2($title) . '</title>';
    echo '<link rel="stylesheet" href="assets/style.css">';
    echo '</head><body class="inst">';
    echo '<div class="inst-box"><h1>🔑 نصب سرور لایسنس <span class="brand">Sahand ClinicPro</span></h1>';
    echo '<div class="inst-body">' . $body . '</div>' . $footer . '</div></body></html>';
}

/* ─────────  جریان نصب  ───────── */

$step = $_POST['step'] ?? $_GET['step'] ?? 'requirements';

try {
    if ($step === 'requirements') {
        $r = inst_requirements();
        $body = '<h2>مرحله ۱ از ۳ — بررسی پیش‌نیازها</h2><table class="table">';
        foreach ($r['checks'] as $c) {
            $icon = $c[1] ? '✅' : '❌';
            $body .= '<tr><td>' . $icon . ' ' . $c[0] . '</td><td>' . $c[2] . '</td></tr>';
        }
        $body .= '</table>';
        if ($r['allOk']) {
            $body .= '<form method="post" action="install.php">'
                . '<input type="hidden" name="step" value="database">'
                . '<button class="btn btn-primary" type="submit">ادامه ← اطلاعات دیتابیس</button></form>';
        } else {
            $body .= '<div class="alert alert-danger">برخی پیش‌نیازهای الزامی برآورده نشده‌اند. '
                . 'از میزبان خود بخواهید آن‌ها را فعال کند (cPanel → Select PHP Version).</div>';
        }
        inst_page('پیش‌نیازها', $body);
        exit;
    }

    if ($step === 'database') {
        // مرحله ۲ — دریافت اطلاعات دیتابیس
        if ($_SERVER['REQUEST_METHOD'] !== 'POST') {
            header('Location: install.php');
            exit;
        }
        inst_page('اطلاعات دیتابیس', '
        <h2>مرحله ۲ از ۳ — اطلاعات دیتابیس MySQL</h2>
        <div class="alert alert-info">ابتدا در cPanel یک دیتابیس MySQL و کاربر بسازید و کاربر را به دیتابیس '
        . 'با دسترسی کامل اضافه کنید (cPanel → MySQL® Databases). سپس اطلاعات را وارد کنید.</div>
        <form method="post" action="install.php" autocomplete="off">
            <input type="hidden" name="step" value="admin">
            <label>میزبان دیتابیس <span class="req">*</span>
                <input type="text" name="db_host" value="localhost" required></label>
            <label>پورت <input type="number" name="db_port" value="3306"></label>
            <label>نام دیتابیس <span class="req">*</span>
                <input type="text" name="db_name" placeholder="cpuser_licsrv" required></label>
            <label>نام کاربری دیتابیس <span class="req">*</span>
                <input type="text" name="db_user" placeholder="cpuser_licuser" required></label>
            <label>رمز عبور دیتابیس <span class="req">*</span>
                <input type="password" name="db_pass" required></label>
            <button class="btn btn-primary" type="submit">بررسی اتصال و ادامه ←</button>
        </form>');
        exit;
    }

    if ($step === 'admin') {
        if ($_SERVER['REQUEST_METHOD'] !== 'POST') {
            header('Location: install.php');
            exit;
        }

        // ۱ — آزمایش اتصال دیتابیس
        $host = trim((string)$_POST['db_host']);
        $port = (int)($_POST['db_port'] ?: 3306);
        $name = trim((string)$_POST['db_name']);
        $user = trim((string)$_POST['db_user']);
        $pass = (string)$_POST['db_pass'];
        try {
            $pdo = new PDO(
                "mysql:host=$host;port=$port;dbname=$name;charset=utf8mb4",
                $user, $pass,
                [PDO::ATTR_ERRMODE => PDO::ERRMODE_EXCEPTION, PDO::ATTR_EMULATE_PREPARES => false]
            );
        } catch (PDOException $e) {
            $back = '<a class="btn" href="javascript:history.back()">→ بازگشت</a>';
            inst_page('خطای اتصال',
                '<div class="alert alert-danger">اتصال به دیتابیس برقرار نشد:<br><code>'
                . ls_e2($e->getMessage()) . '</code></div>' . $back);
            exit;
        }

        // ۲ — فرم حساب مدیر
        inst_page('حساب مدیر', '
        <h2>مرحله ۳ از ۳ — حساب مدیر و نهایی‌سازی</h2>
        <div class="alert alert-success">✅ اتصال به دیتابیس موفق بود.</div>
        <form method="post" action="install.php" autocomplete="off">
            <input type="hidden" name="step" value="finish">
            <input type="hidden" name="db_host" value="' . ls_e2($host) . '">
            <input type="hidden" name="db_port" value="' . $port . '">
            <input type="hidden" name="db_name" value="' . ls_e2($name) . '">
            <input type="hidden" name="db_user" value="' . ls_e2($user) . '">
            <input type="hidden" name="db_pass" value="' . ls_e2($pass) . '">
            <label>عنوان پنل <input type="text" name="portal_title" value="سامانه لایسنس درمانگاه"></label>
            <label>نام نمایشی مدیر <span class="req">*</span>
                <input type="text" name="admin_name" value="مدیر سیستم" required></label>
            <label>نام کاربری مدیر <span class="req">*</span>
                <input type="text" name="admin_user" value="admin" required minlength="3"></label>
            <label>رمز عبور مدیر <span class="req">*</span>
                <input type="password" name="admin_pass" required minlength="8"></label>
            <div class="hint">حداقل ۸ کاراکتر — ترکیب حرف بزرگ/کوچک + عدد + علامت توصیه می‌شود.</div>
            <button class="btn btn-primary" type="submit">🏁 اجرای نصب نهایی</button>
        </form>');
        exit;
    }

    if ($step === 'finish') {
        if ($_SERVER['REQUEST_METHOD'] !== 'POST') {
            header('Location: install.php');
            exit;
        }

        // ۱ — اتصال مجدد
        $host = trim((string)$_POST['db_host']);
        $port = (int)($_POST['db_port'] ?: 3306);
        $name = trim((string)$_POST['db_name']);
        $user = trim((string)$_POST['db_user']);
        $pass = (string)$_POST['db_pass'];
        $pdo = new PDO(
            "mysql:host=$host;port=$port;dbname=$name;charset=utf8mb4",
            $user, $pass,
            [PDO::ATTR_ERRMODE => PDO::ERRMODE_EXCEPTION, PDO::ATTR_EMULATE_PREPARES => false]
        );

        // ۲ — ساخت جداول
        inst_run_schema($pdo);

        // ۳ — حساب مدیر
        $adminUser = trim((string)$_POST['admin_user']);
        $adminName = trim((string)$_POST['admin_name']);
        $adminPass = (string)$_POST['admin_pass'];
        $userLen = function_exists('mb_strlen') ? mb_strlen($adminUser) : strlen($adminUser);
        if ($userLen < 3 || strlen($adminPass) < 8) {
            throw new RuntimeException('نام کاربری حداقل ۳ و رمز عبور حداقل ۸ کاراکتر باشد.');
        }
        $stmt = $pdo->prepare('SELECT COUNT(*) FROM ls_admins WHERE username = ?');
        $stmt->execute([$adminUser]);
        if ((int)$stmt->fetchColumn() > 0) {
            throw new RuntimeException('این نام کاربری قبلاً ثبت شده است.');
        }
        $pdo->prepare('INSERT INTO ls_admins (username, password_hash, full_name, is_active, created_at)
             VALUES (?, ?, ?, 1, NOW())')
            ->execute([$adminUser, password_hash($adminPass, PASSWORD_DEFAULT), $adminName]);

        // ۴ — تنظیمات پیش‌فرض
        $title = trim((string)($_POST['portal_title'] ?: 'سامانه لایسنس درمانگاه'));
        $pdo->prepare('INSERT INTO ls_settings (skey, svalue) VALUES (?, ?)
             ON DUPLICATE KEY UPDATE svalue = VALUES(svalue)')
            ->execute(['portal_title', $title]);
        foreach ([['api_rate_max', '60'], ['api_rate_window', '600'], ['offline_grace_days', '30']] as $s) {
            $pdo->prepare('INSERT INTO ls_settings (skey, svalue) VALUES (?, ?)
                 ON DUPLICATE KEY UPDATE svalue = VALUES(svalue)')->execute($s);
        }

        // ۵ — تولید کلیدهای RSA
        inst_generate_keys();

        // ۶ — نوشتن پیکربندی + قفل نصب + محافظت پوشه‌ها
        inst_write_config($host, $port, $name, $user, $pass,
            bin2hex(random_bytes(24)));
        inst_write_protection();
        file_put_contents(LS_INSTALL_LOCK, 'installed ' . date('c'));

        // ۷ — نمایش کلید عمومی برای تنظیم سمت کلاینت دات‌نت
        require __DIR__ . '/inc/functions.php';
        $pem = ls_public_key_pem();
        $xml = ls_public_key_xml();
        $scheme = (!empty($_SERVER['HTTPS']) && $_SERVER['HTTPS'] !== 'off') ? 'https' : 'http';
        $portalUrl = $scheme . '://' . ($_SERVER['HTTP_HOST'] ?? 'your-domain.com')
            . rtrim(dirname($_SERVER['SCRIPT_NAME'] ?? '/'), '/\\');

        $body = '<h2>🎉 نصب با موفقیت کامل شد</h2>
        <div class="alert alert-success">همه جداول ساخته شد، حساب مدیر ثبت شد و جفت‌کلید RSA-2048 تولید شد.</div>
        <h3>⚙️ تنظیم سمت کلاینت (.NET) — فایل appsettings.json</h3>
        <pre class="code">' . ls_e2(json_encode([
            'License' => [
                'PortalUrl' => $portalUrl . '/api.php',
                'HardwareFingerprint' => 'اثر انگشت هر کلاینت',
                'RsaPublicKey' => 'بخش XML زیر را اینجا بگذارید (یا قالب PEM)',
            ],
        ], JSON_PRETTY_PRINT | JSON_UNESCAPED_SLASHES | JSON_UNESCAPED_UNICODE)) . '</pre>
        <h3>🗝️ کلید عمومی PEM</h3>
        <pre class="code">' . ls_e2($pem) . '</pre>
        <h3>🗝️ کلید عمومی XML (برای FromXmlString در دات‌نت)</h3>
        <pre class="code">' . ls_e2($xml) . '</pre>
        <div class="alert alert-warning">⚠️ اکنون فایل <code>install.php</code> را از هاست حذف کنید.</div>
        <a class="btn btn-primary" href="index.php">ورود به پنل مدیریت ←</a>';
        inst_page('پایان نصب', $body);
        exit;
    }
} catch (Throwable $e) {
    inst_page('خطای نصب',
        '<div class="alert alert-danger">خطا در نصب:<br><code>' . ls_e2($e->getMessage())
        . '</code></div><a class="btn" href="install.php">→ شروع مجدد</a>');
    exit;
}
