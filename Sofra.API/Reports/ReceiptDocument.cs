using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace Sofra.API.Reports;

public record ReceiptItemRow(string MenuItemName, int Quantity, decimal UnitPrice, decimal LineTotal);

public record ReceiptData(
    string RestaurantName, string OrderNumber, DateTime CreatedAt, int? DiningTableNumber,
    IReadOnlyList<ReceiptItemRow> Items,
    decimal Subtotal, decimal Discount, decimal Tax, decimal Total,
    string? PaymentMethodName, bool IsPaid);

public class ReceiptDocument(ReceiptData data) : IDocument
{
    public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Margin(30);
            page.Size(QuestPDF.Helpers.PageSizes.A5);

            page.Header().Column(column =>
            {
                column.Item().Text(data.RestaurantName).FontSize(16).Bold();
                column.Item().Text($"Račun za narudžbu {data.OrderNumber}").FontSize(11);
                column.Item().Text($"Datum: {data.CreatedAt:dd.MM.yyyy. HH:mm}").FontSize(9);
                if (data.DiningTableNumber.HasValue)
                {
                    column.Item().Text($"Sto: {data.DiningTableNumber}").FontSize(9);
                }

                column.Item().PaddingTop(5).LineHorizontal(1);
            });

            page.Content().PaddingTop(10).Column(column =>
            {
                column.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(3);
                        columns.RelativeColumn();
                        columns.RelativeColumn();
                        columns.RelativeColumn();
                    });

                    table.Header(header =>
                    {
                        header.Cell().Text("Jelo").Bold();
                        header.Cell().AlignRight().Text("Kol.").Bold();
                        header.Cell().AlignRight().Text("Cijena").Bold();
                        header.Cell().AlignRight().Text("Iznos").Bold();
                    });

                    foreach (var item in data.Items)
                    {
                        table.Cell().Text(item.MenuItemName);
                        table.Cell().AlignRight().Text(item.Quantity.ToString());
                        table.Cell().AlignRight().Text($"{item.UnitPrice:0.00}");
                        table.Cell().AlignRight().Text($"{item.LineTotal:0.00}");
                    }
                });

                column.Item().PaddingTop(8).LineHorizontal(1);
                column.Item().PaddingTop(4).Row(row =>
                {
                    row.RelativeItem().Text("Osnovica");
                    row.ConstantItem(80).AlignRight().Text($"{data.Subtotal:0.00} KM");
                });
                if (data.Discount > 0)
                {
                    column.Item().Row(row =>
                    {
                        row.RelativeItem().Text("Popust");
                        row.ConstantItem(80).AlignRight().Text($"-{data.Discount:0.00} KM");
                    });
                }

                column.Item().Row(row =>
                {
                    row.RelativeItem().Text("PDV (17%)");
                    row.ConstantItem(80).AlignRight().Text($"{data.Tax:0.00} KM");
                });
                column.Item().PaddingTop(4).Row(row =>
                {
                    row.RelativeItem().Text("Ukupno").Bold();
                    row.ConstantItem(80).AlignRight().Text($"{data.Total:0.00} KM").Bold();
                });

                column.Item().PaddingTop(10).Text($"Način plaćanja: {data.PaymentMethodName ?? "Neplaćeno"}").FontSize(9);
                column.Item().Text(data.IsPaid ? "Status: PLAĆENO" : "Status: NEPLAĆENO").FontSize(9).Bold();
            });

            page.Footer().AlignCenter().Text($"Hvala što ste izabrali {data.RestaurantName}!").FontSize(9);
        });
    }
}
