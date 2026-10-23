// ═══════════════════════════════════════════════════════════════════════
//  LabResultViewModel
//  صفحه‌ی «ثبت نتیجه آزمایش» · گروه «آزمایشگاه» · کهن‌الگوی «form-lines»
//
//  متناظر با:  03-views/LabResultWindow.xaml
//  وایرفریم:    01-wireframes-html/LabResultWindow.html
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
    /// ViewModelِ صفحه‌ی «ثبت نتیجه آزمایش»
    /// </summary>
    /// <remarks>
    /// کهن‌الگوی طراحی: <c>form-lines</c> — چیدمان متناسب با همین قالب در View پیاده شده است.
    /// گروه: آزمایشگاه
    /// </remarks>
    public sealed class LabResultViewModel : FormViewModelBase
    {
        /// <summary>ایجاد ViewModel و بارگذاریِ دادهٔ نمونه</summary>
        /// <param name="logger">ثبت‌کننده‌ی رویدادها (اختیاری)</param>
        public LabResultViewModel(ILogger? logger = null) : base(logger)
        {
            // ارتباطات
            Relations.Add(new RelationItem("ثبت نتیجه آزمایش", "آزمایشگاه", "وضعیت به «آماده» تغییر می‌کند"));
            Relations.Add(new RelationItem("ثبت نتیجه آزمایش", "پرونده‌ی بیمار", "نتیجه در پرونده ثبت می‌شود"));
        }

        /// <summary>ارتباطاتِ این صفحه با دیگر صفحات</summary>
        public ObservableCollection<RelationItem> Relations { get; } = new();

        // ══════════════════════════════════════════════════════
        //  ویژگی‌های فرم — مقدارِ آغازین برابرِ دادهٔ نمونه است
        // ══════════════════════════════════════════════════════

        /// <summary>کد درخواست</summary>
        public string OrderCode
        {
            get => _orderCode;
            set
            {
                if (SetProperty(ref _orderCode, value))
                {
                    MarkDirty();
                }
            }
        }
        private string _orderCode = "L-۷۴۰";

        /// <summary>بیمار</summary>
        public string Patient
        {
            get => _patient;
            set
            {
                if (SetProperty(ref _patient, value))
                {
                    MarkDirty();
                }
            }
        }
        private string _patient = "سارا احمدی";

        /// <summary>تاریخ نمونه</summary>
        public DateTime? SampleDate
        {
            get => _sampleDate;
            set
            {
                if (SetProperty(ref _sampleDate, value))
                {
                    MarkDirty();
                }
            }
        }
        private DateTime? _sampleDate = null;

        /// <summary>تاریخ پاسخ</summary>
        public DateTime? ReportDate
        {
            get => _reportDate;
            set
            {
                if (SetProperty(ref _reportDate, value))
                {
                    MarkDirty();
                }
            }
        }
        private DateTime? _reportDate = null;

        // ══════════════════════════════════════════════════════
        //  فرمان‌ها
        // ══════════════════════════════════════════════════════

        /// <summary>تأیید نهایی</summary>
        public IAsyncRelayCommand ApproveCommand =>
            new AsyncRelayCommand(ApproveAsync);

        /// <summary>اجرای «تأیید نهایی»</summary>
        private async Task ApproveAsync()
        {
            // TODO: پیوند به سرویسِ واقعی — «تأیید نهایی»
            await RunAsync(_ => Task.CompletedTask, "تأیید نهایی…");
        }

        /// <summary>ارسال پیامک</summary>
        public IAsyncRelayCommand SendSmsCommand =>
            new AsyncRelayCommand(SendSmsAsync);

        /// <summary>اجرای «ارسال پیامک»</summary>
        private async Task SendSmsAsync()
        {
            // TODO: پیوند به سرویسِ واقعی — «ارسال پیامک»
            await RunAsync(_ => Task.CompletedTask, "ارسال پیامک…");
        }

        /// <summary>
        /// اعتبارسنجیِ سمتِ کلاینت پیش از ذخیره
        /// </summary>
        protected override bool Validate()
        {
            var ok = true;

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