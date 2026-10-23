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


# ═══ چرخهٔ ۸۱ — ریشهٔ قرمزیِ دائمیِ این job ═══
# سرویس MySQL در CI رمز root را «testroot» می‌گذارد (MYSQL_ROOT_PASSWORD)، ولی
# همهٔ اتصال‌های این اسکریپت با رمز خالی («root», "") باز می‌شدند؛ نتیجه: هر
# پرس‌وجو Access denied → همهٔ بررسی‌ها ❌ و خروج با کد ۱. اکنون رمز از همان
# متغیر محیطی خوانده می‌شود (روی ماشین محلی با رمز خالی هم کار می‌کند).
PHP=php
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
LS="$SCRIPT_DIR/../src/LicenseServer.Php"
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
try { $p = new PDO("mysql:host=127.0.0.1;port=3306", "root", (getenv("MYSQL_ROOT_PASSWORD") ?: ""));
  $p->exec("DROP DATABASE IF EXISTS lsc12");
  $p->exec("CREATE DATABASE lsc12 CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci");
  echo "DB-OK\n";
} catch (Exception $e) { die("DB-FAIL: " . $e->getMessage() . "\n"); }' | tee /tmp/lsc12-db-bootstrap.log
# گارد صریح: اگر ساخت دیتابیس شکست بخورد، ادامهٔ ۳۰ بررسی بعدی فقط «❌» تولید
# می‌کند و ریشه (رمز/سرویس MySQL) در انبوه خروجی گم می‌شود
if ! grep -q 'DB-OK' /tmp/lsc12-db-bootstrap.log; then
  echo "❌ آماده‌سازی دیتابیس MySQL شکست خورد — رمز root (MYSQL_ROOT_PASSWORD) یا سرویس را بررسی کنید"
  cat /tmp/lsc12-db-bootstrap.log
  exit 1
fi

cat > $TESTDIR/data/config.php <<CFG
<?php
define('LS_DB_HOST', '127.0.0.1');
define('LS_DB_PORT', 3306);
define('LS_DB_NAME', 'lsc12');
define('LS_DB_USER', 'root');
define('LS_DB_PASS', getenv('MYSQL_ROOT_PASSWORD') ?: '');
CFG

# اجرای نصب‌کننده — مرحله نهایی (finish) مستقیماً با اطلاعات دیتابیس
$PHP -r '
$_POST = [
  "step" => "finish",
  "db_host" => "127.0.0.1", "db_port" => "3306",
  "db_name" => "lsc12", "db_user" => "root", "db_pass" => getenv("MYSQL_ROOT_PASSWORD") ?: "",
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
  $p = new PDO("mysql:host=127.0.0.1;port=3306;dbname=lsc12", "root", (getenv("MYSQL_ROOT_PASSWORD") ?: ""));
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

# تپ‌قلب دوم — «تحویل تک‌بار» باید برقرار بماند
HB=$(curl -s -X POST "http://127.0.0.1:8899/api.php" -H "Content-Type: application/json" \
  -d "{\"action\":\"heartbeat\",\"licenseKey\":\"$LKEY\",\"fingerprint\":\"$FP\"}")
echo "$HB" | grep -q '"isValid":true' && ok "تپ‌قلب دوم معتبر" || bad "تپ‌قلب دوم رد شد"
# ═══ ریشه‌یابی (چرخهٔ ۸۰) ═══
# این بررسی پیش‌تر «نبودن کلید messages» را نشانهٔ تحویل تک‌بار می‌گرفت، ولی
# چرخهٔ ۶۷ عمداً پروتکل را عوض کرد: فهرست کامل پیام‌ها (تحویل‌شده + تحویل‌نشده)
# همیشه در پاسخ هست تا داشبورد آینهٔ پنل بماند. بنابراین نبودِ messages دیگر
# درست نیست و باید «تکرار نشدن خودِ پیام» سنجیده شود: عنوان دقیقاً یک بار در
# payload + تعداد ردیف‌های جدول بدون تغییر + delivered_at ست شده باشد.
MSG_ROWS_BEFORE=$($PHP -r '$p=new PDO("mysql:host=127.0.0.1;port=3306;dbname=lsc12","root",(getenv("MYSQL_ROOT_PASSWORD")?:""));
echo (string)$p->query("SELECT COUNT(*) FROM ls_messages WHERE license_id=1")->fetchColumn();')
HB_TITLE_COUNT=$(printf '%s' "$HB" | grep -o 'نسخه ۱.۹.۰' | wc -l | tr -d ' ')
[ "$HB_TITLE_COUNT" = "1" ] \
  && ok "پیام در تپ‌قلب دوم تک‌بار برگشت (تکرار $HB_TITLE_COUNT)" \
  || bad "پیام تکراری در payload تپ‌قلب دوم! (تکرار: $HB_TITLE_COUNT)"
MSG_ROWS_AFTER=$($PHP -r '$p=new PDO("mysql:host=127.0.0.1;port=3306;dbname=lsc12","root",(getenv("MYSQL_ROOT_PASSWORD")?:""));
echo (string)$p->query("SELECT COUNT(*) FROM ls_messages WHERE license_id=1")->fetchColumn();')
[ "$MSG_ROWS_BEFORE" = "$MSG_ROWS_AFTER" ] \
  && ok "تپ‌قلب دوم ردیف پیام تازه نساخت (تحویل تک‌بار)" \
  || bad "تپ‌قلب دوم ردیف پیام تازه ساخت! ($MSG_ROWS_BEFORE → $MSG_ROWS_AFTER)"
MSG_DELIVERED=$($PHP -r '$p=new PDO("mysql:host=127.0.0.1;port=3306;dbname=lsc12","root",(getenv("MYSQL_ROOT_PASSWORD")?:""));
echo (string)$p->query("SELECT COUNT(*) FROM ls_messages WHERE license_id=1 AND delivered_at IS NOT NULL")->fetchColumn();')
[ "$MSG_DELIVERED" -ge 1 ] \
  && ok "پیام پس از نخستین تحویل delivered_at گرفت ($MSG_DELIVERED ردیف)" \
  || bad "delivered_at پیام ست نشد"

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
$PHP -r '$p=new PDO("mysql:host=127.0.0.1;port=3306;dbname=lsc12","root",(getenv("MYSQL_ROOT_PASSWORD")?:"")); $c=$p->query("SELECT COUNT(*) FROM ls_licenses")->fetchColumn(); echo ($c==1?"EXISTS\n":"GONE\n");' | grep -q EXISTS && ok "لایسنس با کلید غلط حذف نشد (حفاظت فعال)" || bad "لایسنس با کلید غلط حذف شد!"

RIGHT=$(curl -s -b $COOKIE -o /dev/null -w "%{http_code}" -X POST "http://127.0.0.1:8899/index.php?page=license_view&id=1" \
  --data-urlencode "action=delete_license" --data-urlencode "csrf=$CSRF" \
  --data-urlencode "id=1" --data-urlencode "confirm_key=$LKEY")
[ "$RIGHT" = "302" ] && ok "حذف با کلید درست موفق" || bad "حذف: $RIGHT"
$PHP -r '$p=new PDO("mysql:host=127.0.0.1;port=3306;dbname=lsc12","root",(getenv("MYSQL_ROOT_PASSWORD")?:"")); $c=$p->query("SELECT COUNT(*) FROM ls_licenses")->fetchColumn(); echo ($c==0?"GONE\n":"EXISTS\n");' | grep -q GONE && ok "لایسنس واقعاً حذف شد" || bad "لایسنس حذف نشد"

# ── استقرار قدیمی: حذف ستون جدید → مهاجرت خودکار ──
say "شبیه‌سازی استقرار قدیمی (حذف ستون clinic_city)"
$PHP -r '$p=new PDO("mysql:host=127.0.0.1;port=3306;dbname=lsc12","root",(getenv("MYSQL_ROOT_PASSWORD")?:"")); $p->exec("ALTER TABLE ls_licenses DROP COLUMN clinic_city"); echo "COL-DROPPED\n";' | grep -q COL-DROPPED && ok "ستون clinic_city حذف شد"

# نشست تازه = بازدید بعدی مدیر؛ مهاجرت خودکار (یک‌بار-به‌نشست) باید ستون را بازسازی کند
rm -f $COOKIE
LOGIN_HTML2=$(curl -s -c $COOKIE "http://127.0.0.1:8899/index.php?page=login")
CSRF2=$(echo "$LOGIN_HTML2" | grep -o 'name="csrf" value="[^"]*"' | head -1 | sed 's/.*value="//;s/"//')
curl -s -b $COOKIE -c $COOKIE -o /dev/null -X POST "http://127.0.0.1:8899/index.php?page=login" \
  --data-urlencode "action=login" --data-urlencode "csrf=$CSRF2" \
  --data-urlencode "username=admin" --data-urlencode "password=Admin12345"
# دیدن داشبورد در نشست تازه → اجرای مهاجرت خودکار
curl -s -b $COOKIE -o /dev/null "http://127.0.0.1:8899/index.php?page=dashboard"
$PHP -r '$p=new PDO("mysql:host=127.0.0.1;port=3306;dbname=lsc12","root",(getenv("MYSQL_ROOT_PASSWORD")?:"")); $c=$p->query("SELECT COUNT(*) FROM information_schema.COLUMNS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME=\"ls_licenses\" AND COLUMN_NAME=\"clinic_city\"")->fetchColumn(); echo ($c==1?"REBUILT\n":"MISSING\n");' | grep -q REBUILT && ok "مهاجرت خودکار در نشست تازه ستون را بازسازی کرد" || bad "ستون clinic_city بازسازی نشد"

CSRF=$(get_csrf license_new)
REISSUE=$(curl -s -b $COOKIE -o /dev/null -w "%{http_code}" -X POST "http://127.0.0.1:8899/index.php?page=license_new" \
  --data-urlencode "action=create_license" --data-urlencode "csrf=$CSRF" \
  --data-urlencode "license_type=1" --data-urlencode "duration_months=6" \
  --data-urlencode "clinic_name=کلینیک مهاجرت" --data-urlencode "clinic_city=تهران" \
  --data-urlencode "modules[]=Dashboard" --data-urlencode "modules[]=Patients")
[ "$REISSUE" = "302" ] && ok "صدور پس از مهاجرت خودکار موفق (302)" || bad "صدور پس از مهاجرت: $REISSUE"
$PHP -r '$p=new PDO("mysql:host=127.0.0.1;port=3306;dbname=lsc12","root",(getenv("MYSQL_ROOT_PASSWORD")?:"")); $r=$p->query("SELECT clinic_name, clinic_city FROM ls_licenses ORDER BY id DESC LIMIT 1")->fetch(PDO::FETCH_ASSOC); echo ($r && $r["clinic_city"]=="تهران"?"MIGRATED-OK\n":"MIGRATED-BAD\n");' | grep -q 'MIGRATED-OK' && ok "لایسنس مهاجرت با شهر تهران ثبت شد" || bad "ثبت لایسنس مهاجرت ناقص"

# ═════════════════════════════════════════════════════════════════
# چرخهٔ ۶۶ — تست‌های بکاپ خودکار و چرخش کلید RSA
# ═════════════════════════════════════════════════════════════════

# ── بکاپ خودکار کرون: کلید رمزی درست → فایل بکاپ gzip ساخته می‌شود ──
say "بکاپ خودکار کرون (چرخهٔ ۶۶)"
# خواندن مطمئن کلید کرون — include خود فایل config (بدون regex شکننده)
CRON_SECRET=$($PHP -r 'require "/tmp/lsc12/data/config.php"; echo defined("LS_CRON_SECRET") ? LS_CRON_SECRET : "";')
if [ -n "$CRON_SECRET" ]; then
  ok "کلید رمزی کرون از config.php خوانده شد"
else
  bad "کلید رمزی کرون یافت نشد"
fi
CRON_OUT=$(cd $TESTDIR && $PHP cron.php "$CRON_SECRET" 2>&1)
echo "$CRON_OUT" | grep -q '"ok":true' && ok "کرون با کلید درست اجرا شد (JSON ok)" || bad "خروجی کرون: $CRON_OUT"
echo "$CRON_OUT" | grep -q '"backup"' && ok "بلوک گزارش بکاپ در خروجی کرون موجود است" || bad "گزارش بکاپ در کرون نیست"
BK_FILE=$(ls $TESTDIR/data/backups/license-backup-*.sql.gz 2>/dev/null | head -1)
[ -n "$BK_FILE" ] && ok "فایل بکاپ gzip ساخته شد: $(basename "$BK_FILE")" || bad "هیچ فایل بکاپی ساخته نشد"
# محتوای بکاپ باید جدول‌ها و INSERT داشته باشد (gzip -dc | grep)
gzip -dc "$BK_FILE" 2>/dev/null | grep -q "CREATE TABLE" && ok "محتوای بکاپ شامل ساخت جدول‌ها است" || bad "محتوای بکاپ ناقص است"
gzip -dc "$BK_FILE" 2>/dev/null | grep -q "INSERT INTO" && ok "محتوای بکاپ شامل داده‌ها (INSERT) است" || bad "بکاپ فاقد داده است"
gzip -dc "$BK_FILE" 2>/dev/null | grep -q "ls_admins" && ok "جدول مدیران در بکاپ هست" || bad "جدول مدیران در بکاپ نیست"

# ── کرون با کلید غلط → 403 Forbidden ──
CRON_BAD=$(cd $TESTDIR && $PHP cron.php "wrong-secret-xyz" 2>&1)
echo "$CRON_BAD" | grep -qi "forbidden" && ok "کرون با کلید غلط رد شد (Forbidden)" || bad "کرون با کلید غلط اجرا شد!"

# ── چرخش جفت‌کلید RSA: امضای همهٔ لایسنس‌ها تازه + بایگانی کلید کهنه ──
say "چرخش جفت‌کلید RSA (چرخهٔ ۶۶)"
SIG_BEFORE=$($PHP -r '$p=new PDO("mysql:host=127.0.0.1;port=3306;dbname=lsc12","root",(getenv("MYSQL_ROOT_PASSWORD")?:"")); echo (string)$p->query("SELECT signature FROM ls_licenses ORDER BY id LIMIT 1")->fetchColumn();')
ROT_CSRF=$(get_csrf settings)
ROT_CODE=$(curl -s -b $COOKIE -o /dev/null -w "%{http_code}" -X POST "http://127.0.0.1:8899/index.php?page=settings" \
  --data-urlencode "action=rotate_keys" --data-urlencode "csrf=$ROT_CSRF" \
  --data-urlencode "confirm_text=REGENERATE")
[ "$ROT_CODE" = "302" ] && ok "چرخش کلید با تأیید REGENERATE انجام شد (302)" || bad "چرخش کلید: HTTP $ROT_CODE"
SIG_AFTER=$($PHP -r '$p=new PDO("mysql:host=127.0.0.1;port=3306;dbname=lsc12","root",(getenv("MYSQL_ROOT_PASSWORD")?:"")); echo (string)$p->query("SELECT signature FROM ls_licenses ORDER BY id LIMIT 1")->fetchColumn();')
[ -n "$SIG_BEFORE" ] && [ -n "$SIG_AFTER" ] && [ "$SIG_BEFORE" != "$SIG_AFTER" ] \
  && ok "امضای لایسنس با کلید جدید عوض شد" || bad "امضای لایسنس تغییر نکرد (یا لایسنسی موجود نیست)"
ls $TESTDIR/data/keys-archive/rsa_private-*.pem >/dev/null 2>&1 \
  && ok "کلید خصوصی کهنه در keys-archive بایگانی شد" || bad "بایگانی کلید کهنه یافت نشد"
# تأیید امضای جدید با کلید عمومی جدید — سازگاری PHP ↔ دات‌نت (قالب payload همان است)
VERIFY=$($PHP -r '
$c = file_get_contents("/tmp/lsc12/data/config.php");
$db = new PDO("mysql:host=127.0.0.1;port=3306;dbname=lsc12", "root", (getenv("MYSQL_ROOT_PASSWORD") ?: ""));
$l = $db->query("SELECT license_key, expiry_date, max_users, signature FROM ls_licenses ORDER BY id LIMIT 1")->fetch(PDO::FETCH_ASSOC);
$pub = file_get_contents("/tmp/lsc12/data/rsa_public.pem");
$payload = $l["license_key"] . "|" . ($l["expiry_date"] ?? "") . "|" . $l["max_users"];
echo openssl_verify($payload, base64_decode((string)$l["signature"]), $pub, OPENSSL_ALGO_SHA256) === 1 ? "VERIFY-OK\n" : "VERIFY-BAD\n";')
echo "$VERIFY" | grep -q 'VERIFY-OK' && ok "امضای جدید با کلید عمومی جدید تأیید شد" || bad "تأیید امضا شکست خورد"
# چرخش با عبارت تأیید غلط → رد: امضا نباید عوض شود
SIG_BEFORE_BAD=$($PHP -r '$p=new PDO("mysql:host=127.0.0.1;port=3306;dbname=lsc12","root",(getenv("MYSQL_ROOT_PASSWORD")?:"")); echo (string)$p->query("SELECT signature FROM ls_licenses ORDER BY id LIMIT 1")->fetchColumn();')
ROT_BAD_CSRF=$(get_csrf settings)
ROT_BAD=$(curl -s -b $COOKIE -o /dev/null -w "%{http_code}" -X POST "http://127.0.0.1:8899/index.php?page=settings" \
  --data-urlencode "action=rotate_keys" --data-urlencode "csrf=$ROT_BAD_CSRF" \
  --data-urlencode "confirm_text=NOPE")
SIG_AFTER_BAD=$($PHP -r '$p=new PDO("mysql:host=127.0.0.1;port=3306;dbname=lsc12","root",(getenv("MYSQL_ROOT_PASSWORD")?:"")); echo (string)$p->query("SELECT signature FROM ls_licenses ORDER BY id LIMIT 1")->fetchColumn();')
[ "$ROT_BAD" = "302" ] && [ "$SIG_BEFORE_BAD" = "$SIG_AFTER_BAD" ] \
  && ok "چرخش با عبارت غلط رد شد (امضا دست‌نخورده ماند)" || bad "چرخش با عبارت غلط اجرا شد یا امضا عوض شد!"

# ── بکاپ دستی از پنل (backup_now) ──
# ═══ ریشه‌یابی (چرخهٔ ۸۰) ═══
# سنجش قبلی «حداقل ۲ فایل» بود و به شمارش قبلی تکیه داشت؛ اگر نام فایل بکاپ
# (مهر زمانی ثانیه‌ای) با فایل کرون یکی می‌شد، فایل بازنویسی می‌شد و شمارش
# ۱ می‌ماند ⇒ ❌ گمراه‌کننده. اکنون «افزایش دقیقاً یک فایل» سنجیده می‌شود و
# در صورت شکست، متن فلش پنل هم چاپ می‌شود تا ریشه (قفل/دسترسی/دیتابیس) دیده شود.
BK_BEFORE=$(ls $TESTDIR/data/backups/license-backup-*.sql.gz 2>/dev/null | wc -l)
BK_CSRF=$(get_csrf settings)
BK_CODE=$(curl -s -b $COOKIE -o /dev/null -w "%{http_code}" -X POST "http://127.0.0.1:8899/index.php?page=settings" \
  --data-urlencode "action=backup_now" --data-urlencode "csrf=$BK_CSRF")
BK_AFTER=$(ls $TESTDIR/data/backups/license-backup-*.sql.gz 2>/dev/null | wc -l)
BK_FLASH=$(curl -s -b $COOKIE "http://127.0.0.1:8899/index.php?page=settings" | grep -o "بکاپ[^<]\{0,120\}" | head -2 | tr "\n" " ")
if [ "$BK_CODE" = "302" ] && [ "$BK_AFTER" -gt "$BK_BEFORE" ]; then
  ok "بکاپ دستی از پنل اجرا شد (مجموع $BK_AFTER بکاپ)"
else
  bad "بکاپ دستی: HTTP $BK_CODE · تعداد $BK_BEFORE→$BK_AFTER · فلش: $(printf '%.200s' "$BK_FLASH")"
fi

# ═════════════════════════════════════════════════════════════════
# چرخهٔ ۶۷ — تست آینهٔ پیام‌های پشتیبانی (حذف از پنل = حذف از داشبورد)
# ═════════════════════════════════════════════════════════════════
say "آینهٔ پیام‌های پشتیبانی (چرخهٔ ۶۷)"
# ═══ ریشه‌یابی (چرخهٔ ۸۰): «قالب کلید لایسنس نامعتبر است» ═══
# این بخش لایسنس id=1 را فرض می‌کرد، در حالی که تست «حذف لایسنس» چند خط
# بالاتر همان لایسنس را پاک می‌کند (۳ بررسی حذف، همگی ✅). از آن لحظه ردیفی
# با id=1 وجود ندارد؛ پس $LIC_KEY تهی می‌شد، پیام به license_id ناموجود
# فرستاده می‌شد و heartbeat با ۴۰۰ برمی‌گشت ⇒ چهار بررسی آینه ❌.
# اکنون «تازه‌ترین لایسنس» (کلینیک مهاجرت) انتخاب و فعال می‌شود و پیام/تپ‌قلب
# روی همان انجام می‌گیرد — دقیقاً همان چیزی که کلاینت واقعی تجربه می‌کند.
LIC_INFO=$($PHP -r '$p=new PDO("mysql:host=127.0.0.1;port=3306;dbname=lsc12","root",(getenv("MYSQL_ROOT_PASSWORD")?:""));
$r=$p->query("SELECT id, license_key FROM ls_licenses ORDER BY id DESC LIMIT 1")->fetch(PDO::FETCH_ASSOC);
echo $r ? $r["id"] . "|" . $r["license_key"] : "";')
LIC_ID=${LIC_INFO%%|*}
LIC_KEY=${LIC_INFO#*|}
echo "لایسنس مرجع آینه: id=$LIC_ID کلید=$LIC_KEY"
FP="FINGERPRINT-C67-MIRROR-0123456789ABCDEF"
ACT67=$(curl -s -X POST "http://127.0.0.1:8899/api.php" -H "Content-Type: application/json" \
  -d "{\"action\":\"activate\",\"licenseKey\":\"$LIC_KEY\",\"fingerprint\":\"$FP\",\"clinicName\":\"کلینیک مهاجرت\",\"appVersion\":\"1.9.0\"}")
echo "$ACT67" | grep -q '"isValid":true' \
  && ok "فعال‌سازی لایسنس مرجع آینه موفق (id=$LIC_ID)" \
  || bad "فعال‌سازی لایسنس مرجع آینه ناموفق — پاسخ: $(printf '%.240s' "$ACT67")"

# ارسال پیام تازه با رنگ سفارشی → heartbeat باید همان پیام + color را برگرداند
MSG_CSRF=$(get_csrf messages)
MSG_CODE=$(curl -s -b $COOKIE -o /dev/null -w "%{http_code}" -X POST "http://127.0.0.1:8899/index.php?page=messages" \
  --data-urlencode "action=send_message" --data-urlencode "csrf=$MSG_CSRF" \
  --data-urlencode "license_id=$LIC_ID" --data-urlencode "level=warning" \
  --data-urlencode "custom_color=on" --data-urlencode "color=%23E53935" \
  --data-urlencode "title=پیام تست آینه" --data-urlencode "body=این پیام برای تست حذف همگام است")
[ "$MSG_CODE" = "302" ] && ok "پیام تست با رنگ سفارشی ارسال شد" || bad "ارسال پیام تست: HTTP $MSG_CODE"
# heartbeat — بلوک messages باید همیشه حاضر باشد و پیام را با color برگرداند
# چرخهٔ ۸۰ — مسیر مستند: action داخل بدنهٔ JSON و آدرس بدون query string.
# پیش‌تر «?action=heartbeat» در URL بود و چون api.php بدنه را فقط در نبودِ
# action در query می‌خواند، licenseKey/fingerprint خالی می‌ماند و پاسخ ۴۰۰
# «قالب کلید لایسنس نامعتبر است» برمی‌گشت ⇒ هر سه بررسی پیام ❌ می‌شد.
HB1=$(curl -s -X POST "http://127.0.0.1:8899/api.php" \
  -H "Content-Type: application/json" \
  -d "{\"action\":\"heartbeat\",\"licenseKey\":\"$LIC_KEY\",\"fingerprint\":\"$FP\"}")
echo "$HB1" | grep -q '"messages"' && ok "heartbeat بلوک messages را همیشه دارد (پروتکل چرخهٔ ۶۷)" || bad "بلوک messages در heartbeat نیست — پاسخ: $(printf '%.240s' "$HB1")"
echo "$HB1" | grep -q '"color":"#E53935"' && ok "رنگ سفارشی پیام در heartbeat رسید" || bad "رنگ سفارشی در heartbeat نیست — پاسخ: $(printf '%.240s' "$HB1")"
echo "$HB1" | grep -q 'پیام تست آینه' && ok "پیام تازه در فهرست heartbeat هست" || bad "پیام تازه در heartbeat نیست — پاسخ: $(printf '%.240s' "$HB1")"
# heartbeat دوم (تحویل‌شده) — پیام باید «بماند» (فهرست کامل، نه فقط تحویل‌نشده)
HB2=$(curl -s -X POST "http://127.0.0.1:8899/api.php" \
  -H "Content-Type: application/json" \
  -d "{\"action\":\"heartbeat\",\"licenseKey\":\"$LIC_KEY\",\"fingerprint\":\"$FP\"}")
echo "$HB2" | grep -q 'پیام تست آینه' && ok "پیام تحویل‌شده در تپ‌قلب دوم هم می‌ماند (فهرست کامل)" || bad "پیام پس از تحویل حذف شد — فهرست کامل کار نمی‌کند"
# حذف پیام از پنل → heartbeat بعدی نباید آن را برگرداند (آینهٔ سرور)
MSG_ID=$($PHP -r '$p=new PDO("mysql:host=127.0.0.1;port=3306;dbname=lsc12","root",(getenv("MYSQL_ROOT_PASSWORD")?:"")); echo (string)$p->query("SELECT id FROM ls_messages WHERE title=\"پیام تست آینه\" ORDER BY id DESC LIMIT 1")->fetchColumn();')
DEL_CSRF=$(get_csrf messages)
DEL_MSG=$(curl -s -b $COOKIE -o /dev/null -w "%{http_code}" -X POST "http://127.0.0.1:8899/index.php?page=messages" \
  --data-urlencode "action=delete_message" --data-urlencode "csrf=$DEL_CSRF" \
  --data-urlencode "id=$MSG_ID")
HB3=$(curl -s -X POST "http://127.0.0.1:8899/api.php" \
  -H "Content-Type: application/json" \
  -d "{\"action\":\"heartbeat\",\"licenseKey\":\"$LIC_KEY\",\"fingerprint\":\"$FP\"}")
if echo "$HB3" | grep -q 'پیام تست آینه'; then
  bad "پیام حذف‌شده هنوز برمی‌گردد!"
else
  ok "پیام حذف‌شده از پنل در heartbeat بعدی نیست (رفع گزارش مالک)"
fi

# ── پایان ──
kill $SRV 2>/dev/null
say "نتیجه نهایی"
echo "════════════════════════════════"
echo "PASS: $PASS | FAIL: $FAIL"
# ═══ چرخهٔ ۶۶ — کد خروج برای CI: بدون این، job سی‌آی حتی با شکست تست‌ها سبز می‌ماند ═══
if [ $FAIL -eq 0 ]; then
  echo "ALL-GREEN ✅"
  exit 0
else
  echo "HAS-FAILURES ❌"
  exit 1
fi
