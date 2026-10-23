// ═══════════════════════════════════════════════════════════════════════
//  AppointmentFormViewModel
//  صفحه‌ی «فرم نوبت» · گروه «نوبت‌دهی» · کهن‌الگوی «form-std»
//
//  متناظر با:  03-views/AppointmentFormWindow.xaml
//  وایرفریم:    01-wireframes-html/AppointmentFormWindow.html
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
    /// ViewModelِ صفحه‌ی «فرم نوبت»
    /// </summary>
    /// <remarks>
    /// کهن‌الگوی طراحی: <c>form-std</c> — چیدمان متناسب با همین قالب در View پیاده شده است.
    /// گروه: نوبت‌دهی
    /// </remarks>
    public sealed class AppointmentFormViewModel : FormViewModelBase
    {
        /// <summary>ایجاد ViewModel و بارگذاریِ دادهٔ نمونه</summary>
        /// <param name="logger">ثبت‌کننده‌ی رویدادها (اختیاری)</param>
        public AppointmentFormViewModel(ILogger? logger = null) : base(logger)
        {
            // ارتباطات
            Relations.Add(new RelationItem("فرم نوبت", "اتاق‌ها", "اتاق از فهرست اتاق‌ها"));
            Relations.Add(new RelationItem("فرم نوبت", "پیامک", "یادآوری پیامکی"));
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

        /// <summary>تاریخ نوبت — الزامی</summary>
        public DateTime? Date
        {
            get => _date;
            set
            {
                if (SetProperty(ref _date, value))
                {
                    MarkDirty();
                    Validate();
                }
            }
        }
        private DateTime? _date = null;

        /// <summary>ساعت — الزامی</summary>
        public string Time
        {
            get => _time;
            set
            {
                if (SetProperty(ref _time, value))
                {
                    MarkDirty();
                    Validate();
                }
            }
        }
        private string _time = "۱۰:۳۰";

        /// <summary>مدت</summary>
        public string Duration
        {
            get => _duration;
            set
            {
                if (SetProperty(ref _duration, value))
                {
                    MarkDirty();
                }
            }
        }
        private string _duration = "۲۰ دقیقه";

        /// <summary>نوع ویزیت</summary>
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
        private string _visitType = "ویزیت عمومی";

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

        /// <summary>علت مراجعه</summary>
        public string Reason
        {
            get => _reason;
            set
            {
                if (SetProperty(ref _reason, value))
                {
                    MarkDirty();
                }
            }
        }
        private string _reason = "درد معده";

        /// <summary>یادداشت</summary>
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

        /// <summary>ارسال پیامک یادآوری</summary>
        public bool SendSms
        {
            get => _sendSms;
            set
            {
                if (SetProperty(ref _sendSms, value))
                {
                    MarkDirty();
                }
            }
        }
        private bool _sendSms = true;

        /// <summary>نوبت فوری</summary>
        public bool IsUrgent
        {
            get => _isUrgent;
            set
            {
                if (SetProperty(ref _isUrgent, value))
                {
                    MarkDirty();
                }
            }
        }
        private bool _isUrgent = false;

        // ══════════════════════════════════════════════════════
        //  فرمان‌ها
        // ══════════════════════════════════════════════════════

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

            if (Date is null)
            {
                AddError("تاریخ نوبت الزامی است", nameof(Date));
            }
            else
            {
                ClearErrors(nameof(Date));
            }

            if (string.IsNullOrWhiteSpace(Time))
            {
                AddError("ساعت الزامی است", nameof(Time));
            }
            else
            {
                ClearErrors(nameof(Time));
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