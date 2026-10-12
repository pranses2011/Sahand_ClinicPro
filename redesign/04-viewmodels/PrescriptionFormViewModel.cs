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
    /// ViewModel صفحه‌ی «فرم نسخه» — صدور نسخه با بررسی تداخل دارویی
    /// </summary>
    /// <remarks>
    /// 🔗 وایرفریم مرجع:   <c>02-wireframes-md/PrescriptionFormWindow.md</c>
    /// 🖼 وایرفریم گرافیکی: <c>01-wireframes-html/PrescriptionFormWindow.html</c>
    /// 🎨 View متناظر:     <c>03-views/PrescriptionFormWindow.xaml</c>
    ///
    /// این کلاس هیچ رشته‌ی متنیِ نمایشی ندارد؛ همه‌ی متن‌ها از
    /// <c>Strings.resx</c> و از طریق افزونه‌ی <c>{loc:Loc}</c> تأمین می‌شوند.
    /// </remarks>
    public class PrescriptionFormViewModel : FormViewModelBase
    {
        public PrescriptionFormViewModel(ILogger? logger = null) : base(logger)
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

        private Guid? _visitId = null;

        /// <summary>ویزیت مرتبط</summary>
        public Guid? VisitId
        {
            get => _visitId;
            set
            {
                if (SetProperty(ref _visitId, value))
                {
                    ClearErrors();
                    MarkDirty();
                }
            }
        }

        private string _diagnosis = string.Empty;

        /// <summary>تشخیص</summary>
        public string Diagnosis
        {
            get => _diagnosis;
            set
            {
                if (SetProperty(ref _diagnosis, value))
                {
                    ClearErrors();
                    MarkDirty();
                }
            }
        }

        private Guid? _drugId = null;

        /// <summary>دارو (الزامی)</summary>
        public Guid? DrugId
        {
            get => _drugId;
            set
            {
                if (SetProperty(ref _drugId, value))
                {
                    ClearErrors();
                    MarkDirty();
                }
            }
        }

        private string _dosage = string.Empty;

        /// <summary>دوز</summary>
        public string Dosage
        {
            get => _dosage;
            set
            {
                if (SetProperty(ref _dosage, value))
                {
                    ClearErrors();
                    MarkDirty();
                }
            }
        }

        private string _frequency = string.Empty;

        /// <summary>دفعات مصرف</summary>
        public string Frequency
        {
            get => _frequency;
            set
            {
                if (SetProperty(ref _frequency, value))
                {
                    ClearErrors();
                    MarkDirty();
                }
            }
        }

        private decimal _durationDays = 0m;

        /// <summary>تعداد روز</summary>
        public decimal DurationDays
        {
            get => _durationDays;
            set
            {
                if (SetProperty(ref _durationDays, value))
                {
                    ClearErrors();
                    MarkDirty();
                }
            }
        }

        private decimal _quantity = 0m;

        /// <summary>تعداد</summary>
        public decimal Quantity
        {
            get => _quantity;
            set
            {
                if (SetProperty(ref _quantity, value))
                {
                    ClearErrors();
                    MarkDirty();
                }
            }
        }

        private string _instructions = string.Empty;

        /// <summary>دستور مصرف</summary>
        public string Instructions
        {
            get => _instructions;
            set
            {
                if (SetProperty(ref _instructions, value))
                {
                    ClearErrors();
                    MarkDirty();
                }
            }
        }

        private bool _interactionWarnings = false;

        /// <summary>هشدار تداخل</summary>
        public bool InteractionWarnings
        {
            get => _interactionWarnings;
            set
            {
                if (SetProperty(ref _interactionWarnings, value))
                {
                    ClearErrors();
                    MarkDirty();
                }
            }
        }

        private bool _allergyWarnings = false;

        /// <summary>هشدار حساسیت</summary>
        public bool AllergyWarnings
        {
            get => _allergyWarnings;
            set
            {
                if (SetProperty(ref _allergyWarnings, value))
                {
                    ClearErrors();
                    MarkDirty();
                }
            }
        }

        private string _overrideReason = string.Empty;

        /// <summary>علت نادیده‌گرفتن هشدار</summary>
        public string OverrideReason
        {
            get => _overrideReason;
            set
            {
                if (SetProperty(ref _overrideReason, value))
                {
                    ClearErrors();
                    MarkDirty();
                }
            }
        }


        // ══════════════════════════════════════════════════════════════
        //  فرمان‌ها
        // ══════════════════════════════════════════════════════════════

        private IAsyncRelayCommand? _saveAndPrintCommand;

        /// <summary>صدور و چاپ</summary>
        public IAsyncRelayCommand SaveAndPrintCommand =>
            _saveAndPrintCommand ??= new AsyncRelayCommand(SaveAndPrintAsync);

        // ══════════════════════════════════════════════════════════════
        //  رفتار
        // ══════════════════════════════════════════════════════════════

        /// <inheritdoc />
        protected override bool Validate()
        {
            ClearAllErrors();
            if (PatientId is null)
                AddError("بیمار الزامی است", nameof(PatientId));
            if (DrugId is null)
                AddError("دارو الزامی است", nameof(DrugId));

            return !HasErrors;
        }

        /// <inheritdoc />
        protected override async Task PersistAsync(CancellationToken ct)
        {
            // TODO: ارسال داده‌ها به سرور؛ نمونه:
            //   var payload = new { ... };
            //   await _apiClient.PostAsync<PrescriptionFormDto>("api/v1/<built-in method lower of str object at 0x7f5384c8b6e0>s", payload, ct);

            await Task.Delay(200, ct);
        }

        // ══════════════════════════════════════════════════════════════
        //  اقدام‌ها
        // ══════════════════════════════════════════════════════════════

        /// <summary>
        /// صدور و چاپ
        /// </summary>
        private async Task saveAndPrintAsync()
        {
            var ok = await RunAsync(async ct =>
            {
                await Task.Delay(150, ct);   // TODO: فراخوانی سرویس واقعی
            }, "صدور و چاپ");

            if (ok) ShowSuccess("عملیات با موفقیت انجام شد");
        }

    }
}
