// ═══════════════════════════════════════════════════════════════════════
//  PrescriptionFormViewModel
//  صفحه‌ی «فرم نسخه» · گروه «نسخه» · کهن‌الگوی «form-lines»
//
//  متناظر با:  03-views/PrescriptionFormWindow.xaml
//  وایرفریم:    01-wireframes-html/PrescriptionFormWindow.html
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
    /// ViewModelِ صفحه‌ی «فرم نسخه»
    /// </summary>
    /// <remarks>
    /// جدول اقلام با ستون‌های دارو، دوز، تعداد دفعات، مدت و تعداد؛ سبدِ
    /// خلاصه در ستون چپ با جمع مبالغ.
    ///
    /// کهن‌الگوی طراحی: <c>form-lines</c> — چیدمان متناسب با همین قالب در View پیاده شده است.
    /// گروه: نسخه
    /// </remarks>
    public sealed class PrescriptionFormViewModel : FormViewModelBase
    {
        /// <summary>ایجاد ViewModel و بارگذاریِ دادهٔ نمونه</summary>
        /// <param name="logger">ثبت‌کننده‌ی رویدادها (اختیاری)</param>
        public PrescriptionFormViewModel(ILogger? logger = null) : base(logger)
        {
            // ارتباطات
            Relations.Add(new RelationItem("فرم نسخه", "داروخانه", "موجودی دارو بررسی می‌شود"));
            Relations.Add(new RelationItem("فرم نسخه", "نسخه الکترونیک", "ارسال به سامانه کشوری"));
            Relations.Add(new RelationItem("فرم نسخه", "پرونده‌ی بیمار", "حساسیت‌ها در تداخل لحاظ می‌شود"));
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

        /// <summary>پزشک — الزامی</summary>
        public string DoctorId
        {
            get => _doctorId;
            set
            {
                if (SetProperty(ref _doctorId, value))
                {
                    MarkDirty();
                    Validate();
                }
            }
        }
        private string _doctorId = "دکتر موسوی";

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

        /// <summary>تشخیص</summary>
        public string Diagnosis
        {
            get => _diagnosis;
            set
            {
                if (SetProperty(ref _diagnosis, value))
                {
                    MarkDirty();
                }
            }
        }
        private string _diagnosis = "K21.9 — رفلاکس معده";

        /// <summary>توصیه‌ها</summary>
        public string Notes
        {
            get => _notes;
            set
            {
                if (SetProperty(ref _notes, value))
                {
                    MarkDirty();
                }
            }
        }
        private string _notes = "یک ساعت پس از غذا مصرف شود";

        // ══════════════════════════════════════════════════════
        //  فرمان‌ها
        // ══════════════════════════════════════════════════════

        /// <summary>بررسی تداخل</summary>
        public IAsyncRelayCommand CheckCommand =>
            new AsyncRelayCommand(CheckAsync);

        /// <summary>اجرای «بررسی تداخل»</summary>
        private async Task CheckAsync()
        {
            // TODO: پیوند به سرویسِ واقعی — «بررسی تداخل»
            await RunAsync(_ => Task.CompletedTask, "بررسی تداخل…");
        }

        /// <summary>ارسال الکترونیک</summary>
        public IAsyncRelayCommand SendCommand =>
            new AsyncRelayCommand(SendAsync);

        /// <summary>اجرای «ارسال الکترونیک»</summary>
        private async Task SendAsync()
        {
            // TODO: پیوند به سرویسِ واقعی — «ارسال الکترونیک»
            await RunAsync(_ => Task.CompletedTask, "ارسال الکترونیک…");
        }
        /// <summary>جستجو و انتخابِ «بیمار»</summary>
        public IAsyncRelayCommand LookupPatientIdCommand =>
            new AsyncRelayCommand(() => LookupAsync("بیمار"));

        /// <summary>جستجو و انتخابِ «تشخیص»</summary>
        public IAsyncRelayCommand LookupDiagnosisCommand =>
            new AsyncRelayCommand(() => LookupAsync("تشخیص"));

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

            if (string.IsNullOrWhiteSpace(DoctorId))
            {
                AddError("پزشک الزامی است", nameof(DoctorId));
            }
            else
            {
                ClearErrors(nameof(DoctorId));
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