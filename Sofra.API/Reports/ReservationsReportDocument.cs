using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace Sofra.API.Reports;

public record ReservationStatusRow(string StatusLabel, int Count);

public class ReservationsReportDocument(string restaurantName, DateOnly from, DateOnly to, IReadOnlyList<ReservationStatusRow> rows) : IDocument
{
    public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Margin(30);
            page.Size(QuestPDF.Helpers.PageSizes.A4);
            page.Header().Element(c => ReportHeader.Compose(c, restaurantName, "Pregled rezervacija po statusima", from, to));

            page.Content().PaddingTop(10).Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(2);
                    columns.RelativeColumn();
                });

                table.Header(header =>
                {
                    header.Cell().Text("Status").Bold();
                    header.Cell().AlignRight().Text("Broj rezervacija").Bold();
                });

                foreach (var row in rows)
                {
                    table.Cell().Text(row.StatusLabel);
                    table.Cell().AlignRight().Text(row.Count.ToString());
                }

                table.Cell().Text("Ukupno").Bold();
                table.Cell().AlignRight().Text(rows.Sum(x => x.Count).ToString()).Bold();
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
