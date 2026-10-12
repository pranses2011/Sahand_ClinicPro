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
    /// ViewModel صفحه‌ی «تنظیمات» — تنظیمات سراسری برنامه
    /// </summary>
    /// <remarks>
    /// 🔗 وایرفریم مرجع:   <c>02-wireframes-md/SettingsView.md</c>
    /// 🖼 وایرفریم گرافیکی: <c>01-wireframes-html/SettingsView.html</c>
    /// 🎨 View متناظر:     <c>03-views/SettingsView.xaml</c>
    ///
    /// این کلاس هیچ رشته‌ی متنیِ نمایشی ندارد؛ همه‌ی متن‌ها از
    /// <c>Strings.resx</c> و از طریق افزونه‌ی <c>{loc:Loc}</c> تأمین می‌شوند.
    /// </remarks>
    public class SettingsViewModel : ViewModelBase
    {
        public SettingsViewModel(ILogger? logger = null) : base(logger)
        {
            _ = LoadAsync();
        }

        // ══════════════════════════════════════════════════════════════
        //  ویژگی‌ها
        // ══════════════════════════════════════════════════════════════




        // ══════════════════════════════════════════════════════════════
        //  فرمان‌ها
        // ══════════════════════════════════════════════════════════════

        private IAsyncRelayCommand? _resetCommand;

        /// <summary>بازگشت به پیش‌فرض</summary>
        public IAsyncRelayCommand ResetCommand =>
            _resetCommand ??= new AsyncRelayCommand(ResetAsync);

        // ══════════════════════════════════════════════════════════════
        //  رفتار
        // ══════════════════════════════════════════════════════════════

        /// <summary>
        /// بارگذاری داده‌های اولیه
        /// </summary>
        public async Task LoadAsync()
        {
            await RunAsync(async ct =>
            {
                // TODO: بارگذاری داده‌ها از سرویس مربوط
                await Task.Delay(200, ct);
            }, "در حال بارگذاری…");
        }

        // ══════════════════════════════════════════════════════════════
        //  اقدام‌ها
        // ══════════════════════════════════════════════════════════════

        /// <summary>
        /// بازگشت به پیش‌فرض
        /// </summary>
        private async Task resetAsync()
        {
            var ok = await RunAsync(async ct =>
            {
                await Task.Delay(150, ct);   // TODO: فراخوانی سرویس واقعی
            }, "بازگشت به پیش‌فرض");

            if (ok) ShowSuccess("عملیات با موفقیت انجام شد");
        }

    }
}
