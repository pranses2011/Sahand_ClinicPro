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
    /// ViewModel صفحه‌ی «فرم نوبت» — ایجاد یا ویرایش نوبت
    /// </summary>
    /// <remarks>
    /// 🔗 وایرفریم مرجع:   <c>02-wireframes-md/AppointmentFormWindow.md</c>
    /// 🖼 وایرفریم گرافیکی: <c>01-wireframes-html/AppointmentFormWindow.html</c>
    /// 🎨 View متناظر:     <c>03-views/AppointmentFormWindow.xaml</c>
    ///
    /// این کلاس هیچ رشته‌ی متنیِ نمایشی ندارد؛ همه‌ی متن‌ها از
    /// <c>Strings.resx</c> و از طریق افزونه‌ی <c>{loc:Loc}</c> تأمین می‌شوند.
    /// </remarks>
    public class AppointmentFormViewModel : FormViewModelBase
    {
        public AppointmentFormViewModel(ILogger? logger = null) : base(logger)
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

        private DateTime? _appointmentDate = null;

        /// <summary>تاریخ نوبت (الزامی)</summary>
        public DateTime? AppointmentDate
        {
            get => _appointmentDate;
            set
            {
                if (SetProperty(ref _appointmentDate, value))
                {
                    ClearErrors();
                    MarkDirty();
                }
            }
        }

        private TimeSpan? _startTime = string.Empty;

        /// <summary>ساعت شروع (الزامی)</summary>
        public TimeSpan? StartTime
        {
            get => _startTime;
            set
            {
                if (SetProperty(ref _startTime, value))
                {
                    ClearErrors();
                    MarkDirty();
                }
            }
        }

        private decimal _durationMinutes = 0m;

        /// <summary>مدت (دقیقه)</summary>
        public decimal DurationMinutes
        {
            get => _durationMinutes;
            set
            {
                if (SetProperty(ref _durationMinutes, value))
                {
                    ClearErrors();
                    MarkDirty();
                }
            }
        }

        private Guid? _roomId = null;

        /// <summary>اتاق</summary>
        public Guid? RoomId
        {
            get => _roomId;
            set
            {
                if (SetProperty(ref _roomId, value))
                {
                    ClearErrors();
                    MarkDirty();
                }
            }
        }

        private string _reason = string.Empty;

        /// <summary>علت مراجعه</summary>
        public string Reason
        {
            get => _reason;
            set
            {
                if (SetProperty(ref _reason, value))
                {
                    ClearErrors();
                    MarkDirty();
                }
            }
        }

        private string _notes = string.Empty;

        /// <summary>یادداشت</summary>
        public string Notes
        {
            get => _notes;
            set
            {
                if (SetProperty(ref _notes, value))
                {
                    ClearErrors();
                    MarkDirty();
                }
            }
        }

        private bool _isFirstVisit = false;

        /// <summary>ویزیت اول</summary>
        public bool IsFirstVisit
        {
            get => _isFirstVisit;
            set
            {
                if (SetProperty(ref _isFirstVisit, value))
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
            if (PatientId is null)
                AddError("بیمار الزامی است", nameof(PatientId));
            if (DoctorId is null)
                AddError("پزشک الزامی است", nameof(DoctorId));
            if (ServiceId is null)
                AddError("خدمت الزامی است", nameof(ServiceId));
            if (AppointmentDate is null)
                AddError("تاریخ نوبت الزامی است", nameof(AppointmentDate));

            return !HasErrors;
        }

        /// <inheritdoc />
        protected override async Task PersistAsync(CancellationToken ct)
        {
            // TODO: ارسال داده‌ها به سرور؛ نمونه:
            //   var payload = new { ... };
            //   await _apiClient.PostAsync<AppointmentFormDto>("api/v1/<built-in method lower of str object at 0x7f5384a94cf0>s", payload, ct);

            await Task.Delay(200, ct);
        }

        // ══════════════════════════════════════════════════════════════
        //  اقدام‌ها
        // ══════════════════════════════════════════════════════════════


    }
}
