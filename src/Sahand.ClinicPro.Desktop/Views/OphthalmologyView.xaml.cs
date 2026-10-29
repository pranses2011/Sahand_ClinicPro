using System.Windows;
using System.Windows.Controls;
using Sahand.ClinicPro.Desktop.Services;
using Sahand.ClinicPro.Desktop.ViewModels;

namespace Sahand.ClinicPro.Desktop.Views;

/// <summary>
/// کد پشت صفحهٔ مستقل «چشم‌پزشکی» — چرخهٔ چهل‌وسوم: تفکیک از پاراکلینیکِ تب‌دار
/// به صفحهٔ اختصاصی (گزارش مالک: «برای هر کدوم یک صفحه مستقل طراحی بکن»).
/// </summary>
public partial class OphthalmologyView : UserControl
{
    /// <summary>سازنده — مقداردهی اولیهٔ اجزا</summary>
    public OphthalmologyView()
    {
        InitializeComponent();
    }
    private void PrintGlassesPrescription_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not OphthalmologyViewModel vm || !vm.IsEyeExamEnabled || vm.EyePatient is not { } patient) return;
        if (!GlassesPrescriptionPrint.TryCreate(patient.Id, patient.FullName, patient.NationalCode,
            vm.OdSphere, vm.OdCylinder, vm.OdAxis, vm.OdAdd,
            vm.OsSphere, vm.OsCylinder, vm.OsAxis, vm.OsAdd, vm.Pd, out var data))
        {
            AppDialog.Warn("چاپ نسخه عینک", "حداقل یک مقدار عدسی معتبر وارد کنید و محور و فاصله مردمک را بررسی کنید؛ اعداد نباید هنگام ذخیره گرد شوند.");
            return;
        }
        var doc = GlassesPrescriptionPrint.BuildDocument(data!, IranClock.Now);
        PrintFlow.Print(doc, "پیش‌نویس نسخه عینک", PrintJobSettings.PrinterFor("GlassesPrescription"), jobKey: "GlassesPrescription");
    }

}
