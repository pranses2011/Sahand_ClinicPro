// ═══════════════════════════════════════════════════════════════════════
//  PatientFormViewModel
//  صفحه‌ی «فرم بیمار» · گروه «بیماران» · کهن‌الگوی «form-std»
//
//  متناظر با:  03-views/PatientFormWindow.xaml
//  وایرفریم:    01-wireframes-html/PatientFormWindow.html
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
    /// ViewModelِ صفحه‌ی «فرم بیمار»
    /// </summary>
    /// <remarks>
    /// فیلدها بر اساس نقشِ خود اندازه گرفته‌اند:
    /// نام/نام‌خانوادگی/کدملی/جنسیت هر یک ۳ ستون از ۱۲؛ نشانی و حساسیت‌ها
    /// عرض کامل.
    ///
    /// کهن‌الگوی طراحی: <c>form-std</c> — چیدمان متناسب با همین قالب در View پیاده شده است.
    /// گروه: بیماران
    /// </remarks>
    public sealed class PatientFormViewModel : FormViewModelBase
    {
        /// <summary>ایجاد ViewModel و بارگذاریِ دادهٔ نمونه</summary>
        /// <param name="logger">ثبت‌کننده‌ی رویدادها (اختیاری)</param>
        public PatientFormViewModel(ILogger? logger = null) : base(logger)
        {
            // ارتباطات
            Relations.Add(new RelationItem("فرم بیمار", "بیماران", "پس از ذخیره به فهرست بازمی‌گردد"));
            Relations.Add(new RelationItem("فرم بیمار", "فرم پذیرش", "بیمه از پرونده می‌آید"));
            Relations.Add(new RelationItem("فرم بیمار", "ویزیت (SOAP)", "حساسیت‌ها در ویزیت نمایش داده می‌شود"));
        }

        /// <summary>ارتباطاتِ این صفحه با دیگر صفحات</summary>
        public ObservableCollection<RelationItem> Relations { get; } = new();

        // ══════════════════════════════════════════════════════
        //  ویژگی‌های فرم — مقدارِ آغازین برابرِ دادهٔ نمونه است
        // ══════════════════════════════════════════════════════

        /// <summary>نام — الزامی</summary>
        public string FirstName
        {
            get => _firstName;
            set
            {
                if (SetProperty(ref _firstName, value))
                {
                    MarkDirty();
                    Validate();
                }
            }
        }
        private string _firstName = "سارا";

        /// <summary>نام خانوادگی — الزامی</summary>
        public string LastName
        {
            get => _lastName;
            set
            {
                if (SetProperty(ref _lastName, value))
                {
                    MarkDirty();
                    Validate();
                }
            }
        }
        private string _lastName = "احمدی";

        /// <summary>کد ملی — الزامی</summary>
        public string NationalId
        {
            get => _nationalId;
            set
            {
                if (SetProperty(ref _nationalId, value))
                {
                    MarkDirty();
                    Validate();
                }
            }
        }
        private string _nationalId = "۰۰۱۲۳۴۵۶۷۸";

        /// <summary>جنسیت — الزامی</summary>
        public string Gender
        {
            get => _gender;
            set
            {
                if (SetProperty(ref _gender, value))
                {
                    MarkDirty();
                    Validate();
                }
            }
        }
        private string _gender = "زن";

        /// <summary>تاریخ تولد</summary>
        public DateTime? BirthDate
        {
            get => _birthDate;
            set
            {
                if (SetProperty(ref _birthDate, value))
                {
                    MarkDirty();
                }
            }
        }
        private DateTime? _birthDate = null;

        /// <summary>وضعیت تأهل</summary>
        public string MaritalStatus
        {
            get => _maritalStatus;
            set
            {
                if (SetProperty(ref _maritalStatus, value))
                {
                    MarkDirty();
                }
            }
        }
        private string _maritalStatus = "متأهل";

        /// <summary>شغل</summary>
        public string Occupation
        {
            get => _occupation;
            set
            {
                if (SetProperty(ref _occupation, value))
                {
                    MarkDirty();
                }
            }
        }
        private string _occupation = "کارمند";

        /// <summary>تحصیلات</summary>
        public string Education
        {
            get => _education;
            set
            {
                if (SetProperty(ref _education, value))
                {
                    MarkDirty();
                }
            }
        }
        private string _education = "کارشناسی";

        /// <summary>موبایل — الزامی</summary>
        public string Phone
        {
            get => _phone;
            set
            {
                if (SetProperty(ref _phone, value))
                {
                    MarkDirty();
                    Validate();
                }
            }
        }
        private string _phone = "۰۹۱۴۴۲۲۰۴۳۰";

        /// <summary>تلفن ثابت</summary>
        public string HomePhone
        {
            get => _homePhone;
            set
            {
                if (SetProperty(ref _homePhone, value))
                {
                    MarkDirty();
                }
            }
        }
        private string _homePhone = "۰۴۱-۳۴۴۳۶۱۴۱";

        /// <summary>رایانامه</summary>
        public string Email
        {
            get => _email;
            set
            {
                if (SetProperty(ref _email, value))
                {
                    MarkDirty();
                }
            }
        }
        private string _email = "info@ea-fixer.ir";

        /// <summary>شماره اضطراری</summary>
        public string EmergencyContact
        {
            get => _emergencyContact;
            set
            {
                if (SetProperty(ref _emergencyContact, value))
                {
                    MarkDirty();
                }
            }
        }
        private string _emergencyContact = "۰۹۱۴۴۲۲۰۴۳۰";

        /// <summary>استان</summary>
        public string Province
        {
            get => _province;
            set
            {
                if (SetProperty(ref _province, value))
                {
                    MarkDirty();
                }
            }
        }
        private string _province = "آذربایجان شرقی";

        /// <summary>شهر</summary>
        public string City
        {
            get => _city;
            set
            {
                if (SetProperty(ref _city, value))
                {
                    MarkDirty();
                }
            }
        }
        private string _city = "تبریز";

        /// <summary>نشانی کامل</summary>
        public string Address
        {
            get => _address;
            set
            {
                if (SetProperty(ref _address, value))
                {
                    MarkDirty();
                }
            }
        }
        private string _address = "تبریز، خیابان ولیعصر، کوچه گلستان، پلاک ۱۲";

        /// <summary>بیمه پایه</summary>
        public string InsuranceId
        {
            get => _insuranceId;
            set
            {
                if (SetProperty(ref _insuranceId, value))
                {
                    MarkDirty();
                }
            }
        }
        private string _insuranceId = "تأمین اجتماعی";

        /// <summary>شماره بیمه</summary>
        public string InsuranceNumber
        {
            get => _insuranceNumber;
            set
            {
                if (SetProperty(ref _insuranceNumber, value))
                {
                    MarkDirty();
                }
            }
        }
        private string _insuranceNumber = "۱۲۳۴۵۶۷۸۹۰";

        /// <summary>تاریخ اعتبار</summary>
        public DateTime? InsuranceExpiry
        {
            get => _insuranceExpiry;
            set
            {
                if (SetProperty(ref _insuranceExpiry, value))
                {
                    MarkDirty();
                }
            }
        }
        private DateTime? _insuranceExpiry = null;

        /// <summary>گروه خونی</summary>
        public string BloodType
        {
            get => _bloodType;
            set
            {
                if (SetProperty(ref _bloodType, value))
                {
                    MarkDirty();
                }
            }
        }
        private string _bloodType = "O+";

        /// <summary>وزن (کیلوگرم)</summary>
        public int Weight
        {
            get => _weight;
            set
            {
                if (SetProperty(ref _weight, value))
                {
                    MarkDirty();
                }
            }
        }
        private int _weight = 65;

        /// <summary>قد (سانتی‌متر)</summary>
        public int Height
        {
            get => _height;
            set
            {
                if (SetProperty(ref _height, value))
                {
                    MarkDirty();
                }
            }
        }
        private int _height = 165;

        /// <summary>بیماری‌های مزمن</summary>
        public string ChronicDiseases
        {
            get => _chronicDiseases;
            set
            {
                if (SetProperty(ref _chronicDiseases, value))
                {
                    MarkDirty();
                }
            }
        }
        private string _chronicDiseases = "دیابت";

        /// <summary>حساسیت‌های دارویی</summary>
        public string Allergies
        {
            get => _allergies;
            set
            {
                if (SetProperty(ref _allergies, value))
                {
                    MarkDirty();
                }
            }
        }
        private string _allergies = "پنی‌سیلین — واکنش پوستی شدید";

        // ══════════════════════════════════════════════════════
        //  فرمان‌ها
        // ══════════════════════════════════════════════════════

        /// <summary>ذخیره و جدید</summary>
        public IAsyncRelayCommand SaveNewCommand =>
            new AsyncRelayCommand(SaveNewAsync);

        /// <summary>اجرای «ذخیره و جدید»</summary>
        private async Task SaveNewAsync()
        {
            // TODO: پیوند به سرویسِ واقعی — «ذخیره و جدید»
            await RunAsync(_ => Task.CompletedTask, "ذخیره و جدید…");
        }

        /// <summary>
        /// اعتبارسنجیِ سمتِ کلاینت پیش از ذخیره
        /// </summary>
        protected override bool Validate()
        {
            var ok = true;

            if (string.IsNullOrWhiteSpace(FirstName))
            {
                AddError("نام الزامی است", nameof(FirstName));
            }
            else
            {
                ClearErrors(nameof(FirstName));
            }

            if (string.IsNullOrWhiteSpace(LastName))
            {
                AddError("نام خانوادگی الزامی است", nameof(LastName));
            }
            else
            {
                ClearErrors(nameof(LastName));
            }

            if (string.IsNullOrWhiteSpace(NationalId))
            {
                AddError("کد ملی الزامی است", nameof(NationalId));
            }
            else
            {
                ClearErrors(nameof(NationalId));
            }

            if (string.IsNullOrWhiteSpace(Gender))
            {
                AddError("جنسیت الزامی است", nameof(Gender));
            }
            else
            {
                ClearErrors(nameof(Gender));
            }

            if (string.IsNullOrWhiteSpace(Phone))
            {
                AddError("موبایل الزامی است", nameof(Phone));
            }
            else
            {
                ClearErrors(nameof(Phone));
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