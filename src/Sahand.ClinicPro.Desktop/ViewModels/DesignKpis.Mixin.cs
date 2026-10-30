namespace Sahand.ClinicPro.Desktop.ViewModels;

// ═══════════════════════════════════════════════════════════════════════
// افزودن «پنج کارت آماری» به شش ViewModel صفحات بازطراحیشده — بدون دستزدن
// به منطق موجود. هر کلاس در فایل اصلیاش partial اعلام شده، بنابراین همین
// تکپراپرتی کافی است تا XAML بتواند به Kpis.V1 ... Kpis.V5 بایند شود.
// مقادیر از دادههای واقعی پر میشوند؛ حالت اسکرینشات مقادیر نمونهای طرح
// تأییدی را روی آنها مینشاند تا مقایسهٔ تصویری معنادار باشد.
// ═══════════════════════════════════════════════════════════════════════

public partial class DentistryViewModel
{
    /// <summary>اعداد پنج کارت آماری بالای صفحهٔ دندانپزشکی.</summary>
    public DesignKpis Kpis { get; } = new();
}

public partial class OphthalmologyViewModel
{
    /// <summary>اعداد پنج کارت آماری بالای صفحهٔ چشمپزشکی.</summary>
    public DesignKpis Kpis { get; } = new();
}

public partial class PhysiotherapyViewModel
{
    /// <summary>اعداد پنج کارت آماری بالای صفحهٔ فیزیوتراپی.</summary>
    public DesignKpis Kpis { get; } = new();
}

public partial class SurveyViewModel
{
    /// <summary>اعداد پنج کارت آماری بالای صفحهٔ نظرسنجی.</summary>
    public DesignKpis Kpis { get; } = new();
}

public partial class ComplementaryViewModel
{
    /// <summary>اعداد پنج کارت آماری بالای صفحهٔ قالب چاپ و طب مکمل.</summary>
    public DesignKpis Kpis { get; } = new();
}

public partial class SettingsViewModel
{
    /// <summary>اعداد پنج کارت آماری بالای بخش چاپگر و چاپ.</summary>
    public DesignKpis Kpis { get; } = new();
}
