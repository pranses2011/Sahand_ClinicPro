<?php
/**
 * رندر صفحات پنل مدیریت سرور لایسنس — جداشده برای خوانایی
 * متغیرهای ورودی از index.php: $page، $admin، $flash
 *
 * @license MIT
 */

declare(strict_types=1);

/* ═══════════════════════  خروج  ═══════════════════════ */
if ($page === 'logout') {
    $_SESSION = [];
    session_destroy();
    header('Location: index.php?page=login');
    exit;
}

/* ═══════════════════════  صفحه ورود  ═══════════════════════ */
if ($page === 'login') {
    if ($admin !== null) {
        header('Location: index.php?page=dashboard');
        exit;
    }
    echo '<!doctype html><html lang="fa" dir="rtl"><head><meta charset="utf-8">';
    echo '<meta name="viewport" content="width=device-width, initial-scale=1">';
    echo '<title>ورود — سامانه لایسنس</title><link rel="stylesheet" href="assets/style.css">';
    echo '</head><body class="login-bg"><div class="login-card">';
    echo '<h1>🔑 سامانه لایسنس</h1><p class="sub">Sahand ClinicPro — پنل مدیریت</p>';
    if ($flash !== null) {
        ls_flash($flash);
    }
    echo '<form method="post" action="index.php?page=login">'
        . ls_csrf_field()
        . '<input type="hidden" name="action" value="login">'
        . '<label>نام کاربری<input type="text" name="username" required autofocus autocomplete="username"></label>'
        . '<label>رمز عبور<input type="password" name="password" required autocomplete="current-password"></label>'
        . '<button class="btn btn-primary btn-block" type="submit">ورود به پنل</button>'
        . '</form><p class="foot">بعد از ۵ خطای پیاپی، حساب ۳۰ دقیقه قفل می‌شود.</p></div></body></html>';
    exit;
}

/* ═══════════════════════  داشبورد  ═══════════════════════ */
if ($page === 'dashboard') {
    $db = ls_db();
    $today = date('Y-m-d');
    $stats = [
        'کل لایسنس‌ها'   => [$db->query('SELECT COUNT(*) c FROM ls_licenses WHERE is_revoked=0')->fetch()['c'], '#1565c0', '#e3f2fd'],
        'فعال'           => [$db->query("SELECT COUNT(*) c FROM ls_licenses WHERE is_revoked=0 AND hardware_fingerprint IS NOT NULL AND (expiry_date IS NULL OR expiry_date>='$today')")->fetch()['c'], '#2e7d32', '#e8f5e9'],
        'در انتظار فعال‌سازی' => [$db->query('SELECT COUNT(*) c FROM ls_licenses WHERE is_revoked=0 AND hardware_fingerprint IS NULL')->fetch()['c'], '#f9a825', '#fffde7'],
        'منقضی‌شده'       => [$db->query("SELECT COUNT(*) c FROM ls_licenses WHERE is_revoked=0 AND expiry_date IS NOT NULL AND expiry_date<'$today'")->fetch()['c'], '#ef6c00', '#fff3e0'],
        'ابطال‌شده'       => [$db->query('SELECT COUNT(*) c FROM ls_licenses WHERE is_revoked=1')->fetch()['c'], '#c62828', '#ffebee'],
        'فعال‌سازی آفلاین' => [$db->query("SELECT COUNT(*) c FROM ls_check_logs WHERE source='offline'")->fetch()['c'], '#6a1b9a', '#f3e5f5'],
    ];
    ls_header('داشبورد', $admin, 'dashboard');
    if ($flash !== null) {
        ls_flash($flash);
    }
    echo '<h2>📊 داشبورد</h2><div class="stat-grid">';
    foreach ($stats as $label => $s) {
        echo '<div class="stat-card" style="border-color:' . $s[1] . ';background:' . $s[2] . '">'
            . '<div class="stat-num" style="color:' . $s[1] . '">' . ls_fa_num((string)$s[0]) . '</div>'
            . '<div class="stat-label">' . ls_e($label) . '</div></div>';
    }
    echo '</div>';

    // منقضی‌شونده در ۳۰ روز آینده
    $soon = $db->query("SELECT license_key, clinic_name, expiry_date FROM ls_licenses
        WHERE is_revoked=0 AND expiry_date IS NOT NULL
        AND expiry_date BETWEEN '$today' AND DATE_ADD('$today', INTERVAL 30 DAY)
        ORDER BY expiry_date ASC LIMIT 15")->fetchAll();
    echo '<h3>⏳ انقضای نزدیک (۳۰ روز آینده)</h3>';
    if (count($soon) === 0) {
        echo '<div class="alert alert-info">لایسنسی در ۳۰ روز آینده منقضی نمی‌شود.</div>';
    } else {
        echo '<table class="table"><tr><th>کلید</th><th>درمانگاه</th><th>تاریخ انقضا</th><th>روز باقیمانده</th></tr>';
        foreach ($soon as $r) {
            $days = (int)((strtotime((string)$r['expiry_date']) - strtotime($today)) / 86400);
            echo '<tr><td class="mono">' . ls_e($r['license_key']) . '</td><td>' . ls_e($r['clinic_name'] ?? '—')
                . '</td><td>' . ls_shamsi($r['expiry_date']) . '</td><td>'
                . ($days <= 7 ? '<span class="badge badge-danger">' : '<span class="badge badge-warning">')
                . ls_fa_num((string)$days) . ' روز</span></td></tr>';
        }
        echo '</table>';
    }

    // آخرین لاگ‌ها
    $logs = $db->query('SELECT * FROM ls_check_logs ORDER BY id DESC LIMIT 12')->fetchAll();
    echo '<h3>📋 آخرین اعتبارسنجی‌ها</h3><table class="table">';
    echo '<tr><th>زمان</th><th>کلید</th><th>نتیجه</th><th>منبع</th><th>پیام</th></tr>';
    foreach ($logs as $l) {
        $badges = [
            'valid' => 'badge-success', 'expired' => 'badge-warning',
            'not_found' => 'badge-danger', 'revoked' => 'badge-danger',
            'fingerprint_mismatch' => 'badge-danger', 'rate_limited' => 'badge-warning',
        ];
        $texts = [
            'valid' => 'معتبر', 'expired' => 'منقضی', 'not_found' => 'ناموجود',
            'revoked' => 'ابطال‌شده', 'fingerprint_mismatch' => 'اثر انگشت نامطباق',
            'rate_limited' => 'محدودیت نرخ',
        ];
        $cls = $badges[$l['result']] ?? 'badge-muted';
        $txt = $texts[$l['result']] ?? $l['result'];
        echo '<tr><td>' . ls_shamsi($l['created_at'], true) . '</td><td class="mono">'
            . ls_e((string)$l['license_key']) . '</td><td><span class="badge ' . $cls . '">'
            . ls_e($txt) . '</span></td><td>' . ($l['source'] === 'offline' ? 'آفلاین' : 'برخط')
            . '</td><td class="dim">' . ls_e((string)$l['message']) . '</td></tr>';
    }
    echo '</table>';
    ls_footer();
    exit;
}

/* ═══════════════════════  فهرست لایسنس‌ها  ═══════════════════════ */
if ($page === 'licenses') {
    $filter = (string)($_GET['filter'] ?? 'all');
    $q = trim((string)($_GET['q'] ?? ''));
    $today = date('Y-m-d');
    $sql = 'FROM ls_licenses WHERE 1=1';
    $params = [];
    if ($filter === 'active') {
        $sql .= " AND is_revoked=0 AND hardware_fingerprint IS NOT NULL AND (expiry_date IS NULL OR expiry_date>='$today')";
    } elseif ($filter === 'pending') {
        $sql .= ' AND is_revoked=0 AND hardware_fingerprint IS NULL';
    } elseif ($filter === 'expired') {
        $sql .= " AND is_revoked=0 AND expiry_date IS NOT NULL AND expiry_date<'$today'";
    } elseif ($filter === 'revoked') {
        $sql .= ' AND is_revoked=1';
    }
    if ($q !== '') {
        $sql .= ' AND (license_key LIKE ? OR clinic_name LIKE ?)';
        $params = ['%' . $q . '%', '%' . $q . '%'];
    }
    $stmt = ls_db()->prepare('SELECT * ' . $sql . ' ORDER BY id DESC LIMIT 300');
    $stmt->execute($params);
    $rows = $stmt->fetchAll();

    $tabs = [
        'all' => 'همه', 'active' => 'فعال', 'pending' => 'در انتظار',
        'expired' => 'منقضی', 'revoked' => 'ابطال‌شده',
    ];
    ls_header('لایسنس‌ها', $admin, 'licenses');
    if ($flash !== null) {
        ls_flash($flash);
    }
    echo '<h2>🔑 لایسنس‌ها</h2><div class="tabs">';
    foreach ($tabs as $k => $t) {
        $cls = $k === $filter ? ' class="active"' : '';
        echo '<a href="index.php?page=licenses&filter=' . $k . '"' . $cls . '>' . $t . '</a>';
    }
    echo '</div>';
    echo '<form class="searchbar" method="get"><input type="hidden" name="page" value="licenses">'
        . '<input type="hidden" name="filter" value="' . ls_e($filter) . '">'
        . '<input type="text" name="q" value="' . ls_e($q) . '" placeholder="جستجوی کلید یا نام درمانگاه…">'
        . '<button class="btn" type="submit">🔍 جستجو</button></form>';
    if (count($rows) === 0) {
        echo '<div class="empty">موردی یافت نشد.</div>';
    } else {
        echo '<table class="table"><tr><th>کلید لایسنس</th><th>نوع</th><th>درمانگاه</th>'
            . '<th>کاربر/پزشک/بیمار</th><th>انقضا</th><th>وضعیت</th><th></th></tr>';
        foreach ($rows as $l) {
            [$code, $text, $color] = ls_license_status($l);
            echo '<tr><td class="mono">' . ls_e($l['license_key']) . '</td>'
                . '<td>' . ($l['license_type'] === 'perpetual' ? 'دائمی' : 'اشتراکی') . '</td>'
                . '<td>' . ls_e($l['clinic_name'] ?? '—') . '</td>'
                . '<td>' . ls_fa_num((string)$l['max_users']) . ' / ' . ls_fa_num((string)$l['max_doctors'])
                . ' / ' . ls_fa_num((string)$l['max_patients']) . '</td>'
                . '<td>' . ls_shamsi($l['expiry_date']) . '</td>'
                . '<td><span class="badge" style="background:' . $color . '20;color:' . $color . '">' . ls_e($text) . '</span></td>'
                . '<td><a class="btn btn-sm" href="index.php?page=license_view&id=' . (int)$l['id'] . '">مشاهده ←</a></td></tr>';
        }
        echo '</table>';
    }
    ls_footer();
    exit;
}

/* ═══════════════════════  صدور لایسنس  ═══════════════════════ */
if ($page === 'license_new') {
    ls_header('صدور لایسنس', $admin, 'license_new');
    if ($flash !== null) {
        ls_flash($flash);
    }
    echo '<h2>➕ صدور لایسنس جدید</h2>';
    echo '<form method="post" action="index.php?page=license_new">' . ls_csrf_field()
        . '<input type="hidden" name="action" value="create_license">';
    echo '<div class="grid-2">
        <label>نام درمانگاه / مشتری<input type="text" name="clinic_name" placeholder="درمانگاه تخصصی سلامت"></label>
        <label>تلفن<input type="text" name="clinic_phone" placeholder="021…"></label>
    </div>';
    echo '<div class="grid-4">
        <label>نوع لایسنس<select name="license_type">
            <option value="1">اشتراکی (مدت‌دار)</option>
            <option value="2">دائمی</option></select></label>
        <label>مدت (ماه)<input type="number" name="duration_months" value="12" min="1" max="60"></label>
        <label>حداکثر کاربر<input type="number" name="max_users" value="5" min="1"></label>
        <label>حداکثر پزشک<input type="number" name="max_doctors" value="10" min="1"></label>
    </div>';
    echo '<div class="grid-4">
        <label>حداکثر بیمار<input type="number" name="max_patients" value="5000" min="1"></label>
        <label>حداکثر شعبه<input type="number" name="max_branches" value="1" min="1"></label>
        <label class="full2">یادداشت<input type="text" name="notes" placeholder="اختیاری"></label>
    </div>';
    echo '<h3>ماژول‌های فعال</h3><div class="modules">';
    foreach (ls_module_catalog() as $code => $fa) {
        $core = in_array($code, ['Dashboard', 'Patients', 'Appointments', 'Visits', 'Prescriptions', 'Billing', 'Settings', 'Users'], true);
        $checked = $core ? ' checked' : '';
        echo '<label class="module"><input type="checkbox" name="modules[]" value="' . ls_e($code) . '"' . $checked . '> '
            . ls_e($fa) . ' <small class="dim">' . ls_e($code) . '</small></label>';
    }
    echo '</div>';
    echo '<button class="btn btn-primary" type="submit">🔑 صدور لایسنس</button></form>';
    ls_footer();
    exit;
}

/* ═══════════════════════  جزئیات لایسنس  ═══════════════════════ */
if ($page === 'license_view') {
    $id = (int)($_GET['id'] ?? 0);
    $stmt = ls_db()->prepare('SELECT * FROM ls_licenses WHERE id = ?');
    $stmt->execute([$id]);
    $l = $stmt->fetch();
    if ($l === false) {
        ls_header('لایسنس', $admin, 'licenses');
        echo '<div class="alert alert-danger">لایسنس یافت نشد.</div>';
        ls_footer();
        exit;
    }
    [$code, $text, $color] = ls_license_status($l);
    ls_header('لایسنس ' . $l['license_key'], $admin, 'licenses');
    if ($flash !== null) {
        ls_flash($flash);
    }

    // بخش مشخصات
    echo '<h2>' . ls_e($l['license_key']) . ' <span class="badge" style="background:' . $color . '20;color:' . $color . '">' . ls_e($text) . '</span></h2>';
    $daysLeft = ls_days_remaining($l);
    echo '<div class="detail-grid">
        <div><small>درمانگاه</small><b>' . ls_e($l['clinic_name'] ?? '—') . '</b></div>
        <div><small>نوع</small><b>' . ($l['license_type'] === 'perpetual' ? 'دائمی' : 'اشتراکی') . '</b></div>
        <div><small>صدور</small><b>' . ls_shamsi($l['issued_date']) . '</b></div>
        <div><small>انقضا</small><b>' . ls_shamsi($l['expiry_date'])
            . ($daysLeft !== null ? ' (' . ls_fa_num((string)$daysLeft) . ' روز باقیمانده)' : ' (بدون انقضا)') . '</b></div>
        <div><small>حداکثر کاربر / پزشک / بیمار / شعبه</small><b>'
            . ls_fa_num((string)$l['max_users']) . ' / ' . ls_fa_num((string)$l['max_doctors'])
            . ' / ' . ls_fa_num((string)$l['max_patients']) . ' / ' . ls_fa_num((string)$l['max_branches']) . '</b></div>
        <div><small>آخرین تپ قلب</small><b>' . ls_shamsi($l['last_heartbeat_at'], true) . '</b></div>
        <div><small>اثر انگشت دستگاه</small><b class="mono small">' . ls_e($l['hardware_fingerprint'] ?? 'متصل نشده') . '</b></div>
        <div><small>کد فعال‌سازی</small><b class="mono">' . ls_e($l['activation_code'] ?? '—') . '</b></div>
    </div>';

    // ماژول‌ها
    $mods = json_decode((string)$l['enabled_modules'], true) ?: [];
    $catalog = ls_module_catalog();
    echo '<h3>ماژول‌های فعال (' . ls_fa_num((string)count($mods)) . ')</h3><div class="modules readonly">';
    foreach ($catalog as $mc => $fa) {
        $on = in_array($mc, $mods, true);
        echo '<span class="module ' . ($on ? 'on' : 'off') . '">' . ($on ? '✅' : '⛔') . ' ' . ls_e($fa) . '</span>';
    }
    echo '</div>';

    // اقدام‌ها
    echo '<div class="actions">';
    // تمدید
    echo '<form method="post" class="inline-form">' . ls_csrf_field()
        . '<input type="hidden" name="action" value="renew"><input type="hidden" name="id" value="' . $id . '">'
        . '<input type="number" name="months" value="12" min="1" max="60" style="width:90px"> ماه '
        . '<button class="btn btn-success" type="submit">🔄 تمدید</button></form>';
    // ابطال
    if ((int)$l['is_revoked'] === 0) {
        echo '<form method="post" class="inline-form" onsubmit="return confirm(\'لایسنس ابطال شود؟ کلاینت آن از این پس رد می‌شود.\')">'
            . ls_csrf_field()
            . '<input type="hidden" name="action" value="revoke"><input type="hidden" name="id" value="' . $id . '">'
            . '<input type="text" name="reason" placeholder="دلیل ابطال" style="width:180px"> '
            . '<button class="btn btn-danger" type="submit">🚫 ابطال</button></form>';
        // جدا کردن دستگاه
        if (!empty($l['hardware_fingerprint'])) {
            echo '<form method="post" class="inline-form" onsubmit="return confirm(\'اثر انگشت جدا شود تا لایسنس روی دستگاه جدید فعال شود؟\')">'
                . ls_csrf_field()
                . '<input type="hidden" name="action" value="unbind"><input type="hidden" name="id" value="' . $id . '">'
                . '<button class="btn btn-warning" type="submit">🔀 انتقال به دستگاه جدید</button></form>';
        }
    }
    echo '</div>';

    // فعال‌سازی آفلاین — تولید فایل lic
    echo '<h3>📴 فعال‌سازی آفلاین</h3>';
    echo '<div class="alert alert-info">محتوای فایل درخواست مشتری (<code>.req</code> — شامل اثر انگشت) را اینجا بچسبانید '
        . 'یا مستقیماً رشته اثر انگشت سخت‌افزار را وارد کنید تا فایل <code>.lic</code> امضاشده تولید و دانلود شود.</div>';
    echo '<form method="post" action="index.php?page=license_view&id=' . $id . '">'
        . ls_csrf_field()
        . '<input type="hidden" name="action" value="offline_activate">'
        . '<input type="hidden" name="id" value="' . $id . '">'
        . '<textarea name="req_content" rows="3" placeholder=\'{"licenseKey":"SCPRO-…","fingerprint":"SHA256…"} یا فقط اثر انگشت\'></textarea>'
        . '<button class="btn btn-primary" type="submit">📤 تولید و دانلود فایل lic</button></form>';

    // فعال‌سازی‌های این لایسنس
    $acts = ls_db()->prepare('SELECT * FROM ls_activations WHERE license_id = ? ORDER BY activated_at DESC LIMIT 10');
    $acts->execute([$id]);
    $rows = $acts->fetchAll();
    echo '<h3>🖥️ دستگاه‌های فعال‌شده</h3>';
    if (count($rows) === 0) {
        echo '<div class="empty">هنوز دستگاهی فعال نشده است.</div>';
    } else {
        echo '<table class="table"><tr><th>اثر انگشت</th><th>IP</th><th>زمان فعال‌سازی</th><th>آخرین بازدید</th><th>وضعیت</th></tr>';
        foreach ($rows as $a) {
            echo '<tr><td class="mono small">' . ls_e((string)$a['hardware_fingerprint']) . '</td><td>' . ls_e($a['ip_address'] ?? '—')
                . '</td><td>' . ls_shamsi($a['activated_at'], true) . '</td><td>' . ls_shamsi($a['last_seen_at'], true)
                . '</td><td>' . ((int)$a['is_active'] === 1 ? '🟢 فعال' : '⚪ غیرفعال') . '</td></tr>';
        }
        echo '</table>';
    }

    // لاگ‌های این لایسنس
    $logs = ls_db()->prepare('SELECT * FROM ls_check_logs WHERE license_id = ? ORDER BY id DESC LIMIT 25');
    $logs->execute([$id]);
    echo '<h3>📋 لاگ اعتبارسنجی</h3><table class="table"><tr><th>زمان</th><th>نتیجه</th><th>منبع</th><th>پیام</th></tr>';
    foreach ($logs->fetchAll() as $lg) {
        echo '<tr><td>' . ls_shamsi($lg['created_at'], true) . '</td><td>' . ls_e($lg['result'])
            . '</td><td>' . ($lg['source'] === 'offline' ? 'آفلاین' : 'برخط') . '</td><td class="dim">' . ls_e((string)$lg['message']) . '</td></tr>';
    }
    echo '</table>';
    ls_footer();
    exit;
}

/* ═══════════════════════  لاگ‌ها  ═══════════════════════ */
if ($page === 'logs') {
    $filter = (string)($_GET['result'] ?? 'all');
    $sql = 'FROM ls_check_logs WHERE 1=1';
    $params = [];
    if ($filter !== 'all') {
        $sql .= ' AND result = ?';
        $params[] = $filter;
    }
    $stmt = ls_db()->prepare('SELECT * ' . $sql . ' ORDER BY id DESC LIMIT 300');
    $stmt->execute($params);
    $rows = $stmt->fetchAll();
    ls_header('لاگ اعتبارسنجی', $admin, 'logs');
    if ($flash !== null) {
        ls_flash($flash);
    }
    $results = ['all' => 'همه', 'valid' => 'معتبر', 'expired' => 'منقضی', 'not_found' => 'ناموجود',
        'revoked' => 'ابطال‌شده', 'fingerprint_mismatch' => 'اثر انگشت نامطباق', 'rate_limited' => 'محدودیت نرخ'];
    echo '<h2>📋 لاگ اعتبارسنجی</h2><div class="tabs">';
    foreach ($results as $k => $t) {
        $cls = $k === $filter ? ' class="active"' : '';
        echo '<a href="index.php?page=logs&result=' . $k . '"' . $cls . '>' . $t . '</a>';
    }
    echo '</div>';
    echo '<table class="table"><tr><th>#</th><th>زمان</th><th>کلید</th><th>نتیجه</th><th>منبع</th><th>IP</th><th>پیام</th></tr>';
    foreach ($rows as $l) {
        echo '<tr><td>' . ls_fa_num((string)$l['id']) . '</td><td>' . ls_shamsi($l['created_at'], true)
            . '</td><td class="mono small">' . ls_e((string)$l['license_key']) . '</td>'
            . '<td>' . ls_e($l['result']) . '</td>'
            . '<td>' . ($l['source'] === 'offline' ? 'آفلاین' : 'برخط') . '</td>'
            . '<td class="small">' . ls_e($l['ip_address'] ?? '') . '</td>'
            . '<td class="dim small">' . ls_e((string)$l['message']) . '</td></tr>';
    }
    echo '</table>';
    ls_footer();
    exit;
}

/* ═══════════════════════  تنظیمات  ═══════════════════════ */
if ($page === 'settings') {
    ls_header('تنظیمات', $admin, 'settings');
    if ($flash !== null) {
        ls_flash($flash);
    }
    echo '<h2>⚙️ تنظیمات پنل</h2>';
    echo '<form method="post" action="index.php?page=settings">' . ls_csrf_field()
        . '<input type="hidden" name="action" value="save_settings">'
        . '<div class="grid-2">'
        . '<label>عنوان پنل<input type="text" name="portal_title" value="' . ls_e(ls_setting('portal_title')) . '"></label>'
        . '<label>حداکثر درخواست API در پنجره<input type="number" name="api_rate_max" value="' . ls_e(ls_setting('api_rate_max', '60')) . '" min="10"></label>'
        . '<label>پنجره محدودیت نرخ (ثانیه)<input type="number" name="api_rate_window" value="' . ls_e(ls_setting('api_rate_window', '600')) . '" min="60"></label>'
        . '<label>مهلت آفلاین کلاینت (روز)<input type="number" name="offline_grace_days" value="' . ls_e(ls_setting('offline_grace_days', '30')) . '" min="0"></label>'
        . '</div><button class="btn btn-primary" type="submit">💾 ذخیره تنظیمات</button></form>';

    echo '<h3>🔑 کلیدهای RSA</h3><div class="alert alert-warning">کلید عمومی را در فایل <code>appsettings.json</code> کلاینت '
        . '(بخش License → RsaPublicKey) جای‌گذاری کنید. قالب XML برای <code>RSA.FromXmlString</code> و قالب PEM برای '
        . '<code>ImportFromPem</code> هر دو پشتیبانی می‌شوند.</div>';
    echo '<h4>کلید عمومی — قالب XML (دات‌نت)</h4><pre class="code">' . ls_e(ls_public_key_xml()) . '</pre>';
    echo '<h4>کلید عمومی — قالب PEM</h4><pre class="code">' . ls_e(ls_public_key_pem()) . '</pre>';

    echo '<h3>🔄 بازتولید کلیدها (خطرناک)</h3>';
    echo '<form method="post" action="index.php?page=settings" onsubmit="return confirm(\'هشدار! همه امضاها با کلید قبلی نامعتبر می‌شوند و باید کلید عمومی جدید را در همه کلاینت‌ها به‌روز کنید. مطمئنید؟\')">'
        . ls_csrf_field()
        . '<input type="hidden" name="action" value="regenerate_keys">'
        . '<label>برای تأیید عبارت <code>REGENERATE</code> را بنویسید<input type="text" name="confirm_text" placeholder="REGENERATE"></label>'
        . '<button class="btn btn-danger" type="submit">تولید جفت‌کلید جدید</button></form>';

    echo '<h3>🔒 تغییر رمز عبور</h3>';
    echo '<form method="post" action="index.php?page=settings">' . ls_csrf_field()
        . '<input type="hidden" name="action" value="change_password">'
        . '<div class="grid-3">'
        . '<label>رمز فعلی<input type="password" name="current_password" required></label>'
        . '<label>رمز جدید<input type="password" name="new_password" required minlength="8"></label>'
        . '<label>تکرار رمز جدید<input type="password" name="confirm_password" required minlength="8"></label>'
        . '</div><button class="btn btn-primary" type="submit">🔐 تغییر رمز</button></form>';

    // آخرین رویدادهای حسابرسی
    $audits = ls_db()->query('SELECT a.*, ad.username FROM ls_admin_audit a
        LEFT JOIN ls_admins ad ON ad.id = a.admin_id ORDER BY a.id DESC LIMIT 15')->fetchAll();
    echo '<h3>🧾 آخرین فعالیت‌های مدیریتی</h3><table class="table">'
        . '<tr><th>زمان</th><th>مدیر</th><th>اقدام</th><th>موجودیت</th><th>جزئیات</th></tr>';
    foreach ($audits as $a) {
        echo '<tr><td>' . ls_shamsi($a['created_at'], true) . '</td><td>' . ls_e($a['username'] ?? '—')
            . '</td><td>' . ls_e($a['action']) . '</td><td>' . ls_e($a['entity'] . ' #' . $a['entity_id'])
            . '</td><td class="dim small">' . ls_e((string)$a['details']) . '</td></tr>';
    }
    echo '</table>';
    ls_footer();
    exit;
}

/* ═══════════════════════  صفحه پیش‌فرض  ═══════════════════════ */
header('Location: index.php?page=dashboard');
exit;
