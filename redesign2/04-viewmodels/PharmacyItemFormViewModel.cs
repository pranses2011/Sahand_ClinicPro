// ═══════════════════════════════════════════════════════════════════════
//  PharmacyItemFormViewModel
//  صفحه‌ی «فرم قلم دارویی» · گروه «داروخانه» · کهن‌الگوی «form-std»
//
//  متناظر با:  03-views/PharmacyItemFormWindow.xaml
//  وایرفریم:    01-wireframes-html/PharmacyItemFormWindow.html
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
    /// ViewModelِ صفحه‌ی «فرم قلم دارویی»
    /// </summary>
    /// <remarks>
    /// کهن‌الگوی طراحی: <c>form-std</c> — چیدمان متناسب با همین قالب در View پیاده شده است.
    /// گروه: داروخانه
    /// </remarks>
    public sealed class PharmacyItemFormViewModel : FormViewModelBase
    {
        /// <summary>ایجاد ViewModel و بارگذاریِ دادهٔ نمونه</summary>
        /// <param name="logger">ثبت‌کننده‌ی رویدادها (اختیاری)</param>
        public PharmacyItemFormViewModel(ILogger? logger = null) : base(logger)
        {
            // ارتباطات
            Relations.Add(new RelationItem("فرم قلم دارویی", "ورود کالا", "بچ برای این قلم"));
        }

        /// <summary>ارتباطاتِ این صفحه با دیگر صفحات</summary>
        public ObservableCollection<RelationItem> Relations { get; } = new();

        // ══════════════════════════════════════════════════════
        //  ویژگی‌های فرم — مقدارِ آغازین برابرِ دادهٔ نمونه است
        // ══════════════════════════════════════════════════════

        /// <summary>کد قلم — الزامی</summary>
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
        private string _code = "D-۱۰۰۵";

        /// <summary>نام تجاری — الزامی</summary>
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
        private string _name = "سفکسیم ۴۰۰ کپسول";

        /// <summary>نام ژنریک — الزامی</summary>
        public string GenericName
        {
            get => _genericName;
            set
            {
                if (SetProperty(ref _genericName, value))
                {
                    MarkDirty();
                    Validate();
                }
            }
        }
        private string _genericName = "Cefixime";

        /// <summary>نوع قلم — الزامی</summary>
        public string Kind
        {
            get => _kind;
            set
            {
                if (SetProperty(ref _kind, value))
                {
                    MarkDirty();
                    Validate();
                }
            }
        }
        private string _kind = "دارو";

        /// <summary>گروه دارویی</summary>
        public string Category
        {
            get => _category;
            set
            {
                if (SetProperty(ref _category, value))
                {
                    MarkDirty();
                }
            }
        }
        private string _category = "آنتی‌بیوتیک";

        /// <summary>شکل دارویی</summary>
        public string Form
        {
            get => _form;
            set
            {
                if (SetProperty(ref _form, value))
                {
                    MarkDirty();
                }
            }
        }
        private string _form = "کپسول";

        /// <summary>واحد</summary>
        public string Unit
        {
            get => _unit;
            set
            {
                if (SetProperty(ref _unit, value))
                {
                    MarkDirty();
                }
            }
        }
        private string _unit = "عدد";

        /// <summary>حد سفارش</summary>
        public int MinStock
        {
            get => _minStock;
            set
            {
                if (SetProperty(ref _minStock, value))
                {
                    MarkDirty();
                }
            }
        }
        private int _minStock = 50;

        /// <summary>قیمت فروش</summary>
        public decimal SalePrice
        {
            get => _salePrice;
            set
            {
                if (SetProperty(ref _salePrice, value))
                {
                    MarkDirty();
                }
            }
        }
        private decimal _salePrice = 65000m;

        /// <summary>قیمت خرید</summary>
        public decimal PurchasePrice
        {
            get => _purchasePrice;
            set
            {
                if (SetProperty(ref _purchasePrice, value))
                {
                    MarkDirty();
                }
            }
        }
        private decimal _purchasePrice = 52000m;

        /// <summary>درصد مالیات</summary>
        public int TaxRate
        {
            get => _taxRate;
            set
            {
                if (SetProperty(ref _taxRate, value))
                {
                    MarkDirty();
                }
            }
        }
        private int _taxRate = 9;

        /// <summary>بارکد</summary>
        public string Barcode
        {
            get => _barcode;
            set
            {
                if (SetProperty(ref _barcode, value))
                {
                    MarkDirty();
                }
            }
        }
        private string _barcode = "۶۲۶۰۱۰۷۰۰۱۲۳۴";

        /// <summary>کد IRC</summary>
        public string IrcCode
        {
            get => _ircCode;
            set
            {
                if (SetProperty(ref _ircCode, value))
                {
                    MarkDirty();
                }
            }
        }
        private string _ircCode = "۱۲۳۴۵۶۷۸۹۰";

        /// <summary>نیازمند نسخه</summary>
        public bool RequiresPrescription
        {
            get => _requiresPrescription;
            set
            {
                if (SetProperty(ref _requiresPrescription, value))
                {
                    MarkDirty();
                }
            }
        }
        private bool _requiresPrescription = true;

        /// <summary>داروی مخدر</summary>
        public bool IsNarcotic
        {
            get => _isNarcotic;
            set
            {
                if (SetProperty(ref _isNarcotic, value))
                {
                    MarkDirty();
                }
            }
        }
        private bool _isNarcotic = false;

        // ══════════════════════════════════════════════════════
        //  فرمان‌ها
        // ══════════════════════════════════════════════════════

        /// <summary>خواندنِ بارکد برای «بارکد»</summary>
        public IRelayCommand ScanBarcodeCommand =>
            new RelayCommand(() => ScanBarcode("بارکد"));

        /// <summary>
        /// اعتبارسنجیِ سمتِ کلاینت پیش از ذخیره
        /// </summary>
        protected override bool Validate()
        {
            var ok = true;

            if (string.IsNullOrWhiteSpace(Code))
            {
                AddError("کد قلم الزامی است", nameof(Code));
            }
            else
            {
                ClearErrors(nameof(Code));
            }

            if (string.IsNullOrWhiteSpace(Name))
            {
                AddError("نام تجاری الزامی است", nameof(Name));
            }
            else
            {
                ClearErrors(nameof(Name));
            }

            if (string.IsNullOrWhiteSpace(GenericName))
            {
                AddError("نام ژنریک الزامی است", nameof(GenericName));
            }
            else
            {
                ClearErrors(nameof(GenericName));
            }

            if (string.IsNullOrWhiteSpace(Kind))
            {
                AddError("نوع قلم الزامی است", nameof(Kind));
            }
            else
            {
                ClearErrors(nameof(Kind));
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