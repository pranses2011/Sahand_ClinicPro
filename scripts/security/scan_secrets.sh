#!/usr/bin/env bash
# ═══════════════════════════════════════════════════════════════════════
#  اسکنر امنیتی/کیفیت ClinicPro — گیت‌های الگوهای ممنوعه (چرخهٔ ۴۹)
#  مرجع: docs/analysis/ai-review/04-EXECUTION-PLAN.md §۹ (چک‌لیست انتشار)
#  اجرا:  bash scripts/security/scan_secrets.sh
#  خروج:  ۰ = سبز (همه گیت‌ها پاس) · ۱ = قرمز (حداقل یک گیت رد شد)
#  سازگار با Git Bash ویندوز (grep -E) — بدون وابستگی به ripgrep
# ═══════════════════════════════════════════════════════════════════════
set -u
cd "$(dirname "$0")/../.." || exit 2

RED='\033[0;31m'; GREEN='\033[0;32m'; YELLOW='\033[1;33m'; NC='\033[0m'
FAIL=0
REPORT=""

gate() { # gate <شماره> <عنوان> <خروجی grep (اگر خالی=سبز)>
  local n="$1" title="$2" hits="$3"
  if [ -n "$hits" ]; then
    FAIL=1
    REPORT="${REPORT}${RED}⛔ گیت $n رد شد${NC} — $title
$hits
"
  else
    REPORT="${REPORT}${GREEN}✅ گیت $n سبز${NC} — $title
"
  fi
}

echo "🔍 اسکنر امنیتی/الگوهای ممنوعه ClinicPro (چرخهٔ ۴۹ به بعد)"
echo "═════════════════════════════════════════════════════════════"

# ── گیت ۱: هیچ مقدار غیرخالی برای کلیدهای محرمانه در پیکربندی‌های JSON ──
# (کلید باید از محیط تزریق شود: Jwt__SecretKey و امثالهم — appsettings فقط خالی)
G1=$(grep -rnE '"(SecretKey|ApiKey|AccessKey|EncryptionKey|Token)": *"[^"]+"' \
     --include='appsettings*.json' src/ 2>/dev/null || true)
gate 1 "مقدار غیرخالی کلید محرمانه در appsettings*.json" "$G1"

# ── گیت ۲: کلید افشاشدهٔ تاریخی مخزن نباید در src بازگردد ──
# استثنای مجاز: IdentitySetup.cs (ثابت تطبیق گارد) و JwtKeyGuardTests.cs
# (تست رگرسیون همان ثابت‌ها) — این دو فایل «حاکی» کلیدند نه «مصرف» آن.
G2=$(grep -rn 'SahandClinicPro-Development-Secret-Key-Change-In-Production-2026' \
     src/ 2>/dev/null \
     | grep -v 'Identity/IdentitySetup.cs' \
     | grep -v 'JwtKeyGuardTests.cs' || true)
gate 2 "کلید JWT افشاشدهٔ تاریخی در src/ (خارج از فایل‌های گارد/تست)" "$G2"

# ── گیت ۳: رمز عبور در رشته اتصال دیتابیس ──
# (Trusted_Connection مجاز است؛ Password= در فایل پیکربندی ممنوع)
G3=$(grep -rnE '(Password|Pwd) *=[^;""]+' \
     --include='appsettings*.json' src/ 2>/dev/null || true)
gate 3 "رمز عبور در رشته اتصال appsettings*.json" "$G3"

# ── گیت ۴: انسداد رشته — GetAwaiter().GetResult() در کل src ──
# (چرخهٔ ۵۰: این الگو در هیچ لایه‌ای پذیرفته نیست؛ معادل async خالص بنویسید)
G4=$(grep -rn 'GetAwaiter().GetResult()' src/ 2>/dev/null || true)
gate 4 "الگوی ممنوع GetAwaiter().GetResult() در src/" "$G4"

# ── گیت ۵: انسداد رشته — Task.Result در دسکتاپ ──
# فقط متغیرهای رخداد «*Task.Result» (مثل revenueTask.Result)؛
# پراپرتی Result روی DTOها (payVm.Result) عمداً بی‌گناه است و تطبیق نمی‌خورد.
G5=$(grep -rnE '[A-Za-z]+Task\.Result\b' src/Sahand.ClinicPro.Desktop/ 2>/dev/null || true)
gate 5 "الگوی ممنوع Task.Result در لایه دسکتاپ" "$G5"

# ── گیت ۶: async void در کل src (سوخت کرش‌های غیرقابل‌گیری) ──
# (کنترل‌کننده‌های رخداد WPF نیز باید async Task + FireAndForgetSafe باشند)
G6=$(grep -rn 'async void' src/ 2>/dev/null || true)
gate 6 "الگوی ممنوع async void در src/" "$G6"

# ── گزارش ──
echo ""
printf "%b" "$REPORT"
echo "═════════════════════════════════════════════════════════════"
if [ "$FAIL" -eq 0 ]; then
  echo -e "${GREEN}✅ همه گیت‌ها سبز است — الگوهای ممنوعه صفر.${NC}"
  exit 0
else
  echo -e "${RED}⛔ حداقل یک گیت قرمز است — قبل از کامیت/انتشار رفع کنید.${NC}"
  exit 1
fi
