using System.ComponentModel.DataAnnotations;

namespace Sofra.API.Options;

public sealed class ReservationOptions
{
    public const string SectionName = "Reservation";

    /// <summary>Korak izmedju ponudjenih termina u minutama (npr. 18:00, 18:30, 19:00...).</summary>
    [Range(5, 240)] public int SlotIntervalMinutes { get; set; } = 30;

    /// <summary>Koliko minuta prije pocetka potvrdjene rezervacije sto prelazi u status Reserved.</summary>
    [Range(5, 240)] public int UpcomingWindowMinutes { get; set; } = 30;
}
