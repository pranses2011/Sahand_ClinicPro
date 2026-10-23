// ═══════════════════════════════════════════════════════════════════════
//  PhotoViewerViewModel
//  صفحه‌ی «نمایش تصویر» · گروه «بیماران» · کهن‌الگوی «viewer»
//
//  متناظر با:  03-views/PhotoViewerWindow.xaml
//  وایرفریم:    01-wireframes-html/PhotoViewerWindow.html
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
    /// ViewModelِ صفحه‌ی «نمایش تصویر»
    /// </summary>
    /// <remarks>
    /// ریل ابزارِ عمودی (بزرگ‌نمایی، چرخش، آینه، تنظیم نور، برش، ذخیره) و
    /// پس‌زمینه تیره برای دید بهتر.
    ///
    /// کهن‌الگوی طراحی: <c>viewer</c> — چیدمان متناسب با همین قالب در View پیاده شده است.
    /// گروه: بیماران
    /// </remarks>
    public sealed class PhotoViewerViewModel : FormViewModelBase
    {
        /// <summary>ایجاد ViewModel و بارگذاریِ دادهٔ نمونه</summary>
        /// <param name="logger">ثبت‌کننده‌ی رویدادها (اختیاری)</param>
        public PhotoViewerViewModel(ILogger? logger = null) : base(logger)
        {
        }

        /// <summary>تصویرِ در حال نمایش</summary>
        public ImageSource? CurrentImage
{
            get => _currentImage;
            set => SetProperty(ref _currentImage, value);
        }
        private ImageSource? _currentImage;

        /// <summary>بزرگ‌نمایی</summary>
        public IRelayCommand ZoomInCommand => new RelayCommand(ZoomIn);

        /// <summary>کوچک‌نمایی</summary>
        public IRelayCommand ZoomOutCommand => new RelayCommand(ZoomOut);

        /// <summary>بزرگ‌نماییِ تصویر</summary>
        private void ZoomIn() => ShowSuccess("بزرگ‌نمایی");

        /// <summary>کوچک‌نماییِ تصویر</summary>
        private void ZoomOut() => ShowSuccess("کوچک‌نمایی");

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