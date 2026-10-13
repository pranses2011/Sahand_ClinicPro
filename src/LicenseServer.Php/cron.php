<?php
/**
 * کار زمان‌بندی‌شده نگهداشت — اجرا از cPanel → Cron Jobs (توصیه: روزانه ۳:۳۰ بامداد)
 * نمونه دستور در cPanel:
 *   php /home/USER/public_html/license/cron.php SECRET
 * مقدار SECRET از فایل data/config.php (LS_CRON_SECRET) خوانده می‌شود.
 *
 * کارها:
 *   ۱) پاک‌سازی پنجره‌های محدودیت نرخ قدیمی (بیش از ۲۴ ساعت)
 *   ۲) حذف لاگ‌های اعتبارسنجی کهنسال (بیش از ۹۰ روز)
 *   ۳) حذف لاگ حسابرسی کهنسال (بیش از ۱۸۰ روز)
 *   ۴) بکاپ خودکار کامل بانک MySQL — چرخهٔ ۶۶ (G-08: ریسک «از دست رفتن
 *      دادهٔ لایسنس مشتریان»)؛ mysqldump بومی یا فروکش PDO، فشردهٔ gzip در
 *      data/backups + چرخش نسخه‌ها (نگه‌داشت LS_BACKUP_KEEP=۱۴ نسخهٔ آخر).
 *      خلاصهٔ بکاپ در خروجی JSON گزارش می‌شود تا در ایمیل کرون cPanel دیده شود.
 *
 * @license MIT
 */

declare(strict_types=1);

require __DIR__ . '/inc/functions.php';

// احراز فراخوانی با کلید رمزی
$secret = (string)($_SERVER['argv'][1] ?? $_GET['secret'] ?? '');
if (!defined('LS_CRON_SECRET') || $secret === '' || !hash_equals(LS_CRON_SECRET, $secret)) {
    http_response_code(403);
    exit('Forbidden');
}

$db = ls_db();

// ۱) پاک‌سازی پنجره‌های نرخ قدیمی‌تر از ۲۴ ساعت
$deleted1 = $db->exec('DELETE FROM ls_rate_limits WHERE window_start < ' . (time() - 86400));

// ۲) حذف لاگ‌های اعتبارسنجی کهنسال‌تر از ۹۰ روز
$deleted2 = $db->exec('DELETE FROM ls_check_logs WHERE created_at < DATE_SUB(NOW(), INTERVAL 90 DAY)');

// ۳) حذف لاگ حسابرسی کهنسال‌تر از ۱۸۰ روز
$deleted3 = $db->exec('DELETE FROM ls_admin_audit WHERE created_at < DATE_SUB(NOW(), INTERVAL 180 DAY)');

// ۴) بکاپ خودکار بانک — چرخهٔ ۶۶؛ adminId تهی = اجرای زمان‌بندی‌شده
//    (حسابرسی با مدیر ۰ ثبت می‌شود تا در پنل قابل تمیز دادن از بکاپ دستی باشد)
$backup = ls_backup_database(null);
if (!$backup['ok']) {
    // شکست بکاپ باید در لاگ خطا هم ثبت شود تا از ایمیل کرون و پنل هر دو دیده شود
    ls_log_error_string('cron:backup', (string)($backup['error'] ?? 'خطای ناشناخته'));
}

echo json_encode([
    'ok' => true,
    'time' => date('c'),
    'cleaned' => [
        'rate_limits' => $deleted1,
        'check_logs'  => $deleted2,
        'admin_audit' => $deleted3,
    ],
    // خلاصهٔ بکاپ — ok=false با پیام خطا هم گزارش می‌شود (کرون نباید بی‌صدا بمیرد)
    'backup' => [
        'ok'     => (bool)$backup['ok'],
        'file'   => (string)$backup['file'],
        'size'   => (int)$backup['size'],
        'method' => (string)$backup['method'],
        'kept'   => (int)$backup['kept'],
        'pruned' => (int)$backup['pruned'],
        'error'  => $backup['error'] ?? null,
    ],
], JSON_UNESCAPED_UNICODE) . PHP_EOL;
