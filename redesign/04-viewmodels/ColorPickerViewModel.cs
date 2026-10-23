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
    /// ViewModel صفحه‌ی «انتخاب رنگ» — انتخاب رنگ برای تنظیمات شخصی
    /// </summary>
    /// <remarks>
    /// 🔗 وایرفریم مرجع:   <c>02-wireframes-md/ColorPickerWindow.md</c>
    /// 🖼 وایرفریم گرافیکی: <c>01-wireframes-html/ColorPickerWindow.html</c>
    /// 🎨 View متناظر:     <c>03-views/ColorPickerWindow.xaml</c>
    ///
    /// این کلاس هیچ رشته‌ی متنیِ نمایشی ندارد؛ همه‌ی متن‌ها از
    /// <c>Strings.resx</c> و از طریق افزونه‌ی <c>{loc:Loc}</c> تأمین می‌شوند.
    /// </remarks>
    public class ColorPickerViewModel : FormViewModelBase
    {
        public ColorPickerViewModel(ILogger? logger = null) : base(logger)
        {
            _ = LoadAsync();
        }

        // ══════════════════════════════════════════════════════════════
        //  ویژگی‌ها
        // ══════════════════════════════════════════════════════════════




        // ══════════════════════════════════════════════════════════════
        //  فرمان‌ها
        // ══════════════════════════════════════════════════════════════

        private IAsyncRelayCommand? _selectCommand;

        /// <summary>انتخاب</summary>
        public IAsyncRelayCommand SelectCommand =>
            _selectCommand ??= new AsyncRelayCommand(SelectAsync);

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
            //   await _apiClient.PostAsync<ColorPickerDto>("api/v1/<built-in method lower of str object at 0x7f5384a951f0>s", payload, ct);

            await Task.Delay(200, ct);
        }

        // ══════════════════════════════════════════════════════════════
        //  اقدام‌ها
        // ══════════════════════════════════════════════════════════════

        /// <summary>
        /// انتخاب
        /// </summary>
        private async Task selectAsync()
        {
            var ok = await RunAsync(async ct =>
            {
                await Task.Delay(150, ct);   // TODO: فراخوانی سرویس واقعی
            }, "انتخاب");

            if (ok) ShowSuccess("عملیات با موفقیت انجام شد");
        }

    }
}
