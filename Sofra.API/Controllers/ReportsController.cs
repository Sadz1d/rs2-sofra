using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sofra.API.Constants;
using Sofra.API.Requests;
using Sofra.API.Services.Interfaces;

namespace Sofra.API.Controllers;

[ApiController]
[Route("api/reports")]
[Authorize(Roles = Roles.Admin)]
public class ReportsController(IReportService service) : ControllerBase
{
    [HttpGet("revenue")]
    public async Task<IActionResult> Revenue([FromQuery] DateRangeRequest request, CancellationToken cancellationToken)
    {
        var pdf = await service.GenerateRevenueReportAsync(request.DateFrom, request.DateTo, cancellationToken);
        return File(pdf, "application/pdf", $"promet-{request.DateFrom:yyyyMMdd}-{request.DateTo:yyyyMMdd}.pdf");
    }

    [HttpGet("top-items")]
    public async Task<IActionResult> TopItems([FromQuery] DateRangeRequest request, CancellationToken cancellationToken)
    {
        var pdf = await service.GenerateTopItemsReportAsync(request.DateFrom, request.DateTo, cancellationToken);
        return File(pdf, "application/pdf", $"najprodavanija-jela-{request.DateFrom:yyyyMMdd}-{request.DateTo:yyyyMMdd}.pdf");
    }

    [HttpGet("reservations")]
    public async Task<IActionResult> Reservations([FromQuery] DateRangeRequest request, CancellationToken cancellationToken)
    {
        var pdf = await service.GenerateReservationsReportAsync(request.DateFrom, request.DateTo, cancellationToken);
        return File(pdf, "application/pdf", $"rezervacije-{request.DateFrom:yyyyMMdd}-{request.DateTo:yyyyMMdd}.pdf");
    }
}
