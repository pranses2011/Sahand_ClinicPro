// ═══════════════════════════════════════════════════════════════════════
//  NotificationsViewModel
//  صفحه‌ی «اعلان‌ها» · گروه «پیشخوان» · کهن‌الگوی «list-dense»
//
//  متناظر با:  03-views/NotificationsView.xaml
//  وایرفریم:    01-wireframes-html/NotificationsView.html
//  پایه:        PageViewModelBase<NotificationsRow>
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
    /// <summary>مدلِ نمایشِ یک ردیف از «اعلان‌ها»</summary>
    /// <remarks>
    /// نام و نوعِ هر ویژگی دقیقاً با ستون‌های وایرفریمِ همین صفحه
    /// تطبیق دارد تا View بدون تغییر به آن متصل شود.
    /// </remarks>
    public sealed class NotificationsRow
    {
        /// <summary>عنوان — text</summary>
        public string? Title { get; set; }

        /// <summary>سطح — chip</summary>
        public string? Level { get; set; }
        /// <summary>تنِ رنگیِ نشانِ «سطح»</summary>
        public string LevelTone { get; set; } = "nu";

        /// <summary>زمان — date</summary>
        public string? Date { get; set; }

        /// <summary>وضعیت — chip</summary>
        public string? IsRead { get; set; }
        /// <summary>تنِ رنگیِ نشانِ «وضعیت»</summary>
        public string IsReadTone { get; set; } = "nu";
    }

    /// <summary>
    /// ViewModelِ صفحه‌ی «اعلان‌ها»
    /// </summary>
    /// <remarks>
    /// کهن‌الگوی طراحی: <c>list-dense</c> — چیدمان متناسب با همین قالب در View پیاده شده است.
    /// گروه: پیشخوان
    /// </remarks>
    public sealed class NotificationsViewModel : PageViewModelBase<NotificationsRow>
    {
        /// <summary>ایجاد ViewModel و بارگذاریِ دادهٔ نمونه</summary>
        /// <param name="logger">ثبت‌کننده‌ی رویدادها (اختیاری)</param>
        public NotificationsViewModel(ILogger? logger = null) : base(logger)
        {
            // ارتباطات
            Relations.Add(new RelationItem("اعلان‌ها", "داروخانه", "هشدار کمبود موجودی"));
            Relations.Add(new RelationItem("اعلان‌ها", "نوبت‌دهی", "هشدار نوبت تأیید‌نشده"));
            Relations.Add(new RelationItem("اعلان‌ها", "وضعیت لایسنس", "هشدار انقضا"));

            // بارگذاریِ نخستین صفحه از داده‌ها
            _ = LoadAsync();
        }

        /// <summary>فیلترِ «نوع»</summary>
        public object? Filter2
        {
            get => _filter2;
            set
            {
                if (SetProperty(ref _filter2, value))
                {
                    PageNumber = 1;
                    _ = LoadAsync();
                }
            }
        }
        private object? _filter2 = "همه";

        /// <summary>فیلترِ «فقط خوانده‌نشده»</summary>
        public bool Filter3
        {
            get => _filter3;
            set
            {
                if (SetProperty(ref _filter3, value))
                {
                    PageNumber = 1;
                    _ = LoadAsync();
                }
            }
        }
        private bool _filter3 = false;

        /// <summary>ارتباطاتِ این صفحه با دیگر صفحات</summary>
        public ObservableCollection<RelationItem> Relations { get; } = new();

        // ══════════════════════════════════════════════════════
        //  فرمان‌ها
        // ══════════════════════════════════════════════════════

        /// <summary>خواندن همه</summary>
        public IAsyncRelayCommand MarkAllReadCommand =>
            new AsyncRelayCommand(MarkAllReadAsync);

        /// <summary>اجرای «خواندن همه»</summary>
        private async Task MarkAllReadAsync()
        {
            // TODO: پیوند به سرویسِ واقعی — «خواندن همه»
            await RunAsync(_ => Task.CompletedTask, "خواندن همه…");
        }

        /// <summary>
        /// دادهٔ نمونه — همان سطرهایی که در وایرفریم دیده می‌شوند.
        /// تا زمانی که سرویسِ واقعی متصل نشده، این داده نمایش داده می‌شود
        /// تا صفحه هم در زمانِ طراحی و هم در اجرا قابلِ درک باشد.
        /// </summary>
        private static IEnumerable<NotificationsRow> SampleData()
        {
            yield return new NotificationsRow { Title = "بحرانی", TitleTone = "dg", Level = "موجودی آموکسی‌سیلین به زیر حد سفارش رسید", Date = "امروز ۰۹:۱۲", IsRead = "خوانده‌نشده", IsReadTone = "wn" };
            yield return new NotificationsRow { Title = "هشدار", TitleTone = "wn", Level = "۳ نوبت تأیید‌نشده برای فردا", Date = "امروز ۰۸:۴۰", IsRead = "خوانده‌نشده", IsReadTone = "wn" };
            yield return new NotificationsRow { Title = "اطلاعات", TitleTone = "in", Level = "پشتیبان‌گیری شبانه با موفقیت انجام شد", Date = "دیروز ۲۳:۰۰", IsRead = "خوانده شده", IsReadTone = "nu" };
            yield return new NotificationsRow { Title = "بحرانی", TitleTone = "dg", Level = "گواهی لایسنس ۷ روز دیگر منقضی می‌شود", Date = "دیروز ۱۸:۲۵", IsRead = "خوانده‌نشده", IsReadTone = "wn" };
        }

        /// <summary>
        /// واکشیِ داده‌ها. تا زمانِ اتصالِ سرویسِ واقعی، دادهٔ نمونه
        /// بازگردانده می‌شود تا صفحه در زمانِ طراحی و اجرا زنده باشد.
        /// </summary>
        protected override Task<PagedResult<NotificationsRow>?> FetchAsync(CancellationToken ct)
        {
            // TODO: جایگزینی با فراخوانیِ سرویسِ واقعی، برای نمونه:
            //   var result = await _service.GetAsync(SearchText, PageNumber, PageSize, ct);
            var items = new List<NotificationsRow>(SampleData());
            return Task.FromResult<PagedResult<NotificationsRow>?>(
                new PagedResult<NotificationsRow>(items, items.Count));
        }
    }
}