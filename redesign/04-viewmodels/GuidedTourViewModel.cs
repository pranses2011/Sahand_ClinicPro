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
    /// ViewModel صفحه‌ی «راهنمای تعاملی» — آموزش گام‌به‌گام محیط برنامه
    /// </summary>
    /// <remarks>
    /// 🔗 وایرفریم مرجع:   <c>02-wireframes-md/GuidedTourWindow.md</c>
    /// 🖼 وایرفریم گرافیکی: <c>01-wireframes-html/GuidedTourWindow.html</c>
    /// 🎨 View متناظر:     <c>03-views/GuidedTourWindow.xaml</c>
    ///
    /// این کلاس هیچ رشته‌ی متنیِ نمایشی ندارد؛ همه‌ی متن‌ها از
    /// <c>Strings.resx</c> و از طریق افزونه‌ی <c>{loc:Loc}</c> تأمین می‌شوند.
    /// </remarks>
    public class GuidedTourViewModel : FormViewModelBase
    {
        public GuidedTourViewModel(ILogger? logger = null) : base(logger)
        {
            _ = LoadAsync();
        }

        // ══════════════════════════════════════════════════════════════
        //  ویژگی‌ها
        // ══════════════════════════════════════════════════════════════




        // ══════════════════════════════════════════════════════════════
        //  فرمان‌ها
        // ══════════════════════════════════════════════════════════════

        private IAsyncRelayCommand? _nextCommand;

        /// <summary>بعدی</summary>
        public IAsyncRelayCommand NextCommand =>
            _nextCommand ??= new AsyncRelayCommand(NextAsync);

        private IAsyncRelayCommand? _skipCommand;

        /// <summary>رد کردن</summary>
        public IAsyncRelayCommand SkipCommand =>
            _skipCommand ??= new AsyncRelayCommand(SkipAsync);

        // ══════════════════════════════════════════════════════════════
        //  رفتار
        // ══════════════════════════════════════════════════════════════

        /// <inheritdoc />
        protected override bool Validate()
        {
            ClearAllErrors();
            return true;
        }

        /// <inheritdoc />
        protected override async Task PersistAsync(CancellationToken ct)
        {
            // TODO: ارسال داده‌ها به سرور؛ نمونه:
            //   var payload = new { ... };
            //   await _apiClient.PostAsync<GuidedTourDto>("api/v1/<built-in method lower of str object at 0x7f5384a94ef0>s", payload, ct);

            await Task.Delay(200, ct);
        }

        // ══════════════════════════════════════════════════════════════
        //  اقدام‌ها
        // ══════════════════════════════════════════════════════════════

        /// <summary>
        /// بعدی
        /// </summary>
        private async Task nextAsync()
        {
            var ok = await RunAsync(async ct =>
            {
                await Task.Delay(150, ct);   // TODO: فراخوانی سرویس واقعی
            }, "بعدی");

            if (ok) ShowSuccess("عملیات با موفقیت انجام شد");
        }

        /// <summary>
        /// رد کردن
        /// </summary>
        private async Task skipAsync()
        {
            var ok = await RunAsync(async ct =>
            {
                await Task.Delay(150, ct);   // TODO: فراخوانی سرویس واقعی
            }, "رد کردن");

            if (ok) ShowSuccess("عملیات با موفقیت انجام شد");
        }

    }
}
