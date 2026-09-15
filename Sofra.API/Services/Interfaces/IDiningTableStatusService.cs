namespace Sofra.API.Services.Interfaces;

public interface IDiningTableStatusService
{
    /// <summary>
    /// Preracunava status stola (Occupied/Reserved/Free) na osnovu aktivnih narudzbi i potvrdjenih
    /// rezervacija unutar bliskog prozora. Jedino mjesto koje smije pisati DiningTable.Status - poziva
    /// se nakon svake promjene narudzbe/rezervacije koja se tice stola (kasnije i iz SignalR/scheduled trigera).
    /// </summary>
    Task RecalculateAsync(int diningTableId, CancellationToken cancellationToken = default);
}
