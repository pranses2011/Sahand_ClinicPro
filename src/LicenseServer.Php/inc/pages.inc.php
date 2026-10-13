<?php
/**
 * رندر صفحات پنل مدیریت سرور لایسنس — جداشده برای خوانایی
 * متغیرهای ورودی از index.php: $page، $admin، $flash
 *
 * چرخه دوازدهم: داشبورد نموداری (۴ نمودار SVG) + ویرایش/حذف لایسنس +
 * اطلاعات کامل درمانگاه + پیام مستقیم به داشبورد + صفحه گزارش‌ها
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
    echo '</head><body class="login-bg"><div class="login-orbs"><i></i><i></i><i></i></div><div class="login-card anim-up">';
    echo '<div class="login-logo">🔑</div>';
    echo '<h1>سامانه لایسنس</h1><p class="sub">Sahand ClinicPro — پنل مدیریت</p>';
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

/* ═══════════════════════  داشبورد — با نمودارهای SVG  ═══════════════════════ */
if ($page === 'dashboard') {
    $db = ls_db();
    $today = date('Y-m-d');
    $stats = [
        'کل لایسنس‌ها'   => [(int)$db->query('SELECT COUNT(*) c FROM ls_licenses WHERE is_revoked=0')->fetch()['c'], '#1565c0', '#e3f2fd', '🔑'],
        'فعال'           => [(int)$db->query("SELECT COUNT(*) c FROM ls_licenses WHERE is_revoked=0 AND hardware_fingerprint IS NOT NULL AND (expiry_date IS NULL OR expiry_date>='$today')")->fetch()['c'], '#2e7d32', '#e8f5e9', '🟢'],
        'در انتظار فعال‌سازی' => [(int)$db->query('SELECT COUNT(*) c FROM ls_licenses WHERE is_revoked=0 AND hardware_fingerprint IS NULL')->fetch()['c'], '#f9a825', '#fffde7', '⏳'],
        'منقضی‌شده'       => [(int)$db->query("SELECT COUNT(*) c FROM ls_licenses WHERE is_revoked=0 AND expiry_date IS NOT NULL AND expiry_date<'$today'")->fetch()['c'], '#ef6c00', '#fff3e0', '⌛'],
        'ابطال‌شده'       => [(int)$db->query('SELECT COUNT(*) c FROM ls_licenses WHERE is_revoked=1')->fetch()['c'], '#c62828', '#ffebee', '🚫'],
        'فعال‌سازی آفلاین' => [(int)$db->query("SELECT COUNT(*) c FROM ls_check_logs WHERE source='offline'")->fetch()['c'], '#6a1b9a', '#f3e5f5', '📴'],
    ];
    ls_header('داشبورد', $admin, 'dashboard');
    if ($flash !== null) {
        ls_flash($flash);
    }
    echo '<h2>📊 داشبورد</h2><div class="stat-grid">';
    $si = 0;
    foreach ($stats as $label => $s) {
        echo '<div class="stat-card anim-up d' . $si . '" style="border-color:' . $s[1] . ';background:' . $s[2] . '">'
            . '<div class="stat-ic" style="color:' . $s[1] . '">' . $s[3] . '</div>'
            . '<div><div class="stat-num count-up" style="color:' . $s[1] . '" data-target="' . (int)$s[0] . '">۰</div>'
            . '<div class="stat-label">' . ls_e($label) . '</div></div></div>';
        $si++;
    }
    echo '</div>';

    /* ── ردیف نمودار ۱: روند ۱۴ روزه اعتبارسنجی (سطحی) + ترکیب وضعیت لایسنس‌ها (دونات) ── */
    $trendRows = $db->query(
        "SELECT DATE(created_at) d, COUNT(*) c FROM ls_check_logs
         WHERE created_at >= DATE_SUB('$today', INTERVAL 14 DAY)
         GROUP BY DATE(created_at) ORDER BY d")->fetchAll();
    $trend = [];
    for ($i = 13; $i >= 0; $i--) {
        $day = date('Y-m-d', strtotime("-$i day"));
        $found = null;
        foreach ($trendRows as $r) {
            if ($r['d'] === $day) { $found = $r; break; }
        }
        $trend[] = ['label' => ls_shamsi($day), 'value' => $found ? (int)$found['c'] : 0];
    }
    $statusMix = [];
    foreach (['فعال' => "is_revoked=0 AND hardware_fingerprint IS NOT NULL AND (expiry_date IS NULL OR expiry_date>='$today')",
              'در انتظار' => 'is_revoked=0 AND hardware_fingerprint IS NULL',
              'منقضی' => "is_revoked=0 AND expiry_date IS NOT NULL AND expiry_date<'$today'",
              'ابطال' => 'is_revoked=1'] as $lbl => $cond) {
        $cnt = (int)$db->query("SELECT COUNT(*) c FROM ls_licenses WHERE $cond")->fetch()['c'];
        if ($cnt > 0) {
            $statusMix[] = ['label' => $lbl, 'value' => $cnt];
        }
    }

    echo '<div class="chart-grid">';
    echo '<div class="chart-card anim-up d2"><h3>📉 روند ۱۴ روزه اعتبارسنجی</h3>'
        . ls_svg_area_chart($trend, 'درخواست') . '</div>';
    echo '<div class="chart-card anim-up d3"><h3>🍩 ترکیب وضعیت لایسنس‌ها</h3>'
        . ls_svg_doughnut_chart($statusMix, 'لایسنس') . '</div>';
    echo '</div>';

    /* ── ردیف نمودار ۲: صدور ماهانه (ستونی شمسی) + پرطرفدارترین ماژول‌ها (میله‌ای افقی) ── */
    $monthlyRows = $db->query(
        "SELECT issued_date d, COUNT(*) c FROM ls_licenses
         WHERE issued_date >= DATE_SUB('$today', INTERVAL 12 MONTH)
         GROUP BY issued_date ORDER BY issued_date")->fetchAll();
    // گروه‌بندی ماه شمسی در حافظه
    $byMonth = [];
    foreach ($monthlyRows as $r) {
        $j = ls_g2j((int)date('Y', strtotime((string)$r['d'])), (int)date('n', strtotime((string)$r['d'])), (int)date('j', strtotime((string)$r['d'])));
        $key = $j[0] . '-' . str_pad((string)$j[1], 2, '0', STR_PAD_LEFT);
        $byMonth[$key] = ($byMonth[$key] ?? 0) + (int)$r['c'];
    }
    ksort($byMonth);
    $monthNames = ['', 'فروردین', 'اردیبهشت', 'خرداد', 'تیر', 'مرداد', 'شهریور', 'مهر', 'آبان', 'آذر', 'دی', 'بهمن', 'اسفند'];
    $monthly = [];
    foreach (array_slice($byMonth, -6, null, true) as $key => $cnt) {
        [$jy, $jm] = explode('-', $key);
        $monthly[] = ['label' => $monthNames[(int)$jm] . ' ' . ls_fa_num((string)$jy), 'value' => $cnt];
    }
    // فراوانی فعال‌سازی ماژول‌ها
    $modRows = $db->query("SELECT enabled_modules FROM ls_licenses WHERE is_revoked=0")->fetchAll();
    $modCount = [];
    foreach ($modRows as $r) {
        $mods = json_decode((string)$r['enabled_modules'], true) ?: [];
        foreach ($mods as $m) {
            $modCount[$m] = ($modCount[$m] ?? 0) + 1;
        }
    }
    arsort($modCount);
    $catalog = ls_module_catalog();
    $modPoints = [];
    foreach (array_slice($modCount, 0, 8, true) as $code => $cnt) {
        $modPoints[] = ['label' => $catalog[$code] ?? $code, 'value' => $cnt];
    }

    echo '<div class="chart-grid">';
    echo '<div class="chart-card anim-up d4"><h3>📊 صدور لایسنس ماه‌های اخیر (شمسی)</h3>'
        . ls_svg_column_chart($monthly, 'لایسنس') . '</div>';
    echo '<div class="chart-card anim-up d5"><h3>🏆 پرطرفدارترین ماژول‌ها</h3>'
        . ls_svg_hbar_chart($modPoints, 'لایسنس') . '</div>';
    echo '</div>';

    // منقضی‌شونده در ۳۰ روز آینده
    $soon = $db->query("SELECT license_key, clinic_name, expiry_date FROM ls_licenses
        WHERE is_revoked=0 AND expiry_date IS NOT NULL
        AND expiry_date BETWEEN '$today' AND DATE_ADD('$today', INTERVAL 30 DAY)
        ORDER BY expiry_date ASC LIMIT 15")->fetchAll();
    echo '<h3 class="anim-up">⏳ انقضای نزدیک (۳۰ روز آینده)</h3>';
    if (count($soon) === 0) {
        echo '<div class="alert alert-info">لایسنسی در ۳۰ روز آینده منقضی نمی‌شود.</div>';
    } else {
        echo '<table class="table reveal"><tr><th>کلید</th><th>درمانگاه</th><th>تاریخ انقضا</th><th>روز باقیمانده</th></tr>';
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
    echo '<h3 class="anim-up">📋 آخرین اعتبارسنجی‌ها</h3><table class="table reveal">';
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
        $sql .= ' AND (license_key LIKE ? OR clinic_name LIKE ? OR clinic_manager LIKE ? OR clinic_city LIKE ?)';
        $params = ['%' . $q . '%', '%' . $q . '%', '%' . $q . '%', '%' . $q . '%'];
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
        . '<input type="text" name="q" value="' . ls_e($q) . '" placeholder="جستجوی کلید، نام درمانگاه، مدیر یا شهر…">'
        . '<button class="btn" type="submit">🔍 جستجو</button></form>';
    if (count($rows) === 0) {
        echo '<div class="empty">موردی یافت نشد.</div>';
    } else {
        echo '<table class="table reveal"><tr><th>کلید لایسنس</th><th>نوع</th><th>درمانگاه</th><th>شهر</th>'
            . '<th>کاربر/پزشک/بیمار</th><th>انقضا</th><th>وضعیت</th><th></th></tr>';
        foreach ($rows as $l) {
            [$code, $text, $color] = ls_license_status($l);
            echo '<tr><td class="mono">' . ls_e($l['license_key']) . '</td>'
                . '<td>' . ($l['license_type'] === 'perpetual' ? 'دائمی' : 'اشتراکی') . '</td>'
                . '<td>' . ls_e($l['clinic_name'] ?? '—') . '</td>'
                . '<td>' . ls_e($l['clinic_city'] ?? '—') . '</td>'
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

/* ═══════════════════════  فرم مشترک فیلدهای درمانگاه  ═══════════════════════ */

/** رندر فیلدهای کامل درمانگاه — مشترک بین صدور و ویرایش */
function ls_clinic_fields(?array $l = null): void
{
    $v = static fn(string $k) => ls_e((string)($l[$k] ?? ''));
    echo '<h3>🏥 اطلاعات درمانگاه</h3><div class="grid-2">'
        . '<label>نام درمانگاه / مشتری<input type="text" name="clinic_name" value="' . $v('clinic_name') . '" placeholder="درمانگاه تخصصی سلامت"></label>'
        . '<label>نام مدیر درمانگاه<input type="text" name="clinic_manager" value="' . $v('clinic_manager') . '" placeholder="دکتر محمدی"></label>'
        . '<label>تلفن درمانگاه<input type="text" name="clinic_phone" value="' . $v('clinic_phone') . '" dir="ltr" placeholder="041-XXXXXXX"></label>'
        . '<label>ایمیل<input type="email" name="clinic_email" value="' . $v('clinic_email') . '" dir="ltr" placeholder="info@clinic.ir"></label>'
        . '<label>شهر<input type="text" name="clinic_city" value="' . $v('clinic_city') . '" placeholder="تبریز"></label>'
        // ═══ چرخهٔ بیست‌وهفتم — تکمیل فیلدهای سرور لایسنس ═══
        // «کد اقتصادی» و «شناسه ملی» درمانگاه — بخش اطلاعات درمانگاه برنامه
        // این دو فیلد را از لایسنس می‌خواند؛ ستون‌ها/فیلدها در پنل تکمیل شد.
        . '<label>کد اقتصادی<input type="text" name="economic_code" value="' . $v('economic_code') . '" dir="ltr" placeholder="411123456789"></label>'
        . '<label>شناسه ملی<input type="text" name="national_id" value="' . $v('national_id') . '" dir="ltr" placeholder="14009933690"></label>'
        . '<label class="full2">نشانی کامل<input type="text" name="clinic_address" value="' . $v('clinic_address') . '" placeholder="خیابان، کوچه، پلاک…"></label>'
        . '</div>';
}

/** رندر فیلدهای سقف‌ها و ماژول‌ها — مشترک بین صدور و ویرایش */
function ls_limits_fields(?array $l = null): void
{
    $num = static fn(string $k, int $def) => (int)($l[$k] ?? $def);
    $mods = $l !== null ? (json_decode((string)$l['enabled_modules'], true) ?: []) : null;
    echo '<h3>⚖️ سقف‌ها و ماژول‌ها</h3><div class="grid-4">'
        . '<label>حداکثر کاربر<input type="number" name="max_users" value="' . $num('max_users', 5) . '" min="1"></label>'
        . '<label>حداکثر پزشک<input type="number" name="max_doctors" value="' . $num('max_doctors', 10) . '" min="1"></label>'
        . '<label>حداکثر بیمار<input type="number" name="max_patients" value="' . $num('max_patients', 5000) . '" min="1"></label>'
        . '<label>حداکثر شعبه<input type="number" name="max_branches" value="' . $num('max_branches', 1) . '" min="1"></label>'
        . '</div>';
    echo '<div class="modules">';
    $core = ['Dashboard', 'Patients', 'Appointments', 'Visits', 'Prescriptions', 'Billing', 'Settings', 'Users'];
    foreach (ls_module_catalog() as $code => $fa) {
        $checked = $l !== null
            ? (in_array($code, $mods, true) ? ' checked' : '')
            : (in_array($code, $core, true) ? ' checked' : '');
        echo '<label class="module"><input type="checkbox" name="modules[]" value="' . ls_e($code) . '"' . $checked . '> '
            . ls_e($fa) . ' <small class="dim">' . ls_e($code) . '</small></label>';
    }
    echo '</div>';
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
    echo '<div class="grid-4">'
        . '<label>نوع لایسنس<select name="license_type">'
        . '<option value="1">اشتراکی (مدت‌دار)</option>'
        . '<option value="2">دائمی</option></select></label>'
        . '<label>مدت (ماه)<input type="number" name="duration_months" value="12" min="1" max="60"></label>'
        . '<label class="full2">یادداشت<input type="text" name="notes" placeholder="اختیاری — توافق مالی، توضیح پکیج و…"></label>'
        . '</div>';
    ls_clinic_fields(null);
    ls_limits_fields(null);
    echo '<button class="btn btn-primary btn-lg" type="submit">🔑 صدور لایسنس</button></form>';
    ls_footer();
    exit;
}

/* ═══════════════════════  ویرایش لایسنس — چرخه دوازدهم  ═══════════════════════ */
if ($page === 'license_edit') {
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
    ls_header('ویرایش لایسنس ' . $l['license_key'], $admin, 'licenses');
    if ($flash !== null) {
        ls_flash($flash);
    }
    echo '<h2>✏️ ویرایش لایسنس <span class="mono">' . ls_e($l['license_key']) . '</span></h2>';
    echo '<div class="alert alert-info">تغییرات سقف‌ها و ماژول‌ها با <b>تپ‌قلب بعدی</b> کلاینت (حداکثر ۲۴ ساعت) اعمال می‌شود. '
        . 'کلید لایسنس، نوع و تاریخ انقضا از این صفحه قابل تغییر نیستند — برای تمدید/ابطال از صفحه مشاهده استفاده کنید.</div>';
    echo '<form method="post" action="index.php?page=license_edit&id=' . $id . '">' . ls_csrf_field()
        . '<input type="hidden" name="action" value="update_license">'
        . '<input type="hidden" name="id" value="' . $id . '">';
    ls_clinic_fields($l);
    ls_limits_fields($l);
    echo '<label>یادداشت<input type="text" name="notes" value="' . ls_e((string)($l['notes'] ?? '')) . '"></label>';
    echo '<div class="actions">'
        . '<button class="btn btn-primary btn-lg" type="submit">💾 ذخیره تغییرات</button>'
        . '<a class="btn" href="index.php?page=license_view&id=' . $id . '">انصراف ←</a></div></form>';
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
    echo '<div class="detail-grid">'
        . '<div><small>درمانگاه</small><b>' . ls_e($l['clinic_name'] ?? '—') . '</b></div>'
        . '<div><small>مدیر درمانگاه</small><b>' . ls_e($l['clinic_manager'] ?? '—') . '</b></div>'
        . '<div><small>نوع</small><b>' . ($l['license_type'] === 'perpetual' ? 'دائمی' : 'اشتراکی') . '</b></div>'
        . '<div><small>صدور</small><b>' . ls_shamsi($l['issued_date']) . '</b></div>'
        . '<div><small>انقضا</small><b>' . ls_shamsi($l['expiry_date'])
        . ($daysLeft !== null ? ' (' . ls_fa_num((string)$daysLeft) . ' روز باقیمانده)' : ' (بدون انقضا)') . '</b></div>'
        . '<div><small>شهر / نشانی</small><b>' . ls_e($l['clinic_city'] ?? '—')
        . ($l['clinic_address'] ? ' — ' . ls_e((string)$l['clinic_address']) : '') . '</b></div>'
        . '<div><small>تلفن / ایمیل</small><b>' . ls_e($l['clinic_phone'] ?? '—')
        . ($l['clinic_email'] ? ' — ' . ls_e((string)$l['clinic_email']) : '') . '</b></div>'
        // چرخهٔ بیست‌وهفتم — نمایش کد اقتصادی و شناسه ملی در جزئیات لایسنس
        . '<div><small>کد اقتصادی</small><b>' . ls_e($l['economic_code'] ?? '—') . '</b></div>'
        . '<div><small>شناسه ملی</small><b>' . ls_e($l['national_id'] ?? '—') . '</b></div>'
        . '<div><small>حداکثر کاربر / پزشک / بیمار / شعبه</small><b>'
        . ls_fa_num((string)$l['max_users']) . ' / ' . ls_fa_num((string)$l['max_doctors'])
        . ' / ' . ls_fa_num((string)$l['max_patients']) . ' / ' . ls_fa_num((string)$l['max_branches']) . '</b></div>'
        . '<div><small>آخرین تپ قلب</small><b>' . ls_shamsi($l['last_heartbeat_at'], true) . '</b></div>'
        . '<div><small>اثر انگشت دستگاه</small><b class="mono small">' . ls_e($l['hardware_fingerprint'] ?? 'متصل نشده') . '</b></div>'
        . '<div><small>کد فعال‌سازی</small><b class="mono">' . ls_e($l['activation_code'] ?? '—') . '</b></div>'
        . '<div class="full"><small>یادداشت</small><b>' . ls_e((string)($l['notes'] ?? '—')) . '</b></div>'
        . '</div>';

    // ماژول‌ها
    $mods = json_decode((string)$l['enabled_modules'], true) ?: [];
    $catalog = ls_module_catalog();
    echo '<h3>ماژول‌های فعال (' . ls_fa_num((string)count($mods)) . ')</h3><div class="modules readonly">';
    foreach ($catalog as $mc => $fa) {
        $on = in_array($mc, $mods, true);
        echo '<span class="module ' . ($on ? 'on' : 'off') . '">' . ($on ? '✅' : '⛔') . ' ' . ls_e($fa) . '</span>';
    }
    echo '</div>';

    // اقدام‌ها — تمدید / ابطال / انتقال / ویرایش / حذف
    echo '<div class="actions">';
    echo '<a class="btn btn-primary" href="index.php?page=license_edit&id=' . $id . '">✏️ ویرایش اطلاعات</a>';
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

    // حذف قطعی — در کارت جداگانه با تأیید دومرحله‌ای
    echo '<div class="card-danger-zone anim-up">'
        . '<h3>🗑 حذف قطعی لایسنس</h3>'
        . '<div class="alert alert-danger">حذف، رکورد لایسنس + فعال‌سازی‌ها + پیام‌های آن را برای همیشه پاک می‌کند و کلاینت از همان لحظه '
        . '«کلید یافت نشد» می‌گیرد. برای احتیاط، کلید لایسنس را دقیقاً وارد کنید.</div>'
        . '<form method="post" class="inline-form" onsubmit="return confirm(\'⚠️ حذف قطعی و برگشت‌ناپذیر است — مطمئنید؟\')">'
        . ls_csrf_field()
        . '<input type="hidden" name="action" value="delete_license">'
        . '<input type="hidden" name="id" value="' . $id . '">'
        . '<input type="text" name="confirm_key" dir="ltr" placeholder="' . ls_e($l['license_key']) . '" style="width:260px"> '
        . '<button class="btn btn-danger" type="submit">حذف برای همیشه</button></form></div>';

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

    // پیام‌های این درمانگاه
    $msgs = ls_db()->prepare(
        'SELECT m.*, DATE_FORMAT(m.created_at, "%Y/%m/%d %H:%i") AS c_at
         FROM ls_messages m WHERE m.license_id = ? ORDER BY m.id DESC LIMIT 20');
    $msgs->execute([$id]);
    $msgRows = $msgs->fetchAll();
    echo '<h3>📨 پیام‌های ارسالی به این درمانگاه (' . ls_fa_num((string)count($msgRows)) . ')</h3>';
    if (count($msgRows) === 0) {
        echo '<div class="empty">هنوز پیامی ارسال نشده — <a href="index.php?page=messages&license=' . $id . '">ارسال پیام جدید ←</a></div>';
    } else {
        echo '<table class="table reveal"><tr><th>عنوان</th><th>سطح</th><th>تاریخ ارسال</th><th>تحویل</th><th></th></tr>';
        $lvlText = ['info' => 'اطلاع‌رسانی', 'success' => 'خبر خوب', 'warning' => 'هشدار', 'danger' => 'مهم/فوری'];
        $lvlBadge = ['info' => 'badge-info', 'success' => 'badge-success', 'warning' => 'badge-warning', 'danger' => 'badge-danger'];
        foreach ($msgRows as $m) {
            echo '<tr><td>' . ls_e((string)$m['title']) . '</td>'
                . '<td><span class="badge ' . ($lvlBadge[$m['level']] ?? 'badge-muted') . '">' . ($lvlText[$m['level']] ?? $m['level']) . '</span></td>'
                . '<td>' . ls_e((string)$m['c_at']) . '</td>'
                . '<td>' . (empty($m['delivered_at'])
                    ? '<span class="badge badge-warning">در انتظار تپ‌قلب</span>'
                    : '<span class="badge badge-success">تحویل شد ' . ls_shamsi((string)$m['delivered_at'], true) . '</span>') . '</td>'
                . '<td><form method="post" class="inline-form" onsubmit="return confirm(\'این پیام حذف شود؟\')">'
                    . ls_csrf_field()
                    . '<input type="hidden" name="action" value="delete_message"><input type="hidden" name="id" value="' . (int)$m['id'] . '">'
                    . '<button class="btn btn-sm btn-danger" type="submit">حذف</button></form></td></tr>';
        }
        echo '</table>';
        echo '<div class="actions"><a class="btn btn-primary" href="index.php?page=messages&license=' . $id . '">📨 ارسال پیام جدید به این درمانگاه</a></div>';
    }

    // فعال‌سازی‌های این لایسنس
    $acts = ls_db()->prepare('SELECT * FROM ls_activations WHERE license_id = ? ORDER BY activated_at DESC LIMIT 10');
    $acts->execute([$id]);
    $rows = $acts->fetchAll();
    echo '<h3>🖥️ دستگاه‌های فعال‌شده</h3>';
    if (count($rows) === 0) {
        echo '<div class="empty">هنوز دستگاهی فعال نشده است.</div>';
    } else {
        echo '<table class="table reveal"><tr><th>اثر انگشت</th><th>IP</th><th>زمان فعال‌سازی</th><th>آخرین بازدید</th><th>وضعیت</th></tr>';
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
    echo '<h3>📋 لاگ اعتبارسنجی</h3><table class="table reveal"><tr><th>زمان</th><th>نتیجه</th><th>منبع</th><th>پیام</th></tr>';
    foreach ($logs->fetchAll() as $lg) {
        echo '<tr><td>' . ls_shamsi($lg['created_at'], true) . '</td><td>' . ls_e($lg['result'])
            . '</td><td>' . ($lg['source'] === 'offline' ? 'آفلاین' : 'برخط') . '</td><td class="dim">' . ls_e((string)$lg['message']) . '</td></tr>';
    }
    echo '</table>';
    ls_footer();
    exit;
}

/* ═══════════════════════  پیام به داشبورد درمانگاه — چرخه دوازدهم  ═══════════════════════ */
if ($page === 'messages') {
    $preselect = (int)($_GET['license'] ?? 0);
    ls_header('پیام به درمانگاه', $admin, 'messages');
    if ($flash !== null) {
        ls_flash($flash);
    }
    echo '<h2>📨 ارسال پیام مستقیم به داشبورد درمانگاه</h2>';
    echo '<div class="alert alert-info">پیام با <b>تپ‌قلب بعدی</b> کلاینت (چرخه روزانه یا هنگام باز شدن برنامه) روی داشبورد '
        . 'درمانگاه مقصد در کارت «پیام‌های پشتیبانی» ظاهر می‌شود — برای اعلام نسخه جدید، تعطیلی، یادآوری تمدید و…</div>';

    // فرم ارسال
    $licenses = ls_db()->query(
        'SELECT id, license_key, clinic_name, clinic_city FROM ls_licenses WHERE is_revoked = 0 ORDER BY clinic_name, id DESC LIMIT 500'
    )->fetchAll();
    echo '<form method="post" action="index.php?page=messages">' . ls_csrf_field()
        . '<input type="hidden" name="action" value="send_message">'
        . '<div class="grid-2">'
        . '<label>درمانگاه مقصد<select name="license_id" required>';
    foreach ($licenses as $li) {
        $sel = $preselect === (int)$li['id'] ? ' selected' : '';
        $name = ($li['clinic_name'] ?? 'بدون نام') . ($li['clinic_city'] ? ' — ' . $li['clinic_city'] : '');
        echo '<option value="' . (int)$li['id'] . '"' . $sel . '>' . ls_e((string)$name)
            . ' <small dir="ltr">(' . ls_e((string)$li['license_key']) . ')</small></option>';
    }
    echo '</select></label>'
        . '<label>سطح اهمیت<select name="level">'
        . '<option value="info">اطلاع‌رسانی (آبی)</option>'
        . '<option value="success">خبر خوب (سبز)</option>'
        . '<option value="warning">هشدار (نارنجی)</option>'
        . '<option value="danger">مهم/فوری (قرمز)</option>'
        . '</select></label>'
        . '<label class="full2">عنوان<input type="text" name="title" required maxlength="200" placeholder="مثلاً: نسخه ۱.۹ منتشر شد"></label>'
        . '</div>'
        . '<label>متن پیام<textarea name="body" rows="4" required maxlength="3000" placeholder="متن پیام که در داشبورد درمانگاه نمایش داده می‌شود…"></textarea></label>'
        . '<button class="btn btn-primary btn-lg" type="submit">📨 ارسال پیام</button></form>';

    // پیام‌های اخیر همه درمانگاه‌ها
    $recent = ls_db()->query(
        'SELECT m.*, l.clinic_name, l.license_key,
                DATE_FORMAT(m.created_at, "%Y/%m/%d %H:%i") AS c_at
         FROM ls_messages m
         LEFT JOIN ls_licenses l ON l.id = m.license_id
         ORDER BY m.id DESC LIMIT 30')->fetchAll();
    echo '<h3>📜 پیام‌های اخیر</h3>';
    if (count($recent) === 0) {
        echo '<div class="empty">هنوز پیامی ارسال نشده است.</div>';
    } else {
        $lvlText = ['info' => 'اطلاع‌رسانی', 'success' => 'خبر خوب', 'warning' => 'هشدار', 'danger' => 'مهم/فوری'];
        $lvlBadge = ['info' => 'badge-info', 'success' => 'badge-success', 'warning' => 'badge-warning', 'danger' => 'badge-danger'];
        echo '<table class="table reveal"><tr><th>درمانگاه</th><th>عنوان</th><th>سطح</th><th>ارسال</th><th>تحویل</th><th></th></tr>';
        foreach ($recent as $m) {
            echo '<tr><td>' . ls_e((string)($m['clinic_name'] ?? $m['license_key'] ?? '—')) . '</td>'
                . '<td><b>' . ls_e((string)$m['title']) . '</b><div class="dim small">' . ls_e(mb_substr((string)$m['body'], 0, 90)) . '…</div></td>'
                . '<td><span class="badge ' . ($lvlBadge[$m['level']] ?? 'badge-muted') . '">' . ($lvlText[$m['level']] ?? $m['level']) . '</span></td>'
                . '<td>' . ls_e((string)$m['c_at']) . '</td>'
                . '<td>' . (empty($m['delivered_at'])
                    ? '<span class="badge badge-warning">در انتظار</span>'
                    : '<span class="badge badge-success">تحویل شد</span>') . '</td>'
                . '<td><form method="post" class="inline-form" onsubmit="return confirm(\'این پیام حذف شود؟\')">'
                    . ls_csrf_field()
                    . '<input type="hidden" name="action" value="delete_message"><input type="hidden" name="id" value="' . (int)$m['id'] . '">'
                    . '<button class="btn btn-sm btn-danger" type="submit">حذف</button></form></td></tr>';
        }
        echo '</table>';
    }
    ls_footer();
    exit;
}

/* ═══════════════════════  گزارش‌های پنل — چرخه دوازدهم  ═══════════════════════ */
if ($page === 'reports') {
    $db = ls_db();
    $today = date('Y-m-d');
    ls_header('گزارش‌ها', $admin, 'reports');
    if ($flash !== null) {
        ls_flash($flash);
    }
    echo '<h2>📈 گزارش‌های سامانه لایسنس</h2>';

    /* ── گزارش ۱: نمای کلی درمانگاه‌ها ── */
    $clinics = $db->query(
        "SELECT l.id, l.license_key, l.clinic_name, l.clinic_manager, l.clinic_city, l.clinic_phone,
                l.expiry_date, l.issued_date, l.last_heartbeat_at, l.activation_count, l.max_users,
                (SELECT COUNT(*) FROM ls_activations a WHERE a.license_id = l.id AND a.is_active = 1) AS devices,
                (SELECT COUNT(*) FROM ls_check_logs c WHERE c.license_id = l.id
                    AND c.created_at >= DATE_SUB('$today', INTERVAL 30 DAY) AND c.result = 'valid') AS checks30,
                DATEDIFF(l.expiry_date, '$today') AS days_left
         FROM ls_licenses l
         WHERE l.is_revoked = 0
         ORDER BY l.clinic_name, l.id DESC LIMIT 500")->fetchAll();
    echo '<h3>🏥 نمای کلی درمانگاه‌ها (' . ls_fa_num((string)count($clinics)) . ')</h3>';
    if (count($clinics) === 0) {
        echo '<div class="empty">هنوز لایسنسی صادر نشده است.</div>';
    } else {
        echo '<table class="table reveal"><tr><th>درمانگاه</th><th>مدیر</th><th>شهر</th><th>تلفن</th>'
            . '<th>دستگاه فعال</th><th>اعتبارسنجی ۳۰ روز</th><th>روز باقیمانده</th><th></th></tr>';
        foreach ($clinics as $c) {
            $days = $c['days_left'] !== null ? (int)$c['days_left'] : null;
            echo '<tr>'
                . '<td><b>' . ls_e((string)($c['clinic_name'] ?? 'بدون نام')) . '</b><div class="dim small mono">' . ls_e((string)$c['license_key']) . '</div></td>'
                . '<td>' . ls_e((string)($c['clinic_manager'] ?? '—')) . '</td>'
                . '<td>' . ls_e((string)($c['clinic_city'] ?? '—')) . '</td>'
                . '<td dir="ltr">' . ls_e((string)($c['clinic_phone'] ?? '—')) . '</td>'
                . '<td>' . ls_fa_num((string)$c['devices']) . '</td>'
                . '<td>' . ls_fa_num((string)$c['checks30']) . '</td>'
                . '<td>' . ($days === null
                    ? '<span class="badge badge-info">دائمی</span>'
                    : ($days <= 7 ? '<span class="badge badge-danger">' : ($days <= 30 ? '<span class="badge badge-warning">' : '<span class="badge badge-success">'))
                      . ls_fa_num((string)$days) . ' روز</span>') . '</td>'
                . '<td><a class="btn btn-sm" href="index.php?page=license_view&id=' . (int)$c['id'] . '">مشاهده ←</a></td></tr>';
        }
        echo '</table>';
    }

    /* ── گزارش ۲: پوشش فعال‌سازی (نرخ اتصال دستگاه) ── */
    $total = (int)$db->query('SELECT COUNT(*) c FROM ls_licenses WHERE is_revoked=0')->fetch()['c'];
    $activated = (int)$db->query('SELECT COUNT(*) c FROM ls_licenses WHERE is_revoked=0 AND hardware_fingerprint IS NOT NULL')->fetch()['c'];
    $rate = $total > 0 ? round($activated * 100 / $total, 1) : 0;
    echo '<div class="chart-grid">';
    echo '<div class="chart-card anim-up"><h3>🔌 پوشش فعال‌سازی</h3>'
        . '<div class="progress-ring" data-percent="' . $rate . '">'
        . '<svg viewBox="0 0 120 120"><circle class="pr-bg" cx="60" cy="60" r="52"></circle>'
        . '<circle class="pr-fg" cx="60" cy="60" r="52" style="stroke-dasharray:327;stroke-dashoffset:327"></circle></svg>'
        . '<div class="pr-text"><b>' . ls_fa_num((string)$rate) . '٪</b><small>اتصال دستگاه</small></div></div>'
        . '<p class="dim">' . ls_fa_num((string)$activated) . ' از ' . ls_fa_num((string)$total)
        . ' لایسنس صادرشده روی دستگاه فعال شده‌اند.</p></div>';

    /* ── گزارش ۳: سلامت تپ‌قلب (لایسنس‌های بدون تپ‌قلب بیش از ۷ روز) ── */
    $silent = $db->query(
        "SELECT license_key, clinic_name, last_heartbeat_at
         FROM ls_licenses
         WHERE is_revoked = 0 AND hardware_fingerprint IS NOT NULL
           AND (last_heartbeat_at IS NULL OR last_heartbeat_at < DATE_SUB(NOW(), INTERVAL 7 DAY))
         ORDER BY last_heartbeat_at ASC LIMIT 50")->fetchAll();
    echo '<div class="chart-card anim-up d1"><h3>💓 لایسنس‌های خاموش (بدون تپ‌قلب +۷ روز)</h3>';
    if (count($silent) === 0) {
        echo '<div class="alert alert-success">همه لایسنس‌های متصل، تپ‌قلب سالم دارند ✅</div>';
    } else {
        echo '<table class="table"><tr><th>درمانگاه</th><th>کلید</th><th>آخرین تپ‌قلب</th></tr>';
        foreach ($silent as $s) {
            echo '<tr><td>' . ls_e((string)($s['clinic_name'] ?? '—')) . '</td>'
                . '<td class="mono small">' . ls_e((string)$s['license_key']) . '</td>'
                . '<td>' . ls_shamsi($s['last_heartbeat_at'], true) . '</td></tr>';
        }
        echo '</table>';
    }
    echo '</div></div>';

    /* ── گزارش ۴: فعال‌ترین درمانگاه‌ها (اعتبارسنجی ۳۰ روز) ── */
    $topActive = $db->query(
        "SELECT l.clinic_name, l.license_key, COUNT(*) c
         FROM ls_check_logs cl
         JOIN ls_licenses l ON l.id = cl.license_id
         WHERE cl.created_at >= DATE_SUB('$today', INTERVAL 30 DAY) AND cl.result = 'valid'
         GROUP BY cl.license_id ORDER BY c DESC LIMIT 10")->fetchAll() ?: [];
    $topPoints = array_map(static fn($r) => [
        'label' => (string)($r['clinic_name'] ?: $r['license_key']),
        'value' => (int)$r['c'],
    ], $topActive);
    echo '<div class="chart-grid">';
    echo '<div class="chart-card anim-up d2"><h3>🏆 فعال‌ترین درمانگاه‌ها (۳۰ روز اخیر)</h3>'
        . ls_svg_hbar_chart($topPoints, 'اعتبارسنجی') . '</div>';

    /* ── گزارش ۵: توزیع نتیجه اعتبارسنجی ۳۰ روز ── */
    $resultRows = $db->query(
        "SELECT result, COUNT(*) c FROM ls_check_logs
         WHERE created_at >= DATE_SUB('$today', INTERVAL 30 DAY)
         GROUP BY result ORDER BY c DESC")->fetchAll();
    $resultText = [
        'valid' => 'معتبر', 'expired' => 'منقضی', 'not_found' => 'ناموجود',
        'revoked' => 'ابطال‌شده', 'fingerprint_mismatch' => 'اثر انگشت نامطباق', 'rate_limited' => 'محدودیت نرخ',
    ];
    $resultPoints = array_map(static fn($r) => [
        'label' => $resultText[$r['result']] ?? $r['result'],
        'value' => (int)$r['c'],
    ], $resultRows);
    echo '<div class="chart-card anim-up d3"><h3>🩺 نتیجه اعتبارسنجی‌ها (۳۰ روز)</h3>'
        . ls_svg_doughnut_chart($resultPoints, 'درخواست') . '</div></div>';

    // دکمه خروجی CSV نمای کلی — در سمت کلاینت تولید می‌شود (app.js)
    echo '<div class="actions"><button class="btn btn-primary" type="button" onclick="lsExportTableToCsv(this)">📄 خروجی CSV جدول نمای کلی</button></div>';
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
    echo '<table class="table reveal"><tr><th>#</th><th>زمان</th><th>کلید</th><th>نتیجه</th><th>منبع</th><th>IP</th><th>پیام</th></tr>';
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

    // ── بخش اطلاعات پشتیبانی محصول ──
    $sup = ls_support_info();
    echo '<form method="post" action="index.php?page=settings">' . ls_csrf_field()
        . '<input type="hidden" name="action" value="save_support">'
        . '<h3>🧑‍💻 اطلاعات پشتیبانی محصول</h3>'
        . '<div class="alert alert-info">این اطلاعات هنگام <b>فعال‌سازی اولیه</b> به نرم‌افزار کلاینت ارسال می‌شود و در سراسر برنامه '
        . '(صفحه لایسنس، درباره برنامه، فوتر و راهنماها) نمایش داده می‌شود. هر تغییر پس از تپ‌قلب بعدی در کلاینت‌ها به‌روز می‌شود.</div>'
        . '<div class="grid-2">'
        . '<label>نام پشتیبان<input type="text" name="support_name" value="' . ls_e($sup['name']) . '" required></label>'
        . '<label>نام شرکت<input type="text" name="support_company" value="' . ls_e($sup['company']) . '" required></label>'
        . '<label>تلفن ثابت (با کد)<input type="text" name="support_phone" value="' . ls_e($sup['phone']) . '" dir="ltr" placeholder="041-XXXXXXX"></label>'
        . '<label>موبایل<input type="text" name="support_mobile" value="' . ls_e($sup['mobile']) . '" dir="ltr" placeholder="09XXXXXXXXX"></label>'
        . '<label>ایمیل<input type="email" name="support_email" value="' . ls_e($sup['email']) . '" dir="ltr"></label>'
        . '<label>وب‌سایت<input type="text" name="support_website" value="' . ls_e($sup['website']) . '" dir="ltr"></label>'
        . '</div><button class="btn btn-primary" type="submit">💾 ذخیره اطلاعات پشتیبانی</button></form>';

    echo '<form method="post" action="index.php?page=settings">' . ls_csrf_field()
        . '<input type="hidden" name="action" value="save_settings">'
        . '<div class="grid-2">'
        . '<label>عنوان پنل<input type="text" name="portal_title" value="' . ls_e(ls_setting('portal_title')) . '"></label>'
        . '<label>حداکثر درخواست API در پنجره<input type="number" name="api_rate_max" value="' . ls_e(ls_setting('api_rate_max', '60')) . '" min="10"></label>'
        . '<label>پنجره محدودیت نرخ (ثانیه)<input type="number" name="api_rate_window" value="' . ls_e(ls_setting('api_rate_window', '600')) . '" min="60"></label>'
        . '<label>مهلت آفلاین کلاینت (روز)<input type="number" name="offline_grace_days" value="' . ls_e(ls_setting('offline_grace_days', '30')) . '" min="0"></label>'
        . '</div><button class="btn btn-primary" type="submit">💾 ذخیره تنظیمات</button></form>';

    echo '<h3>🔧 سلامت و ترمیم بانک اطلاعاتی</h3>';
    echo '<div class="alert alert-info">اگر هنگام صدور لایسنس یا سایر عملیات با خطای «HTTP 500» روبه‌رو شدید، این دکمه جداول و ستون‌های گمشده را '
        . 'به‌صورت خودکار می‌سازد (بدون دست‌زدن به داده‌های موجود). پس از به‌روزرسانی دستی فایل‌های سرور روی هاست هم اجرایش توصیه می‌شود.</div>';
    echo '<form method="post" action="index.php?page=settings">' . ls_csrf_field()
        . '<input type="hidden" name="action" value="repair_db">'
        . '<button class="btn btn-primary" type="submit">🔧 بررسی و ترمیم بانک اطلاعاتی</button></form>';

    // آخرین خطاهای ثبت‌شده
    $errTail = ls_log_error_tail(20);
    if (count($errTail) > 0) {
        echo '<h3>🩺 آخرین خطاهای ثبت‌شده (data/error.log)</h3>';
        echo '<div class="alert alert-warning">این خطاها برای ریشه‌یابی ثبت شده‌اند — اگر خطای تازه‌ای می‌بینید،'
            . ' همین متن‌ها را برای پشتیبانی ارسال کنید.</div>';
        echo '<pre class="code" dir="ltr" style="max-height:260px;overflow:auto">'
            . ls_e(implode("\n", $errTail)) . '</pre>';
    }

    echo '<h3>🔑 کلیدهای RSA</h3><div class="alert alert-warning">کلید عمومی را در فایل <code>appsettings.json</code> کلاینت '
        . '(بخش License → RsaPublicKey) جای‌گذاری کنید. قالب XML برای <code>RSA.FromXmlString</code> و قالب PEM برای '
        . '<code>ImportFromPem</code> هر دو پشتیبانی می‌شوند.</div>';
    echo '<h4>کلید عمومی — قالب XML (دات‌نت)</h4><pre class="code">' . ls_e(ls_public_key_xml()) . '</pre>';
    echo '<h4>کلید عمومی — قالب PEM</h4><pre class="code">' . ls_e(ls_public_key_pem()) . '</pre>';

    /* ═══ چرخهٔ ۶۶ — بکاپ بانک: دکمهٔ دستی + راهنمای کرون خودکار ═══ */
    echo '<h3>💾 بکاپ بانک اطلاعاتی</h3>';
    echo '<div class="alert alert-info">بکاپ کامل بانک (کلیدها، مشتریان، پیام‌ها) به‌صورت فایل فشردهٔ '
        . '<code>sql.gz</code> در پوشهٔ <code>data/backups</code> ذخیره می‌شود و ۱۴ نسخهٔ آخر نگه‌داشته می‌شود. '
        . 'برای <b>بکاپ خودکار روزانه</b>، کار زمان‌بندی‌شدهٔ زیر را در cPanel → Cron Jobs ثبت کنید:'
        . '<pre class="code" dir="ltr">php /home/USER/public_html/license/cron.php YOUR_SECRET</pre>'
        . 'مقدار SECRET همان LS_CRON_SECRET فایل <code>data/config.php</code> است (توصیه: ۳:۳۰ بامداد). '
        . 'همین کار کرون، پاک‌سازی لاگ‌های کهنه را هم انجام می‌دهد.</div>';
    // فهرست بکاپ‌های موجود — شفافیت وضعیت برای مدیر
    $backups = glob(LS_DATA_DIR . '/backups/license-backup-*.sql.gz') ?: [];
    rsort($backups);
    if (count($backups) > 0) {
        echo '<table class="table reveal"><tr><th>فایل بکاپ</th><th>حجم</th><th>زمان</th></tr>';
        foreach (array_slice($backups, 0, 10) as $bf) {
            $bn = basename((string)$bf);
            $sz = @filesize($bf) ?: 0;
            // نام زمان‌دار — نمایش تاریخ از خود نام فایل
            echo '<tr><td dir="ltr">' . ls_e($bn) . '</td><td>' . ls_fa_num(number_format($sz / 1024, 1)) . ' KB</td>'
                . '<td class="dim">' . ls_e(mb_substr($bn, 15, 8)) . '</td></tr>';
        }
        echo '</table>';
    } else {
        echo '<div class="alert alert-warning">هنوز هیچ بکاپی موجود نیست — با دکمهٔ زیر نخستین بکاپ را بگیرید.</div>';
    }
    echo '<form method="post" action="index.php?page=settings">' . ls_csrf_field()
        . '<input type="hidden" name="action" value="backup_now">'
        . '<button class="btn btn-primary" type="submit">💾 بکاپ فوری همین حالا</button></form>';

    /* ═══ چرخهٔ ۶۶ — چرخش امانت‌دار (جایگزین بازتولید ویرانگر) ═══ */
    echo '<h3>🔄 چرخش کلیدهای RSA (امانت‌دار)</h3>';
    echo '<div class="alert alert-info">چرخش کلید: کلید قدیمی در <code>data/keys-archive</code> بایگانی می‌شود، '
        . 'جفت‌کلید جدید ساخته می‌شود و <b>همهٔ لایسنس‌های موجود به‌طور خودکار با کلید جدید امضا می‌شوند</b> — '
        . 'بعد از جای‌گذاری کلید عمومی جدید در کلاینت‌ها، هیچ لایسنسی نامعتبر نمی‌شود. توصیه: چرخش سالانه.</div>';
    echo '<form method="post" action="index.php?page=settings" onsubmit="return confirm(\'هشدار! کلید عمومی جدید باید در همه کلاینت‌ها (appsettings → License → RsaPublicKey) جای‌گذاری شود. لایسنس‌ها خودکار دوباره امضا می‌شوند. مطمئنید؟\')">'
        . ls_csrf_field()
        . '<input type="hidden" name="action" value="rotate_keys">'
        . '<label>برای تأیید عبارت <code>REGENERATE</code> را بنویسید<input type="text" name="confirm_text" placeholder="REGENERATE"></label>'
        . '<button class="btn btn-danger" type="submit">🔄 چرخش جفت‌کلید + امضای دوبارهٔ لایسنس‌ها</button></form>';

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
    echo '<h3>🧾 آخرین فعالیت‌های مدیریتی</h3><table class="table reveal">'
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
