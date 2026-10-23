// ═══════════════════════════════════════════════════════════════════════
//  CompanyFormViewModel
//  صفحه‌ی «فرم شرکت بیمه» · گروه «مالی» · کهن‌الگوی «form-narrow»
//
//  متناظر با:  03-views/CompanyFormWindow.xaml
//  وایرفریم:    01-wireframes-html/CompanyFormWindow.html
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
    /// ViewModelِ صفحه‌ی «فرم شرکت بیمه»
    /// </summary>
    /// <remarks>
    /// کهن‌الگوی طراحی: <c>form-narrow</c> — چیدمان متناسب با همین قالب در View پیاده شده است.
    /// گروه: مالی
    /// </remarks>
    public sealed class CompanyFormViewModel : FormViewModelBase
    {
        /// <summary>ایجاد ViewModel و بارگذاریِ دادهٔ نمونه</summary>
        /// <param name="logger">ثبت‌کننده‌ی رویدادها (اختیاری)</param>
        public CompanyFormViewModel(ILogger? logger = null) : base(logger)
        {
            // ارتباطات
            Relations.Add(new RelationItem("فرم شرکت بیمه", "بیمه و مطالبات", "در فهرست بیمه‌ها"));
            Relations.Add(new RelationItem("فرم شرکت بیمه", "تعرفه خدمات", "تعرفه برای این بیمه"));
        }

        /// <summary>ارتباطاتِ این صفحه با دیگر صفحات</summary>
        public ObservableCollection<RelationItem> Relations { get; } = new();

        // ══════════════════════════════════════════════════════
        //  ویژگی‌های فرم — مقدارِ آغازین برابرِ دادهٔ نمونه است
        // ══════════════════════════════════════════════════════

        /// <summary>نام شرکت — الزامی</summary>
        public string Name
        {
            get => _name;
            set
            {
                if (SetProperty(ref _name, value))
                {
                    MarkDirty();
                    Validate();
                }
            }
        }
        private string _name = "بیمه تأمین اجتماعی";

        /// <summary>کد شرکت — الزامی</summary>
        public string Code
        {
            get => _code;
            set
            {
                if (SetProperty(ref _code, value))
                {
                    MarkDirty();
                    Validate();
                }
            }
        }
        private string _code = "INS-۰۰۱";

        /// <summary>شماره قرارداد</summary>
        public string ContractNo
        {
            get => _contractNo;
            set
            {
                if (SetProperty(ref _contractNo, value))
                {
                    MarkDirty();
                }
            }
        }
        private string _contractNo = "۱۴۰۴/۲۳۴";

        /// <summary>درصد پوشش</summary>
        public int CoveragePercent
        {
            get => _coveragePercent;
            set
            {
                if (SetProperty(ref _coveragePercent, value))
                {
                    MarkDirty();
                }
            }
        }
        private int _coveragePercent = 70;

        /// <summary>سقف تعهد ماهانه</summary>
        public decimal Ceiling
        {
            get => _ceiling;
            set
            {
                if (SetProperty(ref _ceiling, value))
                {
                    MarkDirty();
                }
            }
        }
        private decimal _ceiling = 50000000m;

        /// <summary>فعال</summary>
        public bool Active
        {
            get => _active;
            set
            {
                if (SetProperty(ref _active, value))
                {
                    MarkDirty();
                }
            }
        }
        private bool _active = true;

        /// <summary>
        /// اعتبارسنجیِ سمتِ کلاینت پیش از ذخیره
        /// </summary>
        protected override bool Validate()
        {
            var ok = true;

            if (string.IsNullOrWhiteSpace(Name))
            {
                AddError("نام شرکت الزامی است", nameof(Name));
            }
            else
            {
                ClearErrors(nameof(Name));
            }

            if (string.IsNullOrWhiteSpace(Code))
            {
                AddError("کد شرکت الزامی است", nameof(Code));
            }
            else
            {
                ClearErrors(nameof(Code));
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