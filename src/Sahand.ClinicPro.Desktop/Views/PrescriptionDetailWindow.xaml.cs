using System.Windows;
using Sahand.ClinicPro.Desktop.Services;
using Sahand.ClinicPro.Desktop.ViewModels;

namespace Sahand.ClinicPro.Desktop.Views;

/// <summary>
/// کد پشت جزئیات نسخه — چرخه بیست‌وسوم (گزارش شانزدهم مالک):
/// «در صفحه جزئیات نسخه، دکمه چاپ مجدد هم باشد» — دکمهٔ چاپ مجدد افزوده شد؛
/// سند چاپ از همان داده‌های بارگذاری‌شدهٔ جزئیات (بیمار/پزشک/تاریخ/اقلام)
/// ساخته و پیش‌نمایش چاپ با دکمهٔ ارسال به پرینتر باز می‌شود.
/// </summary>
public partial class PrescriptionDetailWindow : Window
{
    /// <summary>ViewModel جزئیات — برای ساخت سند چاپ نگه داشته می‌شود</summary>
    private readonly PrescriptionDetailViewModel _vm;

    /// <summary>سازنده با ViewModel جزئیات</summary>
    public PrescriptionDetailWindow(PrescriptionDetailViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        _vm = viewModel;
        viewModel.CloseRequested += (_, _) => Close();
    }

    /// <summary>دکمه بستن</summary>
    private void OnCloseClick(object sender, RoutedEventArgs e) => Close();

    /// <summary>
    /// دکمهٔ چاپ مجدد — چرخه بیست‌وسوم: سند نسخه از داده‌های جزئیات ساخته و
    /// پنجرهٔ پیش‌نمایش چاپ (با دکمهٔ «چاپ» و ارسال مستقیم به پرینتر) باز می‌شود.
    /// خطای احتمالی با دیالوگ فارسی برنامه نمایش داده می‌شود (قانون مالک).
    /// </summary>
    private void OnReprintClick(object sender, RoutedEventArgs e)
    {
        try
        {
            // اقلام به قالب هلپر چاپ نگاشت می‌شوند:
            //   نام دارو | زیرعنوان (واحد) | مصرف (دوز × تکرار × مدت) | مسیر | توضیح
            // (مسیر در دادهٔ جزئیات ذخیره نمی‌شود — ستون خالی برای یادداشت دستی)
            var items = _vm.Items.Select(i => (
                i.MedicationName ?? string.Empty,
                (string?)(string.IsNullOrWhiteSpace(i.Unit) ? null : i.Unit),
                $"{i.Dosage ?? ""} × {i.Frequency ?? ""} × {i.Duration ?? ""}".Trim(),
                (string?)"",
                (string?)(i.Instructions ?? string.Empty))).ToList();

            // چرخه ۲۳: طرح چاپ از آخرین انتخاب کاربر شروع می‌شود و در پیش‌نمایش
            // با انتخابگر «طرح چاپ» زنده قابل تعویض است (کلاسیک/مدرن/فشرده)
            // ═══ چرخهٔ بیست‌وششم (گزارش هجدهم مالک) ═══ «در چاپ نسخه به جای کد
            // ملی بیمار، کد پیگیری می‌اندازد» — پیش‌تر کد ملی در جزئیات نمی‌آمد
            // و کد نسخه جایش می‌نشست؛ اکنون کد ملی واقعی بیمار در سربرگ چاپ
            // می‌نشیند و کد نسخه فقط در جای کد رهگیری می‌ماند
            Services.PrescriptionPrintHelper.ShowPreview(
                style => PrescriptionPrintHelper.BuildDocument(
                    _vm.PatientName,
                    _vm.PatientNationalCode ?? "—",
                    _vm.DoctorName,
                    _vm.DateShamsi,
                    _vm.PrescriptionCode,
                    items,
                    style),
                $"چاپ مجدد — {_vm.Header}");
        }
        catch (Exception ex)
        {
            // دیالوگ فارسی خود برنامه به‌جای MessageBox ویندوز (قانون مالک)
            AppDialog.Error("خطای چاپ مجدد", $"آماده‌سازی سند چاپ ناموفق بود:{Environment.NewLine}{ex.Message}");
        }
    }
}
