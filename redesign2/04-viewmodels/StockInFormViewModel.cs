// ═══════════════════════════════════════════════════════════════════════
//  StockInFormViewModel
//  صفحه‌ی «ورود کالا» · گروه «داروخانه» · کهن‌الگوی «form-lines»
//
//  متناظر با:  03-views/StockInFormWindow.xaml
//  وایرفریم:    01-wireframes-html/StockInFormWindow.html
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
    /// ViewModelِ صفحه‌ی «ورود کالا»
    /// </summary>
    /// <remarks>
    /// کهن‌الگوی طراحی: <c>form-lines</c> — چیدمان متناسب با همین قالب در View پیاده شده است.
    /// گروه: داروخانه
    /// </remarks>
    public sealed class StockInFormViewModel : FormViewModelBase
    {
        /// <summary>ایجاد ViewModel و بارگذاریِ دادهٔ نمونه</summary>
        /// <param name="logger">ثبت‌کننده‌ی رویدادها (اختیاری)</param>
        public StockInFormViewModel(ILogger? logger = null) : base(logger)
        {
            // ارتباطات
            Relations.Add(new RelationItem("ورود کالا", "داروخانه", "موجودی به‌روز می‌شود"));
            Relations.Add(new RelationItem("ورود کالا", "حسابداری", "سند خرید صادر می‌شود"));
        }

        /// <summary>ارتباطاتِ این صفحه با دیگر صفحات</summary>
        public ObservableCollection<RelationItem> Relations { get; } = new();

        // ══════════════════════════════════════════════════════
        //  ویژگی‌های فرم — مقدارِ آغازین برابرِ دادهٔ نمونه است
        // ══════════════════════════════════════════════════════

        /// <summary>تأمین‌کننده — الزامی</summary>
        public string Supplier
        {
            get => _supplier;
            set
            {
                if (SetProperty(ref _supplier, value))
                {
                    MarkDirty();
                    Validate();
                }
            }
        }
        private string _supplier = "شرکت پخش البرز";

        /// <summary>شماره فاکتور — الزامی</summary>
        public string InvoiceNo
        {
            get => _invoiceNo;
            set
            {
                if (SetProperty(ref _invoiceNo, value))
                {
                    MarkDirty();
                    Validate();
                }
            }
        }
        private string _invoiceNo = "F-۹۸۷۶۵";

        /// <summary>تاریخ ورود — الزامی</summary>
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

        /// <summary>جمع فاکتور</summary>
        public decimal Total
        {
            get => _total;
            set
            {
                if (SetProperty(ref _total, value))
                {
                    MarkDirty();
                }
            }
        }
        private decimal _total = 12400000m;

        // ══════════════════════════════════════════════════════
        //  فرمان‌ها
        // ══════════════════════════════════════════════════════

        /// <summary>افزودن ردیف</summary>
        public IAsyncRelayCommand AddRowCommand =>
            new AsyncRelayCommand(AddRowAsync);

        /// <summary>اجرای «افزودن ردیف»</summary>
        private async Task AddRowAsync()
        {
            // TODO: پیوند به سرویسِ واقعی — «افزودن ردیف»
            await RunAsync(_ => Task.CompletedTask, "افزودن ردیف…");
        }

        /// <summary>
        /// اعتبارسنجیِ سمتِ کلاینت پیش از ذخیره
        /// </summary>
        protected override bool Validate()
        {
            var ok = true;

            if (string.IsNullOrWhiteSpace(Supplier))
            {
                AddError("تأمین‌کننده الزامی است", nameof(Supplier));
            }
            else
            {
                ClearErrors(nameof(Supplier));
            }

            if (string.IsNullOrWhiteSpace(InvoiceNo))
            {
                AddError("شماره فاکتور الزامی است", nameof(InvoiceNo));
            }
            else
            {
                ClearErrors(nameof(InvoiceNo));
            }

            if (Date is null)
            {
                AddError("تاریخ ورود الزامی است", nameof(Date));
            }
            else
            {
                ClearErrors(nameof(Date));
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