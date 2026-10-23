using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Sahand.ClinicPro.Desktop.Base;
using Sahand.ClinicPro.Desktop.Localization;
using Sahand.ClinicPro.Desktop.Themes;
namespace Sahand.ClinicPro.Desktop.ViewModels.Redesign
{
    /// <summary>
    /// ViewModel صفحه‌ی «فرم پذیرش» — ثبت پذیرش جدید
    /// </summary>
    /// <remarks>
    /// 🔗 وایرفریم مرجع:   <c>02-wireframes-md/ReceptionFormWindow.md</c>
    /// 🖼 وایرفریم گرافیکی: <c>01-wireframes-html/ReceptionFormWindow.html</c>
    /// 🎨 View متناظر:     <c>03-views/ReceptionFormWindow.xaml</c>
    ///
    /// این کلاس هیچ رشته‌ی متنیِ نمایشی ندارد؛ همه‌ی متن‌ها از
    /// <c>Strings.resx</c> و از طریق افزونه‌ی <c>{loc:Loc}</c> تأمین می‌شوند.
    /// </remarks>
    public class ReceptionFormViewModel : FormViewModelBase
    {
        public ReceptionFormViewModel(ILogger? logger = null) : base(logger)
        {
            _ = LoadAsync();
        }

        // ══════════════════════════════════════════════════════════════
        //  ویژگی‌ها
        // ══════════════════════════════════════════════════════════════


        private Guid? _patientId = null;

        /// <summary>بیمار (الزامی)</summary>
        public Guid? PatientId
        {
            get => _patientId;
            set
            {
                if (SetProperty(ref _patientId, value))
                {
                    ClearErrors();
                    MarkDirty();
                }
            }
        }

        private Guid? _doctorId = null;

        /// <summary>پزشک (الزامی)</summary>
        public Guid? DoctorId
        {
            get => _doctorId;
            set
            {
                if (SetProperty(ref _doctorId, value))
                {
                    ClearErrors();
                    MarkDirty();
                }
            }
        }

        private Guid? _serviceId = null;

        /// <summary>خدمت اصلی (الزامی)</summary>
        public Guid? ServiceId
        {
            get => _serviceId;
            set
            {
                if (SetProperty(ref _serviceId, value))
                {
                    ClearErrors();
                    MarkDirty();
                }
            }
        }

        private Guid? _additionalServices = null;

        /// <summary>خدمات تکمیلی</summary>
        public Guid? AdditionalServices
        {
            get => _additionalServices;
            set
            {
                if (SetProperty(ref _additionalServices, value))
                {
                    ClearErrors();
                    MarkDirty();
                }
            }
        }

        private Guid? _insuranceId = null;

        /// <summary>بیمه</summary>
        public Guid? InsuranceId
        {
            get => _insuranceId;
            set
            {
                if (SetProperty(ref _insuranceId, value))
                {
                    ClearErrors();
                    MarkDirty();
                }
            }
        }

        private string _referralNumber = string.Empty;

        /// <summary>شماره ارجاع</summary>
        public string ReferralNumber
        {
            get => _referralNumber;
            set
            {
                if (SetProperty(ref _referralNumber, value))
                {
                    ClearErrors();
                    MarkDirty();
                }
            }
        }

        private decimal _totalAmount = 0m;

        /// <summary>مبلغ کل</summary>
        public decimal TotalAmount
        {
            get => _totalAmount;
            set
            {
                if (SetProperty(ref _totalAmount, value))
                {
                    ClearErrors();
                    MarkDirty();
                }
            }
        }

        private decimal _insuranceShare = 0m;

        /// <summary>سهم بیمه</summary>
        public decimal InsuranceShare
        {
            get => _insuranceShare;
            set
            {
                if (SetProperty(ref _insuranceShare, value))
                {
                    ClearErrors();
                    MarkDirty();
                }
            }
        }

        private decimal _patientShare = 0m;

        /// <summary>سهم بیمار</summary>
        public decimal PatientShare
        {
            get => _patientShare;
            set
            {
                if (SetProperty(ref _patientShare, value))
                {
                    ClearErrors();
                    MarkDirty();
                }
            }
        }

        private decimal _discountAmount = 0m;

        /// <summary>تخفیف</summary>
        public decimal DiscountAmount
        {
            get => _discountAmount;
            set
            {
                if (SetProperty(ref _discountAmount, value))
                {
                    ClearErrors();
                    MarkDirty();
                }
            }
        }


        // ══════════════════════════════════════════════════════════════
        //  فرمان‌ها
        // ══════════════════════════════════════════════════════════════

        private IAsyncRelayCommand? _saveAndPayCommand;

        /// <summary>ثبت و دریافت</summary>
        public IAsyncRelayCommand SaveAndPayCommand =>
            _saveAndPayCommand ??= new AsyncRelayCommand(SaveAndPayAsync);

        // ══════════════════════════════════════════════════════════════
        //  رفتار
        // ══════════════════════════════════════════════════════════════

        /// <inheritdoc />
        protected override bool Validate()
        {
            ClearAllErrors();
            if (PatientId is null)
                AddError("بیمار الزامی است", nameof(PatientId));
            if (DoctorId is null)
                AddError("پزشک الزامی است", nameof(DoctorId));
            if (ServiceId is null)
                AddError("خدمت اصلی الزامی است", nameof(ServiceId));

            return !HasErrors;
        }

        /// <inheritdoc />
        protected override async Task PersistAsync(CancellationToken ct)
        {
            // TODO: ارسال داده‌ها به سرور؛ نمونه:
            //   var payload = new { ... };
            //   await _apiClient.PostAsync<ReceptionFormDto>("api/v1/<built-in method lower of str object at 0x7f5384a951b0>s", payload, ct);

            await Task.Delay(200, ct);
        }

        // ══════════════════════════════════════════════════════════════
        //  اقدام‌ها
        // ══════════════════════════════════════════════════════════════

        /// <summary>
        /// ثبت و دریافت
        /// </summary>
        private async Task saveAndPayAsync()
        {
            var ok = await RunAsync(async ct =>
            {
                await Task.Delay(150, ct);   // TODO: فراخوانی سرویس واقعی
            }, "ثبت و دریافت");

            if (ok) ShowSuccess("عملیات با موفقیت انجام شد");
        }

    }
}
