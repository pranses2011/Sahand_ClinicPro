<?php
/**
 * پنل مدیریت سرور لایسنس Sahand ClinicPro — PHP خالص فارسی RTL
 * صفحات: ورود، داشبورد، لایسنس‌ها (صدور/تمدید/ابطال/آفلاین)، لاگ‌ها، تنظیمات
 * امنیت: نشست امن + CSRF روی همه فرم‌ها + قفل ۵ خطا + لاگ حسابرسی مدیران
 *
 * @license MIT
 */

declare(strict_types=1);

require __DIR__ . '/inc/functions.php';
ls_load_config();
ls_session_start();

$page = (string)($_GET['page'] ?? 'dashboard');
$admin = ls_admin();

/** هدایت پس از فرم — یا ریدایرکت یا پاسخ JSON برای فرم‌های ایجکس */
function ls_json_or_redirect(string $to): void
{
    if (!empty($_SERVER['HTTP_X_REQUESTED_WITH'])
        && strtolower($_SERVER['HTTP_X_REQUESTED_WITH']) === 'xmlhttprequest') {
        header('Content-Type: application/json; charset=utf-8');
        echo json_encode(['redirect' => $to], JSON_UNESCAPED_SLASHES);
        exit;
    }
    header('Location: ' . $to);
    exit;
}

/* ═══════════════════════  پردازش فرم‌های POST  ═══════════════════════ */

if ($_SERVER['REQUEST_METHOD'] === 'POST') {
    ls_csrf_check();
    $act = (string)($_POST['action'] ?? '');

    /* ── ورود ── */
    if ($act === 'login') {
        [$ok, $msg] = ls_admin_login(trim((string)$_POST['username']), (string)$_POST['password']);
        $_SESSION['flash'] = [$ok ? 'success' : 'danger', $msg];
        ls_json_or_redirect($ok ? 'index.php?page=dashboard' : 'index.php?page=login');
    }

    /* ── بقیه اقدام‌ها نیازمند ورود ── */
    if ($admin === null) {
        header('Location: index.php?page=login');
        exit;
    }

    if ($act === 'create_license') {
        $type = ($_POST['license_type'] ?? '1') === '2' ? 'perpetual' : 'subscription';
        $months = max(1, min(60, (int)($_POST['duration_months'] ?? 12)));
        $modules = array_values(array_intersect(
            array_keys(ls_module_catalog()),
            (array)($_POST['modules'] ?? [])
        ));
        if (count($modules) === 0) {
            $modules = ['Dashboard', 'Patients', 'Appointments', 'Visits', 'Prescriptions', 'Billing', 'Settings', 'Users'];
        }
        // تولید کلید یکتا
        $db = ls_db();
        for ($try = 0; $try < 8; $try++) {
            $key = ls_generate_key();
            $stmt = $db->prepare('SELECT COUNT(*) FROM ls_licenses WHERE license_key = ?');
            $stmt->execute([$key]);
            if ((int)$stmt->fetchColumn() === 0) {
                break;
            }
        }
        $expiry = $type === 'perpetual' ? null : date('Y-m-d', strtotime('+' . $months . ' months'));
        $db->prepare(
            'INSERT INTO ls_licenses
             (license_key, license_type, clinic_name, clinic_phone, max_users, max_doctors,
              max_patients, max_branches, enabled_modules, issued_date, expiry_date, notes, created_at, updated_at)
             VALUES (?,?,?,?,?,?,?,?,?,?,?,?,?,?,NOW(),NOW())'
        )->execute([
            $key, $type,
            mb_substr(trim((string)($_POST['clinic_name'] ?? '')), 0, 200) ?: null,
            mb_substr(trim((string)($_POST['clinic_phone'] ?? '')), 0, 30) ?: null,
            max(1, (int)($_POST['max_users'] ?? 5)),
            max(1, (int)($_POST['max_doctors'] ?? 10)),
            max(1, (int)($_POST['max_patients'] ?? 5000)),
            max(1, (int)($_POST['max_branches'] ?? 1)),
            json_encode($modules, JSON_UNESCAPED_SLASHES),
            date('Y-m-d'), $expiry,
            mb_substr(trim((string)($_POST['notes'] ?? '')), 0, 1000) ?: null,
        ]);
        $newId = (int)$db->lastInsertId();
        ls_audit($admin['id'], 'issue_license', 'license', $newId,
            'صدور ' . ($type === 'perpetual' ? 'دائمی' : 'اشتراکی ' . $months . ' ماهه') . ' — ' . $key);
        $_SESSION['flash'] = ['success', 'لایسنس «' . $key . '» صادر شد.'];
        ls_json_or_redirect('index.php?page=license_view&id=' . $newId);
    }

    if ($act === 'renew') {
        $id = (int)($_POST['id'] ?? 0);
        $months = max(1, min(60, (int)($_POST['months'] ?? 12)));
        $stmt = ls_db()->prepare('SELECT * FROM ls_licenses WHERE id = ? AND is_revoked = 0');
        $stmt->execute([$id]);
        $l = $stmt->fetch();
        if ($l !== false) {
            $base = (!empty($l['expiry_date']) && $l['expiry_date'] > date('Y-m-d'))
                ? (string)$l['expiry_date'] : date('Y-m-d');
            $newExpiry = date('Y-m-d', strtotime($base . ' +' . $months . ' months'));
            ls_db()->prepare('UPDATE ls_licenses SET expiry_date = ?, license_type = "subscription", updated_at = NOW() WHERE id = ?')
                ->execute([$newExpiry, $id]);
            ls_audit($admin['id'], 'renew_license', 'license', $id, 'تمدید ' . $months . ' ماهه تا ' . $newExpiry);
            $_SESSION['flash'] = ['success', 'لایسنس تا ' . ls_shamsi($newExpiry) . ' تمدید شد.'];
        } else {
            $_SESSION['flash'] = ['danger', 'لایسنس یافت نشد.'];
        }
        ls_json_or_redirect('index.php?page=license_view&id=' . $id);
    }

    if ($act === 'revoke') {
        $id = (int)($_POST['id'] ?? 0);
        $reason = mb_substr(trim((string)($_POST['reason'] ?? 'ابطال توسط مدیر')), 0, 255);
        ls_db()->prepare('UPDATE ls_licenses SET is_revoked = 1, revoked_at = NOW(), revoked_reason = ?, updated_at = NOW() WHERE id = ?')
            ->execute([$reason, $id]);
        ls_audit($admin['id'], 'revoke_license', 'license', $id, 'دلیل: ' . $reason);
        $_SESSION['flash'] = ['warning', 'لایسنس ابطال شد — از این پس کلاینت آن رد می‌کند.'];
        ls_json_or_redirect('index.php?page=license_view&id=' . $id);
    }

    if ($act === 'unbind') {
        $id = (int)($_POST['id'] ?? 0);
        ls_db()->prepare('UPDATE ls_licenses SET hardware_fingerprint = NULL, activation_code = NULL, updated_at = NOW() WHERE id = ?')
            ->execute([$id]);
        ls_audit($admin['id'], 'unbind_license', 'license', $id, 'جدا کردن اثر انگشت برای انتقال به دستگاه جدید');
        $_SESSION['flash'] = ['success', 'لایسنس آزاد شد — روی دستگاه جدید قابل فعال‌سازی است.'];
        ls_json_or_redirect('index.php?page=license_view&id=' . $id);
    }

    if ($act === 'offline_activate') {
        // فعال‌سازی آفلاین — ورودی: محتوای فایل .req (JSON) یا اثر انگشت خام
        $id = (int)($_POST['id'] ?? 0);
        $input = trim((string)($_POST['req_content'] ?? ''));
        $fingerprint = '';
        $reqKey = '';
        if ($input !== '' && $input[0] === '{') {
            $req = json_decode($input, true);
            if (is_array($req)) {
                $fingerprint = trim((string)($req['fingerprint'] ?? $req['hardwareFingerprint'] ?? ''));
                $reqKey = strtoupper(trim((string)($req['licenseKey'] ?? '')));
            }
        } else {
            $fingerprint = $input;
        }
        $stmt = ls_db()->prepare('SELECT * FROM ls_licenses WHERE id = ?');
        $stmt->execute([$id]);
        $l = $stmt->fetch();
        if ($l === false || strlen($fingerprint) < 16) {
            $_SESSION['flash'] = ['danger', 'لایسنس یا اثر انگشت نامعتبر است. (حداقل ۱۶ کاراکتر)'];
            ls_json_or_redirect('index.php?page=license_view&id=' . $id);
        }
        if ($reqKey !== '' && $reqKey !== (string)$l['license_key']) {
            $_SESSION['flash'] = ['danger', 'کلید داخل فایل درخواست با این لایسنس نمی‌خواند.'];
            ls_json_or_redirect('index.php?page=license_view&id=' . $id);
        }
        // اتصال اثر انگشت + تولید فایل lic برای دانلود
        ls_db()->prepare('UPDATE ls_licenses SET hardware_fingerprint = ?, activation_code = ?,
            activation_count = activation_count + 1, last_validated_at = NOW(), updated_at = NOW() WHERE id = ?')
            ->execute([$fingerprint, 'ACT-' . strtoupper(bin2hex(random_bytes(5))), $id]);
        ls_db()->prepare('INSERT INTO ls_activations (license_id, hardware_fingerprint, activated_at, is_active)
            VALUES (?, ?, NOW(), 1)
            ON DUPLICATE KEY UPDATE is_active = 1, activated_at = NOW()')
            ->execute([$id, $fingerprint]);
        ls_log_check($id, (string)$l['license_key'], 'valid', 'offline', 'فعال‌سازی آفلاین توسط مدیر');
        ls_audit($admin['id'], 'offline_activate', 'license', $id, 'تولید فایل lic برای اثر انگشت ' . substr($fingerprint, 0, 16) . '…');
        // دانلود فایل lic
        $stmt = ls_db()->prepare('SELECT * FROM ls_licenses WHERE id = ?');
        $stmt->execute([$id]);
        $fresh = $stmt->fetch();
        $lic = ls_build_lic_file($fresh, $fingerprint);
        $fname = str_replace(['SCPRO-', '-'], ['', ''], (string)$l['license_key']) . '.lic';
        header('Content-Type: application/octet-stream');
        header('Content-Disposition: attachment; filename="' . $fname . '"');
        header('Content-Length: ' . strlen($lic));
        echo $lic;
        exit;
    }

    if ($act === 'save_settings') {
        ls_setting_set('portal_title', mb_substr(trim((string)($_POST['portal_title'] ?? '')), 0, 120));
        ls_setting_set('api_rate_max', (string)max(10, (int)($_POST['api_rate_max'] ?? 60)));
        ls_setting_set('api_rate_window', (string)max(60, (int)($_POST['api_rate_window'] ?? 600)));
        ls_setting_set('offline_grace_days', (string)max(0, (int)($_POST['offline_grace_days'] ?? 30)));
        ls_audit($admin['id'], 'save_settings', 'settings', null, 'به‌روزرسانی تنظیمات پنل');
        $_SESSION['flash'] = ['success', 'تنظیمات ذخیره شد.'];
        ls_json_or_redirect('index.php?page=settings');
    }

    if ($act === 'change_password') {
        $current = (string)($_POST['current_password'] ?? '');
        $new = (string)($_POST['new_password'] ?? '');
        $confirm = (string)($_POST['confirm_password'] ?? '');
        $stmt = ls_db()->prepare('SELECT * FROM ls_admins WHERE id = ?');
        $stmt->execute([$admin['id']]);
        $a = $stmt->fetch();
        if ($a === false || !password_verify($current, (string)$a['password_hash'])) {
            $_SESSION['flash'] = ['danger', 'رمز فعلی نادرست است.'];
        } elseif (strlen($new) < 8) {
            $_SESSION['flash'] = ['danger', 'رمز جدید حداقل ۸ کاراکتر باشد.'];
        } elseif ($new !== $confirm) {
            $_SESSION['flash'] = ['danger', 'تکرار رمز با رمز جدید یکسان نیست.'];
        } else {
            ls_db()->prepare('UPDATE ls_admins SET password_hash = ? WHERE id = ?')
                ->execute([password_hash($new, PASSWORD_DEFAULT), $admin['id']]);
            ls_audit($admin['id'], 'change_password', 'admin', $admin['id'], 'تغییر رمز عبور');
            $_SESSION['flash'] = ['success', 'رمز عبور با موفقیت تغییر کرد.'];
        }
        ls_json_or_redirect('index.php?page=settings');
    }

    if ($act === 'regenerate_keys') {
        $confirmText = trim((string)($_POST['confirm_text'] ?? ''));
        if ($confirmText !== 'REGENERATE') {
            $_SESSION['flash'] = ['danger', 'عبارت تأیید دقیقاً باید REGENERATE باشد.'];
        } else {
            // تولید مجدد جفت‌کلید — همه امضاها با کلید قبلی باطل می‌شوند
            $res = openssl_pkey_new(['private_key_bits' => 2048, 'private_key_type' => OPENSSL_KEYTYPE_RSA]);
            $private = '';
            openssl_pkey_export($res, $private);
            $details = openssl_pkey_get_details($res);
            file_put_contents(LS_DATA_DIR . '/rsa_private.pem', $private, LOCK_EX);
            file_put_contents(LS_DATA_DIR . '/rsa_public.pem', $details['key'], LOCK_EX);
            @chmod(LS_DATA_DIR . '/rsa_private.pem', 0640);
            ls_audit($admin['id'], 'regenerate_keys', 'settings', null, 'بازتولید جفت‌کلید RSA — کلید عمومی جدید باید در کلاینت‌ها هم به‌روز شود');
            $_SESSION['flash'] = ['warning', 'کلیدهای جدید ساخته شدند — کلید عمومی جدید را در تنظیمات کلاینت‌ها هم جای‌گذاری کنید.'];
        }
        ls_json_or_redirect('index.php?page=settings');
    }

    ls_json_or_redirect('index.php?page=dashboard');
}

/* ═══════════════════════  ورود  ═══════════════════════ */

if ($admin === null && $page !== 'login') {
    header('Location: index.php?page=login');
    exit;
}

$flash = null;
if (!empty($_SESSION['flash'])) {
    $flash = $_SESSION['flash'];
    unset($_SESSION['flash']);
}

/* ═══════════════════════  چیدمان مشترک  ═══════════════════════ */

function ls_header(string $title, array $admin, string $active): void
{
    $portalTitle = ls_setting('portal_title', 'سامانه لایسنس درمانگاه');
    echo '<!doctype html><html lang="fa" dir="rtl"><head><meta charset="utf-8">';
    echo '<meta name="viewport" content="width=device-width, initial-scale=1">';
    echo '<title>' . ls_e($title) . ' — ' . ls_e($portalTitle) . '</title>';
    echo '<link rel="stylesheet" href="assets/style.css">';
    echo '</head><body><div class="layout">';
    // نوار کنار
    $nav = [
        'dashboard'  => ['📊', 'داشبورد'],
        'licenses'   => ['🔑', 'لایسنس‌ها'],
        'license_new'=> ['➕', 'صدور لایسنس'],
        'logs'       => ['📋', 'لاگ اعتبارسنجی'],
        'settings'   => ['⚙️', 'تنظیمات'],
    ];
    echo '<aside class="sidebar"><div class="brand">🔑<div><b>' . ls_e($portalTitle) . '</b>'
        . '<small>Sahand ClinicPro v' . LS_VERSION . '</small></div></div><nav>';
    foreach ($nav as $k => $v) {
        $cls = $k === $active ? ' class="active"' : '';
        echo '<a href="index.php?page=' . $k . '"' . $cls . '><span>' . $v[0] . '</span>' . $v[1] . '</a>';
    }
    echo '</nav><div class="sidebar-foot"><div class="user">👤 ' . ls_e($admin['name'])
        . '</div><a class="btn btn-outline btn-sm" href="index.php?page=logout">خروج</a></div></aside>';
    echo '<main class="content">';
}

function ls_footer(): void
{
    echo '</main></div></body></html>';
}

function ls_flash(array $f): void
{
    echo '<div class="alert alert-' . ls_e($f[0]) . '">' . ls_e($f[1]) . '</div>';
}

require __DIR__ . '/inc/pages.inc.php';
