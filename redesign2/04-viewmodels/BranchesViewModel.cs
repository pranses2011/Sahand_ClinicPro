// ═══════════════════════════════════════════════════════════════════════
//  BranchesViewModel
//  صفحه‌ی «شعب» · گروه «سازمانی» · کهن‌الگوی «list-cards»
//
//  متناظر با:  03-views/BranchesView.xaml
//  وایرفریم:    01-wireframes-html/BranchesView.html
//  پایه:        PageViewModelBase<BranchesRow>
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
    /// <summary>مدلِ نمایشِ یک ردیف از «شعب»</summary>
    /// <remarks>
    /// نام و نوعِ هر ویژگی دقیقاً با ستون‌های وایرفریمِ همین صفحه
    /// تطبیق دارد تا View بدون تغییر به آن متصل شود.
    /// </remarks>
    public sealed class BranchesRow
    {
        /// <summary>شعبه — text</summary>
        public string? Name { get; set; }

        /// <summary>کد — num</summary>
        public int Code { get; set; }

        /// <summary>شهر — text</summary>
        public string? City { get; set; }

        /// <summary>پرسنل — num</summary>
        public int Staff { get; set; }

        /// <summary>وضعیت — chip</summary>
        public string? Status { get; set; }
        /// <summary>تنِ رنگیِ نشانِ «وضعیت»</summary>
        public string StatusTone { get; set; } = "nu";
    }

    /// <summary>
    /// ViewModelِ صفحه‌ی «شعب»
    /// </summary>
    /// <remarks>
    /// کهن‌الگوی طراحی: <c>list-cards</c> — چیدمان متناسب با همین قالب در View پیاده شده است.
    /// گروه: سازمانی
    /// </remarks>
    public sealed class BranchesViewModel : PageViewModelBase<BranchesRow>
    {
        /// <summary>ایجاد ViewModel و بارگذاریِ دادهٔ نمونه</summary>
        /// <param name="logger">ثبت‌کننده‌ی رویدادها (اختیاری)</param>
        public BranchesViewModel(ILogger? logger = null) : base(logger)
        {
            // ارتباطات
            Relations.Add(new RelationItem("شعب", "پرسنل", "انتساب پرسنل به شعبه"));
            Relations.Add(new RelationItem("شعب", "بیماران", "فیلتر بر اساس شعبه"));

            // کارت‌ها
            Cards.Add(new CardItem("۱", "تبریز", ""));
            Cards.Add(new CardItem("۲", "تبریز", ""));
            Cards.Add(new CardItem("۳", "اهر", ""));

            // بارگذاریِ نخستین صفحه از داده‌ها
            _ = LoadAsync();
        }

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
        private object? _filter2 = "فعال";

        /// <summary>ارتباطاتِ این صفحه با دیگر صفحات</summary>
        public ObservableCollection<RelationItem> Relations { get; } = new();

        /// <summary>کارت‌های نمایش‌داده‌شده</summary>
        public ObservableCollection<CardItem> Cards { get; } = new();

        // ══════════════════════════════════════════════════════
        //  فرمان‌ها
        // ══════════════════════════════════════════════════════

        /// <summary>شعبه جدید</summary>
        public IAsyncRelayCommand NewCommand =>
            new AsyncRelayCommand(NewAsync);

        /// <summary>اجرای «شعبه جدید»</summary>
        private async Task NewAsync()
        {
            // TODO: پیوند به سرویسِ واقعی — «شعبه جدید»
            await RunAsync(_ => Task.CompletedTask, "شعبه جدید…");
        }

        /// <summary>
        /// دادهٔ نمونه — همان سطرهایی که در وایرفریم دیده می‌شوند.
        /// تا زمانی که سرویسِ واقعی متصل نشده، این داده نمایش داده می‌شود
        /// تا صفحه هم در زمانِ طراحی و هم در اجرا قابلِ درک باشد.
        /// </summary>
        private static IEnumerable<BranchesRow> SampleData()
        {
            yield return new BranchesRow { Name = "شعبه مرکزی", Code = 1, City = "تبریز", Staff = 22, Status = "فعال", StatusTone = "ok" };
            yield return new BranchesRow { Name = "شعبه ولیعصر", Code = 2, City = "تبریز", Staff = 9, Status = "فعال", StatusTone = "ok" };
            yield return new BranchesRow { Name = "شعبه اهر", Code = 3, City = "اهر", Staff = 3, Status = "غیرفعال", StatusTone = "nu" };
        }

        /// <summary>
        /// واکشیِ داده‌ها. تا زمانِ اتصالِ سرویسِ واقعی، دادهٔ نمونه
        /// بازگردانده می‌شود تا صفحه در زمانِ طراحی و اجرا زنده باشد.
        /// </summary>
        protected override Task<PagedResult<BranchesRow>?> FetchAsync(CancellationToken ct)
        {
            // TODO: جایگزینی با فراخوانیِ سرویسِ واقعی، برای نمونه:
            //   var result = await _service.GetAsync(SearchText, PageNumber, PageSize, ct);
            var items = new List<BranchesRow>(SampleData());
            return Task.FromResult<PagedResult<BranchesRow>?>(
                new PagedResult<BranchesRow>(items, items.Count));
        }
    }
}