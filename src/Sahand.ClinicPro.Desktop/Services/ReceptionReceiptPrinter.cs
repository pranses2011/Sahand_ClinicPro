using System.Windows;
using System.Windows.Documents;
using Sahand.ClinicPro.Application.Common.Helpers;
using Sahand.ClinicPro.Desktop.Helpers;

namespace Sahand.ClinicPro.Desktop.Services;

/// <summary>
/// یک قلم قبض پذیرش — نام خدمت/کالا، تعداد و مبلغ واحد (ریال).
/// </summary>
/// <param name="Name">شرح قلم — «ویزیت …»، نام خدمت یا «لوازم مصرفی: …»</param>
/// <param name="Quantity">تعداد (وزن‌دار هم می‌تواند باشد)</param>
/// <param name="UnitPrice">مبلغ واحد به ریال</param>
public sealed record ReceptionReceiptItem(string Name, decimal Quantity, decimal UnitPrice)
{
    /// <summary>جمع سطر = تعداد × مبلغ واحد</summary>
    public decimal Total => Quantity * UnitPrice;
}

/// <summary>
/// دادهٔ کامل قبض پذیرش — ساخته‌شده در لحظهٔ ثبت، پیش از پاک‌سازی فرم.
/// </summary>
/// <param name="QueueNumber">شمارهٔ صف پذیرش (شناسهٔ پذیرش ثبت‌شده)</param>
/// <param name="InvoiceNumber">شمارهٔ فاکتور صادرشده</param>
/// <param name="PatientName">نام بیمار</param>
/// <param name="NationalCode">کد ملی بیمار</param>
/// <param name="Mobile">موبایل بیمار</param>
/// <param name="DoctorName">پزشک (حالت ویزیت) یا انجام‌دهندهٔ خدمت (حالت خدمتی)</param>
/// <param name="ServiceName">نام خدمت درمانی — در حالت ویزیت تهی</param>
/// <param name="RoomName">اتاق انجام خدمت — اختیاری</param>
/// <param name="InsuranceName">بیمهٔ پذیرش یا «آزاد»</param>
/// <param name="DateText">تاریخ شمسی ثبت</param>
/// <param name="TimeText">ساعت ثبت</param>
/// <param name="Items">اقلام (خدمت/ویزیت + لوازم مصرفی)</param>
/// <param name="ItemsTotal">جمع اقلام</param>
/// <param name="InsuranceShare">سهم بیمه — برای بیمار آزاد صفر</param>
/// <param name="PatientShare">سهم بیمار</param>
/// <param name="Payable">مبلغ قابل پرداخت (سهم بیمار + لوازم مصرفی)</param>
/// <param name="Paid">مبلغ پرداخت‌شده در همین لحظه</param>
/// <param name="PaymentMethod">روش پرداخت — ۱ نقدی، ۲ کارتخوان</param>
/// <param name="ReferenceNumber">شمارهٔ پیگیری کارتخوان</param>
/// <param name="CardNumber">چهار رقم آخر کارت</param>
public sealed record ReceptionReceipt(
    long QueueNumber,
    long InvoiceNumber,
    string PatientName,
    string? NationalCode,
    string? Mobile,
    string DoctorName,
    string? ServiceName,
    string? RoomName,
    string InsuranceName,
    string DateText,
    string TimeText,
    IReadOnlyList<ReceptionReceiptItem> Items,
    decimal ItemsTotal,
    decimal InsuranceShare,
    decimal PatientShare,
    decimal Payable,
    decimal Paid,
    int PaymentMethod,
    string? ReferenceNumber,
    string? CardNumber);

/// <summary>
/// چاپ قبض پذیرش و پرداخت — چرخهٔ ۸۲ (گزارش مالک):
/// «در پذیرش، بعد از زدن پذیرش و پرداخت و انتخاب روش پرداخت و تأیید، قبضی
/// چاپ نمیشه؛ یک قبض کامل و شیک برای پرینتر حرارتی ۸ سانتی می‌خوام».
/// </summary>
/// <remarks>
/// ═══ چرا یک کلاس مستقل ═══
/// قبض‌های حرارتی موجود (قبض نوبت، رسید شیفت) هرکدام داده و چیدمان خودشان را
/// دارند و اندازهٔ کاغذ ۸۰ میلی‌متری (۳۰۲ پیکسل در ۹۶dpi) را رعایت می‌کنند؛
/// این قبض اما «سند مالی» پذیرش است و باید اقلام، سهم بیمه/بیمار، مبلغ
/// پرداخت‌شده و روش پرداخت را یک‌جا نشان دهد، پس چیدمان ستون‌بندی‌شده دارد:
///
///   • سربرگ: نام درمانگاه (از تنظیمات چاپ) + «قبض پذیرش و پرداخت»
///   • کادر شمارهٔ صف — درشت و برجسته، مهم‌ترین دادهٔ قبض برای راهنمایی بیمار
///   • شناسنامه: بیمار، کد ملی، موبایل، پزشک/خدمت، اتاق، بیمه، تاریخ و ساعت
///   • جدول اقلام: شرح | تعداد | مبلغ (با سرستون و خط‌کشی، ارقام فارسی)
///   • جمع‌بندی: جمع اقلام، سهم بیمه، سهم بیمار، و کادر «قابل پرداخت»
///   • پرداخت: مبلغ پرداختی، روش (نقدی/کارتخوان)، شمارهٔ پیگیری و کارت
///   • پانوشت: شمارهٔ فاکتور، پیام تشکر و تاریخ چاپ
///
/// ═══ پیش‌نمایش قبل از چاپ ═══
/// طبق قانون چرخهٔ ۶۷ همهٔ چاپ‌ها از دروازهٔ <see cref="PrintFlow"/> می‌گذرند؛
/// اگر تنظیم «نمایش پیش‌نمایش قبل از چاپ» روشن باشد، کاربر قبض را پیش از
/// ارسال به چاپگر می‌بیند و خودش تأیید می‌کند — دقیقاً همان چیزی که مالک
/// خواسته بود. چاپگر مقصد: «Printer.Receipt» و در نبودش «Printer.Ticket» و
/// در نبود هر دو، چاپگر پیش‌فرض ویندوز.
/// </remarks>
public static class ReceptionReceiptPrinter
{
    /// <summary>عرض قبض — ۸۰ میلی‌متر کاغذ حرارتی رایج (پیکسل در ۹۶dpi)</summary>
    private const double ReceiptWidth = 302;

    /// <summary>ارتفاع پایهٔ صفحه — با تعداد اقلام بلندتر می‌شود</summary>
    private const double ReceiptBaseHeight = 660;

    /// <summary>رنگ تأکید سربرگ و کادرها — همان آبی برند برنامه</summary>
    private const string Accent = "#0078D7";

    /// <summary>
    /// ساخت سند و ارسال به چاپ — با احترام به تنظیم پیش‌نمایش.
    /// </summary>
    /// <param name="receipt">دادهٔ قبض — در لحظهٔ ثبت پذیرش ساخته می‌شود</param>
    /// <returns>true اگر چاپ (یا پیش‌نمایشِ منتهی به چاپ) انجام شد</returns>
    public static bool Print(ReceptionReceipt receipt)
    {
        var doc = BuildReceipt(receipt);

        // ارتفاع بر اساس محتوا — هر قلم ≈ ۱۸px؛ کف و سقف امن برای درایورها
        var height = Math.Clamp(ReceiptBaseHeight + receipt.Items.Count * 18d, 620d, 1400d);
        doc.PageWidth = ReceiptWidth;
        doc.PageHeight = height;

        // چاپگر مقصد — اختصاصی رسید، وگرنه چاپگر قبض نوبت، وگرنه پیش‌فرض ویندوز
        // (Get هرگز null نمی‌دهد؛ رشتهٔ خالی یعنی تنظیم‌نشده)
        var receiptPrinter = LocalSettingsStore.Get("Printer.Receipt");
        var ticketPrinter = LocalSettingsStore.Get("Printer.Ticket");
        var printerName = !string.IsNullOrWhiteSpace(receiptPrinter)
            ? receiptPrinter
            : !string.IsNullOrWhiteSpace(ticketPrinter) ? ticketPrinter : null;

        return PrintFlow.Print(doc, $"قبض پذیرش {receipt.QueueNumber}", printerName,
            ReceiptWidth, height);
    }

    /// <summary>ساخت سند قبض — راست‌چین، ارقام فارسی، فونت چاپ جاسازی‌شده</summary>
    private static FlowDocument BuildReceipt(ReceptionReceipt r)
    {
        var doc = new FlowDocument
        {
            FlowDirection = FlowDirection.RightToLeft,
            FontFamily = FontAssets.PrintFont,
            FontSize = 10.5,
            PagePadding = new Thickness(8, 8, 8, 10),
            ColumnWidth = double.PositiveInfinity,
        };

        // ─── سربرگ درمانگاه ───
        var clinic = LocalSettingsStore.Get("Printer.ClinicName");
        doc.Blocks.Add(Center(string.IsNullOrWhiteSpace(clinic) ? "درمانگاه سهند" : clinic,
            15.5, FontWeights.Bold, Accent));
        doc.Blocks.Add(Center("قبض پذیرش و پرداخت", 11.5, FontWeights.SemiBold, "#37474F"));
        doc.Blocks.Add(Divider());

        // ─── کادر شمارهٔ صف — مهم‌ترین دادهٔ قبض ───
        var queue = new Paragraph
        {
            TextAlignment = TextAlignment.Center,
            Background = Brush("#EAF3FC"),
            BorderBrush = Brush(Accent),
            BorderThickness = new Thickness(1),
            Padding = new Thickness(4, 3, 4, 3),
            Margin = new Thickness(0, 3, 0, 5),
        };
        queue.Inlines.Add(new Run("شمارهٔ صف  ") { FontSize = 11, Foreground = Brush("#37474F") });
        queue.Inlines.Add(new Run(Persian(r.QueueNumber.ToString()))
        {
            FontSize = 22,
            FontWeight = FontWeights.Bold,
            Foreground = Brush(Accent),
        });
        doc.Blocks.Add(queue);

        // ─── شناسنامهٔ پذیرش — دو ستون برچسب/مقدار ───
        var info = new Table
        {
            CellSpacing = 0,
            BorderBrush = Brush("#CFD8DC"),
            BorderThickness = new Thickness(0.5),
        };
        info.Columns.Add(new TableColumn { Width = new GridLength(62) });  // برچسب
        info.Columns.Add(new TableColumn { Width = new GridLength(222) }); // مقدار
        var infoRows = new TableRowGroup();
        AddInfoRow(infoRows, "بیمار", r.PatientName);
        if (!string.IsNullOrWhiteSpace(r.NationalCode))
            AddInfoRow(infoRows, "کد ملی", Persian(r.NationalCode));
        if (!string.IsNullOrWhiteSpace(r.Mobile))
            AddInfoRow(infoRows, "موبایل", Persian(r.Mobile));
        AddInfoRow(infoRows, string.IsNullOrWhiteSpace(r.ServiceName) ? "پزشک" : "خدمت",
            string.IsNullOrWhiteSpace(r.ServiceName) ? r.DoctorName : r.ServiceName!);
        // در حالت خدمتی، انجام‌دهنده جدا نمایش داده می‌شود (پزشک = انجام‌دهنده)
        if (!string.IsNullOrWhiteSpace(r.ServiceName) && !string.IsNullOrWhiteSpace(r.DoctorName))
            AddInfoRow(infoRows, "انجام‌دهنده", r.DoctorName);
        if (!string.IsNullOrWhiteSpace(r.RoomName))
            AddInfoRow(infoRows, "اتاق", r.RoomName!);
        AddInfoRow(infoRows, "بیمه", r.InsuranceName);
        AddInfoRow(infoRows, "تاریخ", $"{Persian(r.DateText)} — ساعت {Persian(r.TimeText)}");
        info.RowGroups.Add(infoRows);
        doc.Blocks.Add(info);

        // ─── جدول اقلام ───
        doc.Blocks.Add(SectionTitle("اقلام"));
        var table = ItemsTable(r.Items);
        doc.Blocks.Add(table);

        // ─── جمع‌بندی ───
        var totals = new Table { CellSpacing = 0 };
        totals.Columns.Add(new TableColumn { Width = new GridLength(150) });
        totals.Columns.Add(new TableColumn { Width = new GridLength(134) });
        var totalsRows = new TableRowGroup();
        AddTotalRow(totalsRows, "جمع اقلام", Money.FormatWithUnit(r.ItemsTotal));
        if (r.InsuranceShare > 0m)
            AddTotalRow(totalsRows, "سهم بیمه", Money.FormatWithUnit(r.InsuranceShare));
        if (r.PatientShare > 0m)
            AddTotalRow(totalsRows, "سهم بیمار", Money.FormatWithUnit(r.PatientShare));
        totals.RowGroups.Add(totalsRows);
        doc.Blocks.Add(totals);

        // کادر «قابل پرداخت» — پررنگ و رنگی
        var payable = new Paragraph
        {
            TextAlignment = TextAlignment.Center,
            Background = Brush("#EAF3FC"),
            BorderBrush = Brush(Accent),
            BorderThickness = new Thickness(1),
            Padding = new Thickness(3, 3, 3, 3),
            Margin = new Thickness(0, 4, 0, 4),
            FontSize = 11.5,
        };
        payable.Inlines.Add(new Run("قابل پرداخت:  ") { FontWeight = FontWeights.SemiBold });
        payable.Inlines.Add(new Run(Money.FormatWithUnit(r.Payable))
        {
            FontSize = 14,
            FontWeight = FontWeights.Bold,
            Foreground = Brush(Accent),
        });
        doc.Blocks.Add(payable);

        doc.Blocks.Add(Divider());

        // ─── بخش پرداخت ───
        var paid = new Paragraph { Margin = new Thickness(0, 2, 0, 2) };
        paid.Inlines.Add(new Run("پرداخت‌شده: ") { FontWeight = FontWeights.Bold });
        paid.Inlines.Add(new Run(Money.FormatWithUnit(r.Paid)));
        doc.Blocks.Add(paid);

        var method = new Paragraph { Margin = new Thickness(0, 1, 0, 1) };
        method.Inlines.Add(new Run("روش پرداخت: ") { FontWeight = FontWeights.Bold });
        method.Inlines.Add(new Run(r.PaymentMethod == 2 ? "کارتخوان (POS)" : "نقدی"));
        doc.Blocks.Add(method);

        if (r.PaymentMethod == 2)
        {
            if (!string.IsNullOrWhiteSpace(r.ReferenceNumber))
                doc.Blocks.Add(Line("شمارهٔ پیگیری", Persian(r.ReferenceNumber!)));
            if (!string.IsNullOrWhiteSpace(r.CardNumber))
                doc.Blocks.Add(Line("کارت", $"**** {Persian(r.CardNumber!)}"));
        }

        doc.Blocks.Add(Line("شمارهٔ فاکتور", Persian(r.InvoiceNumber.ToString())));
        doc.Blocks.Add(Divider());

        // ─── پانوشت ───
        doc.Blocks.Add(Center("از پرداخت شما سپاسگزاریم", 10.5, FontWeights.SemiBold, "#37474F"));
        doc.Blocks.Add(Center("این قبض سند دریافت وجه است — آن را تا پایان مراجعه نگه دارید",
            8.5, FontWeights.Normal, "#78909C"));
        doc.Blocks.Add(Center($"چاپ: {PersianDateHelper.ToShamsiFa(DateTime.Now)} — " +
                              PersianDateHelper.ToClockFa(DateTime.Now, withSeconds: false),
            8.5, FontWeights.Normal, "#78909C"));

        return doc;
    }

    /// <summary>جدول اقلام — سرستون خاکستری + ستون‌های شرح/تعداد/مبلغ</summary>
    private static Table ItemsTable(IReadOnlyList<ReceptionReceiptItem> items)
    {
        // عرض قابل استفاده: ۳۰۲ − پدینگ ۸+۸ − خط‌کشی ≈ ۲۸۴
        var table = new Table
        {
            CellSpacing = 0,
            BorderBrush = Brush("#CFD8DC"),
            BorderThickness = new Thickness(0.5),
            Margin = new Thickness(0, 0, 0, 4),
        };
        table.Columns.Add(new TableColumn { Width = new GridLength(154) }); // شرح
        table.Columns.Add(new TableColumn { Width = new GridLength(30) });  // تعداد
        table.Columns.Add(new TableColumn { Width = new GridLength(100) }); // مبلغ

        var head = new TableRowGroup { Background = Brush("#ECEFF1") };
        var headRow = new TableRow();
        headRow.Cells.Add(HeaderCell("شرح", TextAlignment.Right));
        headRow.Cells.Add(HeaderCell("تعداد", TextAlignment.Center));
        headRow.Cells.Add(HeaderCell("مبلغ", TextAlignment.Left));
        head.Rows.Add(headRow);
        table.RowGroups.Add(head);

        var body = new TableRowGroup();
        foreach (var item in items)
        {
            var row = new TableRow();
            row.Cells.Add(BodyCell(item.Name, TextAlignment.Right, bold: true));
            row.Cells.Add(BodyCell(PersianNumber(item.Quantity), TextAlignment.Center));
            row.Cells.Add(BodyCell(Money.Format(item.Total), TextAlignment.Left));

            // مبلغ واحد زیر نام، وقتی تعداد بیش از یکی است — شفافیت محاسبه
            if (item.Quantity > 1m)
            {
                var unit = new Paragraph(new Run($"واحد: {Money.Format(item.UnitPrice)}"))
                {
                    Margin = new Thickness(0),
                    FontSize = 8.5,
                    Foreground = Brush("#78909C"),
                    TextAlignment = TextAlignment.Right,
                };
                row.Cells[0].Blocks.Add(unit);
            }
            body.Rows.Add(row);
        }
        table.RowGroups.Add(body);
        return table;
    }

    /// <summary>ردیف شناسنامه — برچسب خاکستری + مقدار</summary>
    private static void AddInfoRow(TableRowGroup group, string label, string value)
    {
        var row = new TableRow();
        row.Cells.Add(new TableCell(new Paragraph(new Run(label))
        {
            Margin = new Thickness(0),
            FontSize = 9.5,
            FontWeight = FontWeights.SemiBold,
            Foreground = Brush("#37474F"),
        })
        {
            Padding = new Thickness(4, 2.5, 4, 2.5),
            Background = Brush("#F5F7F9"),
            BorderBrush = Brush("#E0E0E0"),
            BorderThickness = new Thickness(0.5),
        });
        row.Cells.Add(new TableCell(new Paragraph(new Run(value))
        {
            Margin = new Thickness(0),
            FontSize = 10,
        })
        {
            Padding = new Thickness(4, 2.5, 4, 2.5),
            BorderBrush = Brush("#E0E0E0"),
            BorderThickness = new Thickness(0.5),
        });
        group.Rows.Add(row);
    }

    /// <summary>ردیف جمع‌بندی — مقدار چپ‌چین، برچسب راست‌چین</summary>
    private static void AddTotalRow(TableRowGroup group, string label, string value)
    {
        var row = new TableRow();
        row.Cells.Add(new TableCell(new Paragraph(new Run(label))
        {
            Margin = new Thickness(0),
            FontWeight = FontWeights.SemiBold,
            TextAlignment = TextAlignment.Right,
        })
        { Padding = new Thickness(4, 2, 4, 2) });
        row.Cells.Add(new TableCell(new Paragraph(new Run(value))
        {
            Margin = new Thickness(0),
            TextAlignment = TextAlignment.Left,
        })
        { Padding = new Thickness(4, 2, 4, 2) });
        group.Rows.Add(row);
    }

    /// <summary>سرستون جدول اقلام</summary>
    private static TableCell HeaderCell(string text, TextAlignment align) => new(
        new Paragraph(new Run(text) { FontWeight = FontWeights.Bold, FontSize = 9 })
        {
            Margin = new Thickness(0),
            TextAlignment = align,
        })
    {
        Padding = new Thickness(4, 2.5, 4, 2.5),
        BorderBrush = Brush("#B0BEC5"),
        BorderThickness = new Thickness(0.5),
    };

    /// <summary>سلول دادهٔ جدول اقلام</summary>
    private static TableCell BodyCell(string text, TextAlignment align, bool bold = false) => new(
        new Paragraph(new Run(text)
        {
            Margin = new Thickness(0),
            FontSize = 9.5,
            FontWeight = bold ? FontWeights.SemiBold : FontWeights.Normal,
            TextAlignment = align,
        })
    {
        Padding = new Thickness(4, 2.5, 4, 2.5),
        BorderBrush = Brush("#E0E0E0"),
        BorderThickness = new Thickness(0.5),
    };

    /// <summary>سطر سادهٔ «برچسب: مقدار» — بخش پرداخت</summary>
    private static Paragraph Line(string label, string value)
    {
        var p = new Paragraph { Margin = new Thickness(0, 1, 0, 1), FontSize = 10 };
        p.Inlines.Add(new Run($"{label}: ") { FontWeight = FontWeights.Bold });
        p.Inlines.Add(new Run(value));
        return p;
    }

    /// <summary>عنوان بخش — نوار کم‌رنگ راست‌چین</summary>
    private static Paragraph SectionTitle(string text)
    {
        var p = new Paragraph(new Run("▌ " + text))
        {
            FontSize = 10.5,
            FontWeight = FontWeights.Bold,
            Background = Brush("#F5F7F9"),
            Padding = new Thickness(3, 1, 3, 1),
            Margin = new Thickness(0, 4, 0, 3),
            TextAlignment = TextAlignment.Right,
        };
        return p;
    }

    /// <summary>پاراگراف مرکزچین با رنگ و اندازهٔ دلخواه</summary>
    private static Paragraph Center(string text, double size, FontWeight weight, string? color = null)
    {
        var p = new Paragraph
        {
            TextAlignment = TextAlignment.Center,
            Margin = new Thickness(0, 1.5, 0, 1.5),
        };
        p.Inlines.Add(new Run(text)
        {
            FontSize = size,
            FontWeight = weight,
            Foreground = color is null ? System.Windows.Media.Brushes.Black : Brush(color),
        });
        return p;
    }

    /// <summary>خط جداکنندهٔ نقطه‌چین</summary>
    private static Paragraph Divider()
    {
        var p = new Paragraph(new Run(new string('·', 44)))
        {
            FontSize = 9,
            Foreground = Brush("#90A4AE"),
            TextAlignment = TextAlignment.Center,
            Margin = new Thickness(0, 3, 0, 3),
        };
        return p;
    }

    /// <summary>تعداد — اعشار فقط وقتی لازم است («۲» و «۱٫۵»)</summary>
    private static string PersianNumber(decimal value) =>
        Money.ToPersianDigits(value == Math.Truncate(value)
            ? ((long)value).ToString()
            : value.ToString("0.##").Replace('.', '٫'));

    /// <summary>تبدیل ارقام لاتین رشته به فارسی</summary>
    private static string Persian(string input) => Money.ToPersianDigits(input);

    /// <summary>براش رنگ از کد hex — فریز برای عملکرد چاپ</summary>
    private static System.Windows.Media.SolidColorBrush Brush(string hex)
    {
        var brush = (System.Windows.Media.SolidColorBrush?)new System.Windows.Media.BrushConverter()
            .ConvertFromString(hex);
        var result = brush ?? System.Windows.Media.Brushes.Black;
        result.Freeze();
        return result;
    }
}
