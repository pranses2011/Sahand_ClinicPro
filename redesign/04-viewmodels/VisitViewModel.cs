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
    /// ViewModel صفحه‌ی «ویزیت (SOAP)» — ثبت شرح‌حال، معاینه، تشخیص و طرح درمان
    /// </summary>
    /// <remarks>
    /// 🔗 وایرفریم مرجع:   <c>02-wireframes-md/VisitWindow.md</c>
    /// 🖼 وایرفریم گرافیکی: <c>01-wireframes-html/VisitWindow.html</c>
    /// 🎨 View متناظر:     <c>03-views/VisitWindow.xaml</c>
    ///
    /// این کلاس هیچ رشته‌ی متنیِ نمایشی ندارد؛ همه‌ی متن‌ها از
    /// <c>Strings.resx</c> و از طریق افزونه‌ی <c>{loc:Loc}</c> تأمین می‌شوند.
    /// </remarks>
    public class VisitViewModel : FormViewModelBase
    {
        public VisitViewModel(ILogger? logger = null) : base(logger)
        {
            _ = LoadAsync();
        }

        // ══════════════════════════════════════════════════════════════
        //  ویژگی‌ها
        // ══════════════════════════════════════════════════════════════


        private string _chiefComplaint = string.Empty;

        /// <summary>شکایت اصلی (الزامی)</summary>
        public string ChiefComplaint
        {
            get => _chiefComplaint;
            set
            {
                if (SetProperty(ref _chiefComplaint, value))
                {
                    ClearErrors();
                    MarkDirty();
                }
            }
        }

        private string _historyOfPresentIllness = string.Empty;

        /// <summary>شرح بیماری فعلی</summary>
        public string HistoryOfPresentIllness
        {
            get => _historyOfPresentIllness;
            set
            {
                if (SetProperty(ref _historyOfPresentIllness, value))
                {
                    ClearErrors();
                    MarkDirty();
                }
            }
        }

        private string _pastMedicalHistory = string.Empty;

        /// <summary>سوابق پزشکی</summary>
        public string PastMedicalHistory
        {
            get => _pastMedicalHistory;
            set
            {
                if (SetProperty(ref _pastMedicalHistory, value))
                {
                    ClearErrors();
                    MarkDirty();
                }
            }
        }

        private string _drugHistory = string.Empty;

        /// <summary>سوابق دارویی</summary>
        public string DrugHistory
        {
            get => _drugHistory;
            set
            {
                if (SetProperty(ref _drugHistory, value))
                {
                    ClearErrors();
                    MarkDirty();
                }
            }
        }

        private string _familyHistory = string.Empty;

        /// <summary>سوابق خانوادگی</summary>
        public string FamilyHistory
        {
            get => _familyHistory;
            set
            {
                if (SetProperty(ref _familyHistory, value))
                {
                    ClearErrors();
                    MarkDirty();
                }
            }
        }

        private string _socialHistory = string.Empty;

        /// <summary>سوابق اجتماعی</summary>
        public string SocialHistory
        {
            get => _socialHistory;
            set
            {
                if (SetProperty(ref _socialHistory, value))
                {
                    ClearErrors();
                    MarkDirty();
                }
            }
        }

        private string _bloodPressure = string.Empty;

        /// <summary>فشار خون</summary>
        public string BloodPressure
        {
            get => _bloodPressure;
            set
            {
                if (SetProperty(ref _bloodPressure, value))
                {
                    ClearErrors();
                    MarkDirty();
                }
            }
        }

        private decimal _pulse = 0m;

        /// <summary>نبض</summary>
        public decimal Pulse
        {
            get => _pulse;
            set
            {
                if (SetProperty(ref _pulse, value))
                {
                    ClearErrors();
                    MarkDirty();
                }
            }
        }

        private decimal _temperature = 0m;

        /// <summary>دما</summary>
        public decimal Temperature
        {
            get => _temperature;
            set
            {
                if (SetProperty(ref _temperature, value))
                {
                    ClearErrors();
                    MarkDirty();
                }
            }
        }

        private decimal _respiratoryRate = 0m;

        /// <summary>تعداد تنفس</summary>
        public decimal RespiratoryRate
        {
            get => _respiratoryRate;
            set
            {
                if (SetProperty(ref _respiratoryRate, value))
                {
                    ClearErrors();
                    MarkDirty();
                }
            }
        }

        private decimal _oxygenSaturation = 0m;

        /// <summary>اشباع اکسیژن</summary>
        public decimal OxygenSaturation
        {
            get => _oxygenSaturation;
            set
            {
                if (SetProperty(ref _oxygenSaturation, value))
                {
                    ClearErrors();
                    MarkDirty();
                }
            }
        }

        private decimal _weight = 0m;

        /// <summary>وزن (کیلوگرم)</summary>
        public decimal Weight
        {
            get => _weight;
            set
            {
                if (SetProperty(ref _weight, value))
                {
                    ClearErrors();
                    MarkDirty();
                }
            }
        }

        private decimal _height = 0m;

        /// <summary>قد (سانتی‌متر)</summary>
        public decimal Height
        {
            get => _height;
            set
            {
                if (SetProperty(ref _height, value))
                {
                    ClearErrors();
                    MarkDirty();
                }
            }
        }

        private string _bmi = string.Empty;

        /// <summary>BMI</summary>
        public string Bmi
        {
            get => _bmi;
            set
            {
                if (SetProperty(ref _bmi, value))
                {
                    ClearErrors();
                    MarkDirty();
                }
            }
        }

        private string _physicalExam = string.Empty;

        /// <summary>یافته‌های معاینه</summary>
        public string PhysicalExam
        {
            get => _physicalExam;
            set
            {
                if (SetProperty(ref _physicalExam, value))
                {
                    ClearErrors();
                    MarkDirty();
                }
            }
        }

        private Guid? _primaryDiagnosisCode = null;

        /// <summary>تشخیص اصلی (ICD-10) (الزامی)</summary>
        public Guid? PrimaryDiagnosisCode
        {
            get => _primaryDiagnosisCode;
            set
            {
                if (SetProperty(ref _primaryDiagnosisCode, value))
                {
                    ClearErrors();
                    MarkDirty();
                }
            }
        }

        private Guid? _secondaryDiagnoses = null;

        /// <summary>تشخیص‌های فرعی</summary>
        public Guid? SecondaryDiagnoses
        {
            get => _secondaryDiagnoses;
            set
            {
                if (SetProperty(ref _secondaryDiagnoses, value))
                {
                    ClearErrors();
                    MarkDirty();
                }
            }
        }

        private string _differentialDiagnosis = string.Empty;

        /// <summary>تشخیص‌های افتراقی</summary>
        public string DifferentialDiagnosis
        {
            get => _differentialDiagnosis;
            set
            {
                if (SetProperty(ref _differentialDiagnosis, value))
                {
                    ClearErrors();
                    MarkDirty();
                }
            }
        }

        private string _assessmentNotes = string.Empty;

        /// <summary>یادداشت ارزیابی</summary>
        public string AssessmentNotes
        {
            get => _assessmentNotes;
            set
            {
                if (SetProperty(ref _assessmentNotes, value))
                {
                    ClearErrors();
                    MarkDirty();
                }
            }
        }

        private string _treatmentPlan = string.Empty;

        /// <summary>طرح درمان (الزامی)</summary>
        public string TreatmentPlan
        {
            get => _treatmentPlan;
            set
            {
                if (SetProperty(ref _treatmentPlan, value))
                {
                    ClearErrors();
                    MarkDirty();
                }
            }
        }

        private string _prescription = string.Empty;

        /// <summary>داروها</summary>
        public string Prescription
        {
            get => _prescription;
            set
            {
                if (SetProperty(ref _prescription, value))
                {
                    ClearErrors();
                    MarkDirty();
                }
            }
        }

        private Guid? _labOrders = null;

        /// <summary>درخواست آزمایش</summary>
        public Guid? LabOrders
        {
            get => _labOrders;
            set
            {
                if (SetProperty(ref _labOrders, value))
                {
                    ClearErrors();
                    MarkDirty();
                }
            }
        }

        private Guid? _imagingOrders = null;

        /// <summary>درخواست تصویربرداری</summary>
        public Guid? ImagingOrders
        {
            get => _imagingOrders;
            set
            {
                if (SetProperty(ref _imagingOrders, value))
                {
                    ClearErrors();
                    MarkDirty();
                }
            }
        }

        private Guid? _referral = null;

        /// <summary>ارجاع</summary>
        public Guid? Referral
        {
            get => _referral;
            set
            {
                if (SetProperty(ref _referral, value))
                {
                    ClearErrors();
                    MarkDirty();
                }
            }
        }

        private DateTime? _followUpDate = null;

        /// <summary>تاریخ پیگیری</summary>
        public DateTime? FollowUpDate
        {
            get => _followUpDate;
            set
            {
                if (SetProperty(ref _followUpDate, value))
                {
                    ClearErrors();
                    MarkDirty();
                }
            }
        }

        private string _patientEducation = string.Empty;

        /// <summary>آموزش به بیمار</summary>
        public string PatientEducation
        {
            get => _patientEducation;
            set
            {
                if (SetProperty(ref _patientEducation, value))
                {
                    ClearErrors();
                    MarkDirty();
                }
            }
        }


        // ══════════════════════════════════════════════════════════════
        //  فرمان‌ها
        // ══════════════════════════════════════════════════════════════

        private IAsyncRelayCommand? _saveAndPrescribeCommand;

        /// <summary>ذخیره و نسخه</summary>
        public IAsyncRelayCommand SaveAndPrescribeCommand =>
            _saveAndPrescribeCommand ??= new AsyncRelayCommand(SaveAndPrescribeAsync);

        private IAsyncRelayCommand? _saveAndLabCommand;

        /// <summary>ذخیره و آزمایش</summary>
        public IAsyncRelayCommand SaveAndLabCommand =>
            _saveAndLabCommand ??= new AsyncRelayCommand(SaveAndLabAsync);

        private IAsyncRelayCommand? _cancelVisitCommand;

        /// <summary>لغو ویزیت</summary>
        public IAsyncRelayCommand CancelVisitCommand =>
            _cancelVisitCommand ??= new AsyncRelayCommand(CancelVisitAsync);

        // ══════════════════════════════════════════════════════════════
        //  رفتار
        // ══════════════════════════════════════════════════════════════

        /// <inheritdoc />
        protected override bool Validate()
        {
            ClearAllErrors();
            if (string.IsNullOrWhiteSpace(ChiefComplaint))
                AddError("شکایت اصلی الزامی است", nameof(ChiefComplaint));
            if (PrimaryDiagnosisCode is null)
                AddError("تشخیص اصلی (ICD-10) الزامی است", nameof(PrimaryDiagnosisCode));
            if (string.IsNullOrWhiteSpace(TreatmentPlan))
                AddError("طرح درمان الزامی است", nameof(TreatmentPlan));

            return !HasErrors;
        }

        /// <inheritdoc />
        protected override async Task PersistAsync(CancellationToken ct)
        {
            // TODO: ارسال داده‌ها به سرور؛ نمونه:
            //   var payload = new { ... };
            //   await _apiClient.PostAsync<VisitDto>("api/v1/<built-in method lower of str object at 0x7f5384a954b0>s", payload, ct);

            await Task.Delay(200, ct);
        }

        // ══════════════════════════════════════════════════════════════
        //  اقدام‌ها
        // ══════════════════════════════════════════════════════════════

        /// <summary>
        /// ذخیره و نسخه
        /// </summary>
        private async Task saveAndPrescribeAsync()
        {
            var ok = await RunAsync(async ct =>
            {
                await Task.Delay(150, ct);   // TODO: فراخوانی سرویس واقعی
            }, "ذخیره و نسخه");

            if (ok) ShowSuccess("عملیات با موفقیت انجام شد");
        }

        /// <summary>
        /// ذخیره و آزمایش
        /// </summary>
        private async Task saveAndLabAsync()
        {
            var ok = await RunAsync(async ct =>
            {
                await Task.Delay(150, ct);   // TODO: فراخوانی سرویس واقعی
            }, "ذخیره و آزمایش");

            if (ok) ShowSuccess("عملیات با موفقیت انجام شد");
        }

        /// <summary>
        /// لغو ویزیت
        /// </summary>
        private async Task cancelVisitAsync()
        {
            var ok = await RunAsync(async ct =>
            {
                await Task.Delay(150, ct);   // TODO: فراخوانی سرویس واقعی
            }, "لغو ویزیت");

            if (ok) ShowSuccess("عملیات با موفقیت انجام شد");
        }

    }
}
