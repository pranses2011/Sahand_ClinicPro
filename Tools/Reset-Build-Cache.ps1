# ═══════════════════════════════════════════════════════════════════
# بازنشانی کش بیلد ClinicPro — چرخهٔ ۷۰
# ═══════════════════════════════════════════════════════════════════
# گزارش مالک (پنج‌باره در چرخه‌های ۴۳-۶۹): «رفع‌ها اعمال نمی‌شوند؛ شمارهٔ
# نسخه به‌روز است اما میانبر/توست همان قدیم می‌ماند».
# ریشه: کشِ طراح ویژوال استودیو (پوشهٔ .vs) و bin/obj کهنه، با Rebuild
# عادی پاک نمی‌شوند؛ «BAML کهنه» در هر بیلدِ ظاهراً تازه دوباره داخل DLL
# نشسته می‌شود. این اسکریپت همان کاری را می‌کند که راهنمای آپدیت به
# Clean-VS-Cache.bat (هرگز-نرسیده) قول می‌داد — این بار داخل خود بسته.
#
# اجرا (پاورشل، از ریشهٔ سولوشن پس از جایگزینی سورس‌ها):
#   powershell -ExecutionPolicy Bypass -File Tools/Reset-Build-Cache.ps1
# سپس در ویژوال استودیو: Clean Solution → Rebuild Solution.
# ═══════════════════════════════════════════════════════════════════

$ErrorActionPreference = "SilentlyContinue"
$root = Split-Path -Parent $MyInvocation.MyCommand.Path | Split-Path -Parent
Write-Host "ریشه سولوشن: $root" -ForegroundColor Cyan

# ۱) بستن ویژوال استودیو در حال ویرایش سولوشن (قفل .vs)
$v = Get-Process devenv -ErrorAction SilentlyContinue
if ($v) {
    Write-Host "ویژوال استودیو باز است — برای پاک‌سازی قطعی، ابتدا ببندید و دوباره اجرا کنید." -ForegroundColor Yellow
}

# ۲) حذف کش طراح (.vs) — عامل «BAML منجمد»
if (Test-Path "$root\.vs") {
    Remove-Item "$root\.vs" -Recurse -Force
    Write-Host "[OK] کش طراح (.vs) پاک شد" -ForegroundColor Green
} else {
    Write-Host "[—] کش طراح (.vs) نبود" -ForegroundColor DarkGray
}

# ۳) حذف bin/obj همه پروژه‌ها
$count = 0
Get-ChildItem "$root\src" -Directory | ForEach-Object {
    foreach ($d in "bin", "obj") {
        $p = Join-Path $_.FullName $d
        if (Test-Path $p) { Remove-Item $p -Recurse -Force; $count++ }
    }
}
Write-Host "[OK] پوشه‌های bin/obj پاک شد: $count مورد" -ForegroundColor Green

# ۴) یادآوری گام بعد
Write-Host ""
Write-Host "اکنون در ویژوال استودیو: Build > Clean Solution و سپس Build > Rebuild Solution" -ForegroundColor Cyan
Write-Host "راه سریع‌تر (بدون ویژوال استودیو): فایل‌های پوشهٔ bin-update این بسته را در پوشهٔ خروجی برنامه جایگزین کنید." -ForegroundColor Cyan
