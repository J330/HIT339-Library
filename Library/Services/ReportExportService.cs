using System.Globalization;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using S379658_HIT339_Assignment2_Library.ViewModels;

namespace S379658_HIT339_Assignment2_Library.Services;

public sealed class ReportExportService
{
    public byte[] ToCsv(ReportTable report)
    {
        using var text = new StringWriter(CultureInfo.InvariantCulture);
        using (var writer = new CsvWriter(text, new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            InjectionOptions = InjectionOptions.Escape // prevent spreadsheet formula execution
        }))
        {
            foreach (var header in report.Columns) writer.WriteField(header);
            writer.NextRecord();
            foreach (var row in report.Rows)
            {
                foreach (var field in row) writer.WriteField(field);
                writer.NextRecord();
            }
        }
        return new UTF8Encoding(true).GetPreamble().Concat(Encoding.UTF8.GetBytes(text.ToString())).ToArray();
    }

    public byte[] ToPdf(ReportTable report)
    {
        // Educational/evaluation configuration; review QuestPDF license before production deployment.
        QuestPDF.Settings.License = LicenseType.Evaluation;
        return Document.Create(document =>
        {
            document.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(28);
                page.DefaultTextStyle(x => x.FontSize(9));
                page.Header().Column(c =>
                {
                    c.Item().Text("Library Management System").FontSize(10).FontColor(Colors.Grey.Darken2);
                    c.Item().Text(report.Title).Bold().FontSize(20);
                    c.Item().Text("Generated (UTC): " + report.GeneratedAtUtc.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture));
                });
                page.Content().PaddingTop(18).Column(c =>
                {
                    c.Item().Text(report.Note).FontSize(9);
                    c.Item().PaddingTop(12).Table(t =>
                    {
                        t.ColumnsDefinition(columns =>
                        {
                            foreach (var _ in report.Columns) columns.RelativeColumn();
                        });
                        t.Header(header =>
                        {
                            foreach (var heading in report.Columns)
                                header.Cell().Element(CellStyle).Background(Colors.Grey.Lighten3).Text(heading).SemiBold();
                        });
                        foreach (var row in report.Rows)
                            foreach (var cell in row)
                                t.Cell().Element(CellStyle).Text(cell);
                    });
                });
                page.Footer().AlignRight().Text(t =>
                {
                    t.Span("Page "); t.CurrentPageNumber();
                });
            });
        }).GeneratePdf();
    }

    private static IContainer CellStyle(IContainer container) => container
        .BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingVertical(6).PaddingHorizontal(4);
}
