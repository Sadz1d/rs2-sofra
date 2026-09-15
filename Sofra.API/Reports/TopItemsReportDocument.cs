using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace Sofra.API.Reports;

public record TopItemRow(string MenuItemName, int QuantitySold, decimal Revenue);

public class TopItemsReportDocument(string restaurantName, DateOnly from, DateOnly to, IReadOnlyList<TopItemRow> rows) : IDocument
{
    public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Margin(30);
            page.Size(QuestPDF.Helpers.PageSizes.A4);
            page.Header().Element(c => ReportHeader.Compose(c, restaurantName, "Najprodavanija jela", from, to));

            page.Content().PaddingTop(10).Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(1);
                    columns.RelativeColumn(3);
                    columns.RelativeColumn();
                    columns.RelativeColumn();
                });

                table.Header(header =>
                {
                    header.Cell().Text("#").Bold();
                    header.Cell().Text("Jelo").Bold();
                    header.Cell().AlignRight().Text("Prodano kom.").Bold();
                    header.Cell().AlignRight().Text("Prihod").Bold();
                });

                for (var i = 0; i < rows.Count; i++)
                {
                    var row = rows[i];
                    table.Cell().Text((i + 1).ToString());
                    table.Cell().Text(row.MenuItemName);
                    table.Cell().AlignRight().Text(row.QuantitySold.ToString());
                    table.Cell().AlignRight().Text($"{row.Revenue:0.00} KM");
                }
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
