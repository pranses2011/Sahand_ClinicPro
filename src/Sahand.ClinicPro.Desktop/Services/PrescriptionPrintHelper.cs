using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using Sahand.ClinicPro.Application.Common.Helpers;
using Sahand.ClinicPro.Desktop.Helpers;

namespace Sahand.ClinicPro.Desktop.Services;

/// <summary>
/// سازندهٔ سند چاپ نسخهٔ دارویی — چرخه بیست‌ویکم (گزارش پانزدهم مالک:
/// «دکمه چاپ کار نمی‌کند»). سند FlowDocument راست‌چین فارسی با سربرگ
/// درمانگاه، مشخصات بیمار/پزشک، جدول اقلام و محل امضا می‌سازد.
/// ═══ چرخه بیست‌وسوم (گزارش شانزدهم مالک) ═══
/// «صفحه چاپ نسخه دکمه‌ای برای ارسال به پرینتر ندارد و قالب‌بندی بهم‌ریخته است»:
///   • FlowDocumentPageViewer پیش‌تر هیچ دکمهٔ چاپی نداشت (فقط زوم) → پنجرهٔ
///     اختصاصی با نوار ابزار فارسی شامل دکمهٔ بزرگ «چاپ» ساخته شد که
///     PrintDialog ویندوز را باز می‌کند و سند را مستقیم به پرینتر می‌فرستد.
///   • ریشهٔ بهم‌ریختگی: «\n» داخل Run در FlowDocument خط جدید نمی‌سازد
///     (نادیده گرفته می‌شود) → عنصر LineBreak استاندارد به‌کار رفت؛ سلول‌های
///     جدول خط‌کشی سراسری گرفتند؛ شماره ردیف‌ها و تاریخ با ارقام فارسی
///     (PersianDateHelper.n) شد؛ ستون «توضیح» جدا شد تا متن‌های طولانی
///     چندسطری و خوانا بمانند.
/// </summary>
public static class PrescriptionPrintHelper
{
    /// <summary>رنگ سازمانی سربرگ — هماهنگ با تم آبی محصول</summary>
    private static readonly SolidColorBrush HeaderBrush = new(Color.FromRgb(0x15, 0x65, 0xC0));

    /// <summary>رنگ متن کم‌رنگ راهنما</summary>
    private static readonly SolidColorBrush MutedBrush = new(Color.FromRgb(0x60, 0x64, 0x68));

    /// <summary>قلم چاپ — وزیرمتن جاسازی‌شدهٔ برنامه (چرخه بیست‌وچهارم:
/// پیش‌تر Tahoma هاردکد بود و ظاهر چاپ با ظاهر برنامه نمی‌خواند؛ کارخانهٔ
/// FontAssets با الگوی صحیح pack-URI فونت جاسازی‌شده را برمی‌گرداند)</summary>
    private static FontFamily PrintFontFamily => FontAssets.PrintFont;

    /// <summary>
    /// طرح چاپ نسخه — چرخه بیست‌وسوم (گزارش شانزدهم مالک): «برای همهٔ موارد
    /// قابل چاپ چندین طرح چاپ اولیه طراحی کن تا از بین آن‌ها انتخاب کنم».
    /// سه طرح اولیه: کلاسیک (جدول رسمی)، مدرن (نوار رنگی + کادرها) و فشرده
    /// (فهرست دوخطی دارویی — سبک نسخه‌های کوتاه). انتخاب طرح در پیش‌نمایش
    /// زنده عوض می‌شود و انتخاب آخر کاربر یادآوری می‌گردد.
    /// </summary>
    public enum PrescriptionPrintStyle
    {
        /// <summary>جدول رسمی با سربرگ وسط‌چین — طرح پیش‌فرض</summary>
        Classic = 0,
        /// <summary>نوار رنگی بالا + کادر مشخصات + جدول رنگی</summary>
        Modern = 1,
        /// <summary>فهرست شماره‌دار فشرده — هر دارو در یک-دو خط</summary>
        Compact = 2
    }

    /// <summary>نام فارسی طرح چاپ برای نمایش در انتخابگر</summary>
    public static string StyleNameFa(PrescriptionPrintStyle style) => style switch
    {
        PrescriptionPrintStyle.Modern => "مدرن",
        PrescriptionPrintStyle.Compact => "فشرده",
        _ => "کلاسیک"
    };

    /// <summary>طرح آخرین انتخاب کاربر — ذخیرهٔ محلی برای اجرای بعدی</summary>
    public static PrescriptionPrintStyle SavedStyle
    {
        get => Enum.TryParse<PrescriptionPrintStyle>(
            LocalSettingsStore.Get("PrescriptionPrintStyle"), out var s) ? s : PrescriptionPrintStyle.Classic;
        set => LocalSettingsStore.Set("PrescriptionPrintStyle", value.ToString());
    }

    /// <summary>
    /// ساخت سند نسخه — اقلام به‌صورت (نام، زیرعنوان، مصرف، مسیر، توضیح)
    /// </summary>
    public static FlowDocument BuildDocument(
        string patientName,
        string patientNationalCode,
        string doctorName,
        string dateShamsi,
        string? trackingCode,
        IReadOnlyList<(string Name, string? Subtitle, string Usage, string? Route, string? Instructions)> items,
        PrescriptionPrintStyle style = PrescriptionPrintStyle.Classic)
    {
        var doc = new FlowDocument
        {
            // راست‌چین و قلم فارسی — فونت وزیرمتن جاسازی‌شدهٔ برنامه (چرخه ۲۴)
            FlowDirection = FlowDirection.RightToLeft,
            FontFamily = PrintFontFamily,
            FontSize = 12,
            PagePadding = new Thickness(40),
            ColumnWidth = double.PositiveInfinity
        };

        // ═══ سربرگ — سه طرح چرخه ۲۳ ═══
        if (style == PrescriptionPrintStyle.Modern)
        {
            // ─── طرح مدرن: نوار رنگی تمام‌عرض با عنوان سفید + مشخصات داخل کادر ───
            var band = new Table { CellSpacing = 0, Margin = new Thickness(0, 0, 0, 10) };
            band.Columns.Add(new TableColumn { Width = new GridLength(1, GridUnitType.Star) });
            band.RowGroups.Add(new TableRowGroup());
            var bandRow = new TableRow();
            var bandCell = new TableCell
            {
                Background = HeaderBrush,
                Padding = new Thickness(14, 10, 14, 10),
                BorderThickness = new Thickness(0)
            };
            var bandPar = new Paragraph { TextAlignment = TextAlignment.Center, Margin = new Thickness(0) };
            bandPar.Inlines.Add(new Run("🩺 نسخهٔ دارویی درمانگاه سهند")
            {
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White
            });
            bandPar.Inlines.Add(new LineBreak());
            bandPar.Inlines.Add(new Run($"تاریخ: {PersianDateHelper.ToPersianDigits(dateShamsi)}" +
                (string.IsNullOrWhiteSpace(trackingCode)
                    ? ""
                    : $"    |    کد رهگیری: {PersianDateHelper.ToPersianDigits(trackingCode!)}"))
            {
                FontSize = 10.5,
                Foreground = new SolidColorBrush(Color.FromRgb(0xDC, 0xE9, 0xFA))
            });
            bandCell.Blocks.Add(bandPar);
            bandRow.Cells.Add(bandCell);
            band.RowGroups[0].Rows.Add(bandRow);
            doc.Blocks.Add(band);

            // کادر مشخصات — پس‌زمینهٔ آبی بسیار روشن
            var infoBox = new Table { CellSpacing = 0, Margin = new Thickness(0, 0, 0, 12) };
            infoBox.Columns.Add(new TableColumn { Width = new GridLength(1, GridUnitType.Star) });
            infoBox.RowGroups.Add(new TableRowGroup());
            var infoRow = new TableRow();
            var infoCell = new TableCell
            {
                Background = new SolidColorBrush(Color.FromRgb(0xF0, 0xF6, 0xFC)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(0xC5, 0xD9, 0xF2)),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(10, 6, 10, 6)
            };
            var infoPar = new Paragraph { Margin = new Thickness(0) };
            infoPar.Inlines.Add(new Run("بیمار: ") { FontWeight = FontWeights.Bold, Foreground = HeaderBrush });
            infoPar.Inlines.Add(new Run($"{patientName}      "));
            infoPar.Inlines.Add(new Run("کد ملی: ") { FontWeight = FontWeights.Bold, Foreground = HeaderBrush });
            infoPar.Inlines.Add(new Run($"{PersianDateHelper.ToPersianDigits(patientNationalCode)}      "));
            infoPar.Inlines.Add(new Run("پزشک: ") { FontWeight = FontWeights.Bold, Foreground = HeaderBrush });
            infoPar.Inlines.Add(new Run(doctorName));
            infoCell.Blocks.Add(infoPar);
            infoRow.Cells.Add(infoCell);
            infoBox.RowGroups[0].Rows.Add(infoRow);
            doc.Blocks.Add(infoBox);
        }
        else if (style == PrescriptionPrintStyle.Compact)
        {
            // ─── طرح فشرده: عنوان یک‌خطی + خط جداکننده + فهرست داروها ───
            var compactHeader = new Paragraph
            {
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 0, 0, 4),
                BorderBrush = HeaderBrush,
                BorderThickness = new Thickness(0, 0, 0, 1.4),
                Padding = new Thickness(0, 0, 0, 4)
            };
            compactHeader.Inlines.Add(new Run("نسخهٔ دارویی")
            {
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Foreground = HeaderBrush
            });
            compactHeader.Inlines.Add(new Run($"     ({PersianDateHelper.ToPersianDigits(dateShamsi)})")
            {
                FontSize = 11,
                Foreground = MutedBrush
            });
            doc.Blocks.Add(compactHeader);

            var compactInfo = new Paragraph
            {
                Margin = new Thickness(0, 4, 0, 8),
                FontSize = 10.5
            };
            compactInfo.Inlines.Add(new Run($"بیمار: {patientName}  |  کد ملی: {PersianDateHelper.ToPersianDigits(patientNationalCode)}  |  پزشک: {doctorName}")
            {
                Foreground = MutedBrush
            });
            doc.Blocks.Add(compactInfo);

            // فهرست شماره‌دار فشرده — هر دارو یک-دو خط (بدون جدول)
            for (var i = 0; i < items.Count; i++)
            {
                var it = items[i];
                var p = new Paragraph { Margin = new Thickness(0, 0, 0, 5) };
                p.Inlines.Add(new Run($"{PersianDateHelper.ToPersianDigits((i + 1).ToString())}. ")
                {
                    FontWeight = FontWeights.Bold,
                    Foreground = HeaderBrush
                });
                p.Inlines.Add(new Run(it.Name) { FontWeight = FontWeights.Bold });
                if (!string.IsNullOrWhiteSpace(it.Subtitle))
                    p.Inlines.Add(new Run($"  ({it.Subtitle})") { FontSize = 10, Foreground = MutedBrush });
                p.Inlines.Add(new Run($"  —  {it.Usage}"));
                if (!string.IsNullOrWhiteSpace(it.Route))
                    p.Inlines.Add(new Run($"  —  {it.Route}") { Foreground = MutedBrush });
                if (!string.IsNullOrWhiteSpace(it.Instructions))
                    p.Inlines.Add(new Run($"  ({it.Instructions})") { FontSize = 10, Foreground = MutedBrush });
                doc.Blocks.Add(p);
            }
        }
        else
        {
        // ─── طرح کلاسیک: همان سربرگ وسط‌چین رسمی ───
        var header = new Paragraph
        {
            TextAlignment = TextAlignment.Center,
            Margin = new Thickness(0, 0, 0, 2)
        };
        header.Inlines.Add(new Run("نسخهٔ دارویی")
        {
            FontSize = 20,
            FontWeight = FontWeights.Bold,
            Foreground = HeaderBrush
        });
        doc.Blocks.Add(header);

        var sub = new Paragraph { TextAlignment = TextAlignment.Center, Margin = new Thickness(0, 0, 0, 10) };
        // تاریخ و کد رهگیری با ارقام فارسی (چرخه ۲۳ — همهٔ اعداد برنامه فارسی)
        sub.Inlines.Add(new Run($"تاریخ: {PersianDateHelper.ToPersianDigits(dateShamsi)}")
        {
            FontSize = 11,
            Foreground = MutedBrush
        });
        if (!string.IsNullOrWhiteSpace(trackingCode))
            sub.Inlines.Add(new Run($"    |    کد رهگیری الکترونیک: {PersianDateHelper.ToPersianDigits(trackingCode)}")
            {
                FontSize = 11,
                Foreground = MutedBrush
            });
        doc.Blocks.Add(sub);

        // ═══ مشخصات بیمار و پزشک ═══
        var info = new Paragraph { Margin = new Thickness(0, 0, 0, 10) };
        info.Inlines.Add(new Run("بیمار: ") { FontWeight = FontWeights.Bold });
        info.Inlines.Add(new Run($"{patientName}   "));
        info.Inlines.Add(new Run("کد ملی: ") { FontWeight = FontWeights.Bold });
        info.Inlines.Add(new Run($"{PersianDateHelper.ToPersianDigits(patientNationalCode)}   "));
        info.Inlines.Add(new Run("پزشک: ") { FontWeight = FontWeights.Bold });
        info.Inlines.Add(new Run(doctorName));
        doc.Blocks.Add(info);
        }

        // ═══ جدول اقلام — فقط برای طرح‌های کلاسیک و مدرن (فشرده بالا ساخته شد) ═══
        if (style != PrescriptionPrintStyle.Compact)
        {
        var border = new Thickness(0.5);
        var borderBrush = Brushes.Gray;
        var table = new Table
        {
            CellSpacing = 0,
            BorderBrush = borderBrush,
            BorderThickness = border,
            Margin = new Thickness(0, 0, 0, 14)
        };
        // ═══ چرخه بیست‌وچهارم (گزارش هفدهم مالک) — رفع «فیلد ردیف کل عرض صفحه
        // را می‌گیرد» ═══ ستون‌های ستاره‌ای (GridUnitType.Star) در جدول‌های
        // FlowDocumentِ راست‌به‌چپ باگ شناخته‌شدهٔ WPF دارند: توزیع عرض به‌درستی
        // انجام نمی‌شود و ستون اول (شمارهٔ ردیف با عرض ثابت) تمام عرض را می‌گیرد
        // و بقیهٔ ستون‌ها له می‌شوند — «قالب‌بندی بهم‌ریخته» گزارش همین بود.
        // راه‌حل قطعی: عرض‌های «پیکسل ثابت» بر اساس عرض قابل چاپ A4
        // (۷۹۳.۷ − ۲×۴۰ حاشیه = ~۷۱۳ پیکسل). مجموع دقیقاً ۷۱۳ است.
        table.Columns.Add(new TableColumn { Width = new GridLength(34) });   // ردیف
        table.Columns.Add(new TableColumn { Width = new GridLength(232) });  // دارو
        table.Columns.Add(new TableColumn { Width = new GridLength(206) });  // مصرف
        table.Columns.Add(new TableColumn { Width = new GridLength(96) });   // مسیر
        table.Columns.Add(new TableColumn { Width = new GridLength(145) });  // توضیح
        table.RowGroups.Add(new TableRowGroup());

        // سرستون‌ها — با خط‌کشی سلول (چرخه ۲۳)
        var head = new TableRow { Background = new SolidColorBrush(Color.FromRgb(0xE3, 0xF2, 0xFD)) };
        foreach (var title in new[] { "#", "دارو", "مصرف", "مسیر", "توضیح" })
        {
            var c = new TableCell(new Paragraph(new Run(title)
            {
                FontWeight = FontWeights.Bold
            }))
            {
                BorderBrush = borderBrush,
                BorderThickness = border,
                Padding = new Thickness(4, 3, 4, 3)
            };
            head.Cells.Add(c);
        }
        table.RowGroups[0].Rows.Add(head);

        // ردیف اقلام
        for (var i = 0; i < items.Count; i++)
        {
            var it = items[i];
            var row = new TableRow();

            // شمارهٔ ردیف — ارقام فارسی (چرخه ۲۳)
            row.Cells.Add(MakeCell(new Paragraph(new Run(PersianDateHelper.ToPersianDigits((i + 1).ToString())))
            {
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0)
            }, border, borderBrush));

            // نام دارو + زیرعنوان (فرم/دوز/سازنده) — چرخه ۲۳: LineBreak واقعی
            // به‌جای «\n» که در FlowDocument نادیده گرفته می‌شد و باعث چسبیدن
            // زیرعنوان به نام دارو (بهم‌ریختگی) می‌شد
            var medPar = new Paragraph(new Run(it.Name)) { Margin = new Thickness(0) };
            if (!string.IsNullOrWhiteSpace(it.Subtitle))
            {
                medPar.Inlines.Add(new LineBreak());
                medPar.Inlines.Add(new Run(it.Subtitle)
                {
                    FontSize = 10,
                    Foreground = MutedBrush
                });
            }
            row.Cells.Add(MakeCell(medPar, border, borderBrush));

            // مصرف (دوز + بازه) — چندسطری با LineBreak
            var usagePar = new Paragraph(new Run(it.Usage)) { Margin = new Thickness(0) };
            if (!string.IsNullOrWhiteSpace(it.Instructions))
            {
                usagePar.Inlines.Add(new LineBreak());
                usagePar.Inlines.Add(new Run($"({it.Instructions})")
                {
                    FontSize = 10,
                    Foreground = MutedBrush
                });
            }
            row.Cells.Add(MakeCell(usagePar, border, borderBrush));

            row.Cells.Add(MakeCell(new Paragraph(new Run(it.Route)) { Margin = new Thickness(0) }, border, borderBrush));

            // ستون توضیح خالی برای یادداشت دستی داروخانه/بیمار
            row.Cells.Add(MakeCell(new Paragraph(new Run(string.Empty)) { Margin = new Thickness(0) }, border, borderBrush));

            // یک‌درمیان رنگ پس‌زمینهٔ ردیف‌ها برای خوانایی (چرخه ۲۳)
            if (i % 2 == 1)
                row.Background = new SolidColorBrush(Color.FromRgb(0xF7, 0xFA, 0xFD));

            table.RowGroups[0].Rows.Add(row);
        }
        doc.Blocks.Add(table);
        } // پایان بلوک طرح‌های جدول‌دار (کلاسیک/مدرن)

        // ═══ پانویس و امضا ═══
        var footer = new Paragraph { Margin = new Thickness(0, 18, 0, 0) };
        footer.Inlines.Add(new Run("مهر و امضای پزشک: ")
        {
            Foreground = MutedBrush,
            FontSize = 11
        });
        footer.Inlines.Add(new Run("......................................") { Foreground = MutedBrush });
        doc.Blocks.Add(footer);

        var note = new Paragraph { Margin = new Thickness(0, 8, 0, 0) };
        note.Inlines.Add(new Run("این نسخه توسط نرم‌افزار مدیریت درمانگاه سهند صادر شده است.")
        {
            FontSize = 9.5,
            Foreground = MutedBrush,
            FontStyle = FontStyles.Italic
        });
        doc.Blocks.Add(note);

        return doc;
    }

    /// <summary>
    /// ساخت سلول جدول با خط‌کشی و فاصلهٔ داخلی یکسان — چرخه ۲۳:
    /// پیش‌تر سلول‌ها Border نداشتند و جدول روی کاغذ ناخوانا/بهم‌ریخته دیده می‌شد.
    /// </summary>
    private static TableCell MakeCell(Paragraph content, Thickness border, Brush borderBrush) =>
        new(content)
        {
            BorderBrush = borderBrush,
            BorderThickness = border,
            Padding = new Thickness(4, 3, 4, 3)
        };

    /// <summary>
    /// باز کردن پنجرهٔ پیش‌نمایش چاپ — چرخه بیست‌وسوم (گزارش شانزدهم مالک):
    /// «صفحه چاپ دکمه‌ای برای ارسال به پرینتر ندارد» — FlowDocumentPageViewer
    /// هیچ دکمهٔ چاپی ندارد؛ پنجرهٔ اختصاصی با نوار ابزار فارسی ساخته شد:
    ///   • دکمهٔ بزرگ «چاپ» → PrintDialog ویندوز (انتخاب پرینتر + تنظیمات) و
    ///     ارسال مستقیم سند به پرینتر
    ///   • زوم +/− و نمایش درصد زوم + دکمهٔ بستن — همهٔ برچسب‌ها فارسی
    ///   • انتخابگر «طرح چاپ» (کلاسیک/مدرن/فشرده) با بازسازی زندهٔ سند
    /// </summary>
    /// <summary>
    /// پیش‌نمایش با سند از پیش ساخته‌شده — سازگاری با فراخوانی‌های قدیمی.
    /// </summary>
    public static void ShowPreview(FlowDocument document, string title)
        => ShowPreview(_ => document, title, document);

    /// <summary>
    /// پیش‌نمایش با سازندهٔ طرح‌دار — چرخه بیست‌وسوم (گزارش شانزدهم مالک):
    /// «چندین طرح چاپ اولیه طراحی کن تا از بین آن‌ها انتخاب کنم» — نوار ابزار
    /// «طرح چاپ» دارد و با تعویضش سند دوباره با طرح منتخب ساخته می‌شود؛ طرح
    /// انتخاب‌شده در ذخیرهٔ محلی می‌ماند تا دفعهٔ بعد همان طرح باز شود.
    /// </summary>
    /// <param name="buildByStyle">سازندهٔ سند بر اساس طرح</param>
    /// <param name="title">عنوان پنجره</param>
    /// <param name="initialDocument">سند اولیه (بهینه‌سازی — بازسازی فقط هنگام تعویض طرح)</param>
    public static void ShowPreview(
        Func<PrescriptionPrintStyle, FlowDocument> buildByStyle,
        string title,
        FlowDocument? initialDocument = null)
    {
        // طرح اولیه = آخرین انتخاب کاربر
        var currentStyle = SavedStyle;
        var document = initialDocument ?? buildByStyle(currentStyle);
        document.PageWidth = 793.7;   // A4 - 210mm با ۹۶dpi
        document.PageHeight = 1122.5; // A4 - 297mm

        // ─── نمایشگر سند ───
        // ═══ چرخهٔ بیست‌وششم (گزارش هجدهم مالک) — حذف نوار ابزار داخلی نمایشگر ═══
        // «دکمه‌های زوم بالای صفحه برعکس عمل می‌کنند و نوشتهٔ + و − دیده نمی‌شود؛
        // اسلایدر زوم پایین هم کار نمی‌کند»: FlowDocumentPageViewer نوار ابزار
        // داخلی خودش (دکمه‌های زوم + اسلایدر + درصد) را هم می‌کشد؛ آن زوم با
        // «زوم مقیاسی اختصاصی برنامه» تداخل دارد، در پنجرهٔ راست‌چین رفتار
        // معکوس می‌دهد و نویسهٔ +/− قالبش هم با فونت فارسی رندر نمی‌شود.
        // قالب مینیمال فقط «صفحهٔ سند» می‌گذارد و تنها زوم فارسی خود برنامه
        // می‌ماند — رفتار یکدست و بدون تعارض.
        var viewer = new FlowDocumentPageViewer
        {
            Document = document,
            MinZoom = 50,
            MaxZoom = 300
        };
        // ═══ قالب مینیمال: فقط «صفحهٔ سند» (PART_ContentHost) بدون نوار ابزار ═══
        // FlowDocumentPageViewer هنگام اعمال قالب، سند را خودش به قسمتِ
        // نام‌گذاری‌شدهٔ PART_ContentHost (از نوع DocumentPageView) وصل می‌کند
        // — الگوی رسمی قالب این کنترل در WPF؛ پس نیازی به اتصال دستی نیست
        // ═══ چرخهٔ سی‌وسوم (گزارش نوزدهم مالک) — «دکمه‌های زوم درست کار نمی‌کنند» ═══
        // قالب مینیمال قبلی DocumentPageView را «برهنه» می‌گذاشت؛ قالب پیش‌فرض
        // این کنترل، صفحه را داخل یک ScrollViewer می‌چیند — بدون آن، هر زومی
        // بزرگ‌تر از ۱۰۰٪ صفحه را از لبهٔ پنجره بیرون می‌برد و بدون نوار پیمایش
        // هیچ راهی برای دیدن لبه‌ها نیست (همان «درست کار نمی‌کند»). اکنون همان
        // چیدمان رسمی بازگردانده شد: ScrollViewer (پیمایش خودکار افقی/عمودی)
        // دور PART_ContentHost — زوم بزرگ = نوار پیمایش تازه؛ زوم کوچک = بدون نوار.
        var bareTemplate = new ControlTemplate(typeof(FlowDocumentPageViewer));
        var scrollFactory = new FrameworkElementFactory(typeof(ScrollViewer));
        // نوارهای پیمایش فقط در نیاز ظاهر می‌شوند (Auto) — در ۱۰۰٪ هیچ نوری نیست
        scrollFactory.SetValue(ScrollViewer.HorizontalScrollBarVisibilityProperty, ScrollBarVisibility.Auto);
        scrollFactory.SetValue(ScrollViewer.VerticalScrollBarVisibilityProperty, ScrollBarVisibility.Auto);
        var pageViewFactory = new FrameworkElementFactory(typeof(DocumentPageView), "PART_ContentHost");
        // سند راست‌چین است؛ خود صفحه همیشه چپ‌به‌راست رندر می‌شود (الگوی قالب
        // پیش‌فرض ویندوز) تا آینه‌ای شدن ناخواسته رخ ندهد
        pageViewFactory.SetValue(FrameworkElement.FlowDirectionProperty, FlowDirection.LeftToRight);
        scrollFactory.AppendChild(pageViewFactory);
        bareTemplate.VisualTree = scrollFactory;
        viewer.Template = bareTemplate;

        // ═══ چرخه بیست‌وچهارم (گزارش هفدهم مالک) — زوم واقعاً کار کند ═══
        // «دکمه‌های زوم کار نمی‌کنند»: تنظیم Zoom روی FlowDocumentPageViewerِ
        // ساخته‌شده در کد (بدون اعمال قالب کامل و کنترل داخلی DocumentPageView)
        // در عمل بی‌اثر می‌ماند. راه‌حل مطمئن و مستقل از قالب: مقیاس‌گذاری
        // خود نمایشگر با ScaleTransform — همیشه روی هر نسخهٔ ویندوز اثر می‌کند.
        double zoomLevel = 100;
        var zoomScale = new ScaleTransform(1, 1);
        viewer.LayoutTransform = zoomScale;

        // ─── دکمهٔ چاپ — ارسال مستقیم به پرینتر از طریق PrintDialog ───
        var printBtn = new Button
        {
            Content = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Children =
                {
                    new TextBlock { Text = "🖨", FontSize = 16, VerticalAlignment = VerticalAlignment.Center },
                    new TextBlock
                    {
                        Text = "چاپ", FontWeight = FontWeights.Bold, FontSize = 13,
                        Margin = new Thickness(7, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center
                    }
                }
            },
            Background = new SolidColorBrush(Color.FromRgb(0x15, 0x65, 0xC0)),
            Foreground = Brushes.White,
            Padding = new Thickness(18, 8, 18, 8),
            BorderThickness = new Thickness(0),
            Cursor = System.Windows.Input.Cursors.Hand,
            FontFamily = PrintFontFamily
        };
        // رویداد کلیک چاپ —.paginator استاندارد سند به پرینتر فرستاده می‌شود؛
        // اگر کاربر پرینتر را لغو کند یا پرینتری نصب نباشد، بی‌صدا بازمی‌گردد
        printBtn.Click += (_, _) =>
        {
            try
            {
                var dlg = new System.Windows.Controls.PrintDialog();
                if (dlg.ShowDialog() == true)
                {
                    // سند را مستقیم به پرینتر انتخابی ارسال می‌کنیم —
                    // IDocumentPaginatorSource صفحه‌بندی استاندارد WPF را می‌سازد
                    if (document is IDocumentPaginatorSource src)
                        dlg.PrintDocument(src.DocumentPaginator, title);
                }
            }
            catch (Exception ex)
            {
                // خطای درایور پرینتر نباید برنامه را بندازد — دیالوگ فارسی برنامه
                Views.AppDialog.Error("خطای چاپ", $"ارسال به پرینتر ناموفق بود:{Environment.NewLine}{ex.Message}");
            }
        };

        // ─── زوم +/− و درصد زوم ───
        var zoomText = new TextBlock
        {
            Text = $"{PersianDateHelper.ToPersianDigits(((int)zoomLevel).ToString())}٪",
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(8, 0, 8, 0),
            FontFamily = PrintFontFamily,
            MinWidth = 44,
            TextAlignment = TextAlignment.Center
        };
        // ═══ چرخهٔ سی‌وسوم (گزارش نوزدهم مالک) — «نوشتهٔ + و − قابل مشاهده نیست» ═══
        // ریشه: محتوای دکمه‌ها نویسهٔ متنی «+»/«−» با فونت فارسی وزیرمتن بود؛ این
        // گلیف‌ها در فونت فارسی بسیار نازک/ریز رسم می‌شوند (و در برخی نگارش‌ها
        // اصلاً فالبک نمی‌شوند) و دکمه‌ها «خالی» دیده می‌شدند. راه‌حل قطعی و
        // مستقل از فونت: آیکون وکتوری پک‌آیکون (Plus/Minus) — در هر فونت و هر
        // تمی همیشه رسم می‌شود؛ دکمه‌ها هم بزرگ‌تر (۳۶×۳۲) شدند و تولتیپ فارسی
        // «بزرگ‌نمایی/کوچک‌نمایی» گرفتند تا کارکردشان هم شفاف باشد.
        var zoomIn = new Button
        {
            Content = new MaterialDesignThemes.Wpf.PackIcon
            {
                Kind = MaterialDesignThemes.Wpf.PackIconKind.Plus,
                Width = 16, Height = 16,
                VerticalAlignment = VerticalAlignment.Center
            },
            Width = 36, Height = 32,
            Cursor = System.Windows.Input.Cursors.Hand,
            ToolTip = "بزرگ‌نمایی"
        };
        var zoomOut = new Button
        {
            Content = new MaterialDesignThemes.Wpf.PackIcon
            {
                Kind = MaterialDesignThemes.Wpf.PackIconKind.Minus,
                Width = 16, Height = 16,
                VerticalAlignment = VerticalAlignment.Center
            },
            Width = 36, Height = 32,
            Cursor = System.Windows.Input.Cursors.Hand,
            ToolTip = "کوچک‌نمایی"
        };

        // ═══ چرخهٔ بیست‌وششم — اسلایدر زوم فارسی ═══ «اسلایدر زوم که در پایین
        // صفحه است عمل نمی‌کند» — اسلایدر قدیمیِ نوار داخلی حذف شد؛ اسلایدر
        // اختصاصی زیر همان زوم مقیاسی برنامه قرار گرفت و با دکمه‌ها و درصد
        // همگام می‌شود
        var zoomSlider = new Slider
        {
            Minimum = 50,
            Maximum = 300,
            Width = 130,
            VerticalAlignment = VerticalAlignment.Center,
            IsMoveToPointEnabled = true,
            IsSnapToTickEnabled = true,
            TickFrequency = 10,
            SmallChange = 10,
            LargeChange = 20,
            FontFamily = PrintFontFamily
        };
        // اعمال زوم مقیاسی — نمایشگر با LayoutTransform مقیاس می‌گیرد و درصد
        // فارسی همان لحظه تازه می‌شود؛ اسلایدر و دکمه‌ها همگی همگام‌اند
        void ApplyZoom(double value)
        {
            zoomLevel = Math.Clamp(value, 50, 300);
            zoomScale.ScaleX = zoomLevel / 100.0;
            zoomScale.ScaleY = zoomLevel / 100.0;
            zoomText.Text = $"{PersianDateHelper.ToPersianDigits(((int)zoomLevel).ToString())}٪";
            // همگام‌سازی اسلایدر (بدون حلقه — فقط هنگام تفاوت واقعی)
            if (Math.Abs(zoomSlider.Value - zoomLevel) > 0.5)
                zoomSlider.Value = zoomLevel;
        }
        zoomIn.Click += (_, _) => ApplyZoom(zoomLevel + 10);
        zoomOut.Click += (_, _) => ApplyZoom(zoomLevel - 10);
        // اسلایدر → زوم؛ مقدار اولیه هم روی اسلایدر می‌نشیند
        zoomSlider.Value = zoomLevel;
        zoomSlider.ValueChanged += (_, e) => ApplyZoom(e.NewValue);

        // ─── دکمهٔ بستن ───
        var closeBtn = new Button
        {
            Content = "بستن",
            Padding = new Thickness(14, 6, 14, 6),
            FontFamily = PrintFontFamily
        };

        // ─── نوار ابزار فارسی بالای پنجره ───
        var toolbar = new DockPanel { Background = new SolidColorBrush(Color.FromRgb(0xF2, 0xF6, 0xFB)), LastChildFill = true };
        var toolbarBorder = new Border
        {
            BorderBrush = new SolidColorBrush(Color.FromRgb(0xD5, 0xDF, 0xEB)),
            BorderThickness = new Thickness(0, 0, 0, 1),
            Padding = new Thickness(10, 6, 10, 6),
            Child = toolbar
        };

        // راست: دکمهٔ چاپ + انتخابگر طرح چاپ (چون RTL است اولین فرزند سمت راست می‌افتد)
        var rightTools = new StackPanel { Orientation = Orientation.Horizontal };
        rightTools.Children.Add(printBtn);

        // ─── انتخابگر «طرح چاپ» — چرخه ۲۳: بازسازی زندهٔ سند با طرح منتخب ───
        var styleLabel = new TextBlock
        {
            Text = "طرح چاپ:",
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(14, 0, 6, 0),
            FontFamily = PrintFontFamily
        };
        var styleCombo = new ComboBox
        {
            Width = 105,
            VerticalAlignment = VerticalAlignment.Center,
            FontFamily = PrintFontFamily,
            SelectedIndex = (int)currentStyle
        };
        foreach (var s in Enum.GetValues<PrescriptionPrintStyle>())
            styleCombo.Items.Add(StyleNameFa(s));
        // تعویض طرح → سند دوباره ساخته می‌شود و انتخاب در ذخیرهٔ محلی می‌ماند
        styleCombo.SelectionChanged += (_, _) =>
        {
            var picked = (PrescriptionPrintStyle)styleCombo.SelectedIndex;
            if (picked == currentStyle) return;
            currentStyle = picked;
            SavedStyle = picked; // یادآوری انتخاب کاربر
            var fresh = buildByStyle(picked);
            fresh.PageWidth = 793.7;
            fresh.PageHeight = 1122.5;
            viewer.Document = fresh;
            document = fresh; // چاپ دکمهٔ «چاپ» همان سند نمای فعال را می‌فرستد
        };
        rightTools.Children.Add(styleLabel);
        rightTools.Children.Add(styleCombo);
        DockPanel.SetDock(rightTools, Dock.Right);
        toolbar.Children.Add(rightTools);

        // چپ: زوم (دکمه‌ها + درصد + اسلایدر) و بستن — چرخه ۲۶: اسلایدر هم اضافه شد
        // ═══ چرخهٔ سی‌وسوم (گزارش نوزدهم مالک): خوشهٔ زوم با جهت چپ‌به‌راست ═══
        // در پنجرهٔ راست‌چین، StackPanel ارثی RTL ترتیب فرزندان را آینه می‌کند و
        // تشخیص اینکه کدام دکمه بزرگ‌نمایی است مبهم می‌شود. خوشهٔ زوم صریحاً LTR
        // شد: همیشه [−] [٪] [+] [اسلایدر] — همان الگوی آشنای زوم مرورگرها؛ بدون
        // ابهام در هیچ جهت چیدمانی.
        var zoomCluster = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            FlowDirection = FlowDirection.LeftToRight,
            VerticalAlignment = VerticalAlignment.Center
        };
        zoomCluster.Children.Add(zoomOut);
        zoomCluster.Children.Add(zoomText);
        zoomCluster.Children.Add(zoomIn);
        zoomCluster.Children.Add(zoomSlider);

        var leftTools = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
        leftTools.Children.Add(zoomCluster);
        leftTools.Children.Add(new Border { Width = 10 });
        leftTools.Children.Add(closeBtn);
        DockPanel.SetDock(leftTools, Dock.Left);
        toolbar.Children.Add(leftTools);

        // عنوان وسط
        var titleText = new TextBlock
        {
            Text = title,
            FontSize = 13,
            FontWeight = FontWeights.SemiBold,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center,
            FontFamily = PrintFontFamily
        };
        toolbar.Children.Add(titleText);

        // ─── چیدمان نهایی پنجره ───
        var grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        Grid.SetRow(toolbarBorder, 0);
        Grid.SetRow(viewer, 1);
        grid.Children.Add(toolbarBorder);
        grid.Children.Add(viewer);

        var window = new Window
        {
            Title = title,
            Content = grid,
            Width = 880,
            Height = 680,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Owner = System.Windows.Application.Current?.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive),
            FlowDirection = FlowDirection.RightToLeft,
            FontFamily = PrintFontFamily
        };
        closeBtn.Click += (_, _) => window.Close();
        // چرخه بیست‌وچهارم (گزارش هفدهم مالک): «کلید کنسل یا ESC برای بستن
        // کادرها» — کلید Escape پنجرهٔ پیش‌نمایش چاپ را هم می‌بندد
        window.PreviewKeyDown += (_, ke) =>
        {
            if (ke.Key == System.Windows.Input.Key.Escape)
            {
                window.Close();
                ke.Handled = true;
            }
        };
        window.ShowDialog();
    }
}
