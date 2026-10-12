// ═══════════════════════════════════════════════════════════════════════
//  VisitViewModel
//  صفحه‌ی «ویزیت (SOAP)» · گروه «ویزیت» · کهن‌الگوی «form-std»
//
//  متناظر با:  03-views/VisitWindow.xaml
//  وایرفریم:    01-wireframes-html/VisitWindow.html
//  پایه:        FormViewModelBase
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
    /// <summary>
    /// ViewModelِ صفحه‌ی «ویزیت (SOAP)»
    /// </summary>
    /// <remarks>
    /// فیلدهای علائم حیاتی در یک ردیفِ شش‌تاییِ کم‌عرض — چون مقدارهای
    /// کوتاه عددی هستند؛ بخش‌های متنی عرض کامل.
    ///
    /// کهن‌الگوی طراحی: <c>form-std</c> — چیدمان متناسب با همین قالب در View پیاده شده است.
    /// گروه: ویزیت
    /// </remarks>
    public sealed class VisitViewModel : FormViewModelBase
    {
        /// <summary>ایجاد ViewModel و بارگذاریِ دادهٔ نمونه</summary>
        /// <param name="logger">ثبت‌کننده‌ی رویدادها (اختیاری)</param>
        public VisitViewModel(ILogger? logger = null) : base(logger)
        {
            // ارتباطات
            Relations.Add(new RelationItem("ویزیت", "فرم نسخه", "نسخه بر اساس تشخیص"));
            Relations.Add(new RelationItem("ویزیت", "درخواست آزمایش", "درخواست پاراکلینیک"));
            Relations.Add(new RelationItem("ویزیت", "پذیرش و صف", "اتمام ویزیت وضعیت پذیرش را عوض می‌کند"));
        }

        /// <summary>ارتباطاتِ این صفحه با دیگر صفحات</summary>
        public ObservableCollection<RelationItem> Relations { get; } = new();

        // ══════════════════════════════════════════════════════
        //  ویژگی‌های فرم — مقدارِ آغازین برابرِ دادهٔ نمونه است
        // ══════════════════════════════════════════════════════

        /// <summary>فشار خون</summary>
        public string BP
        {
            get => _bP;
            set
            {
                if (SetProperty(ref _bP, value))
                {
                    MarkDirty();
                }
            }
        }
        private string _bP = "۱۲۰/۸۰";

        /// <summary>نبض</summary>
        public int Pulse
        {
            get => _pulse;
            set
            {
                if (SetProperty(ref _pulse, value))
                {
                    MarkDirty();
                }
            }
        }
        private int _pulse = 78;

        /// <summary>دما</summary>
        public int Temp
        {
            get => _temp;
            set
            {
                if (SetProperty(ref _temp, value))
                {
                    MarkDirty();
                }
            }
        }
        private int _temp = 0;

        /// <summary>تنفس</summary>
        public int RR
        {
            get => _rR;
            set
            {
                if (SetProperty(ref _rR, value))
                {
                    MarkDirty();
                }
            }
        }
        private int _rR = 16;

        /// <summary>اشباع O₂</summary>
        public int O2Sat
        {
            get => _o2Sat;
            set
            {
                if (SetProperty(ref _o2Sat, value))
                {
                    MarkDirty();
                }
            }
        }
        private int _o2Sat = 98;

        /// <summary>وزن</summary>
        public int Weight
        {
            get => _weight;
            set
            {
                if (SetProperty(ref _weight, value))
                {
                    MarkDirty();
                }
            }
        }
        private int _weight = 65;

        /// <summary>شرح‌حال از زبان بیمار — الزامی</summary>
        public string Subjective
        {
            get => _subjective;
            set
            {
                if (SetProperty(ref _subjective, value))
                {
                    MarkDirty();
                    Validate();
                }
            }
        }
        private string _subjective = "درد مبهم شکم از سه روز پیش که پس از صرف غذا تشدید می‌شود؛ تهوع بدون استفراغ…";

        /// <summary>یافته‌های معاینه</summary>
        public string Objective
        {
            get => _objective;
            set
            {
                if (SetProperty(ref _objective, value))
                {
                    MarkDirty();
                }
            }
        }
        private string _objective = "حساسیت خفیف در اپی‌گاستر، بدون دفاع عضلانی؛ صداهای روده‌ای طبیعی…";

        /// <summary>کد ICD-10 — الزامی</summary>
        public string IcdCode
        {
            get => _icdCode;
            set
            {
                if (SetProperty(ref _icdCode, value))
                {
                    MarkDirty();
                    Validate();
                }
            }
        }
        private string _icdCode = "K21.9 — رفلاکس معده";

        /// <summary>تشخیص</summary>
        public string Assessment
        {
            get => _assessment;
            set
            {
                if (SetProperty(ref _assessment, value))
                {
                    MarkDirty();
                }
            }
        }
        private string _assessment = "رفلاکس معدی‌مروی بدون ازوفاژیت";

        /// <summary>برنامه درمان</summary>
        public string Plan
        {
            get => _plan;
            set
            {
                if (SetProperty(ref _plan, value))
                {
                    MarkDirty();
                }
            }
        }
        private string _plan = "امپرازول ۲۰ میلی‌گرم هر شب…";

        /// <summary>پیگیری</summary>
        public string FollowUp
        {
            get => _followUp;
            set
            {
                if (SetProperty(ref _followUp, value))
                {
                    MarkDirty();
                }
            }
        }
        private string _followUp = "۲ هفته دیگر";

        /// <summary>ارجاع</summary>
        public string Referral
        {
            get => _referral;
            set
            {
                if (SetProperty(ref _referral, value))
                {
                    MarkDirty();
                }
            }
        }
        private string _referral = "ندارد";

        /// <summary>روز استراحت</summary>
        public int RestDays
        {
            get => _restDays;
            set
            {
                if (SetProperty(ref _restDays, value))
                {
                    MarkDirty();
                }
            }
        }
        private int _restDays = 0;

        // ══════════════════════════════════════════════════════
        //  فرمان‌ها
        // ══════════════════════════════════════════════════════

        /// <summary>تکمیل ویزیت</summary>
        public IAsyncRelayCommand SaveVisitCommand =>
            new AsyncRelayCommand(SaveVisitAsync);

        /// <summary>اجرای «تکمیل ویزیت»</summary>
        private async Task SaveVisitAsync()
        {
            // TODO: پیوند به سرویسِ واقعی — «تکمیل ویزیت»
            await RunAsync(_ => Task.CompletedTask, "تکمیل ویزیت…");
        }

        /// <summary>نسخه</summary>
        public IAsyncRelayCommand PrescribeCommand =>
            new AsyncRelayCommand(PrescribeAsync);

        /// <summary>اجرای «نسخه»</summary>
        private async Task PrescribeAsync()
        {
            // TODO: پیوند به سرویسِ واقعی — «نسخه»
            await RunAsync(_ => Task.CompletedTask, "نسخه…");
        }
        /// <summary>جستجو و انتخابِ «کد ICD-10»</summary>
        public IAsyncRelayCommand LookupIcdCodeCommand =>
            new AsyncRelayCommand(() => LookupAsync("کد ICD-10"));

        /// <summary>
        /// اعتبارسنجیِ سمتِ کلاینت پیش از ذخیره
        /// </summary>
        protected override bool Validate()
        {
            var ok = true;

            if (string.IsNullOrWhiteSpace(Subjective))
            {
                AddError("شرح‌حال از زبان بیمار الزامی است", nameof(Subjective));
            }
            else
            {
                ClearErrors(nameof(Subjective));
            }

            if (string.IsNullOrWhiteSpace(IcdCode))
            {
                AddError("کد ICD-10 الزامی است", nameof(IcdCode));
            }
            else
            {
                ClearErrors(nameof(IcdCode));
            }
            return ok && !HasErrors;
        }

        /// <summary>
        /// ذخیره‌ی داده‌ها. در صورت نبودِ سرویس، فقط وضعیت موفقیت نمایش
        /// داده می‌شود تا رفتارِ فرم قابلِ بررسی باشد.
        /// </summary>
        protected override Task PersistAsync(CancellationToken ct)
        {
            // TODO: جایگزینی با فراخوانیِ سرویسِ واقعی
            return Task.CompletedTask;
        }

        /// <summary>جستجو در یک فیلدِ جستجومحور</summary>
        private Task LookupAsync(string field)
            => RunAsync(_ => Task.CompletedTask, $"جستجوی {field}…");

        /// <summary>خواندنِ بارکد برای یک فیلد</summary>
        private void ScanBarcode(string field)
            => ShowSuccess($"بارکدِ {field} خوانده شد");
    }
}