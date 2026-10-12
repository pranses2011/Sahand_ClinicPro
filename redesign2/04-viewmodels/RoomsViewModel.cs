// ═══════════════════════════════════════════════════════════════════════
//  RoomsViewModel
//  صفحه‌ی «اتاق‌ها» · گروه «پذیرش» · کهن‌الگوی «list-cards»
//
//  متناظر با:  03-views/RoomsView.xaml
//  وایرفریم:    01-wireframes-html/RoomsView.html
//  پایه:        PageViewModelBase<RoomsRow>
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
    /// <summary>مدلِ نمایشِ یک ردیف از «اتاق‌ها»</summary>
    /// <remarks>
    /// نام و نوعِ هر ویژگی دقیقاً با ستون‌های وایرفریمِ همین صفحه
    /// تطبیق دارد تا View بدون تغییر به آن متصل شود.
    /// </remarks>
    public sealed class RoomsRow
    {
        /// <summary>اتاق — text</summary>
        public string? Name { get; set; }

        /// <summary>شماره — num</summary>
        public int Number { get; set; }

        /// <summary>پزشک مستقر — text</summary>
        public string? Doctor { get; set; }

        /// <summary>وضعیت — chip</summary>
        public string? Status { get; set; }
        /// <summary>تنِ رنگیِ نشانِ «وضعیت»</summary>
        public string StatusTone { get; set; } = "nu";
    }

    /// <summary>
    /// ViewModelِ صفحه‌ی «اتاق‌ها»
    /// </summary>
    /// <remarks>
    /// کهن‌الگوی طراحی: <c>list-cards</c> — چیدمان متناسب با همین قالب در View پیاده شده است.
    /// گروه: پذیرش
    /// </remarks>
    public sealed class RoomsViewModel : PageViewModelBase<RoomsRow>
    {
        /// <summary>ایجاد ViewModel و بارگذاریِ دادهٔ نمونه</summary>
        /// <param name="logger">ثبت‌کننده‌ی رویدادها (اختیاری)</param>
        public RoomsViewModel(ILogger? logger = null) : base(logger)
        {
            // ارتباطات
            Relations.Add(new RelationItem("اتاق‌ها", "مدیریت اتاق‌ها", "ویرایش گروهی"));
            Relations.Add(new RelationItem("اتاق‌ها", "نوبت‌دهی", "تخصیص اتاق به نوبت"));

            // کارت‌ها
            Cards.Add(new CardItem("۱", "دکتر موسوی", ""));
            Cards.Add(new CardItem("۲", "دکتر رضایی", ""));
            Cards.Add(new CardItem("۳", "—", ""));
            Cards.Add(new CardItem("۴", "—", ""));

            // بارگذاریِ نخستین صفحه از داده‌ها
            _ = LoadAsync();
        }

        /// <summary>فیلترِ «شعبه»</summary>
        public object? Filter1
        {
            get => _filter1;
            set
            {
                if (SetProperty(ref _filter1, value))
                {
                    PageNumber = 1;
                    _ = LoadAsync();
                }
            }
        }
        private object? _filter1 = "همه";

        /// <summary>فیلترِ «وضعیت»</summary>
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

        /// <summary>ارتباطاتِ این صفحه با دیگر صفحات</summary>
        public ObservableCollection<RelationItem> Relations { get; } = new();

        /// <summary>کارت‌های نمایش‌داده‌شده</summary>
        public ObservableCollection<CardItem> Cards { get; } = new();

        // ══════════════════════════════════════════════════════
        //  فرمان‌ها
        // ══════════════════════════════════════════════════════

        /// <summary>مدیریت اتاق‌ها</summary>
        public IAsyncRelayCommand ManageCommand =>
            new AsyncRelayCommand(ManageAsync);

        /// <summary>اجرای «مدیریت اتاق‌ها»</summary>
        private async Task ManageAsync()
        {
            // TODO: پیوند به سرویسِ واقعی — «مدیریت اتاق‌ها»
            await RunAsync(_ => Task.CompletedTask, "مدیریت اتاق‌ها…");
        }

        /// <summary>اتاق جدید</summary>
        public IAsyncRelayCommand NewCommand =>
            new AsyncRelayCommand(NewAsync);

        /// <summary>اجرای «اتاق جدید»</summary>
        private async Task NewAsync()
        {
            // TODO: پیوند به سرویسِ واقعی — «اتاق جدید»
            await RunAsync(_ => Task.CompletedTask, "اتاق جدید…");
        }

        /// <summary>
        /// دادهٔ نمونه — همان سطرهایی که در وایرفریم دیده می‌شوند.
        /// تا زمانی که سرویسِ واقعی متصل نشده، این داده نمایش داده می‌شود
        /// تا صفحه هم در زمانِ طراحی و هم در اجرا قابلِ درک باشد.
        /// </summary>
        private static IEnumerable<RoomsRow> SampleData()
        {
            yield return new RoomsRow { Name = "اتاق معاینه ۱", Number = 1, Doctor = "دکتر موسوی", Status = "اشغال", StatusTone = "dg" };
            yield return new RoomsRow { Name = "اتاق معاینه ۲", Number = 2, Doctor = "دکتر رضایی", Status = "اشغال", StatusTone = "dg" };
            yield return new RoomsRow { Name = "اتاق معاینه ۳", Number = 3, Doctor = "—", Status = "آزاد", StatusTone = "ok" };
            yield return new RoomsRow { Name = "اتاق تزریقات", Number = 4, Doctor = "—", Status = "آزاد", StatusTone = "ok" };
        }

        /// <summary>
        /// واکشیِ داده‌ها. تا زمانِ اتصالِ سرویسِ واقعی، دادهٔ نمونه
        /// بازگردانده می‌شود تا صفحه در زمانِ طراحی و اجرا زنده باشد.
        /// </summary>
        protected override Task<PagedResult<RoomsRow>?> FetchAsync(CancellationToken ct)
        {
            // TODO: جایگزینی با فراخوانیِ سرویسِ واقعی، برای نمونه:
            //   var result = await _service.GetAsync(SearchText, PageNumber, PageSize, ct);
            var items = new List<RoomsRow>(SampleData());
            return Task.FromResult<PagedResult<RoomsRow>?>(
                new PagedResult<RoomsRow>(items, items.Count));
        }
    }
}