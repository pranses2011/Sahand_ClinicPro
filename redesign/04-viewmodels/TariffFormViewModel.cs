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
    /// ViewModel صفحه‌ی «تعرفه خدمات» — تعریف و ویرایش تعرفه‌ی خدمات
    /// </summary>
    /// <remarks>
    /// 🔗 وایرفریم مرجع:   <c>02-wireframes-md/TariffFormWindow.md</c>
    /// 🖼 وایرفریم گرافیکی: <c>01-wireframes-html/TariffFormWindow.html</c>
    /// 🎨 View متناظر:     <c>03-views/TariffFormWindow.xaml</c>
    ///
    /// این کلاس هیچ رشته‌ی متنیِ نمایشی ندارد؛ همه‌ی متن‌ها از
    /// <c>Strings.resx</c> و از طریق افزونه‌ی <c>{loc:Loc}</c> تأمین می‌شوند.
    /// </remarks>
    public class TariffFormViewModel : FormViewModelBase
    {
        public TariffFormViewModel(ILogger? logger = null) : base(logger)
        {
            _ = LoadAsync();
        }

        // ══════════════════════════════════════════════════════════════
        //  ویژگی‌ها
        // ══════════════════════════════════════════════════════════════


        private Guid? _serviceId = null;

        /// <summary>خدمت (الزامی)</summary>
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

        private decimal _basePrice = 0m;

        /// <summary>قیمت پایه (الزامی)</summary>
        public decimal BasePrice
        {
            get => _basePrice;
            set
            {
                if (SetProperty(ref _basePrice, value))
                {
                    ClearErrors();
                    MarkDirty();
                }
            }
        }

        private decimal _insurancePrice = 0m;

        /// <summary>قیمت بیمه</summary>
        public decimal InsurancePrice
        {
            get => _insurancePrice;
            set
            {
                if (SetProperty(ref _insurancePrice, value))
                {
                    ClearErrors();
                    MarkDirty();
                }
            }
        }

        private decimal _cashPrice = 0m;

        /// <summary>قیمت نقدی</summary>
        public decimal CashPrice
        {
            get => _cashPrice;
            set
            {
                if (SetProperty(ref _cashPrice, value))
                {
                    ClearErrors();
                    MarkDirty();
                }
            }
        }

        private DateTime? _effectiveDate = null;

        /// <summary>تاریخ اجرا (الزامی)</summary>
        public DateTime? EffectiveDate
        {
            get => _effectiveDate;
            set
            {
                if (SetProperty(ref _effectiveDate, value))
                {
                    ClearErrors();
                    MarkDirty();
                }
            }
        }

        private bool _isActive = false;

        /// <summary>فعال</summary>
        public bool IsActive
        {
            get => _isActive;
            set
            {
                if (SetProperty(ref _isActive, value))
                {
                    ClearErrors();
                    MarkDirty();
                }
            }
        }


        // ══════════════════════════════════════════════════════════════
        //  فرمان‌ها
        // ══════════════════════════════════════════════════════════════


        // ══════════════════════════════════════════════════════════════
        //  رفتار
        // ══════════════════════════════════════════════════════════════

        /// <inheritdoc />
        protected override bool Validate()
        {
            ClearAllErrors();
            if (ServiceId is null)
                AddError("خدمت الزامی است", nameof(ServiceId));
            if (BasePrice <= 0)
                AddError("قیمت پایه باید بزرگ‌تر از صفر باشد", nameof(BasePrice));
            if (EffectiveDate is null)
                AddError("تاریخ اجرا الزامی است", nameof(EffectiveDate));

            return !HasErrors;
        }

        /// <inheritdoc />
        protected override async Task PersistAsync(CancellationToken ct)
        {
            // TODO: ارسال داده‌ها به سرور؛ نمونه:
            //   var payload = new { ... };
            //   await _apiClient.PostAsync<TariffFormDto>("api/v1/<built-in method lower of str object at 0x7f5384a94ef0>s", payload, ct);

            await Task.Delay(200, ct);
        }

        // ══════════════════════════════════════════════════════════════
        //  اقدام‌ها
        // ══════════════════════════════════════════════════════════════


    }
}
