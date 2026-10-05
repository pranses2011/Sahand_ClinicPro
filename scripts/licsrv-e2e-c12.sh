#!/bin/bash
# ════════════════════════════════════════════════════════════════
# تست E2E چرخه دوازدهم — ارتقای پنل لایسنس PHP
# سناریوهای تازه:
#   ۱) نصب تمیز + ورود
#   ۲) صدور لایسنس با اطلاعات کامل درمانگاه (مدیر/شهر/آدرس/ایمیل)
#   ۳) ویرایش لایسنس (تغییر شهر + سقف‌ها) — باید ذخیره شود
#   ۴) ارسال پیام به درمانگاه
#   ۵) API heartbeat → بلاک messages باید پیام را برگرداند + تحویل‌شده
#   ۶) heartbeat دوم → نباید پیام تکراری برگردد
#   ۷) صفحه گزارش‌ها + داشبورد (نمودارها) → باید ۲۰۰ با SVG بدهند
#   ۸) حذف پیام + حذف لایسنس (با کلید اشتباه رد؛ با کلید درست انجام)
#   ۹) مهاجرت استقرار قدیمی: حذف ستون clinic_city → نشست تازه → صدور موفق
# ════════════════════════════════════════════════════════════════
set -u
export LD_LIBRARY_PATH=/tmp/mariadb/root/usr/lib/x86_64-linux-gnu:/tmp/mariadb/root/usr/lib/mariadb
export OPENSSL_CONF=/tmp/openssl.cnf
PHP=/tmp/php
LS=/home/z/my-project/Sahand_ClinicPro/src/LicenseServer.Php
TESTDIR=/tmp/lsc12
PASS=0; FAIL=0

say() { echo ""; echo "════ $* ════"; }
ok()  { PASS=$((PASS+1)); echo "✅ $*"; }
bad() { FAIL=$((FAIL+1)); echo "❌ $*"; }

# ── آماده‌سازی: نصب تمیز ──
say "آماده‌سازی بانک و نصب تمیز"
rm -rf $TESTDIR && mkdir -p $TESTDIR
cp -r $LS/* $TESTDIR/
rm -rf $TESTDIR/data && mkdir -p $TESTDIR/data
chmod 777 $TESTDIR/data
MYSQL="$PHP -r"
$PHP -r '
try { $p = new PDO("mysql:host=127.0.0.1;port=33061", "root", "");
  $p->exec("DROP DATABASE IF EXISTS lsc12");
  $p->exec("CREATE DATABASE lsc12 CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci");
  echo "DB-OK\n";
} catch (Exception $e) { die("DB-FAIL: " . $e->getMessage() . "\n"); }'

cat > $TESTDIR/data/config.php <<CFG
<?php
define('LS_DB_HOST', '127.0.0.1');
define('LS_DB_PORT', 33061);
define('LS_DB_NAME', 'lsc12');
define('LS_DB_USER', 'root');
define('LS_DB_PASS', '');
CFG

# اجرای نصب‌کننده — مرحله نهایی (finish) مستقیماً با اطلاعات دیتابیس
$PHP -r '
$_POST = [
  "step" => "finish",
  "db_host" => "127.0.0.1", "db_port" => "33061",
  "db_name" => "lsc12", "db_user" => "root", "db_pass" => "",
  "portal_title" => "سامانه لایسنس تست",
  "admin_name" => "مدیر تست", "admin_user" => "admin", "admin_pass" => "Admin12345",
];
$_SERVER["REQUEST_METHOD"] = "POST";
$_SERVER["REMOTE_ADDR"] = "127.0.0.1";
ob_start();
chdir("/tmp/lsc12");
include "/tmp/lsc12/install.php";
// نصب‌کننده با exit تمام می‌شود — خروجی را کنار می‌گذاریم و صحت را از دیتابیس می‌سنجیم
ob_end_clean();
echo "INSTALL-RAN\n";' 2>&1 | grep -q 'INSTALL-RAN' || true
# راستی‌آزمایی از روی خود دیتابیس — ادمین + جداول + ستون‌های جدید
$PHP -r '
try {
  $p = new PDO("mysql:host=127.0.0.1;port=33061;dbname=lsc12", "root", "");
  $admins = (int)$p->query("SELECT COUNT(*) FROM ls_admins")->fetchColumn();
  $tables = array_flip($p->query("SHOW TABLES")->fetchAll(PDO::FETCH_COLUMN));
  $cols = array_flip($p->query("SHOW COLUMNS FROM ls_licenses")->fetchAll(PDO::FETCH_COLUMN));
  $ok = $admins === 1
     && isset($tables["ls_messages"]) && isset($tables["ls_licenses"]) && isset($tables["ls_settings"])
     && isset($cols["clinic_manager"]) && isset($cols["clinic_city"])
     && isset($cols["clinic_address"]) && isset($cols["clinic_email"]);
  echo ($ok ? "INSTALL-VERIFIED\n" : "INSTALL-INVALID admins=$admins\n");
} catch (Exception $e) { die("INSTALL-FAIL: " . $e->getMessage() . "\n"); }' | grep -q 'INSTALL-VERIFIED' && ok "نصب کامل (ادمین + جداول + ۴ ستون جدید + ls_messages)" || bad "راستی‌آزمایی نصب شکست خورد"

# وب‌سرور داخلی PHP
cd $TESTDIR
$PHP -S 127.0.0.1:8899 >/tmp/lsc12-server.log 2>&1 &
SRV=$!
sleep 1

# ── ورود ──
say "ورود به پنل"
COOKIE=/tmp/lsc12-cookies.txt
rm -f $COOKIE
# صفحه ورود برای توکن CSRF
LOGIN_HTML=$(curl -s -c $COOKIE "http://127.0.0.1:8899/index.php?page=login")
CSRF=$(echo "$LOGIN_HTML" | grep -o 'name="csrf" value="[^"]*"' | head -1 | sed 's/.*value="//;s/"//')
LOGIN_CODE=$(curl -s -b $COOKIE -c $COOKIE -o /dev/null -w "%{http_code}" -X POST "http://127.0.0.1:8899/index.php?page=login" \
  --data-urlencode "action=login" --data-urlencode "csrf=$CSRF" \
  --data-urlencode "username=admin" --data-urlencode "password=Admin12345")
[ "$LOGIN_CODE" = "302" ] && ok "ورود موفق (302)" || bad "ورود: $LOGIN_CODE"

# تابع کمکی برای گرفتن CSRF تازه از هر صفحه
get_csrf() {
  curl -s -b $COOKIE "http://127.0.0.1:8899/index.php?page=$1" | grep -o 'name="csrf" value="[^"]*"' | head -1 | sed 's/.*value="//;s/"//'
}

# ── صدور لایسنس با اطلاعات کامل ──
say "صدور لایسنس با اطلاعات کامل درمانگاه"
CSRF=$(get_csrf license_new)
ISSUE_CODE=$(curl -s -b $COOKIE -o /dev/null -w "%{http_code}" -X POST "http://127.0.0.1:8899/index.php?page=license_new" \
  --data-urlencode "action=create_license" --data-urlencode "csrf=$CSRF" \
  --data-urlencode "license_type=1" --data-urlencode "duration_months=12" \
  --data-urlencode "clinic_name=درمانگاه تخصصی سپید" \
  --data-urlencode "clinic_phone=041-35500000" \
  --data-urlencode "clinic_manager=دکتر رضایی" \
  --data-urlencode "clinic_email=info@sepid.ir" \
  --data-urlencode "clinic_city=تبریز" \
  --data-urlencode "clinic_address=خیابان آبرسان، ساختمان سپید" \
  --data-urlencode "max_users=8" --data-urlencode "max_doctors=15" \
  --data-urlencode "max_patients=8000" --data-urlencode "max_branches=2" \
  --data-urlencode "notes=پکیج کامل" \
  --data-urlencode "modules[]=Dashboard" --data-urlencode "modules[]=Patients" --data-urlencode "modules[]=Billing")
[ "$ISSUE_CODE" = "302" ] && ok "صدور موفق (302)" || bad "صدور: $ISSUE_CODE"

VIEW_HTML=$(curl -s -b $COOKIE "http://127.0.0.1:8899/index.php?page=license_view&id=1")
echo "$VIEW_HTML" | grep -q "دکتر رضایی" && ok "مدیر درمانگاه در جزئیات ثبت شد" || bad "مدیر درمانگاه ثبت نشد"
echo "$VIEW_HTML" | grep -q "تبریز" && ok "شهر در جزئیات ثبت شد" || bad "شهر ثبت نشد"
echo "$VIEW_HTML" | grep -q "خیابان آبرسان" && ok "نشانی در جزئیات ثبت شد" || bad "نشانی ثبت نشد"
LKEY=$(echo "$VIEW_HTML" | grep -o 'SCPRO-[A-Z2-9]\{5\}-[A-Z2-9]\{5\}-[A-Z2-9]\{5\}' | head -1)
[ -n "$LKEY" ] && ok "کلید استخراج شد: $LKEY" || bad "کلید یافت نشد"

# ── ویرایش ──
say "ویرایش لایسنس"
CSRF=$(get_csrf "license_edit&id=1")
EDIT_CODE=$(curl -s -b $COOKIE -o /dev/null -w "%{http_code}" -X POST "http://127.0.0.1:8899/index.php?page=license_edit&id=1" \
  --data-urlencode "action=update_license" --data-urlencode "csrf=$CSRF" --data-urlencode "id=1" \
  --data-urlencode "clinic_name=درمانگاه تخصصی سپید" \
  --data-urlencode "clinic_phone=041-35500001" \
  --data-urlencode "clinic_manager=دکتر رضایی‌فر" \
  --data-urlencode "clinic_email=info@sepid.ir" \
  --data-urlencode "clinic_city=مراغه" \
  --data-urlencode "clinic_address=خیابان امام" \
  --data-urlencode "max_users=12" --data-urlencode "max_doctors=20" \
  --data-urlencode "max_patients=9000" --data-urlencode "max_branches=3" \
  --data-urlencode "notes=ویرایش شده" \
  --data-urlencode "modules[]=Dashboard" --data-urlencode "modules[]=Patients" \
  --data-urlencode "modules[]=Billing" --data-urlencode "modules[]=Laboratory")
[ "$EDIT_CODE" = "302" ] && ok "ویرایش موفق (302)" || bad "ویرایش: $EDIT_CODE"
VIEW2=$(curl -s -b $COOKIE "http://127.0.0.1:8899/index.php?page=license_view&id=1")
echo "$VIEW2" | grep -q "مراغه" && ok "شهر ویرایش‌شده ذخیره شد" || bad "ویرایش شهر اعمال نشد"
echo "$VIEW2" | grep -q "رضایی‌فر" && ok "مدیر ویرایش‌شده ذخیره شد" || bad "ویرایش مدیر اعمال نشد"

# ── ارسال پیام ──
say "ارسال پیام به داشبورد درمانگاه"
CSRF=$(get_csrf messages)
MSG_CODE=$(curl -s -b $COOKIE -o /dev/null -w "%{http_code}" -X POST "http://127.0.0.1:8899/index.php?page=messages" \
  --data-urlencode "action=send_message" --data-urlencode "csrf=$CSRF" \
  --data-urlencode "license_id=1" --data-urlencode "level=info" \
  --data-urlencode "title=نسخه ۱.۹.۰ منتشر شد" \
  --data-urlencode "body=نسخه جدید با گزارش‌های نموداری تازه آماده نصب است.")
[ "$MSG_CODE" = "302" ] && ok "ارسال پیام موفق (302)" || bad "ارسال پیام: $MSG_CODE"
MSG_PAGE=$(curl -s -b $COOKIE "http://127.0.0.1:8899/index.php?page=messages")
echo "$MSG_PAGE" | grep -q "در انتظار" && ok "پیام در فهرست «در انتظار تپ‌قلب» است" || bad "پیام در فهرست نیست"

# ── فعال‌سازی + تپ‌قلب با پیام ──
say "فعال‌سازی و تحویل پیام از مسیر API"
FP="FINGERPRINT-C12-TEST-0123456789ABCDEF"
ACT=$(curl -s -X POST "http://127.0.0.1:8899/api.php" -H "Content-Type: application/json" \
  -d "{\"action\":\"activate\",\"licenseKey\":\"$LKEY\",\"fingerprint\":\"$FP\",\"clinicName\":\"درمانگاه سپید\",\"appVersion\":\"1.9.0\"}")
echo "$ACT" | grep -q '"isValid":true' && ok "فعال‌سازی موفق" || bad "فعال‌سازی ناموفق: $(echo $ACT | head -c 200)"
echo "$ACT" | grep -q '"messages"' && ok "بلاک messages در پاسخ فعال‌سازی هست" || bad "بلاک messages نیست"
echo "$ACT" | grep -q 'نسخه ۱.۹.۰' && ok "عنوان پیام در پاسخ هست" || bad "عنوان پیام در پاسخ نیست"

# تپ‌قلب دوم — نباید پیام تکراری بیاید
HB=$(curl -s -X POST "http://127.0.0.1:8899/api.php" -H "Content-Type: application/json" \
  -d "{\"action\":\"heartbeat\",\"licenseKey\":\"$LKEY\",\"fingerprint\":\"$FP\"}")
echo "$HB" | grep -q '"isValid":true' && ok "تپ‌قلب دوم معتبر" || bad "تپ‌قلب دوم رد شد"
echo "$HB" | grep -q '"messages"' && bad "پیام تکراری تحویل شد!" || ok "پیام تکراری تحویل نشد (تحویل تک‌بار)"

# ── صفحات نموداری ──
say "صفحات داشبورد/گزارش‌ها با نمودار SVG"
DASH=$(curl -s -b $COOKIE "http://127.0.0.1:8899/index.php?page=dashboard")
echo "$DASH" | grep -q '<svg' && ok "داشبورد نمودار SVG دارد" || bad "داشبورد بدون SVG"
echo "$DASH" | grep -q 'chart-area' && ok "نمودار سطحی روند ۱۴روزه هست" || bad "نمودار سطحی نیست"
echo "$DASH" | grep -q 'chart-donut' && ok "نمودار دونات وضعیت هست" || bad "دونات نیست"
echo "$DASH" | grep -q 'chart-hbar' && ok "نمودار میله‌ای ماژول‌ها هست" || bad "میله‌ای نیست"
echo "$DASH" | grep -q 'app.js' && ok "اسکریپت انیمیشن متصل است" || bad "app.js متصل نیست"

REP=$(curl -s -b $COOKIE "http://127.0.0.1:8899/index.php?page=reports")
echo "$REP" | grep -q 'progress-ring' && ok "گزارش‌ها حلقه پیشرفت دارد" || bad "حلقه پیشرفت نیست"
echo "$REP" | grep -q 'نمای کلی درمانگاه‌ها' && ok "گزارش نمای کلی هست" || bad "گزارش نمای کلی نیست"
echo "$REP" | grep -q 'لایسنس‌های خاموش' && ok "گزارش تپ‌قلب خاموش هست" || bad "گزارش خاموش نیست"

# ── حذف پیام ──
say "حذف پیام"
CSRF=$(get_csrf messages)
DEL_MSG=$(curl -s -b $COOKIE -o /dev/null -w "%{http_code}" -X POST "http://127.0.0.1:8899/index.php?page=messages" \
  --data-urlencode "action=delete_message" --data-urlencode "csrf=$CSRF" --data-urlencode "id=1")
[ "$DEL_MSG" = "302" ] && ok "حذف پیام موفق" || bad "حذف پیام: $DEL_MSG"

# ── حذف لایسنس: کلید غلط → رد؛ کلید درست → انجام ──
say "حذف لایسنس با حفاظت دومرحله‌ای"
CSRF=$(get_csrf "license_view&id=1")
WRONG=$(curl -s -b $COOKIE -o /dev/null -w "%{http_code}" -X POST "http://127.0.0.1:8899/index.php?page=license_view&id=1" \
  --data-urlencode "action=delete_license" --data-urlencode "csrf=$CSRF" \
  --data-urlencode "id=1" --data-urlencode "confirm_key=WRONG-KEY")
[ "$WRONG" = "302" ] && ok "کلید غلط پذیرفته نشد (بازگشت با فلش خطا)" || bad "کلید غلط: $WRONG"
# بررسی اینکه لایسنس هنوز هست
$PHP -r '$p=new PDO("mysql:host=127.0.0.1;port=33061;dbname=lsc12","root",""); $c=$p->query("SELECT COUNT(*) FROM ls_licenses")->fetchColumn(); echo ($c==1?"EXISTS\n":"GONE\n");' | grep -q EXISTS && ok "لایسنس با کلید غلط حذف نشد (حفاظت فعال)" || bad "لایسنس با کلید غلط حذف شد!"

RIGHT=$(curl -s -b $COOKIE -o /dev/null -w "%{http_code}" -X POST "http://127.0.0.1:8899/index.php?page=license_view&id=1" \
  --data-urlencode "action=delete_license" --data-urlencode "csrf=$CSRF" \
  --data-urlencode "id=1" --data-urlencode "confirm_key=$LKEY")
[ "$RIGHT" = "302" ] && ok "حذف با کلید درست موفق" || bad "حذف: $RIGHT"
$PHP -r '$p=new PDO("mysql:host=127.0.0.1;port=33061;dbname=lsc12","root",""); $c=$p->query("SELECT COUNT(*) FROM ls_licenses")->fetchColumn(); echo ($c==0?"GONE\n":"EXISTS\n");' | grep -q GONE && ok "لایسنس واقعاً حذف شد" || bad "لایسنس حذف نشد"

# ── استقرار قدیمی: حذف ستون جدید → مهاجرت خودکار ──
say "شبیه‌سازی استقرار قدیمی (حذف ستون clinic_city)"
$PHP -r '$p=new PDO("mysql:host=127.0.0.1;port=33061;dbname=lsc12","root",""); $p->exec("ALTER TABLE ls_licenses DROP COLUMN clinic_city"); echo "COL-DROPPED\n";' | grep -q COL-DROPPED && ok "ستون clinic_city حذف شد"

# نشست تازه = بازدید بعدی مدیر؛ مهاجرت خودکار (یک‌بار-به‌نشست) باید ستون را بازسازی کند
rm -f $COOKIE
LOGIN_HTML2=$(curl -s -c $COOKIE "http://127.0.0.1:8899/index.php?page=login")
CSRF2=$(echo "$LOGIN_HTML2" | grep -o 'name="csrf" value="[^"]*"' | head -1 | sed 's/.*value="//;s/"//')
curl -s -b $COOKIE -c $COOKIE -o /dev/null -X POST "http://127.0.0.1:8899/index.php?page=login" \
  --data-urlencode "action=login" --data-urlencode "csrf=$CSRF2" \
  --data-urlencode "username=admin" --data-urlencode "password=Admin12345"
# دیدن داشبورد در نشست تازه → اجرای مهاجرت خودکار
curl -s -b $COOKIE -o /dev/null "http://127.0.0.1:8899/index.php?page=dashboard"
$PHP -r '$p=new PDO("mysql:host=127.0.0.1;port=33061;dbname=lsc12","root",""); $c=$p->query("SELECT COUNT(*) FROM information_schema.COLUMNS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME=\"ls_licenses\" AND COLUMN_NAME=\"clinic_city\"")->fetchColumn(); echo ($c==1?"REBUILT\n":"MISSING\n");' | grep -q REBUILT && ok "مهاجرت خودکار در نشست تازه ستون را بازسازی کرد" || bad "ستون clinic_city بازسازی نشد"

CSRF=$(get_csrf license_new)
REISSUE=$(curl -s -b $COOKIE -o /dev/null -w "%{http_code}" -X POST "http://127.0.0.1:8899/index.php?page=license_new" \
  --data-urlencode "action=create_license" --data-urlencode "csrf=$CSRF" \
  --data-urlencode "license_type=1" --data-urlencode "duration_months=6" \
  --data-urlencode "clinic_name=کلینیک مهاجرت" --data-urlencode "clinic_city=تهران" \
  --data-urlencode "modules[]=Dashboard" --data-urlencode "modules[]=Patients")
[ "$REISSUE" = "302" ] && ok "صدور پس از مهاجرت خودکار موفق (302)" || bad "صدور پس از مهاجرت: $REISSUE"
$PHP -r '$p=new PDO("mysql:host=127.0.0.1;port=33061;dbname=lsc12","root",""); $r=$p->query("SELECT clinic_name, clinic_city FROM ls_licenses ORDER BY id DESC LIMIT 1")->fetch(PDO::FETCH_ASSOC); echo ($r && $r["clinic_city"]=="تهران"?"MIGRATED-OK\n":"MIGRATED-BAD\n");' | grep -q 'MIGRATED-OK' && ok "لایسنس مهاجرت با شهر تهران ثبت شد" || bad "ثبت لایسنس مهاجرت ناقص"

# ── پایان ──
kill $SRV 2>/dev/null
say "نتیجه نهایی"
echo "════════════════════════════════"
echo "PASS: $PASS | FAIL: $FAIL"
[ $FAIL -eq 0 ] && echo "ALL-GREEN ✅" || echo "HAS-FAILURES ❌"
