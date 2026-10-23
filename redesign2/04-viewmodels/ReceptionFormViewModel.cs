// ═══════════════════════════════════════════════════════════════════════
//  ReceptionFormViewModel
//  صفحه‌ی «فرم پذیرش» · گروه «پذیرش» · کهن‌الگوی «form-std»
//
//  متناظر با:  03-views/ReceptionFormWindow.xaml
//  وایرفریم:    01-wireframes-html/ReceptionFormWindow.html
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
    /// ViewModelِ صفحه‌ی «فرم پذیرش»
    /// </summary>
    /// <remarks>
    /// کهن‌الگوی طراحی: <c>form-std</c> — چیدمان متناسب با همین قالب در View پیاده شده است.
    /// گروه: پذیرش
    /// </remarks>
    public sealed class ReceptionFormViewModel : FormViewModelBase
    {
        /// <summary>ایجاد ViewModel و بارگذاریِ دادهٔ نمونه</summary>
        /// <param name="logger">ثبت‌کننده‌ی رویدادها (اختیاری)</param>
        public ReceptionFormViewModel(ILogger? logger = null) : base(logger)
        {
            // ارتباطات
            Relations.Add(new RelationItem("فرم پذیرش", "تعرفه خدمات", "قیمت از تعرفه"));
            Relations.Add(new RelationItem("فرم پذیرش", "بیمه و مطالبات", "درصد از قرارداد بیمه"));
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

        /// <summary>خدمت — الزامی</summary>
        public string ServiceId
        {
            get => _serviceId;
            set
            {
                if (SetProperty(ref _serviceId, value))
                {
                    MarkDirty();
                    Validate();
                }
            }
        }
        private string _serviceId = "ویزیت عمومی";

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

        /// <summary>نوع مراجعه</summary>
        public string VisitType
        {
            get => _visitType;
            set
            {
                if (SetProperty(ref _visitType, value))
                {
                    MarkDirty();
                }
            }
        }
        private string _visitType = "نوبت‌دار";

        /// <summary>اتاق</summary>
        public string RoomId
        {
            get => _roomId;
            set
            {
                if (SetProperty(ref _roomId, value))
                {
                    MarkDirty();
                }
            }
        }
        private string _roomId = "اتاق ۲";

        /// <summary>تعرفه پایه — الزامی</summary>
        public decimal BasePrice
        {
            get => _basePrice;
            set
            {
                if (SetProperty(ref _basePrice, value))
                {
                    MarkDirty();
                    Validate();
                }
            }
        }
        private decimal _basePrice = 250000m;

        /// <summary>سهم بیمه</summary>
        public decimal InsuranceShare
        {
            get => _insuranceShare;
            set
            {
                if (SetProperty(ref _insuranceShare, value))
                {
                    MarkDirty();
                }
            }
        }
        private decimal _insuranceShare = 175000m;

        /// <summary>سهم بیمار — الزامی</summary>
        public decimal PatientShare
        {
            get => _patientShare;
            set
            {
                if (SetProperty(ref _patientShare, value))
                {
                    MarkDirty();
                    Validate();
                }
            }
        }
        private decimal _patientShare = 75000m;

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

        /// <summary>ثبت و دریافت وجه</summary>
        public IAsyncRelayCommand SavePayCommand =>
            new AsyncRelayCommand(SavePayAsync);

        /// <summary>اجرای «ثبت و دریافت وجه»</summary>
        private async Task SavePayAsync()
        {
            // TODO: پیوند به سرویسِ واقعی — «ثبت و دریافت وجه»
            await RunAsync(_ => Task.CompletedTask, "ثبت و دریافت وجه…");
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
                AddError("پزشک الزامی است", nameof(DoctorId));
            }
            else
            {
                ClearErrors(nameof(DoctorId));
            }

            if (string.IsNullOrWhiteSpace(ServiceId))
            {
                AddError("خدمت الزامی است", nameof(ServiceId));
            }
            else
            {
                ClearErrors(nameof(ServiceId));
            }

            if (BasePrice <= 0)
            {
                AddError("تعرفه پایه الزامی است", nameof(BasePrice));
            }
            else
            {
                ClearErrors(nameof(BasePrice));
            }

            if (PatientShare <= 0)
            {
                AddError("سهم بیمار الزامی است", nameof(PatientShare));
            }
            else
            {
                ClearErrors(nameof(PatientShare));
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