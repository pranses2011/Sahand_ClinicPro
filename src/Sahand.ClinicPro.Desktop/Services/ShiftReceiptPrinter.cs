using System.Printing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using Sahand.ClinicPro.Desktop.Helpers;

namespace Sahand.ClinicPro.Desktop.Services;

/// <summary>مدل رسید بستن شیفت برای پیش‌نمایش و چاپ — چرخهٔ چهل‌وسوم</summary>
/// <param name="ReceiptNumber">شمارهٔ رسید</param>
/// <param name="RegisterName">نام صندوق</param>
/// <param name="OpenedText">متن بازشدن شیفت (شمسی)</param>
/// <param name="ClosedText">متن بسته‌شدن شیفت (شمسی)</param>
/// <param name="ClosedByName">بستن‌دهنده</param>
/// <param name="SystemCash">مبلغ نقدی سیستم (ریال)</param>
/// <param name="SystemCard">مبلغ کارتی سیستم (ریال)</param>
/// <param name="SystemOnline">مبلغ آنلاین سیستم (ریال)</param>
/// <param name="SystemTotal">جمع سیستم (ریال)</param>
/// <param name="CountedCash">شمارش نقدی (ریال)</param>
/// <param name="CountedCard">شمارش کارتی (ریال)</param>
/// <param name="CountedTotal">جمع شمارش (ریال)</param>
/// <param name="CashDifference">اختلاف نقدی (ریال)</param>
/// <param name="CardDifference">اختلاف کارتی (ریال)</param>
/// <param name="NetDifference">اختلاف خالص (ریال)</param>
/// <param name="DifferenceState">متن وضعیت اختلاف</param>
/// <param name="DifferenceColor">رنگ وضعیت اختلاف</param>
/// <param name="PaymentCount">تعداد تراکنش‌های شیفت</param>
/// <param name="HandoverNote">یادداشت تحویل</param>
public sealed record ShiftReceipt(
    string ReceiptNumber, string RegisterName, string OpenedText, string ClosedText,
    string ClosedByName,
    decimal SystemCash, decimal SystemCard, decimal SystemOnline, decimal SystemTotal,
    decimal CountedCash, decimal CountedCard, decimal CountedTotal,
    decimal CashDifference, decimal CardDifference, decimal NetDifference,
    string DifferenceState, string DifferenceColor,
    int PaymentCount, string? HandoverNote);

/// <summary>
/// چاپ رسید بستن شیفت و تحویل صندوق — چرخهٔ چهل‌وسوم (گزارش مالک):
/// «با بستن شیفت میگه که رسید ثبت شد ولی پیشنمایش رسید رو نشون نمیده تا
/// چاپ بکنیم». چیدمان رسد حرارتی ۸۰ میلی‌متری هم‌خانوادهٔ چاپگر قبض نوبت
/// (AppointmentTicketPrinter) با ستون‌بندی نظام‌مند: مبالغ سیستم، شمارش
/// فیزیکی و اختلاف‌ها با ارقام فارسی و واحد پول نمایشی کاربر.
/// </summary>
public static class ShiftReceiptPrinter
{
    /// <summary>عرض رسید — ۸۰ میلی‌متر کاغذ حرارتی رایج (پیکسل در ۹۶dpi)</summary>
    private const double TicketWidth = 302;

    /// <summary>ارتفاع صفحهٔ رسید — خودکار به اندازهٔ محتوا کوتاه می‌شود</summary>
    private const double TicketHeight = 900;

    /// <summary>
    /// چاپ رسید — از دروازهٔ یکپارچهٔ PrintFlow (چرخهٔ ۶۷): تنظیم «نمایش
    /// پیش‌نمایش قبل از چاپ» روشن باشد پنجرهٔ پیش‌نمایش فارسی باز می‌شود،
    /// خاموش باشد همان چاپ مستقیم قبلی. چاپگر مقصد از تنظیمات
    /// («Printer.Receipt») و در نبودش، همان چاپگر قبض نوبت و سپس پیش‌فرض ویندوز.
    /// </summary>
    /// <param name="receipt">داده‌های رسید</param>
    /// <param name="clinicName">نام درمانگاه — سربرگ رسید</param>
    /// <returns>true اگر چاپ با موفقیت ارسال شد</returns>
    public static bool Print(ShiftReceipt receipt, string clinicName)
    {
        // چاپگر مقصد — اختصاصی رسید، یا قبض نوبت، یا پیش‌فرض
        // (Get هرگز null نمی‌دهد — تهی یعنی تنظیم نشده؛ برعکسِ ?? قبلی که
        // هرگز به چاپگر قبض نمی‌رسید چون رشتهٔ خالی تهی‌بودن را پنهان می‌کرد)
        var receiptPrinter = LocalSettingsStore.Get("Printer.Receipt");
        var ticketPrinter = LocalSettingsStore.Get("Printer.Ticket");
        var printerName = !string.IsNullOrWhiteSpace(receiptPrinter)
            ? receiptPrinter
            : !string.IsNullOrWhiteSpace(ticketPrinter) ? ticketPrinter : null;

        var document = BuildReceipt(receipt, clinicName);
        document.PagePadding = new Thickness(4, 8, 4, 12);
        document.PageWidth = TicketWidth;
        document.PageHeight = TicketHeight;

        // ═══ چرخهٔ ۶۷ — گذر از دروازهٔ یکپارچهٔ چاپ (پیش‌نمایش اختیاری) ═══
        return PrintFlow.Print(document, $"رسید بستن شیفت {receipt.ReceiptNumber}",
            printerName, TicketWidth, TicketHeight);
    }

    /// <summary>ساخت سند رسید — ستون‌بندی دوسویهٔ راست‌چین با ارقام فارسی</summary>
    private static FlowDocument BuildReceipt(ShiftReceipt r, string clinicName)
    {
        var doc = NewDocument();
        doc.Blocks.Add(Center(clinicName, 15, FontWeights.Bold));
        doc.Blocks.Add(Center("رسید بستن شیفت و تحویل صندوق", 12, FontWeights.Bold));
        doc.Blocks.Add(Divider());

        doc.Blocks.Add(Row($"رسید: {Money.ToPersianDigits(r.ReceiptNumber)}",
                           $"صندوق: {r.RegisterName}"));
        doc.Blocks.Add(Row($"بازه: {r.OpenedText} تا {r.ClosedText}", ""));
        doc.Blocks.Add(Row($"بستن‌دهنده: {r.ClosedByName}", ""));
        doc.Blocks.Add(Divider());

        doc.Blocks.Add(SectionHeader("مبالغ سیستم (ثبت‌شده)"));
        doc.Blocks.Add(Row("نقدی", Money.FormatWithUnit(r.SystemCash)));
        doc.Blocks.Add(Row("کارتخوان", Money.FormatWithUnit(r.SystemCard)));
        doc.Blocks.Add(Row("آنلاین", Money.FormatWithUnit(r.SystemOnline)));
        doc.Blocks.Add(RowBold("جمع سیستم", Money.FormatWithUnit(r.SystemTotal)));
        doc.Blocks.Add(Divider());

        doc.Blocks.Add(SectionHeader("شمارش فیزیکی"));
        doc.Blocks.Add(Row("وجه نقد", Money.FormatWithUnit(r.CountedCash)));
        doc.Blocks.Add(Row("تراکنش کارت", Money.FormatWithUnit(r.CountedCard)));
        doc.Blocks.Add(RowBold("جمع شمارش", Money.FormatWithUnit(r.CountedTotal)));
        doc.Blocks.Add(Divider());

        doc.Blocks.Add(SectionHeader("اختلاف‌سنجی"));
        doc.Blocks.Add(Row("اختلاف نقدی", Money.FormatWithUnit(r.CashDifference)));
        doc.Blocks.Add(Row("اختلاف کارتی", Money.FormatWithUnit(r.CardDifference)));
        doc.Blocks.Add(RowBold("اختلاف خالص", Money.FormatWithUnit(r.NetDifference)));
        doc.Blocks.Add(Center(r.DifferenceState, 11.5, FontWeights.Bold));
        doc.Blocks.Add(Divider());

        doc.Blocks.Add(Row($"تعداد تراکنش‌های شیفت: {Money.ToPersianDigits(r.PaymentCount.ToString())}", ""));
        if (!string.IsNullOrWhiteSpace(r.HandoverNote))
        {
            doc.Blocks.Add(SectionHeader("یادداشت تحویل"));
            var note = new Paragraph(new Run(r.HandoverNote)) { FontSize = 10.5 };
            note.TextAlignment = TextAlignment.Right;
            doc.Blocks.Add(note);
        }

        doc.Blocks.Add(Divider());
        doc.Blocks.Add(Center("امضای تحویل‌دهنده", 10.5, FontWeights.Regular));
        doc.Blocks.Add(Spacer());
        doc.Blocks.Add(Center("امضای تحویل‌گیرنده", 10.5, FontWeights.Regular));
        return doc;
    }

    /// <summary>سند پایه با فونت برنامه و جریان راست‌به‌چپ</summary>
    private static FlowDocument NewDocument() => new()
    {
        FontFamily = (System.Windows.Media.FontFamily)System.Windows.Application.Current
            .FindResource("App.FontFamily"),
        FontSize = 11,
        FlowDirection = FlowDirection.RightToLeft,
        PageWidth = TicketWidth,
        PageHeight = TicketHeight,
    };

    /// <summary>پاراگراف وسط‌چین — سربرگ‌ها</summary>
    private static Paragraph Center(string text, double size, FontWeight weight)
    {
        var p = new Paragraph(new Run(text)) { FontSize = size, FontWeight = weight };
        p.TextAlignment = TextAlignment.Center;
        return p;
    }

    /// <summary>سرصفحهٔ بخش — با پس‌زمینهٔ ملایم</summary>
    private static Paragraph SectionHeader(string text)
    {
        var p = new Paragraph(new Run("▌ " + text))
        {
            FontSize = 11,
            FontWeight = FontWeights.Bold,
            Background = System.Windows.Media.Brushes.WhiteSmoke,
            Padding = new Thickness(3, 1, 3, 1),
        };
        p.TextAlignment = TextAlignment.Right;
        return p;
    }

    /// <summary>سطر دوستونهٔ راست‌چین — برچسب راست، مقدار چپ (چیدمان رسید کاغذی)</summary>
    private static Block Row(string label, string value)
    {
        var table = new Table { CellSpacing = 0, FontSize = 11 };
        table.Columns.Add(new TableColumn { Width = new GridLength(140) });
        table.Columns.Add(new TableColumn { Width = new GridLength(1, GridUnitType.Star) });
        var row = new TableRowGroup().Rows;
        var tr = new TableRow();
        var c1 = new TableCell(new Paragraph(new Run(label))) { TextAlignment = TextAlignment.Right };
        var c2 = new TableCell(new Paragraph(new Run(value))) { TextAlignment = TextAlignment.Left };
        tr.Cells.Add(c1);
        tr.Cells.Add(c2);
        var group = new TableRowGroup();
        group.Rows.Add(tr);
        table.RowGroups.Add(group);
        return table;
    }

    /// <summary>سطر پررنگ دوسویه — جمع‌ها</summary>
    private static Block RowBold(string label, string value)
    {
        var table = (Table)Row(label, value);
        table.FontWeight = FontWeights.Bold;
        return table;
    }

    /// <summary>خط جداکنندهٔ نقطه‌چین</summary>
    private static Paragraph Divider()
    {
        var p = new Paragraph(new Run(new string('·', 46))) { FontSize = 9, Foreground = System.Windows.Media.Brushes.Gray };
        p.TextAlignment = TextAlignment.Center;
        return p;
    }

    /// <summary>فاصلهٔ امضا</summary>
    private static Paragraph Spacer() => new() { FontSize = 6 };
}
