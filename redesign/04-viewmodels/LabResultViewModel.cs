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
    /// ViewModel صفحه‌ی «ثبت نتیجه آزمایش» — ورود و تأیید نتایج آزمایش
    /// </summary>
    /// <remarks>
    /// 🔗 وایرفریم مرجع:   <c>02-wireframes-md/LabResultWindow.md</c>
    /// 🖼 وایرفریم گرافیکی: <c>01-wireframes-html/LabResultWindow.html</c>
    /// 🎨 View متناظر:     <c>03-views/LabResultWindow.xaml</c>
    ///
    /// این کلاس هیچ رشته‌ی متنیِ نمایشی ندارد؛ همه‌ی متن‌ها از
    /// <c>Strings.resx</c> و از طریق افزونه‌ی <c>{loc:Loc}</c> تأمین می‌شوند.
    /// </remarks>
    public class LabResultViewModel : FormViewModelBase
    {
        public LabResultViewModel(ILogger? logger = null) : base(logger)
        {
            _ = LoadAsync();
        }

        // ══════════════════════════════════════════════════════════════
        //  ویژگی‌ها
        // ══════════════════════════════════════════════════════════════


        private string _testName = string.Empty;

        /// <summary>آزمایش</summary>
        public string TestName
        {
            get => _testName;
            set
            {
                if (SetProperty(ref _testName, value))
                {
                    ClearErrors();
                    MarkDirty();
                }
            }
        }

        private string _resultValue = string.Empty;

        /// <summary>مقدار نتیجه (الزامی)</summary>
        public string ResultValue
        {
            get => _resultValue;
            set
            {
                if (SetProperty(ref _resultValue, value))
                {
                    ClearErrors();
                    MarkDirty();
                }
            }
        }

        private string _unit = string.Empty;

        /// <summary>واحد</summary>
        public string Unit
        {
            get => _unit;
            set
            {
                if (SetProperty(ref _unit, value))
                {
                    ClearErrors();
                    MarkDirty();
                }
            }
        }

        private string _referenceRange = string.Empty;

        /// <summary>محدوده مرجع</summary>
        public string ReferenceRange
        {
            get => _referenceRange;
            set
            {
                if (SetProperty(ref _referenceRange, value))
                {
                    ClearErrors();
                    MarkDirty();
                }
            }
        }

        private bool _isAbnormal = false;

        /// <summary>غیرطبیعی</summary>
        public bool IsAbnormal
        {
            get => _isAbnormal;
            set
            {
                if (SetProperty(ref _isAbnormal, value))
                {
                    ClearErrors();
                    MarkDirty();
                }
            }
        }

        private bool _isCritical = false;

        /// <summary>مقدار بحرانی</summary>
        public bool IsCritical
        {
            get => _isCritical;
            set
            {
                if (SetProperty(ref _isCritical, value))
                {
                    ClearErrors();
                    MarkDirty();
                }
            }
        }

        private string _technicianNote = string.Empty;

        /// <summary>یادداشت تکنسین</summary>
        public string TechnicianNote
        {
            get => _technicianNote;
            set
            {
                if (SetProperty(ref _technicianNote, value))
                {
                    ClearErrors();
                    MarkDirty();
                }
            }
        }


        // ══════════════════════════════════════════════════════════════
        //  فرمان‌ها
        // ══════════════════════════════════════════════════════════════

        private IAsyncRelayCommand? _approveCommand;

        /// <summary>تأیید نهایی</summary>
        public IAsyncRelayCommand ApproveCommand =>
            _approveCommand ??= new AsyncRelayCommand(ApproveAsync);

        // ══════════════════════════════════════════════════════════════
        //  رفتار
        // ══════════════════════════════════════════════════════════════

        /// <inheritdoc />
        protected override bool Validate()
        {
            ClearAllErrors();
            if (string.IsNullOrWhiteSpace(ResultValue))
                AddError("مقدار نتیجه الزامی است", nameof(ResultValue));

            return !HasErrors;
        }

        /// <inheritdoc />
        protected override async Task PersistAsync(CancellationToken ct)
        {
            // TODO: ارسال داده‌ها به سرور؛ نمونه:
            //   var payload = new { ... };
            //   await _apiClient.PostAsync<LabResultDto>("api/v1/<built-in method lower of str object at 0x7f5384a951f0>s", payload, ct);

            await Task.Delay(200, ct);
        }

        // ══════════════════════════════════════════════════════════════
        //  اقدام‌ها
        // ══════════════════════════════════════════════════════════════

        /// <summary>
        /// تأیید نهایی
        /// </summary>
        private async Task approveAsync()
        {
            var ok = await RunAsync(async ct =>
            {
                await Task.Delay(150, ct);   // TODO: فراخوانی سرویس واقعی
            }, "تأیید نهایی");

            if (ok) ShowSuccess("عملیات با موفقیت انجام شد");
        }

    }
}
