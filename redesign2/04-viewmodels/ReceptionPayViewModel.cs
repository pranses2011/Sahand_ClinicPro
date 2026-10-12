// ═══════════════════════════════════════════════════════════════════════
//  ReceptionPayViewModel
//  صفحه‌ی «دریافت وجه» · گروه «پذیرش» · کهن‌الگوی «form-narrow»
//
//  متناظر با:  03-views/ReceptionPayWindow.xaml
//  وایرفریم:    01-wireframes-html/ReceptionPayWindow.html
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
    /// ViewModelِ صفحه‌ی «دریافت وجه»
    /// </summary>
    /// <remarks>
    /// کهن‌الگوی طراحی: <c>form-narrow</c> — چیدمان متناسب با همین قالب در View پیاده شده است.
    /// گروه: پذیرش
    /// </remarks>
    public sealed class ReceptionPayViewModel : FormViewModelBase
    {
        /// <summary>ایجاد ViewModel و بارگذاریِ دادهٔ نمونه</summary>
        /// <param name="logger">ثبت‌کننده‌ی رویدادها (اختیاری)</param>
        public ReceptionPayViewModel(ILogger? logger = null) : base(logger)
        {
            // ارتباطات
            Relations.Add(new RelationItem("دریافت وجه", "صورتحساب", "فاکتور صادر می‌شود"));
            Relations.Add(new RelationItem("دریافت وجه", "بستن شیفت", "در پایان شیفت جمع‌بندی می‌شود"));
        }

        /// <summary>ارتباطاتِ این صفحه با دیگر صفحات</summary>
        public ObservableCollection<RelationItem> Relations { get; } = new();

        // ══════════════════════════════════════════════════════
        //  ویژگی‌های فرم — مقدارِ آغازین برابرِ دادهٔ نمونه است
        // ══════════════════════════════════════════════════════

        /// <summary>جمع کل</summary>
        public decimal Total
        {
            get => _total;
            set
            {
                if (SetProperty(ref _total, value))
                {
                    MarkDirty();
                }
            }
        }
        private decimal _total = 250000m;

        /// <summary>سهم بیمه</summary>
        public decimal Insurance
        {
            get => _insurance;
            set
            {
                if (SetProperty(ref _insurance, value))
                {
                    MarkDirty();
                }
            }
        }
        private decimal _insurance = 175000m;

        /// <summary>قابل پرداخت — الزامی</summary>
        public decimal PatientPay
        {
            get => _patientPay;
            set
            {
                if (SetProperty(ref _patientPay, value))
                {
                    MarkDirty();
                    Validate();
                }
            }
        }
        private decimal _patientPay = 75000m;

        /// <summary>تخفیف</summary>
        public decimal Discount
        {
            get => _discount;
            set
            {
                if (SetProperty(ref _discount, value))
                {
                    MarkDirty();
                }
            }
        }
        private decimal _discount = 0m;

        /// <summary>شیوه پرداخت — الزامی</summary>
        public string PayMethod
        {
            get => _payMethod;
            set
            {
                if (SetProperty(ref _payMethod, value))
                {
                    MarkDirty();
                    Validate();
                }
            }
        }
        private string _payMethod = "نقد";

        /// <summary>شماره پیگیری</summary>
        public string Ref
        {
            get => _ref;
            set
            {
                if (SetProperty(ref _ref, value))
                {
                    MarkDirty();
                }
            }
        }
        private string _ref = "";

        // ══════════════════════════════════════════════════════
        //  فرمان‌ها
        // ══════════════════════════════════════════════════════

        /// <summary>ثبت پرداخت</summary>
        public IAsyncRelayCommand PayCommand =>
            new AsyncRelayCommand(PayAsync);

        /// <summary>اجرای «ثبت پرداخت»</summary>
        private async Task PayAsync()
        {
            // TODO: پیوند به سرویسِ واقعی — «ثبت پرداخت»
            await RunAsync(_ => Task.CompletedTask, "ثبت پرداخت…");
        }

        /// <summary>چاپ رسید</summary>
        public IAsyncRelayCommand PrintCommand =>
            new AsyncRelayCommand(PrintAsync);

        /// <summary>اجرای «چاپ رسید»</summary>
        private async Task PrintAsync()
        {
            // TODO: پیوند به سرویسِ واقعی — «چاپ رسید»
            await RunAsync(_ => Task.CompletedTask, "چاپ رسید…");
        }

        /// <summary>
        /// اعتبارسنجیِ سمتِ کلاینت پیش از ذخیره
        /// </summary>
        protected override bool Validate()
        {
            var ok = true;

            if (PatientPay <= 0)
            {
                AddError("قابل پرداخت الزامی است", nameof(PatientPay));
            }
            else
            {
                ClearErrors(nameof(PatientPay));
            }

            if (string.IsNullOrWhiteSpace(PayMethod))
            {
                AddError("شیوه پرداخت الزامی است", nameof(PayMethod));
            }
            else
            {
                ClearErrors(nameof(PayMethod));
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