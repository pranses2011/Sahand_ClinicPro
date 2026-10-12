// ═══════════════════════════════════════════════════════════════════════
//  DoctorsViewModel
//  صفحه‌ی «پزشکان» · گروه «منابع انسانی» · کهن‌الگوی «list-cards»
//
//  متناظر با:  03-views/DoctorsView.xaml
//  وایرفریم:    01-wireframes-html/DoctorsView.html
//  پایه:        PageViewModelBase<DoctorsRow>
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
    /// <summary>مدلِ نمایشِ یک ردیف از «پزشکان»</summary>
    /// <remarks>
    /// نام و نوعِ هر ویژگی دقیقاً با ستون‌های وایرفریمِ همین صفحه
    /// تطبیق دارد تا View بدون تغییر به آن متصل شود.
    /// </remarks>
    public sealed class DoctorsRow
    {
        /// <summary>پزشک — who</summary>
        public string? Name { get; set; }
        /// <summary>زیرنویسِ «پزشک» (مانند کد یا سن)</summary>
        public string? NameSub { get; set; }
        /// <summary>حروفِ اختصاریِ نشان برای «پزشک»</summary>
        public string? NameInitials { get; set; }
        /// <summary>رنگِ نشانِ «پزشک» — از تم خوانده می‌شود</summary>
        public Brush? NameAvatar { get; set; }

        /// <summary>تخصص — text</summary>
        public string? Specialty { get; set; }

        /// <summary>روزهای کاری — text</summary>
        public string? Days { get; set; }

        /// <summary>ظرفیت روزانه — num</summary>
        public int Capacity { get; set; }

        /// <summary>وضعیت — chip</summary>
        public string? Status { get; set; }
        /// <summary>تنِ رنگیِ نشانِ «وضعیت»</summary>
        public string StatusTone { get; set; } = "nu";
    }

    /// <summary>
    /// ViewModelِ صفحه‌ی «پزشکان»
    /// </summary>
    /// <remarks>
    /// کهن‌الگوی طراحی: <c>list-cards</c> — چیدمان متناسب با همین قالب در View پیاده شده است.
    /// گروه: منابع انسانی
    /// </remarks>
    public sealed class DoctorsViewModel : PageViewModelBase<DoctorsRow>
    {
        /// <summary>ایجاد ViewModel و بارگذاریِ دادهٔ نمونه</summary>
        /// <param name="logger">ثبت‌کننده‌ی رویدادها (اختیاری)</param>
        public DoctorsViewModel(ILogger? logger = null) : base(logger)
        {
            // ارتباطات
            Relations.Add(new RelationItem("پزشکان", "فرم پزشک", "ایجاد/ویرایش"));
            Relations.Add(new RelationItem("پزشکان", "نوبت‌دهی", "ظرفیت برنامه"));
            Relations.Add(new RelationItem("پزشکان", "گزارش کارمزد", "کارمزد"));

            // کارت‌ها
            Cards.Add(new CardItem("متخصص داخلی", "شنبه · یکشنبه · دوشنبه", ""));
            Cards.Add(new CardItem("متخصص قلب", "شنبه · دوشنبه · چهارشنبه", ""));
            Cards.Add(new CardItem("پزشک عمومی", "همه‌روزه", ""));

            // بارگذاریِ نخستین صفحه از داده‌ها
            _ = LoadAsync();
        }

        /// <summary>فیلترِ «تخصص»</summary>
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

        /// <summary>فیلترِ «وضعیت»</summary>
        public object? Filter3
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
        private object? _filter3 = "فعال";

        /// <summary>ارتباطاتِ این صفحه با دیگر صفحات</summary>
        public ObservableCollection<RelationItem> Relations { get; } = new();

        /// <summary>کارت‌های نمایش‌داده‌شده</summary>
        public ObservableCollection<CardItem> Cards { get; } = new();

        // ══════════════════════════════════════════════════════
        //  فرمان‌ها
        // ══════════════════════════════════════════════════════

        /// <summary>پزشک جدید</summary>
        public IAsyncRelayCommand NewCommand =>
            new AsyncRelayCommand(NewAsync);

        /// <summary>اجرای «پزشک جدید»</summary>
        private async Task NewAsync()
        {
            // TODO: پیوند به سرویسِ واقعی — «پزشک جدید»
            await RunAsync(_ => Task.CompletedTask, "پزشک جدید…");
        }

        /// <summary>برنامه هفتگی</summary>
        public IAsyncRelayCommand ScheduleCommand =>
            new AsyncRelayCommand(ScheduleAsync);

        /// <summary>اجرای «برنامه هفتگی»</summary>
        private async Task ScheduleAsync()
        {
            // TODO: پیوند به سرویسِ واقعی — «برنامه هفتگی»
            await RunAsync(_ => Task.CompletedTask, "برنامه هفتگی…");
        }

        /// <summary>
        /// دادهٔ نمونه — همان سطرهایی که در وایرفریم دیده می‌شوند.
        /// تا زمانی که سرویسِ واقعی متصل نشده، این داده نمایش داده می‌شود
        /// تا صفحه هم در زمانِ طراحی و هم در اجرا قابلِ درک باشد.
        /// </summary>
        private static IEnumerable<DoctorsRow> SampleData()
        {
            yield return new DoctorsRow { Name = "دکتر موسوی", NameSub = "متخصص داخلی", NameInitials = "دم", NameAvatar = Application.Current.TryFindResource("Brush.Warning") as Brush, Specialty = "متخصص داخلی", Days = "شنبه · یکشنبه · دوشنبه", Capacity = 40, Status = "فعال", StatusTone = "ok" };
            yield return new DoctorsRow { Name = "دکتر رضایی", NameSub = "متخصص قلب", NameInitials = "در", NameAvatar = Application.Current.TryFindResource("Brush.Primary") as Brush, Specialty = "متخصص قلب", Days = "شنبه · دوشنبه · چهارشنبه", Capacity = 30, Status = "فعال", StatusTone = "ok" };
            yield return new DoctorsRow { Name = "دکتر کریمی", NameSub = "عمومی", NameInitials = "دک", NameAvatar = Application.Current.TryFindResource("Brush.Secondary") as Brush, Specialty = "پزشک عمومی", Days = "همه‌روزه", Capacity = 50, Status = "فعال", StatusTone = "ok" };
        }

        /// <summary>
        /// واکشیِ داده‌ها. تا زمانِ اتصالِ سرویسِ واقعی، دادهٔ نمونه
        /// بازگردانده می‌شود تا صفحه در زمانِ طراحی و اجرا زنده باشد.
        /// </summary>
        protected override Task<PagedResult<DoctorsRow>?> FetchAsync(CancellationToken ct)
        {
            // TODO: جایگزینی با فراخوانیِ سرویسِ واقعی، برای نمونه:
            //   var result = await _service.GetAsync(SearchText, PageNumber, PageSize, ct);
            var items = new List<DoctorsRow>(SampleData());
            return Task.FromResult<PagedResult<DoctorsRow>?>(
                new PagedResult<DoctorsRow>(items, items.Count));
        }
    }
}