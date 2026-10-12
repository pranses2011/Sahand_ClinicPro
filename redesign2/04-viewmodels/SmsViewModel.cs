// ═══════════════════════════════════════════════════════════════════════
//  SmsViewModel
//  صفحه‌ی «پیامک» · گروه «ارتباطات» · کهن‌الگوی «detail-tabs»
//
//  متناظر با:  03-views/SmsView.xaml
//  وایرفریم:    01-wireframes-html/SmsView.html
//  پایه:        PageViewModelBase<SmsRow>
//
//  این کلاس با مولدِ tools/gen_viewmodels.py از همان مشخصاتِ وایرفریم
//  ساخته شده است؛ دادهٔ نمونه درونِ آن همان چیزی است که در وایرفریم دیده
//  می‌شود تا رفتارِ صفحه پیش از اتصالِ سرویسِ واقعی قابلِ بررسی باشد.
// ═══════════════════════════════════════════════════════════════════════

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Sahand.ClinicPro.Desktop.Base;

namespace Sahand.ClinicPro.Desktop.ViewModels
{
    /// <summary>مدلِ نمایشِ یک ردیف از «پیامک»</summary>
    /// <remarks>
    /// نام و نوعِ هر ویژگی دقیقاً با ستون‌های وایرفریمِ همین صفحه
    /// تطبیق دارد تا View بدون تغییر به آن متصل شود.
    /// </remarks>
    public sealed class SmsRow
    {
        /// <summary>زمان — date</summary>
        public string? Time { get; set; }

        /// <summary>گیرنده — who</summary>
        public string? Recipient { get; set; }
        /// <summary>زیرنویسِ «گیرنده» (مانند کد یا سن)</summary>
        public string? RecipientSub { get; set; }
        /// <summary>حروفِ اختصاریِ نشان برای «گیرنده»</summary>
        public string? RecipientInitials { get; set; }
        /// <summary>رنگِ نشانِ «گیرنده» — از تم خوانده می‌شود</summary>
        public Brush? RecipientAvatar { get; set; }

        /// <summary>الگو — text</summary>
        public string? Template { get; set; }

        /// <summary>هزینه — money</summary>
        public decimal Cost { get; set; }

        /// <summary>وضعیت — chip</summary>
        public string? Status { get; set; }
        /// <summary>تنِ رنگیِ نشانِ «وضعیت»</summary>
        public string StatusTone { get; set; } = "nu";
    }

    /// <summary>
    /// ViewModelِ صفحه‌ی «پیامک»
    /// </summary>
    /// <remarks>
    /// شمارندهٔ نویسه‌ها و هزینهٔ تخمینی زیر جعبهٔ متن؛ متغیرهای الگو با
    /// دکمه‌های درج خودکار.
    ///
    /// کهن‌الگوی طراحی: <c>detail-tabs</c> — چیدمان متناسب با همین قالب در View پیاده شده است.
    /// گروه: ارتباطات
    /// </remarks>
    public sealed class SmsViewModel : PageViewModelBase<SmsRow>
    {
        /// <summary>ایجاد ViewModel و بارگذاریِ دادهٔ نمونه</summary>
        /// <param name="logger">ثبت‌کننده‌ی رویدادها (اختیاری)</param>
        public SmsViewModel(ILogger? logger = null) : base(logger)
        {
            // کارت‌های شاخص
            Kpis.Add(new KpiItem("ارسالی امروز", "۱۴۲", 21, Application.Current.TryFindResource("Icon.Send") as Geometry, Application.Current.TryFindResource("Brush.Primary") as Brush));
            Kpis.Add(new KpiItem("تحویل شده", "۱۳۶", null, Application.Current.TryFindResource("Icon.CircleCheck") as Geometry, Application.Current.TryFindResource("Brush.Success") as Brush));
            Kpis.Add(new KpiItem("ناموفق", "۶", -2, Application.Current.TryFindResource("Icon.CircleX") as Geometry, Application.Current.TryFindResource("Brush.Danger") as Brush));
            Kpis.Add(new KpiItem("هزینه امروز", "۴۲,۶۰۰", null, Application.Current.TryFindResource("Icon.Wallet") as Geometry, Application.Current.TryFindResource("Brush.Warning") as Brush));

            // تب‌ها
            Tabs.Add(new TabItemModel("ارسال جدید", Application.Current.TryFindResource("Icon.Send") as Geometry, null));
            TabContents.Add(null);  // محتوای تب «ارسال جدید»
            Tabs.Add(new TabItemModel("الگوها", Application.Current.TryFindResource("Icon.FileText") as Geometry, null));
            TabContents.Add(null);  // محتوای تب «الگوها»
            Tabs.Add(new TabItemModel("گزارش ارسال", Application.Current.TryFindResource("Icon.ChartBar") as Geometry, null));
            TabContents.Add(null);  // محتوای تب «گزارش ارسال»
            Tabs.Add(new TabItemModel("تنظیمات پنل", Application.Current.TryFindResource("Icon.Settings") as Geometry, null));
            TabContents.Add(null);  // محتوای تب «تنظیمات پنل»

            // ارتباطات
            Relations.Add(new RelationItem("پیامک", "نوبت‌دهی", "یادآوری نوبت"));
            Relations.Add(new RelationItem("پیامک", "آزمایشگاه", "اطلاع‌رسانی نتیجه"));
            Relations.Add(new RelationItem("پیامک", "بیماران", "دریافت‌کنندگان"));

            // بارگذاریِ نخستین صفحه از داده‌ها
            _ = LoadAsync();
        }

        /// <summary>کارت‌های شاخصِ بالای صفحه</summary>
        public ObservableCollection<KpiItem> Kpis { get; } = new();

        /// <summary>محتوای تب‌ها</summary>
        public ObservableCollection<TabItemModel> Tabs { get; } = new();

        /// <summary>محتوای تب‌ها — هر اندیس متناظر با یک تب در View است</summary>
        public ObservableCollection<object?> TabContents { get; } = new();

        /// <summary>ارتباطاتِ این صفحه با دیگر صفحات</summary>
        public ObservableCollection<RelationItem> Relations { get; } = new();

        // ══════════════════════════════════════════════════════
        //  ویژگی‌های فرم — مقدارِ آغازین برابرِ دادهٔ نمونه است
        // ══════════════════════════════════════════════════════

        /// <summary>گیرندگان — الزامی</summary>
        public string Recipients
        {
            get => _recipients;
            set
            {
                if (SetProperty(ref _recipients, value))
                {
                    Validate();
                }
            }
        }
        private string _recipients = "بیماران امروز";

        /// <summary>الگو</summary>
        public string TemplateId
        {
            get => _templateId;
            set
            {
                if (SetProperty(ref _templateId, value))
                {
                }
            }
        }
        private string _templateId = "یادآوری نوبت";

        /// <summary>شماره‌های دستی</summary>
        public string ManualNumbers
        {
            get => _manualNumbers;
            set
            {
                if (SetProperty(ref _manualNumbers, value))
                {
                }
            }
        }
        private string _manualNumbers = "";

        /// <summary>متن پیام — الزامی</summary>
        public string Message
        {
            get => _message;
            set
            {
                if (SetProperty(ref _message, value))
                {
                    Validate();
                }
            }
        }
        private string _message = " بیمار گرامی، نوبت شما فردا ساعت ۱۰:۳۰ نزد دکتر موسوی است.";

        /// <summary>زمان ارسال</summary>
        public DateTime? SendAt
        {
            get => _sendAt;
            set
            {
                if (SetProperty(ref _sendAt, value))
                {
                }
            }
        }
        private DateTime? _sendAt = null;

        /// <summary>پنل ارسال</summary>
        public string Provider
        {
            get => _provider;
            set
            {
                if (SetProperty(ref _provider, value))
                {
                }
            }
        }
        private string _provider = "کاوه‌نگار";

        // ══════════════════════════════════════════════════════
        //  فرمان‌ها
        // ══════════════════════════════════════════════════════

        /// <summary>ارسال</summary>
        public IAsyncRelayCommand SendCommand =>
            new AsyncRelayCommand(SendAsync);

        /// <summary>اجرای «ارسال»</summary>
        private async Task SendAsync()
        {
            // TODO: پیوند به سرویسِ واقعی — «ارسال»
            await RunAsync(_ => Task.CompletedTask, "ارسال…");
        }

        /// <summary>پیش‌نمایش</summary>
        public IAsyncRelayCommand PreviewCommand =>
            new AsyncRelayCommand(PreviewAsync);

        /// <summary>اجرای «پیش‌نمایش»</summary>
        private async Task PreviewAsync()
        {
            // TODO: پیوند به سرویسِ واقعی — «پیش‌نمایش»
            await RunAsync(_ => Task.CompletedTask, "پیش‌نمایش…");
        }

        /// <summary>ذخیره الگو</summary>
        public IAsyncRelayCommand SaveTemplateCommand =>
            new AsyncRelayCommand(SaveTemplateAsync);

        /// <summary>اجرای «ذخیره الگو»</summary>
        private async Task SaveTemplateAsync()
        {
            // TODO: پیوند به سرویسِ واقعی — «ذخیره الگو»
            await RunAsync(_ => Task.CompletedTask, "ذخیره الگو…");
        }

        /// <summary>
        /// دادهٔ نمونه — همان سطرهایی که در وایرفریم دیده می‌شوند.
        /// تا زمانی که سرویسِ واقعی متصل نشده، این داده نمایش داده می‌شود
        /// تا صفحه هم در زمانِ طراحی و هم در اجرا قابلِ درک باشد.
        /// </summary>
        private static IEnumerable<SmsRow> SampleData()
        {
            yield return new SmsRow { Time = "۱۴۰۴/۰۷/۱۹ ۰۹:۰۰", Recipient = "سارا احمدی", RecipientSub = "۰۹۱۴-۴۲۲-۰۸۷۱", RecipientInitials = "سا", RecipientAvatar = Application.Current.TryFindResource("Brush.Info") as Brush, Template = "یادآوری نوبت", Cost = 300m, Status = "تحویل شد", StatusTone = "ok" };
            yield return new SmsRow { Time = "۱۴۰۴/۰۷/۱۹ ۰۹:۰۲", Recipient = "محمد رضایی", RecipientSub = "۰۹۱۲-۳۳۵-۱۲۹۰", RecipientInitials = "مر", RecipientAvatar = Application.Current.TryFindResource("Brush.Primary") as Brush, Template = "یادآوری نوبت", Cost = 300m, Status = "تحویل شد", StatusTone = "ok" };
            yield return new SmsRow { Time = "۱۴۰۴/۰۷/۱۹ ۰۹:۰۵", Recipient = "فاطمه کریمی", RecipientSub = "۰۹۳۸-۱۱۴-۲۳۴۵", RecipientInitials = "فک", RecipientAvatar = Application.Current.TryFindResource("Brush.Secondary") as Brush, Template = "نتیجه آزمایش", Cost = 300m, Status = "ناموفق", StatusTone = "dg" };
        }

        /// <summary>
        /// واکشیِ داده‌ها. تا زمانِ اتصالِ سرویسِ واقعی، دادهٔ نمونه
        /// بازگردانده می‌شود تا صفحه در زمانِ طراحی و اجرا زنده باشد.
        /// </summary>
        protected override Task<PagedResult<SmsRow>?> FetchAsync(CancellationToken ct)
        {
            // TODO: جایگزینی با فراخوانیِ سرویسِ واقعی، برای نمونه:
            //   var result = await _service.GetAsync(SearchText, PageNumber, PageSize, ct);
            var items = new List<SmsRow>(SampleData());
            return Task.FromResult<PagedResult<SmsRow>?>(
                new PagedResult<SmsRow>(items, items.Count));
        }
    }
}