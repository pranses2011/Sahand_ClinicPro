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
    /// ViewModel صفحه‌ی «فرم کارمند» — ایجاد یا ویرایش اطلاعات کارمند
    /// </summary>
    /// <remarks>
    /// 🔗 وایرفریم مرجع:   <c>02-wireframes-md/EmployeeFormWindow.md</c>
    /// 🖼 وایرفریم گرافیکی: <c>01-wireframes-html/EmployeeFormWindow.html</c>
    /// 🎨 View متناظر:     <c>03-views/EmployeeFormWindow.xaml</c>
    ///
    /// این کلاس هیچ رشته‌ی متنیِ نمایشی ندارد؛ همه‌ی متن‌ها از
    /// <c>Strings.resx</c> و از طریق افزونه‌ی <c>{loc:Loc}</c> تأمین می‌شوند.
    /// </remarks>
    public class EmployeeFormViewModel : FormViewModelBase
    {
        public EmployeeFormViewModel(ILogger? logger = null) : base(logger)
        {
            _ = LoadAsync();
        }

        // ══════════════════════════════════════════════════════════════
        //  ویژگی‌ها
        // ══════════════════════════════════════════════════════════════


        private string _firstName = string.Empty;

        /// <summary>نام (الزامی)</summary>
        public string FirstName
        {
            get => _firstName;
            set
            {
                if (SetProperty(ref _firstName, value))
                {
                    ClearErrors();
                    MarkDirty();
                }
            }
        }

        private string _lastName = string.Empty;

        /// <summary>نام خانوادگی (الزامی)</summary>
        public string LastName
        {
            get => _lastName;
            set
            {
                if (SetProperty(ref _lastName, value))
                {
                    ClearErrors();
                    MarkDirty();
                }
            }
        }

        private string _nationalCode = string.Empty;

        /// <summary>کد ملی (الزامی)</summary>
        public string NationalCode
        {
            get => _nationalCode;
            set
            {
                if (SetProperty(ref _nationalCode, value))
                {
                    ClearErrors();
                    MarkDirty();
                }
            }
        }

        private DateTime? _birthDate = null;

        /// <summary>تاریخ تولد</summary>
        public DateTime? BirthDate
        {
            get => _birthDate;
            set
            {
                if (SetProperty(ref _birthDate, value))
                {
                    ClearErrors();
                    MarkDirty();
                }
            }
        }

        private string _gender = string.Empty;

        /// <summary>جنسیت</summary>
        public string Gender
        {
            get => _gender;
            set
            {
                if (SetProperty(ref _gender, value))
                {
                    ClearErrors();
                    MarkDirty();
                }
            }
        }

        private string _maritalStatus = string.Empty;

        /// <summary>وضعیت تأهل</summary>
        public string MaritalStatus
        {
            get => _maritalStatus;
            set
            {
                if (SetProperty(ref _maritalStatus, value))
                {
                    ClearErrors();
                    MarkDirty();
                }
            }
        }

        private string _employeeCode = string.Empty;

        /// <summary>کد پرسنلی (الزامی)</summary>
        public string EmployeeCode
        {
            get => _employeeCode;
            set
            {
                if (SetProperty(ref _employeeCode, value))
                {
                    ClearErrors();
                    MarkDirty();
                }
            }
        }

        private Guid? _positionId = null;

        /// <summary>سمت (الزامی)</summary>
        public Guid? PositionId
        {
            get => _positionId;
            set
            {
                if (SetProperty(ref _positionId, value))
                {
                    ClearErrors();
                    MarkDirty();
                }
            }
        }

        private Guid? _departmentId = null;

        /// <summary>بخش (الزامی)</summary>
        public Guid? DepartmentId
        {
            get => _departmentId;
            set
            {
                if (SetProperty(ref _departmentId, value))
                {
                    ClearErrors();
                    MarkDirty();
                }
            }
        }

        private DateTime? _hireDate = null;

        /// <summary>تاریخ استخدام (الزامی)</summary>
        public DateTime? HireDate
        {
            get => _hireDate;
            set
            {
                if (SetProperty(ref _hireDate, value))
                {
                    ClearErrors();
                    MarkDirty();
                }
            }
        }

        private string _employmentType = string.Empty;

        /// <summary>نوع استخدام</summary>
        public string EmploymentType
        {
            get => _employmentType;
            set
            {
                if (SetProperty(ref _employmentType, value))
                {
                    ClearErrors();
                    MarkDirty();
                }
            }
        }

        private Guid? _managerId = null;

        /// <summary>مدیر مستقیم</summary>
        public Guid? ManagerId
        {
            get => _managerId;
            set
            {
                if (SetProperty(ref _managerId, value))
                {
                    ClearErrors();
                    MarkDirty();
                }
            }
        }

        private string _mobileNumber = string.Empty;

        /// <summary>موبایل</summary>
        public string MobileNumber
        {
            get => _mobileNumber;
            set
            {
                if (SetProperty(ref _mobileNumber, value))
                {
                    ClearErrors();
                    MarkDirty();
                }
            }
        }

        private string _email = string.Empty;

        /// <summary>ایمیل</summary>
        public string Email
        {
            get => _email;
            set
            {
                if (SetProperty(ref _email, value))
                {
                    ClearErrors();
                    MarkDirty();
                }
            }
        }

        private string _address = string.Empty;

        /// <summary>نشانی</summary>
        public string Address
        {
            get => _address;
            set
            {
                if (SetProperty(ref _address, value))
                {
                    ClearErrors();
                    MarkDirty();
                }
            }
        }

        private decimal _baseSalary = 0m;

        /// <summary>حقوق پایه</summary>
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

        private decimal _allowances = 0m;

        /// <summary>مزایا</summary>
        public decimal Allowances
        {
            get => _allowances;
            set
            {
                if (SetProperty(ref _allowances, value))
                {
                    ClearErrors();
                    MarkDirty();
                }
            }
        }

        private string _insuranceNumber = string.Empty;

        /// <summary>شماره بیمه</summary>
        public string InsuranceNumber
        {
            get => _insuranceNumber;
            set
            {
                if (SetProperty(ref _insuranceNumber, value))
                {
                    ClearErrors();
                    MarkDirty();
                }
            }
        }

        private string _bankAccount = string.Empty;

        /// <summary>شماره حساب</summary>
        public string BankAccount
        {
            get => _bankAccount;
            set
            {
                if (SetProperty(ref _bankAccount, value))
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
            if (string.IsNullOrWhiteSpace(FirstName))
                AddError("نام الزامی است", nameof(FirstName));
            if (string.IsNullOrWhiteSpace(LastName))
                AddError("نام خانوادگی الزامی است", nameof(LastName));
            if (string.IsNullOrWhiteSpace(NationalCode))
                AddError("کد ملی الزامی است", nameof(NationalCode));
            if (string.IsNullOrWhiteSpace(EmployeeCode))
                AddError("کد پرسنلی الزامی است", nameof(EmployeeCode));
            if (PositionId is null)
                AddError("سمت الزامی است", nameof(PositionId));
            if (DepartmentId is null)
                AddError("بخش الزامی است", nameof(DepartmentId));
            if (HireDate is null)
                AddError("تاریخ استخدام الزامی است", nameof(HireDate));

            return !HasErrors;
        }

        /// <inheritdoc />
        protected override async Task PersistAsync(CancellationToken ct)
        {
            // TODO: ارسال داده‌ها به سرور؛ نمونه:
            //   var payload = new { ... };
            //   await _apiClient.PostAsync<EmployeeFormDto>("api/v1/<built-in method lower of str object at 0x7f5384a94ef0>s", payload, ct);

            await Task.Delay(200, ct);
        }

        // ══════════════════════════════════════════════════════════════
        //  اقدام‌ها
        // ══════════════════════════════════════════════════════════════


    }
}
