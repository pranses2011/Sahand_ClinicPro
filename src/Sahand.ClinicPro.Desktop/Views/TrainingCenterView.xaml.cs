using System.Windows;
using System.Windows.Controls;
using Sahand.ClinicPro.Desktop.ViewModels;

namespace Sahand.ClinicPro.Desktop.Views;

/// <summary>
/// کد پشت مرکز آموزش — چرخه سیزدهم (درخواست ۸-۶):
/// نمایش HTML درس‌ها در WebBrowser داخلی راست‌به‌چپ
/// </summary>
public partial class TrainingCenterView : UserControl
{
    /// <summary>سازنده — اتصال نما به ViewModel پس از آماده‌سازی</summary>
    public TrainingCenterView()
    {
        InitializeComponent();
        DataContextChanged += (_, _) =>
        {
            if (DataContext is TrainingCenterViewModel vm)
            {
                vm.AttachView(this);
                if (vm.SelectedLesson is not null) ShowLesson(vm.SelectedLesson.Html);
            }
        };
    }

    /// <summary>نمایش محتوای HTML درس در نمایشگر داخلی</summary>
    public void ShowLesson(string html)
    {
        LessonBrowser.Visibility = Visibility.Visible;
        LessonBrowser.NavigateToString(html);
    }
}
