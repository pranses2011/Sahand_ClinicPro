<?php
/**
 * API عمومی سرور لایسنس Sahand ClinicPro — نقطه تماس کلاینت دات‌نت
 * بدون احراز هویت نشست — لایسنس با کلید + اثر انگشت سخت‌افزار احراز می‌شود.
 *
 * اقدام‌ها (POST با بدنه JSON):
 *   action=activate  { licenseKey, fingerprint, clinicName?, appVersion? }
 *   action=heartbeat { licenseKey, fingerprint, appVersion? }
 *   action=ping      (GET یا POST) — بررسی سلامت
 *
 * پاسخ موفق:
 *   { isValid, status:"valid", message, serverTime, license:{...}, signature }
 * پاسخ خطا:
 *   { isValid:false, status:"not_found|expired|revoked|fingerprint_mismatch|rate_limited", message }
 *
 * امضا: RSA-2048/SHA256/PKCS1 روی رشته «کلید|انقضا yyyy-MM-dd|حداکثر کاربر»
 *
 * @license MIT
 */

declare(strict_types=1);

require __DIR__ . '/inc/functions.php';
ls_load_config();

header('X-Frame-Options: DENY');
header('X-Content-Type-Options: nosniff');
header('Referrer-Policy: no-referrer');

/* ─────────  ورودی  ───────── */

/*
 * ═══ ریشه‌یابی (چرخهٔ ۸۰): «heartbeat با ?action= در URL همیشه 400 می‌داد» ═══
 * پیش‌تر بدنهٔ JSON فقط وقتی خوانده می‌شد که action در query string نبود. اگر
 * فراخواننده action را در URL می‌گذاشت و پارامترها را در بدنهٔ JSON می‌فرستاد
 * (حالت رایج HttpClient/curl)، licenseKey و fingerprint خالی می‌ماند و پاسخ
 * ۴۰۰ «قالب کلید لایسنس نامعتبر است» بود — نه ۲۰۰ با بلوک messages. اکنون
 * بدنهٔ JSON همیشه خوانده می‌شود و با query string ادغام می‌گردد (بدنه اولویت
 * دارد). این هم مسیر مستند (بدنهٔ JSON) و هم مسیر query را پشتیبانی می‌کند.
 */
$raw     = (string)file_get_contents('php://input');
$decoded = json_decode($raw, true);
$json    = is_array($decoded) ? $decoded : [];

$action = (string)($_GET['action'] ?? '');
if ($action === '') {
    if ($raw !== '' && $decoded === null) {
        ls_json_out(['isValid' => false, 'status' => 'bad_request',
            'message' => 'بدنه درخواست باید JSON معتبر باشد.'], 400);
    }
    $action = (string)($json['action'] ?? '');
}
// پارامترها: query string + بدنهٔ JSON (بدنه اولویت دارد)
$_REQUEST = array_merge($_GET, $json);

/* ─────────  اقدام: پینگ — بدون Rate Limit  ───────── */

if ($action === 'ping') {
    ls_json_out([
        'ok'        => true,
        'status'    => 'pong',
        'server'    => 'Sahand ClinicPro License Server',
        'version'   => LS_VERSION,
        'serverTime'=> gmdate('Y-m-d\TH:i:s\Z'),
    ]);
}

/* ─────────  اقدام: support — اطلاعات پشتیبانی عمومی (چرخه هجدهم)
   گزارش دوازدهم مالک: صفحهٔ فعال‌سازی لایسنس دسکتاپ «بصورت پیش‌فرض اطلاعات
   پشتیبانی موجود باشه و از دفعات بعد بروزرسانی از سرور لایسنس رو انجام بده».
   بدون احراز هویت و بدون Rate Limit (مثل ping) — فقط اطلاعات تماس عمومی است.  ───────── */

if ($action === 'support') {
    ls_json_out([
        'ok'      => true,
        'status'  => 'support',
        'support' => ls_support_info(),
    ]);
}

/* ─────────  محدودسازی نرخ — ۶۰ درخواست در هر ۱۰ دقیقه برای هر IP  ───────── */

$maxHits = (int)(ls_setting('api_rate_max', '60') ?: 60);
$window = (int)(ls_setting('api_rate_window', '600') ?: 600);
if (!ls_rate_allow(ls_ip(), 'api', $maxHits, $window)) {
    ls_log_check(null, (string)($_REQUEST['licenseKey'] ?? ''), 'rate_limited', 'online',
        'تعداد درخواست‌ها بیش از حد مجاز');
    ls_json_out(['isValid' => false, 'status' => 'rate_limited',
        'message' => 'تعداد درخواست‌ها بیش از حد مجاز است — کمی بعد دوباره تلاش کنید.',
        'retryAfterSeconds' => $window], 429);
}

/* ─────────  اعتبارسنجی ورودی  ───────── */

$licenseKey = strtoupper(trim((string)($_REQUEST['licenseKey'] ?? '')));
$fingerprint = trim((string)($_REQUEST['fingerprint'] ?? ''));

if (!preg_match('/^SCPRO-[A-Z2-9]{5}-[A-Z2-9]{5}-[A-Z2-9]{5}$/', $licenseKey)) {
    ls_json_out(['isValid' => false, 'status' => 'bad_request',
        'message' => 'قالب کلید لایسنس نامعتبر است. (قالب صحیح: SCPRO-XXXXX-XXXXX-XXXXX)'], 400);
}
if (strlen($fingerprint) < 16 || strlen($fingerprint) > 150) {
    ls_json_out(['isValid' => false, 'status' => 'bad_request',
        'message' => 'اثر انگشت سخت‌افزار نامعتبر است.'], 400);
}

/* ─────────  اجرای اقدام  ───────── */

if ($action === 'activate' || $action === 'heartbeat') {
    try {
        $result = ls_validate_license($licenseKey, $fingerprint, $action === 'activate');
        // چرخه دوازدهم — پیام‌های مستقیم پشتیبانی به داشبورد درمانگاه:
        // داخل ls_validate_license به پاسخ موفق افزوده می‌شوند (آنجا شناسه لایسنس در دسترس است)
        // و کلاینت دسکتاپ آن‌ها را در کارت «پیام‌های پشتیبانی» داشبورد نمایش می‌دهد.
        ls_json_out($result, $result['isValid'] ? 200 : 403);
    } catch (Throwable $e) {
        ls_log_error('api:' . $action, $e);
        ls_json_out(['isValid' => false, 'status' => 'server_error',
            'message' => 'خطای داخلی سرور — بعداً تلاش کنید.'], 500);
    }
}

ls_json_out(['isValid' => false, 'status' => 'bad_request',
    'message' => 'action نامعتبر است. مقادیر مجاز: activate | heartbeat | ping'], 400);
