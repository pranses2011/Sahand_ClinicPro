// ═══════════════════════════════════════════════════════════════════════
//  EmployeeFormViewModel
//  صفحه‌ی «فرم کارمند» · گروه «منابع انسانی» · کهن‌الگوی «form-std»
//
//  متناظر با:  03-views/EmployeeFormWindow.xaml
//  وایرفریم:    01-wireframes-html/EmployeeFormWindow.html
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
    /// ViewModelِ صفحه‌ی «فرم کارمند»
    /// </summary>
    /// <remarks>
    /// کهن‌الگوی طراحی: <c>form-std</c> — چیدمان متناسب با همین قالب در View پیاده شده است.
    /// گروه: منابع انسانی
    /// </remarks>
    public sealed class EmployeeFormViewModel : FormViewModelBase
    {
        /// <summary>ایجاد ViewModel و بارگذاریِ دادهٔ نمونه</summary>
        /// <param name="logger">ثبت‌کننده‌ی رویدادها (اختیاری)</param>
        public EmployeeFormViewModel(ILogger? logger = null) : base(logger)
        {
            // ارتباطات
            Relations.Add(new RelationItem("فرم کارمند", "حقوق و دستمزد", "حقوق پایه مبنای محاسبه"));
            Relations.Add(new RelationItem("فرم کارمند", "شیفت‌ها", "تخصیص به شیفت"));
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
        private string _firstName = "مریم";

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
        private string _lastName = "نوری";

        /// <summary>کد ملی</summary>
        public string NationalId
        {
            get => _nationalId;
            set
            {
                if (SetProperty(ref _nationalId, value))
                {
                    MarkDirty();
                }
            }
        }
        private string _nationalId = "۰۰۹۸۷۶۵۴۳۲";

        /// <summary>جنسیت</summary>
        public string Gender
        {
            get => _gender;
            set
            {
                if (SetProperty(ref _gender, value))
                {
                    MarkDirty();
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
        private string _maritalStatus = "مجرد";

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

        /// <summary>عکس پرسنلی</summary>
        public string Photo
        {
            get => _photo;
            set
            {
                if (SetProperty(ref _photo, value))
                {
                    MarkDirty();
                }
            }
        }
        private string _photo = "profile.jpg";

        /// <summary>کد پرسنلی — الزامی</summary>
        public string EmployeeCode
        {
            get => _employeeCode;
            set
            {
                if (SetProperty(ref _employeeCode, value))
                {
                    MarkDirty();
                    Validate();
                }
            }
        }
        private string _employeeCode = "E-۱۰۰۱";

        /// <summary>سمت — الزامی</summary>
        public string PositionId
        {
            get => _positionId;
            set
            {
                if (SetProperty(ref _positionId, value))
                {
                    MarkDirty();
                    Validate();
                }
            }
        }
        private string _positionId = "کارشناس پذیرش";

        /// <summary>دپارتمان — الزامی</summary>
        public string DepartmentId
        {
            get => _departmentId;
            set
            {
                if (SetProperty(ref _departmentId, value))
                {
                    MarkDirty();
                    Validate();
                }
            }
        }
        private string _departmentId = "پذیرش";

        /// <summary>تاریخ استخدام — الزامی</summary>
        public DateTime? HireDate
        {
            get => _hireDate;
            set
            {
                if (SetProperty(ref _hireDate, value))
                {
                    MarkDirty();
                    Validate();
                }
            }
        }
        private DateTime? _hireDate = null;

        /// <summary>نوع همکاری</summary>
        public string EmploymentType
        {
            get => _employmentType;
            set
            {
                if (SetProperty(ref _employmentType, value))
                {
                    MarkDirty();
                }
            }
        }
        private string _employmentType = "تمام‌وقت";

        /// <summary>شعبه</summary>
        public string BranchId
        {
            get => _branchId;
            set
            {
                if (SetProperty(ref _branchId, value))
                {
                    MarkDirty();
                }
            }
        }
        private string _branchId = "مرکزی";

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

        /// <summary>حقوق پایه</summary>
        public decimal BaseSalary
        {
            get => _baseSalary;
            set
            {
                if (SetProperty(ref _baseSalary, value))
                {
                    MarkDirty();
                }
            }
        }
        private decimal _baseSalary = 12000000m;

        /// <summary>نرخ ساعتی</summary>
        public decimal HourlyRate
        {
            get => _hourlyRate;
            set
            {
                if (SetProperty(ref _hourlyRate, value))
                {
                    MarkDirty();
                }
            }
        }
        private decimal _hourlyRate = 75000m;

        /// <summary>شماره بیمه</summary>
        public string InsuranceNo
        {
            get => _insuranceNo;
            set
            {
                if (SetProperty(ref _insuranceNo, value))
                {
                    MarkDirty();
                }
            }
        }
        private string _insuranceNo = "۱۲۳۴۵۶۷۸۹۰";

        /// <summary>شماره حساب</summary>
        public string BankAccount
        {
            get => _bankAccount;
            set
            {
                if (SetProperty(ref _bankAccount, value))
                {
                    MarkDirty();
                }
            }
        }
        private string _bankAccount = "IR۱۲۰۱۷۰۰۰۰۰۰۰۱۲۳۴۵۶۷۸۹۰";

        /// <summary>موبایل</summary>
        public string Phone
        {
            get => _phone;
            set
            {
                if (SetProperty(ref _phone, value))
                {
                    MarkDirty();
                }
            }
        }
        private string _phone = "۰۹۱۴۴۲۲۰۴۳۰";

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

        /// <summary>نشانی</summary>
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
        private string _address = "تبریز، آبرسان، خیابان …";

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

            if (string.IsNullOrWhiteSpace(EmployeeCode))
            {
                AddError("کد پرسنلی الزامی است", nameof(EmployeeCode));
            }
            else
            {
                ClearErrors(nameof(EmployeeCode));
            }

            if (string.IsNullOrWhiteSpace(PositionId))
            {
                AddError("سمت الزامی است", nameof(PositionId));
            }
            else
            {
                ClearErrors(nameof(PositionId));
            }

            if (string.IsNullOrWhiteSpace(DepartmentId))
            {
                AddError("دپارتمان الزامی است", nameof(DepartmentId));
            }
            else
            {
                ClearErrors(nameof(DepartmentId));
            }

            if (HireDate is null)
            {
                AddError("تاریخ استخدام الزامی است", nameof(HireDate));
            }
            else
            {
                ClearErrors(nameof(HireDate));
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