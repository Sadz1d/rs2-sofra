using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace Sofra.API.Reports;

public record RevenueReportRow(DateOnly Date, int OrderCount, decimal Subtotal, decimal Tax, decimal Total);

public class RevenueReportDocument(string restaurantName, DateOnly from, DateOnly to, IReadOnlyList<RevenueReportRow> rows) : IDocument
{
    public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Margin(30);
            page.Size(QuestPDF.Helpers.PageSizes.A4);
            page.Header().Element(c => ReportHeader.Compose(c, restaurantName, "Izvještaj o prometu", from, to));

            page.Content().PaddingTop(10).Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(2);
                    columns.RelativeColumn();
                    columns.RelativeColumn();
                    columns.RelativeColumn();
                    columns.RelativeColumn();
                });

                table.Header(header =>
                {
                    header.Cell().Text("Datum").Bold();
                    header.Cell().AlignRight().Text("Broj narudžbi").Bold();
                    header.Cell().AlignRight().Text("Osnovica").Bold();
                    header.Cell().AlignRight().Text("PDV").Bold();
                    header.Cell().AlignRight().Text("Ukupno").Bold();
                });

                foreach (var row in rows)
                {
                    table.Cell().Text(row.Date.ToString("dd.MM.yyyy."));
                    table.Cell().AlignRight().Text(row.OrderCount.ToString());
                    table.Cell().AlignRight().Text($"{row.Subtotal:0.00} KM");
                    table.Cell().AlignRight().Text($"{row.Tax:0.00} KM");
                    table.Cell().AlignRight().Text($"{row.Total:0.00} KM");
                }

                table.Cell().ColumnSpan(1).Text("Ukupno").Bold();
                table.Cell().AlignRight().Text(rows.Sum(x => x.OrderCount).ToString()).Bold();
                table.Cell().AlignRight().Text($"{rows.Sum(x => x.Subtotal):0.00} KM").Bold();
                table.Cell().AlignRight().Text($"{rows.Sum(x => x.Tax):0.00} KM").Bold();
                table.Cell().AlignRight().Text($"{rows.Sum(x => x.Total):0.00} KM").Bold();
            });

            page.Footer().AlignCenter().Text(text =>
            {
                text.CurrentPageNumber();
                text.Span(" / ");
                text.TotalPages();
            });
        });
    }
}
