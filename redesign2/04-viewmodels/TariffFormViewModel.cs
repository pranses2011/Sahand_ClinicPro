// ═══════════════════════════════════════════════════════════════════════
//  TariffFormViewModel
//  صفحه‌ی «تعرفه خدمات» · گروه «مالی» · کهن‌الگوی «form-std»
//
//  متناظر با:  03-views/TariffFormWindow.xaml
//  وایرفریم:    01-wireframes-html/TariffFormWindow.html
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
    /// ViewModelِ صفحه‌ی «تعرفه خدمات»
    /// </summary>
    /// <remarks>
    /// کهن‌الگوی طراحی: <c>form-std</c> — چیدمان متناسب با همین قالب در View پیاده شده است.
    /// گروه: مالی
    /// </remarks>
    public sealed class TariffFormViewModel : FormViewModelBase
    {
        /// <summary>ایجاد ViewModel و بارگذاریِ دادهٔ نمونه</summary>
        /// <param name="logger">ثبت‌کننده‌ی رویدادها (اختیاری)</param>
        public TariffFormViewModel(ILogger? logger = null) : base(logger)
        {
            // ارتباطات
            Relations.Add(new RelationItem("تعرفه خدمات", "صورتحساب", "قیمت‌گذاری فاکتور"));
            Relations.Add(new RelationItem("تعرفه خدمات", "فرم شرکت بیمه", "بیمهٔ مرتبط"));
        }

        /// <summary>ارتباطاتِ این صفحه با دیگر صفحات</summary>
        public ObservableCollection<RelationItem> Relations { get; } = new();

        // ══════════════════════════════════════════════════════
        //  ویژگی‌های فرم — مقدارِ آغازین برابرِ دادهٔ نمونه است
        // ══════════════════════════════════════════════════════

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

        /// <summary>بیمه — الزامی</summary>
        public string InsuranceId
        {
            get => _insuranceId;
            set
            {
                if (SetProperty(ref _insuranceId, value))
                {
                    MarkDirty();
                    Validate();
                }
            }
        }
        private string _insuranceId = "تأمین اجتماعی";

        /// <summary>سال — الزامی</summary>
        public string Year
        {
            get => _year;
            set
            {
                if (SetProperty(ref _year, value))
                {
                    MarkDirty();
                    Validate();
                }
            }
        }
        private string _year = "۱۴۰۴";

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

        /// <summary>سهم پزشک</summary>
        public decimal DoctorShare
        {
            get => _doctorShare;
            set
            {
                if (SetProperty(ref _doctorShare, value))
                {
                    MarkDirty();
                }
            }
        }
        private decimal _doctorShare = 125000m;

        /// <summary>فعال</summary>
        public bool Active
        {
            get => _active;
            set
            {
                if (SetProperty(ref _active, value))
                {
                    MarkDirty();
                }
            }
        }
        private bool _active = true;

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

        /// <summary>رونوشت از سال قبل</summary>
        public IAsyncRelayCommand CopyCommand =>
            new AsyncRelayCommand(CopyAsync);

        /// <summary>اجرای «رونوشت از سال قبل»</summary>
        private async Task CopyAsync()
        {
            // TODO: پیوند به سرویسِ واقعی — «رونوشت از سال قبل»
            await RunAsync(_ => Task.CompletedTask, "رونوشت از سال قبل…");
        }

        /// <summary>
        /// اعتبارسنجیِ سمتِ کلاینت پیش از ذخیره
        /// </summary>
        protected override bool Validate()
        {
            var ok = true;

            if (string.IsNullOrWhiteSpace(ServiceId))
            {
                AddError("خدمت الزامی است", nameof(ServiceId));
            }
            else
            {
                ClearErrors(nameof(ServiceId));
            }

            if (string.IsNullOrWhiteSpace(InsuranceId))
            {
                AddError("بیمه الزامی است", nameof(InsuranceId));
            }
            else
            {
                ClearErrors(nameof(InsuranceId));
            }

            if (string.IsNullOrWhiteSpace(Year))
            {
                AddError("سال الزامی است", nameof(Year));
            }
            else
            {
                ClearErrors(nameof(Year));
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