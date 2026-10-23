// ═══════════════════════════════════════════════════════════════════════
//  InvoiceFormViewModel
//  صفحه‌ی «فرم صورتحساب» · گروه «مالی» · کهن‌الگوی «form-lines»
//
//  متناظر با:  03-views/InvoiceFormWindow.xaml
//  وایرفریم:    01-wireframes-html/InvoiceFormWindow.html
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
    /// ViewModelِ صفحه‌ی «فرم صورتحساب»
    /// </summary>
    /// <remarks>
    /// کهن‌الگوی طراحی: <c>form-lines</c> — چیدمان متناسب با همین قالب در View پیاده شده است.
    /// گروه: مالی
    /// </remarks>
    public sealed class InvoiceFormViewModel : FormViewModelBase
    {
        /// <summary>ایجاد ViewModel و بارگذاریِ دادهٔ نمونه</summary>
        /// <param name="logger">ثبت‌کننده‌ی رویدادها (اختیاری)</param>
        public InvoiceFormViewModel(ILogger? logger = null) : base(logger)
        {
            // ارتباطات
            Relations.Add(new RelationItem("فرم صورتحساب", "تعرفه خدمات", "قیمت از تعرفه"));
            Relations.Add(new RelationItem("فرم صورتحساب", "حسابداری", "سند خودکارِ متوازن"));
        }

        /// <summary>ارتباطاتِ این صفحه با دیگر صفحات</summary>
        public ObservableCollection<RelationItem> Relations { get; } = new();

        // ══════════════════════════════════════════════════════
        //  ویژگی‌های فرم — مقدارِ آغازین برابرِ دادهٔ نمونه است
        // ══════════════════════════════════════════════════════

        /// <summary>بیمار — الزامی</summary>
        public string PatientId
        {
            get => _patientId;
            set
            {
                if (SetProperty(ref _patientId, value))
                {
                    MarkDirty();
                    Validate();
                }
            }
        }
        private string _patientId = "سارا احمدی — P-۱۰۲۴";

        /// <summary>تاریخ — الزامی</summary>
        public DateTime? Date
        {
            get => _date;
            set
            {
                if (SetProperty(ref _date, value))
                {
                    MarkDirty();
                    Validate();
                }
            }
        }
        private DateTime? _date = null;

        /// <summary>بیمه</summary>
        public string InsuranceId
        {
            get => _insuranceId;
            set
            {
                if (SetProperty(ref _insuranceId, value))
                {
                    MarkDirty();
                }
            }
        }
        private string _insuranceId = "تأمین اجتماعی";

        /// <summary>پذیرش مرتبط</summary>
        public string ReceptionId
        {
            get => _receptionId;
            set
            {
                if (SetProperty(ref _receptionId, value))
                {
                    MarkDirty();
                }
            }
        }
        private string _receptionId = "R-۱۲۸۴";

        /// <summary>پزشک</summary>
        public string DoctorId
        {
            get => _doctorId;
            set
            {
                if (SetProperty(ref _doctorId, value))
                {
                    MarkDirty();
                }
            }
        }
        private string _doctorId = "دکتر موسوی";

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

        /// <summary>چاپ فاکتور</summary>
        public IAsyncRelayCommand PrintCommand =>
            new AsyncRelayCommand(PrintAsync);

        /// <summary>اجرای «چاپ فاکتور»</summary>
        private async Task PrintAsync()
        {
            // TODO: پیوند به سرویسِ واقعی — «چاپ فاکتور»
            await RunAsync(_ => Task.CompletedTask, "چاپ فاکتور…");
        }
        /// <summary>جستجو و انتخابِ «بیمار»</summary>
        public IAsyncRelayCommand LookupPatientIdCommand =>
            new AsyncRelayCommand(() => LookupAsync("بیمار"));

        /// <summary>جستجو و انتخابِ «پذیرش مرتبط»</summary>
        public IAsyncRelayCommand LookupReceptionIdCommand =>
            new AsyncRelayCommand(() => LookupAsync("پذیرش مرتبط"));

        /// <summary>
        /// اعتبارسنجیِ سمتِ کلاینت پیش از ذخیره
        /// </summary>
        protected override bool Validate()
        {
            var ok = true;

            if (string.IsNullOrWhiteSpace(PatientId))
            {
                AddError("بیمار الزامی است", nameof(PatientId));
            }
            else
            {
                ClearErrors(nameof(PatientId));
            }

            if (Date is null)
            {
                AddError("تاریخ الزامی است", nameof(Date));
            }
            else
            {
                ClearErrors(nameof(Date));
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