// ═══════════════════════════════════════════════════════════════════════
//  LabOrderFormViewModel
//  صفحه‌ی «درخواست آزمایش» · گروه «آزمایشگاه» · کهن‌الگوی «form-std»
//
//  متناظر با:  03-views/LabOrderFormWindow.xaml
//  وایرفریم:    01-wireframes-html/LabOrderFormWindow.html
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
    /// ViewModelِ صفحه‌ی «درخواست آزمایش»
    /// </summary>
    /// <remarks>
    /// کهن‌الگوی طراحی: <c>form-std</c> — چیدمان متناسب با همین قالب در View پیاده شده است.
    /// گروه: آزمایشگاه
    /// </remarks>
    public sealed class LabOrderFormViewModel : FormViewModelBase
    {
        /// <summary>ایجاد ViewModel و بارگذاریِ دادهٔ نمونه</summary>
        /// <param name="logger">ثبت‌کننده‌ی رویدادها (اختیاری)</param>
        public LabOrderFormViewModel(ILogger? logger = null) : base(logger)
        {
            // ارتباطات
            Relations.Add(new RelationItem("درخواست آزمایش", "آزمایشگاه", "در فهرست درخواست‌ها"));
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

        /// <summary>پزشک تجویزکننده — الزامی</summary>
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

        /// <summary>اولویت</summary>
        public string Priority
        {
            get => _priority;
            set
            {
                if (SetProperty(ref _priority, value))
                {
                    MarkDirty();
                }
            }
        }
        private string _priority = "عادی";

        /// <summary>آزمایشگاه</summary>
        public string Lab
        {
            get => _lab;
            set
            {
                if (SetProperty(ref _lab, value))
                {
                    MarkDirty();
                }
            }
        }
        private string _lab = "آزمایشگاه مرکزی";

        /// <summary>ناشتا</summary>
        public bool Fasting
        {
            get => _fasting;
            set
            {
                if (SetProperty(ref _fasting, value))
                {
                    MarkDirty();
                }
            }
        }
        private bool _fasting = true;

        /// <summary>آزمایش‌ها — الزامی</summary>
        public string Tests
        {
            get => _tests;
            set
            {
                if (SetProperty(ref _tests, value))
                {
                    MarkDirty();
                    Validate();
                }
            }
        }
        private string _tests = "CBC، قند ناشتا";

        /// <summary>توضیحات</summary>
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
        private string _notes = "";

        // ══════════════════════════════════════════════════════
        //  فرمان‌ها
        // ══════════════════════════════════════════════════════

        /// <summary>چاپ برچسب</summary>
        public IAsyncRelayCommand PrintCommand =>
            new AsyncRelayCommand(PrintAsync);

        /// <summary>اجرای «چاپ برچسب»</summary>
        private async Task PrintAsync()
        {
            // TODO: پیوند به سرویسِ واقعی — «چاپ برچسب»
            await RunAsync(_ => Task.CompletedTask, "چاپ برچسب…");
        }
        /// <summary>جستجو و انتخابِ «بیمار»</summary>
        public IAsyncRelayCommand LookupPatientIdCommand =>
            new AsyncRelayCommand(() => LookupAsync("بیمار"));

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
                AddError("پزشک تجویزکننده الزامی است", nameof(DoctorId));
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

            if (string.IsNullOrWhiteSpace(Tests))
            {
                AddError("آزمایش‌ها الزامی است", nameof(Tests));
            }
            else
            {
                ClearErrors(nameof(Tests));
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