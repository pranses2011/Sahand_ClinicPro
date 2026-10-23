// ═══════════════════════════════════════════════════════════════════════
//  EmployeeSalaryEditViewModel
//  صفحه‌ی «ویرایش حقوق» · گروه «منابع انسانی» · کهن‌الگوی «form-narrow»
//
//  متناظر با:  03-views/EmployeeSalaryEditWindow.xaml
//  وایرفریم:    01-wireframes-html/EmployeeSalaryEditWindow.html
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
    /// ViewModelِ صفحه‌ی «ویرایش حقوق»
    /// </summary>
    /// <remarks>
    /// کهن‌الگوی طراحی: <c>form-narrow</c> — چیدمان متناسب با همین قالب در View پیاده شده است.
    /// گروه: منابع انسانی
    /// </remarks>
    public sealed class EmployeeSalaryEditViewModel : FormViewModelBase
    {
        /// <summary>ایجاد ViewModel و بارگذاریِ دادهٔ نمونه</summary>
        /// <param name="logger">ثبت‌کننده‌ی رویدادها (اختیاری)</param>
        public EmployeeSalaryEditViewModel(ILogger? logger = null) : base(logger)
        {
            // ارتباطات
            Relations.Add(new RelationItem("ویرایش حقوق", "حقوق و دستمزد", "در دوره بعد اعمال می‌شود"));
        }

        /// <summary>ارتباطاتِ این صفحه با دیگر صفحات</summary>
        public ObservableCollection<RelationItem> Relations { get; } = new();

        // ══════════════════════════════════════════════════════
        //  ویژگی‌های فرم — مقدارِ آغازین برابرِ دادهٔ نمونه است
        // ══════════════════════════════════════════════════════

        /// <summary>پرسنل</summary>
        public string Employee
        {
            get => _employee;
            set
            {
                if (SetProperty(ref _employee, value))
                {
                    MarkDirty();
                }
            }
        }
        private string _employee = "مریم نوری — E-۱۰۰۱";

        /// <summary>حقوق پایه — الزامی</summary>
        public decimal BaseSalary
        {
            get => _baseSalary;
            set
            {
                if (SetProperty(ref _baseSalary, value))
                {
                    MarkDirty();
                    Validate();
                }
            }
        }
        private decimal _baseSalary = 14000000m;

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
        private decimal _hourlyRate = 87500m;

        /// <summary>نرخ اضافه‌کار</summary>
        public decimal OvertimeRate
        {
            get => _overtimeRate;
            set
            {
                if (SetProperty(ref _overtimeRate, value))
                {
                    MarkDirty();
                }
            }
        }
        private decimal _overtimeRate = 120000m;

        /// <summary>درصد کارمزد</summary>
        public int Percent
        {
            get => _percent;
            set
            {
                if (SetProperty(ref _percent, value))
                {
                    MarkDirty();
                }
            }
        }
        private int _percent = 0;

        /// <summary>تاریخ اجرا — الزامی</summary>
        public DateTime? EffectiveDate
        {
            get => _effectiveDate;
            set
            {
                if (SetProperty(ref _effectiveDate, value))
                {
                    MarkDirty();
                    Validate();
                }
            }
        }
        private DateTime? _effectiveDate = null;

        /// <summary>
        /// اعتبارسنجیِ سمتِ کلاینت پیش از ذخیره
        /// </summary>
        protected override bool Validate()
        {
            var ok = true;

            if (BaseSalary <= 0)
            {
                AddError("حقوق پایه الزامی است", nameof(BaseSalary));
            }
            else
            {
                ClearErrors(nameof(BaseSalary));
            }

            if (EffectiveDate is null)
            {
                AddError("تاریخ اجرا الزامی است", nameof(EffectiveDate));
            }
            else
            {
                ClearErrors(nameof(EffectiveDate));
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