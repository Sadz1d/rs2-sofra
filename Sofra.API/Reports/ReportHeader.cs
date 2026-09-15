using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace Sofra.API.Reports;

/// <summary>Zajednicko zaglavlje za sve PDF izvjestaje - naziv restorana, naslov, period, datum generisanja.</summary>
public static class ReportHeader
{
    public const string RestaurantName = "Sofra";

    public static void Compose(IContainer container, string title, DateOnly from, DateOnly to)
    {
        container.Column(column =>
        {
            column.Item().Text(RestaurantName).FontSize(18).Bold();
            column.Item().Text(title).FontSize(14).SemiBold();
            column.Item().PaddingTop(5).Text($"Period: {from:dd.MM.yyyy.} - {to:dd.MM.yyyy.}").FontSize(10);
            column.Item().Text($"Generisano: {DateTime.UtcNow:dd.MM.yyyy. HH:mm}").FontSize(10);
            column.Item().PaddingTop(8).LineHorizontal(1);
        });
    }
}
