// ═══════════════════════════════════════════════════════════════════════
//  DoctorFormViewModel
//  صفحه‌ی «فرم پزشک» · گروه «منابع انسانی» · کهن‌الگوی «form-std»
//
//  متناظر با:  03-views/DoctorFormWindow.xaml
//  وایرفریم:    01-wireframes-html/DoctorFormWindow.html
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
    /// ViewModelِ صفحه‌ی «فرم پزشک»
    /// </summary>
    /// <remarks>
    /// کهن‌الگوی طراحی: <c>form-std</c> — چیدمان متناسب با همین قالب در View پیاده شده است.
    /// گروه: منابع انسانی
    /// </remarks>
    public sealed class DoctorFormViewModel : FormViewModelBase
    {
        /// <summary>ایجاد ViewModel و بارگذاریِ دادهٔ نمونه</summary>
        /// <param name="logger">ثبت‌کننده‌ی رویدادها (اختیاری)</param>
        public DoctorFormViewModel(ILogger? logger = null) : base(logger)
        {
            // ارتباطات
            Relations.Add(new RelationItem("فرم پزشک", "نوبت‌دهی", "ظرفیت برنامه هفتگی"));
            Relations.Add(new RelationItem("فرم پزشک", "پرسنل", "پزشک از میان پرسنل"));
        }

        /// <summary>ارتباطاتِ این صفحه با دیگر صفحات</summary>
        public ObservableCollection<RelationItem> Relations { get; } = new();

        // ══════════════════════════════════════════════════════
        //  ویژگی‌های فرم — مقدارِ آغازین برابرِ دادهٔ نمونه است
        // ══════════════════════════════════════════════════════

        /// <summary>پرسنل مرتبط — الزامی</summary>
        public string PersonnelId
        {
            get => _personnelId;
            set
            {
                if (SetProperty(ref _personnelId, value))
                {
                    MarkDirty();
                    Validate();
                }
            }
        }
        private string _personnelId = "دکتر موسوی (E-۱۰۰۳)";

        /// <summary>تخصص — الزامی</summary>
        public string SpecialtyId
        {
            get => _specialtyId;
            set
            {
                if (SetProperty(ref _specialtyId, value))
                {
                    MarkDirty();
                    Validate();
                }
            }
        }
        private string _specialtyId = "متخصص داخلی";

        /// <summary>شماره نظام پزشکی — الزامی</summary>
        public string MedicalCouncilNo
        {
            get => _medicalCouncilNo;
            set
            {
                if (SetProperty(ref _medicalCouncilNo, value))
                {
                    MarkDirty();
                    Validate();
                }
            }
        }
        private string _medicalCouncilNo = "۱۲۳۴۵";

        /// <summary>درصد قرارداد</summary>
        public int ContractPercent
        {
            get => _contractPercent;
            set
            {
                if (SetProperty(ref _contractPercent, value))
                {
                    MarkDirty();
                }
            }
        }
        private int _contractPercent = 50;

        /// <summary>مدت هر ویزیت</summary>
        public string VisitDuration
        {
            get => _visitDuration;
            set
            {
                if (SetProperty(ref _visitDuration, value))
                {
                    MarkDirty();
                }
            }
        }
        private string _visitDuration = "۲۰ دقیقه";

        /// <summary>ظرفیت روزانه</summary>
        public int MaxPerDay
        {
            get => _maxPerDay;
            set
            {
                if (SetProperty(ref _maxPerDay, value))
                {
                    MarkDirty();
                }
            }
        }
        private int _maxPerDay = 40;

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

        /// <summary>شنبه — شروع</summary>
        public string SatStart
        {
            get => _satStart;
            set
            {
                if (SetProperty(ref _satStart, value))
                {
                    MarkDirty();
                }
            }
        }
        private string _satStart = "۰۸:۰۰";

        /// <summary>شنبه — پایان</summary>
        public string SatEnd
        {
            get => _satEnd;
            set
            {
                if (SetProperty(ref _satEnd, value))
                {
                    MarkDirty();
                }
            }
        }
        private string _satEnd = "۱۴:۰۰";

        /// <summary>یکشنبه — شروع</summary>
        public string SunStart
        {
            get => _sunStart;
            set
            {
                if (SetProperty(ref _sunStart, value))
                {
                    MarkDirty();
                }
            }
        }
        private string _sunStart = "۰۸:۰۰";

        /// <summary>یکشنبه — پایان</summary>
        public string SunEnd
        {
            get => _sunEnd;
            set
            {
                if (SetProperty(ref _sunEnd, value))
                {
                    MarkDirty();
                }
            }
        }
        private string _sunEnd = "۱۴:۰۰";

        /// <summary>دوشنبه — شروع</summary>
        public string MonStart
        {
            get => _monStart;
            set
            {
                if (SetProperty(ref _monStart, value))
                {
                    MarkDirty();
                }
            }
        }
        private string _monStart = "۱۴:۰۰";

        /// <summary>دوشنبه — پایان</summary>
        public string MonEnd
        {
            get => _monEnd;
            set
            {
                if (SetProperty(ref _monEnd, value))
                {
                    MarkDirty();
                }
            }
        }
        private string _monEnd = "۲۰:۰۰";

        /// <summary>فعال</summary>
        public bool IsActive
        {
            get => _isActive;
            set
            {
                if (SetProperty(ref _isActive, value))
                {
                    MarkDirty();
                }
            }
        }
        private bool _isActive = true;

        /// <summary>پذیرش بدون نوبت</summary>
        public bool AcceptsWalkIn
        {
            get => _acceptsWalkIn;
            set
            {
                if (SetProperty(ref _acceptsWalkIn, value))
                {
                    MarkDirty();
                }
            }
        }
        private bool _acceptsWalkIn = false;

        /// <summary>
        /// اعتبارسنجیِ سمتِ کلاینت پیش از ذخیره
        /// </summary>
        protected override bool Validate()
        {
            var ok = true;

            if (string.IsNullOrWhiteSpace(PersonnelId))
            {
                AddError("پرسنل مرتبط الزامی است", nameof(PersonnelId));
            }
            else
            {
                ClearErrors(nameof(PersonnelId));
            }

            if (string.IsNullOrWhiteSpace(SpecialtyId))
            {
                AddError("تخصص الزامی است", nameof(SpecialtyId));
            }
            else
            {
                ClearErrors(nameof(SpecialtyId));
            }

            if (string.IsNullOrWhiteSpace(MedicalCouncilNo))
            {
                AddError("شماره نظام پزشکی الزامی است", nameof(MedicalCouncilNo));
            }
            else
            {
                ClearErrors(nameof(MedicalCouncilNo));
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