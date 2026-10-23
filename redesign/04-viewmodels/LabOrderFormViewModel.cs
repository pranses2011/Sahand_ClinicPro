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
    /// ViewModel صفحه‌ی «درخواست آزمایش» — ثبت درخواست آزمایش برای بیمار
    /// </summary>
    /// <remarks>
    /// 🔗 وایرفریم مرجع:   <c>02-wireframes-md/LabOrderFormWindow.md</c>
    /// 🖼 وایرفریم گرافیکی: <c>01-wireframes-html/LabOrderFormWindow.html</c>
    /// 🎨 View متناظر:     <c>03-views/LabOrderFormWindow.xaml</c>
    ///
    /// این کلاس هیچ رشته‌ی متنیِ نمایشی ندارد؛ همه‌ی متن‌ها از
    /// <c>Strings.resx</c> و از طریق افزونه‌ی <c>{loc:Loc}</c> تأمین می‌شوند.
    /// </remarks>
    public class LabOrderFormViewModel : FormViewModelBase
    {
        public LabOrderFormViewModel(ILogger? logger = null) : base(logger)
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

        private Guid? _requestingDoctorId = null;

        /// <summary>پزشک درخواست‌کننده (الزامی)</summary>
        public Guid? RequestingDoctorId
        {
            get => _requestingDoctorId;
            set
            {
                if (SetProperty(ref _requestingDoctorId, value))
                {
                    ClearErrors();
                    MarkDirty();
                }
            }
        }

        private Guid? _testIds = null;

        /// <summary>آزمایش‌های درخواستی (الزامی)</summary>
        public Guid? TestIds
        {
            get => _testIds;
            set
            {
                if (SetProperty(ref _testIds, value))
                {
                    ClearErrors();
                    MarkDirty();
                }
            }
        }

        private string _priority = string.Empty;

        /// <summary>اولویت</summary>
        public string Priority
        {
            get => _priority;
            set
            {
                if (SetProperty(ref _priority, value))
                {
                    ClearErrors();
                    MarkDirty();
                }
            }
        }

        private bool _fastingRequired = false;

        /// <summary>نیاز به ناشتایی</summary>
        public bool FastingRequired
        {
            get => _fastingRequired;
            set
            {
                if (SetProperty(ref _fastingRequired, value))
                {
                    ClearErrors();
                    MarkDirty();
                }
            }
        }

        private string _sampleType = string.Empty;

        /// <summary>نوع نمونه</summary>
        public string SampleType
        {
            get => _sampleType;
            set
            {
                if (SetProperty(ref _sampleType, value))
                {
                    ClearErrors();
                    MarkDirty();
                }
            }
        }

        private DateTime? _collectionDate = null;

        /// <summary>تاریخ نمونه‌گیری</summary>
        public DateTime? CollectionDate
        {
            get => _collectionDate;
            set
            {
                if (SetProperty(ref _collectionDate, value))
                {
                    ClearErrors();
                    MarkDirty();
                }
            }
        }

        private string _clinicalNotes = string.Empty;

        /// <summary>توضیحات بالینی</summary>
        public string ClinicalNotes
        {
            get => _clinicalNotes;
            set
            {
                if (SetProperty(ref _clinicalNotes, value))
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

        /// <summary>ثبت و چاپ برچسب</summary>
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
            if (RequestingDoctorId is null)
                AddError("پزشک درخواست‌کننده الزامی است", nameof(RequestingDoctorId));
            if (TestIds is null)
                AddError("آزمایش‌های درخواستی الزامی است", nameof(TestIds));

            return !HasErrors;
        }

        /// <inheritdoc />
        protected override async Task PersistAsync(CancellationToken ct)
        {
            // TODO: ارسال داده‌ها به سرور؛ نمونه:
            //   var payload = new { ... };
            //   await _apiClient.PostAsync<LabOrderFormDto>("api/v1/<built-in method lower of str object at 0x7f5384a94cf0>s", payload, ct);

            await Task.Delay(200, ct);
        }

        // ══════════════════════════════════════════════════════════════
        //  اقدام‌ها
        // ══════════════════════════════════════════════════════════════

        /// <summary>
        /// ثبت و چاپ برچسب
        /// </summary>
        private async Task saveAndPrintAsync()
        {
            var ok = await RunAsync(async ct =>
            {
                await Task.Delay(150, ct);   // TODO: فراخوانی سرویس واقعی
            }, "ثبت و چاپ برچسب");

            if (ok) ShowSuccess("عملیات با موفقیت انجام شد");
        }

    }
}
