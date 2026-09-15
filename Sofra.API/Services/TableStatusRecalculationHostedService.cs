using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sofra.API.Data;
using Sofra.API.Enums;
using Sofra.API.Options;
using Sofra.API.Services.Interfaces;

namespace Sofra.API.Services;

/// <summary>
/// Status stola se inace preracunava samo reaktivno (promjena narudzbe/rezervacije) - ali sam protok
/// vremena, bez ikakve akcije, takodjer treba da prebaci sto u Reserved kad udje u bliski prozor, i
/// nazad u Free kad termin prodje bez formalnog NoShow-a. Ovaj servis periodicno prodje kroz potvrdjene
/// rezervacije i pozove postojeci DiningTableStatusService (jedino mjesto koje pise DiningTable.Status
/// i emituje SignalR poruku), umjesto da duplira tu logiku.
/// </summary>
public class TableStatusRecalculationHostedService(
    IServiceScopeFactory scopeFactory,
    IOptions<ReservationOptions> reservationOptions,
    ILogger<TableStatusRecalculationHostedService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromMinutes(reservationOptions.Value.RecalculationIntervalMinutes);
        using var timer = new PeriodicTimer(interval);

        do
        {
            try
            {
                await RecalculateAllAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Greška pri periodičnom preračunu statusa stolova.");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task RecalculateAllAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var statusService = scope.ServiceProvider.GetRequiredService<IDiningTableStatusService>();

        // Samo stolovi potvrdjenih rezervacija mogu promijeniti status cistim protokom vremena bez
        // ikakve akcije (ulazak u/izlazak iz Reserved prozora) - RecalculateAsync je idempotentan.
        var tableIds = await dbContext.Reservations
            .Where(x => x.Status == ReservationStatus.Confirmed && x.DiningTableId != null)
            .Select(x => x.DiningTableId!.Value)
            .Distinct()
            .ToListAsync(cancellationToken);

        foreach (var tableId in tableIds)
        {
            await statusService.RecalculateAsync(tableId, cancellationToken);
        }
    }
}
