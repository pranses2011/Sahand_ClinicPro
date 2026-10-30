<div dir="rtl" align="right">

# 🐞 ۰۶ · فهرست ایرادها با شواهد کد

<div align="center">

![کل](https://img.shields.io/badge/کل%20ایراد-۴۲-0d9488?style=flat-square)
![P0](https://img.shields.io/badge/P0%20بحرانی-۱-dc2626?style=flat-square)
![P1](https://img.shields.io/badge/P1%20مهم-۱۰-ea580c?style=flat-square)
![P2](https://img.shields.io/badge/P2%20متوسط-۱۶-ca8a04?style=flat-square)
![P3](https://img.shields.io/badge/P3%20جزئی-۱۵-0d9488?style=flat-square)

</div>

> [!NOTE]
> هر ردیف شامل **شاهد دقیق** (`مسیر/فایل:خط`) است تا بتوانید مستقیم بازبینی کنید. شدت بر اساس «اثر بر کاربر واقعی در درمانگاه» تعیین شده، نه بر اساس زیبایی کد.

---

## 🔴 P0 — بحرانی (۱ مورد)

<div align="center">

<table dir="rtl" width="100%">
<thead><tr><th width="5%">کد</th><th width="26%">عنوان</th><th width="27%">شاهد</th><th width="42%">اثر و اصلاح</th></tr></thead>
<tbody>
<tr>
<td><b>D-01</b></td>
<td><b>ده مجوز استفاده‌شده بذر نمی‌شوند ⇒ قابلیت‌ها فقط با نقش مدیر کار می‌کنند</b></td>
<td>
<code>Infrastructure/Persistence/Seeders/PermissionSeeder.cs:24-47</code><br/>
۲۶ ماژول × ۶ عمل = ۱۵۶ مجوز<br/><br/>
اما در کنترلرها این مجوزها استفاده شده‌اند:<br/>
<code>payroll.manage</code> · <code>emr.advanced</code> · <code>surveys.manage</code> · <code>surveys.view</code> · <code>sms.bulksend</code> · <code>pharmacy.stocktake</code> · <code>attendance.approve</code> · <code>attendance.devices</code> · <code>attendance.shifts</code> · <code>notifications.manage</code>
</td>
<td>
<b>اثر:</b> مدیر درمانگاه، حسابدار، پرستار و کارشناس آزمایشگاه <b>هرگز</b> نمی‌توانند به حقوق و دستمزد، پروندهٔ بالینی پیشرفته، نظرسنجی، پیامک انبوه، انبارگردانی، تأیید حضور/دستگاه‌ها/شیفت و مدیریت اعلان دسترسی بگیرند — چون این مجوزها در جدول <code>Permissions</code> وجود ندارند و در ماتریس نقش‌ها قابل تیک‌زدن نیستند. درمانگاه برای کار کردن مجبور می‌شود رمز «مدیر سامانه» را بچرخاند (نقض تفکیک وظایف).<br/><br/>
<b>اصلاح (۰٫۵ روز):</b> افزودن ۱۰ مجوز به بذر + مهاجرت دادهٔ کوچک برای درج در دیتابیس‌های موجود + آزمون CI زیر:<br/>
<code>assert: usedPermissions ⊆ seededPermissions</code>
</td>
</tr>
</tbody>
</table>

</div>

```csharp
// آزمون گارد پیشنهادی — tests/Sahand.ClinicPro.Application.UnitTests/Security/PermissionCoverageTests.cs
[Fact]
public void Every_permission_used_in_controllers_must_be_seeded()
{
    var root = FindRepositoryRoot();
    var used = Directory.GetFiles(Path.Combine(root, "src/Sahand.ClinicPro.API/Controllers"), "*.cs")
        .SelectMany(f => Regex.Matches(File.ReadAllText(f), "HasPermission\\(\"([^\"]+)\"\\)").Select(m => m.Groups[1].Value))
        .ToHashSet();

    var seeded = PermissionCatalog.All;   // از PermissionSeeder استخراج شود

    var missing = used.Except(seeded).OrderBy(x => x).ToList();
    missing.Should().BeEmpty($"این مجوزها در کنترلر استفاده شده‌اند اما بذر نشده‌اند: {string.Join(", ", missing)}");
}
```

---

## 🟠 P1 — مهم (۱۰ مورد)

<div align="center">

<table dir="rtl" width="100%">
<thead><tr><th width="5%">کد</th><th width="24%">عنوان</th><th width="26%">شاهد</th><th width="11%">اثر</th><th width="34%">اصلاح</th></tr></thead>
<tbody>

<tr><td><b>D-02</b></td><td><b>توکن تازه‌سازی به‌صورت متن‌ساده در دیتابیس</b></td>
<td><code>Identity/IdentityService.cs:151-160</code><br/><code>Token = refreshToken</code><br/>جستجو: <code>t.Token == refreshToken</code> (:184)</td>
<td align="center">امنیت<br/>🔴</td>
<td>ذخیرهٔ <code>SHA256(token)</code> و مقایسهٔ هش؛ جدول مهاجرت + بازآفرینی توکن‌های موجود. (۰٫۵ روز)</td></tr>

<tr><td><b>D-03</b></td><td><b>۱۵۶ ادعای مجوز در هر JWT</b></td>
<td><code>JwtTokenGenerator.cs:67</code><br/><code>claims.AddRange(permissions…)</code><br/>+ ۱۵۶ مجوز برای نقش مدیر</td>
<td align="center">کارایی<br/>🟠</td>
<td>توکن فقط نقش + <code>permVersion</code>؛ مجوزها در سرور با <code>IMemoryCache</code> به‌مدت ۵ دقیقه. (۱٫۵ روز)</td></tr>

<tr><td><b>D-04</b></td><td><b>پرچم «مشغول» در خطا آزاد نمی‌شود</b></td>
<td>۱۷۷ × <code>IsBusy = true</code><br/>در برابر ۷۶ × <code>finally</code><br/>نمونهٔ شاخص: <code>PatientsViewModel.cs:87-111</code> و <code>:224-238</code></td>
<td align="center">تجربه<br/>🔴</td>
<td>الگوی <code>BusyScope</code> (IDisposable) در <code>ViewModelBase</code> + جایگزینی مکانیکی. (۲–۳ روز)</td></tr>

<tr><td><b>D-05</b></td><td><b>۱۵۴ فراخوانی بی‌صاحب <code>_ = …Async()</code></b></td>
<td><code>grep -n "_ = .*Async()" ViewModels/*.cs</code><br/>نمونه: <code>PatientsViewModel.cs:83</code> (در سازنده)</td>
<td align="center">پایداری<br/>🟠</td>
<td>انتقال به <code>Loaded</code> یا <code>IAsyncInitialize</code> و انتظار در ناوبری. (۲ روز)</td></tr>

<tr><td><b>D-06</b></td><td><b>فقط ۳ مدل‌نما از ۷۳ <code>CancellationToken</code> دارند</b></td>
<td><code>grep -l CancellationToken ViewModels/*.cs → 3</code></td>
<td align="center">کارایی<br/>🟡</td>
<td>توکن صفحه در <code>ViewModelBase</code>؛ لغو در <code>OnNavigatedFrom</code>. (۲ روز)</td></tr>

<tr><td><b>D-07</b></td><td><b>۱۶ پنجره بلندتر از ۷۰۰px و ۱۷ پنجرهٔ NoResize</b></td>
<td><code>VisitWindow</code> ۱۲۰۰×۸۰۰ · <code>PayslipWindow</code> ۷۶۰×۸۸۰ · <code>ShiftCloseWindow</code> ۱۲۲۰×۸۲۰ · <code>LoginWindow</code> ۶۰۰×۷۴۰</td>
<td align="center">تجربه<br/>🔴</td>
<td>مهار خودکار به ناحیهٔ کاری + <code>SizeToContent</code> برای دیالوگ‌ها. (۱ روز)</td></tr>

<tr><td><b>D-08</b></td><td><b>لاگ عملیات فقط نوشتن‌ها را ثبت می‌کند</b></td>
<td><code>API/Middleware/AuditLogMiddleware.cs:22</code><br/><code>WriteMethods = { POST, PUT, PATCH, DELETE }</code><br/><code>:44</code> ← خروج زودهنگام برای GET</td>
<td align="center">انطباق<br/>🟠</td>
<td>لاگ «مشاهدهٔ پرونده» برای مسیرهای حساس (<code>patients/{id}</code>، <code>visits</code>، <code>lab</code>) با نرخ نمونه‌برداری. (۱ روز)</td></tr>

<tr><td><b>D-09</b></td><td><b>مهاجرت خودکار دیتابیس در استارتاپ روشن است</b></td>
<td><code>API/Program.cs:447-452</code><br/><code>Database:AutoMigrateOnStartup</code> پیش‌فرض <code>true</code></td>
<td align="center">عملیات<br/>🟠</td>
<td>پیش‌فرض <code>false</code> + انجام در نصب‌کننده (که همین حالا <code>--migrate-database</code> دارد). (۰٫۵ روز)</td></tr>

<tr><td><b>D-10</b></td><td><b>ذخیرهٔ بخشی از JWT به‌عنوان شناسهٔ نشست</b></td>
<td><code>IdentityService.cs:165</code><br/><code>SessionToken = accessToken[..64]</code></td>
<td align="center">امنیت<br/>🟡</td>
<td>ذخیرهٔ <code>jti</code> به‌جای برش توکن؛ مقدار ۶۴ نویسه‌ای شامل سرآیند و ابتدای ادعاهاست. (۰٫۲ روز)</td></tr>

<tr><td><b>D-11</b></td><td><b>بدون هیچ پشتیبانی صفحه‌خوان/میان‌بر Alt</b></td>
<td><code>AutomationProperties = 0</code> در ۹۲ نما<br/><code>AccessText = 2</code></td>
<td align="center">دسترس‌پذیری<br/>🟠</td>
<td>حداقل روی ۶ فرم پرکاربرد (بیمار/پذیرش/ویزیت/نسخه/فاکتور/تنظیمات). (۲ روز)</td></tr>

</tbody>
</table>

</div>

---

## 🟡 P2 — متوسط (۱۶ مورد)

<div align="center">

<table dir="rtl" width="100%">
<thead><tr><th width="5%">کد</th><th width="25%">عنوان</th><th width="28%">شاهد</th><th width="42%">توضیح / اصلاح</th></tr></thead>
<tbody>

<tr><td><b>D-12</b></td><td>داشبورد ۸ فراخوانی متوالی دارد</td>
<td><code>DashboardViewModel.cs:1395, 1420, 1441, 1456, 1479, 1497, 1525, 1684</code></td>
<td>هر درخواست پس از قبلی اجرا می‌شود. <b>اصلاح:</b> <code>Task.WhenAll</code> (حداقل) یا یک اندپوینت تجمیعی.</td></tr>

<tr><td><b>D-13</b></td><td>جستجوی فهرست با <code>LIKE '%…%'</code> روی ۶ ستون</td>
<td><code>GetAllPatientsQueryHandler.cs:44-49</code></td>
<td>غیرقابل استفاده از ایندکس. <b>اصلاح:</b> ایندکس full-text یا ستون نرمال‌شده + جستجوی پیشوندی.</td></tr>

<tr><td><b>D-14</b></td><td>جستجوی سراسری سه کوئری متوالی</td>
<td><code>UnifiedSearchController.cs:54, 74, 96</code></td>
<td>با هر تایپ (Debounce ۲۵۰ms) سه اسکن. <b>اصلاح:</b> موازی‌سازی + ایندکس.</td></tr>

<tr><td><b>D-15</b></td><td>انتقال ستون‌های اضافی به کلاینت</td>
<td><code>GetAllPatientsQueryHandler.cs:36-37, 60-66</code></td>
<td><code>Include</code> + <code>ToListAsync</code> + نگاشت در حافظه؛ DTO فقط ۹ ستون می‌خواهد. <b>اصلاح:</b> <code>Select</code> مستقیم به DTO.</td></tr>

<tr><td><b>D-16</b></td><td>صفحه‌های Singleton + بازسازی نما در هر ناوبری</td>
<td><code>MainWindowViewModel.cs:1166-1190</code></td>
<td>فرمان Refresh بازتابی + <code>CurrentPage = null</code>. <b>اصلاح:</b> کش صفحه و تازه‌سازی پس‌زمینه.</td></tr>

<tr><td><b>D-17</b></td><td>تشخیص «تغییرات ذخیره‌نشده» با Snapshot بازتابی و تایمر ۲٫۵ ثانیه</td>
<td><code>App.xaml.cs:143-190</code></td>
<td>رفتار کاربری خوب اما شکننده و کند. <b>اصلاح:</b> قرارداد صریح <code>IHasUnsavedChanges</code> در همهٔ فرم‌ها.</td></tr>

<tr><td><b>D-18</b></td><td>۳ جدول از ۴۴ جدول صفحه‌بندی دارند</td>
<td><code>PaginationBar</code> در ۳ فایل</td>
<td>خطر بارگذاری فهرست بزرگ. <b>اصلاح:</b> قالب مشترک فهرست.</td></tr>

<tr><td><b>D-19</b></td><td>۸ حالت خالی از ۴۴ جدول</td>
<td><code>EmptyState</code> در ۸ فایل</td>
<td>«نتیجهٔ صفر» از «خطا» قابل تفکیک نیست.</td></tr>

<tr><td><b>D-20</b></td><td>۳۳۴ رنگ هاردکد و ~۴۰ اندازهٔ فونت</td>
<td><code>grep -o "#[0-9A-F]\{6\}" Views/*.xaml App.xaml</code></td>
<td>ناپایداری در تم تاریک؛ تنظیم اندازهٔ فونت فقط جزئی اثر می‌کند.</td></tr>

<tr><td><b>D-21</b></td><td>بدون فایل منابع (<code>.resx</code>)</td>
<td><code>find src -name "*.resx" → 0</code></td>
<td>ترجمه/سفارشی‌سازی واژه‌ها ممکن نیست.</td></tr>

<tr><td><b>D-22</b></td><td>دریافت ۵۰۰ کاربر در یک درخواست</td>
<td><code>UserFormViewModel.cs:225</code></td>
<td><code>users?page=1&amp;pageSize=500</code> — جایگزین: جستجوی سروری با صفحه‌بندی کوچک.</td></tr>

<tr><td><b>D-23</b></td><td>سه پنجرهٔ هم‌پوشان ثبت نتیجهٔ آزمایش</td>
<td><code>LabResultWindow</code> · <code>LabTestResultEntryWindow</code> · <code>LabTestEditWindow</code></td>
<td>کاربر نمی‌داند کدام مسیر درست است؛ یکی باید مرجع شود.</td></tr>

<tr><td><b>D-24</b></td><td>نبود بازخورد «ذخیره شد» در تنظیمات</td>
<td><code>SettingsView.xaml</code> (نمای دیده‌شده فاقد دکمهٔ ذخیره در تب اطلاعات)</td>
<td>کاربر از ذخیره‌شدن مطمئن نیست. <b>اصلاح:</b> اندیکاتور زمان ذخیره.</td></tr>

<tr><td><b>D-25</b></td><td>تأیید ساده برای عملیات بازگشت‌ناپذیر</td>
<td><code>BackupView</code> ← بازیابی دیتابیس</td>
<td>بازیابی، مخرب‌ترین عملیات سامانه است. <b>اصلاح:</b> تایپ عبارت تأیید + گزارش قبل/بعد.</td></tr>

<tr><td><b>D-26</b></td><td><code>PhotoViewerWindow</code> بدون <code>WindowStartupLocation</code></td>
<td>تنها پنجرهٔ فاقد این تنظیم</td>
<td>روی مانیتور دوم نامناسب باز می‌شود. (۵ دقیقه)</td></tr>

<tr><td><b>D-27</b></td><td>ناهم‌ترازی مستندات با واقعیت</td>
<td><code>README.md</code>: «۹۷ جدول» در برابر ۲۲۸ پیکربندی؛ «۳۰۰+ اندپوینت» ✅؛ «۶۴ فرم» در برابر ۹۲</td>
<td>اعتماد فنی خریدار سازمانی را کم می‌کند؛ اعداد را خودکار از کد تولید کنید.</td></tr>

</tbody>
</table>

</div>

---

## 🟢 P3 — جزئی و بهبود (۱۵ مورد)

<div align="center">

<table dir="rtl" width="100%">
<thead><tr><th width="5%">کد</th><th width="27%">عنوان</th><th width="26%">شاهد</th><th width="42%">توضیح</th></tr></thead>
<tbody>
<tr><td><b>D-28</b></td><td>۶ کلاس غول‌آسا</td><td>Receptions ۲۰۷۳ · Dashboard ۱۷۱۸ · ReportsController ۱۶۶۴ · Settings ۱۵۲۰ · MainWindow ۱۵۰۸ · Appointments ۱۳۸۸ خط</td><td>تقسیم به سرویس‌های تخصصی (مثلاً <code>ReceptionPriceCalculator</code>، <code>DashboardLoader</code>).</td></tr>
<tr><td><b>D-29</b></td><td>۷٬۴۷۶ خط code-behind در نماها</td><td>بیشترین: <code>WaitingRoomDisplayWindow</code> ۶۴۹ خط</td><td>انتقال منطق به مدل‌نما/رفتار الحاقی.</td></tr>
<tr><td><b>D-30</b></td><td>اعتبارسنجی پراکنده</td><td>۳۹ Validator برای ~۱۰۰ فرمان</td><td>قاعدهٔ «هر فرمان نوشتنی باید Validator داشته باشد».</td></tr>
<tr><td><b>D-31</b></td><td>کلاینت، اسمبلی‌های سروری را حمل می‌کند</td><td><code>Desktop.csproj</code> ← <code>Application</code></td><td>پروژهٔ <code>Contracts</code> برای DTOها.</td></tr>
<tr><td><b>D-32</b></td><td>فقط یک ارائه‌دهندهٔ پیامک</td><td><code>Services/SMS/Providers/KavenegarProvider.cs</code></td><td>افزودن اپراتور دوم + صف جایگزین.</td></tr>
<tr><td><b>D-33</b></td><td>نمایشگر DICOM حداقلی</td><td><code>DicomViewerWindow.xaml.cs</code> ۱۶۸ خط</td><td>Window/Level، زوم، اندازه‌گیری.</td></tr>
<tr><td><b>D-34</b></td><td>یکپارچه‌سازی دولتی «عمومی» است</td><td><code>Services/Government/GovernmentServices.cs:19-70</code></td><td>پروتکل رسمی + امضا برای نسخهٔ الکترونیک.</td></tr>
<tr><td><b>D-35</b></td><td>پوشش آزمون دسکتاپ کم است</td><td>~۴۰ تأیید برای ۹۲ نما</td><td>افزودن آزمون مدل‌نما برای پذیرش/صندوق/نسخه.</td></tr>
<tr><td><b>D-36</b></td><td>پیام‌های خطای فنی به کاربر</td><td>الگو: <code>_api.LastError</code> خام نمایش داده می‌شود</td><td>نگاشت خطا به زبان کاربر + «چه کار کنم؟».</td></tr>
<tr><td><b>D-37</b></td><td>نبود شاخص اعتبار پیامک</td><td><code>SmsView</code></td><td>نمایش اعتبار و هشدار کم‌بودن.</td></tr>
<tr><td><b>D-38</b></td><td>یکسان‌سازی نشدن عنوان ماژول‌ها</td><td>«مدیریت سامانه (تب‌بندی کامل)» در منو</td><td>حذف پرانتز توضیحی از عنوان منو.</td></tr>
<tr><td><b>D-39</b></td><td>نبود «سبد بازیابی» برای حذف نرم</td><td>حذف نرم پیاده شده، نمای بازیابی نیست</td><td>صفحهٔ ساده برای مدیر سامانه.</td></tr>
<tr><td><b>D-40</b></td><td>بی‌صدا بودن برخی <code>catch</code></td><td>۲۹۷ بلوک <code>catch</code> در مخزن</td><td>قاعدهٔ «هر catch باید لاگ یا پیام داشته باشد».</td></tr>
<tr><td><b>D-41</b></td><td>سربرگ صفحهٔ داشبورد با بنرهای بزرگ اشغال شده</td><td><code>DashboardView.xaml</code> ۱۵۴۶ خط</td><td>کوچک‌کردن بنرها به یک خط با «بستن».</td></tr>
<tr><td><b>D-42</b></td><td>نبود دادهٔ نمونه/دمو برای آزمون فروش</td><td>مدل‌های بذر فقط دادهٔ مرجع دارند</td><td>«حالت دمو» با ۱۰۰ بیمار و ۲۰۰ نوبت ساختگی برای ارائه.</td></tr>
</tbody>
</table>

</div>

---

## 📊 نقشهٔ حرارتی ایرادها بر اساس ماژول

<div align="center">

| ماژول | P0 | P1 | P2 | P3 | جمع | سلامت |
|:--|:--:|:--:|:--:|:--:|:--:|:--:|
| 🔐 امنیت/دسترسی | ۱ | ۳ | — | — | **۴** | 🔴 |
| 🖥 لایهٔ نمایش (WPF) | — | ۴ | ۶ | ۵ | **۱۵** | 🟠 |
| ⚡ کارایی/پرس‌وجو | — | ۲ | ۴ | ۳ | **۹** | 🟡 |
| 🎨 رابط و طراحی | — | ۱ | ۴ | ۲ | **۷** | 🟡 |
| 🏛 معماری | — | ۱ | ۲ | ۲ | **۵** | 🟡 |
| 📜 مستندات | — | — | ۱ | — | **۱** | 🟢 |
| 🧪 آزمون | — | — | — | ۱ | **۱** | 🟢 |

</div>

> [!TIP]
> **نسبت سیگنال به نوفه خوب است:** ۸۵٪ ایرادها در دو ناحیه متمرکزند (دسترسی + لایهٔ نمایش) و هر دو با همان ۱۰ اقدام نخست سند [۰۴](04-ui-ux-fixes.md) برطرف می‌شوند. خبری از «هرج‌ومرج درهم‌تنیده» نیست — که نشانهٔ بلوغ کدبیس است.

</div>
