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
    /// ViewModel صفحه‌ی «ویرایش حقوق» — تنظیم حقوق و مزایای کارمند
    /// </summary>
    /// <remarks>
    /// 🔗 وایرفریم مرجع:   <c>02-wireframes-md/EmployeeSalaryEditWindow.md</c>
    /// 🖼 وایرفریم گرافیکی: <c>01-wireframes-html/EmployeeSalaryEditWindow.html</c>
    /// 🎨 View متناظر:     <c>03-views/EmployeeSalaryEditWindow.xaml</c>
    ///
    /// این کلاس هیچ رشته‌ی متنیِ نمایشی ندارد؛ همه‌ی متن‌ها از
    /// <c>Strings.resx</c> و از طریق افزونه‌ی <c>{loc:Loc}</c> تأمین می‌شوند.
    /// </remarks>
    public class EmployeeSalaryEditViewModel : FormViewModelBase
    {
        public EmployeeSalaryEditViewModel(ILogger? logger = null) : base(logger)
        {
            _ = LoadAsync();
        }

        // ══════════════════════════════════════════════════════════════
        //  ویژگی‌ها
        // ══════════════════════════════════════════════════════════════


        private decimal _baseSalary = 0m;

        /// <summary>حقوق پایه (الزامی)</summary>
        public decimal BaseSalary
        {
            get => _baseSalary;
            set
            {
                if (SetProperty(ref _baseSalary, value))
                {
                    ClearErrors();
                    MarkDirty();
                }
            }
        }

        private decimal _housingAllowance = 0m;

        /// <summary>حق مسکن</summary>
        public decimal HousingAllowance
        {
            get => _housingAllowance;
            set
            {
                if (SetProperty(ref _housingAllowance, value))
                {
                    ClearErrors();
                    MarkDirty();
                }
            }
        }

        private decimal _familyAllowance = 0m;

        /// <summary>حق عائله‌مندی</summary>
        public decimal FamilyAllowance
        {
            get => _familyAllowance;
            set
            {
                if (SetProperty(ref _familyAllowance, value))
                {
                    ClearErrors();
                    MarkDirty();
                }
            }
        }

        private decimal _transportAllowance = 0m;

        /// <summary>حق ایاب‌وذهاب</summary>
        public decimal TransportAllowance
        {
            get => _transportAllowance;
            set
            {
                if (SetProperty(ref _transportAllowance, value))
                {
                    ClearErrors();
                    MarkDirty();
                }
            }
        }

        private decimal _otherAllowance = 0m;

        /// <summary>سایر مزایا</summary>
        public decimal OtherAllowance
        {
            get => _otherAllowance;
            set
            {
                if (SetProperty(ref _otherAllowance, value))
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
            if (BaseSalary <= 0)
                AddError("حقوق پایه باید بزرگ‌تر از صفر باشد", nameof(BaseSalary));
            if (EffectiveDate is null)
                AddError("تاریخ اجرا الزامی است", nameof(EffectiveDate));

            return !HasErrors;
        }

        /// <inheritdoc />
        protected override async Task PersistAsync(CancellationToken ct)
        {
            // TODO: ارسال داده‌ها به سرور؛ نمونه:
            //   var payload = new { ... };
            //   await _apiClient.PostAsync<EmployeeSalaryEditDto>("api/v1/<built-in method lower of str object at 0x7f5384c8b6e0>s", payload, ct);

            await Task.Delay(200, ct);
        }

        // ══════════════════════════════════════════════════════════════
        //  اقدام‌ها
        // ══════════════════════════════════════════════════════════════


    }
}
