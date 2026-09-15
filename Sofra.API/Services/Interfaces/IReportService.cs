namespace Sofra.API.Services.Interfaces;

public interface IReportService
{
    Task<byte[]> GenerateRevenueReportAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken = default);
    Task<byte[]> GenerateTopItemsReportAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken = default);
    Task<byte[]> GenerateReservationsReportAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken = default);
    Task<byte[]> GenerateReceiptAsync(int orderId, int actorUserId, bool isStaff, CancellationToken cancellationToken = default);
}
