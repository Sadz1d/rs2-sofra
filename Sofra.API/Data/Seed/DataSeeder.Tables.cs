using Microsoft.EntityFrameworkCore;
using Sofra.API.Entities;
using Sofra.API.Enums;

namespace Sofra.API.Data.Seed;

/// <summary>
/// Grupa 4: stolovi. Rasporedjeni po 3 zone (Sala/Terasa/VIP), zbir kapaciteta stolova po zoni
/// ne prelazi kapacitet same zone iz sifarnika.
/// </summary>
public partial class DataSeeder
{
    private async Task SeedDiningTablesAsync(CancellationToken cancellationToken)
    {
        if (await dbContext.DiningTables.AnyAsync(cancellationToken))
        {
            return;
        }

        var zones = await dbContext.Zones.ToDictionaryAsync(x => x.Name, x => x.Id, cancellationToken);
        var tableTypes = await dbContext.TableTypes.ToDictionaryAsync(x => x.Name, x => x.Id, cancellationToken);
        var waiters = await dbContext.Users
            .Where(u => u.UserName == "konobar" || u.UserName == "konobar2" || u.UserName == "konobar3")
            .ToDictionaryAsync(u => u.UserName!, u => u.Id, cancellationToken);

        (int Number, int Capacity, string Zone, string TableType, string QrCode, TableStatus Status, string Waiter)[] tables =
        [
            (1, 2, "Sala", "Standardni", "QR-SALA-01", TableStatus.Free, "konobar"),
            (2, 2, "Sala", "Standardni", "QR-SALA-02", TableStatus.Free, "konobar"),
            (3, 4, "Sala", "Standardni", "QR-SALA-03", TableStatus.Occupied, "konobar"),
            (4, 4, "Sala", "Standardni", "QR-SALA-04", TableStatus.Free, "konobar2"),
            (5, 4, "Sala", "Standardni", "QR-SALA-05", TableStatus.Free, "konobar2"),
            (6, 4, "Sala", "Visoki", "QR-SALA-06", TableStatus.Reserved, "konobar2"),
            (7, 6, "Sala", "Standardni", "QR-SALA-07", TableStatus.Free, "konobar3"),
            (8, 6, "Sala", "Standardni", "QR-SALA-08", TableStatus.Free, "konobar3"),
            (9, 8, "Sala", "Standardni", "QR-SALA-09", TableStatus.Free, "konobar3"),
            (10, 2, "Terasa", "Baštenski", "QR-TERASA-10", TableStatus.Free, "konobar"),
            (11, 4, "Terasa", "Baštenski", "QR-TERASA-11", TableStatus.Occupied, "konobar"),
            (12, 4, "Terasa", "Baštenski", "QR-TERASA-12", TableStatus.Free, "konobar2"),
            (13, 4, "Terasa", "Baštenski", "QR-TERASA-13", TableStatus.Free, "konobar2"),
            (14, 6, "Terasa", "Baštenski", "QR-TERASA-14", TableStatus.Reserved, "konobar3"),
            (15, 6, "Terasa", "Baštenski", "QR-TERASA-15", TableStatus.Free, "konobar3"),
            (16, 4, "VIP", "Separe", "QR-VIP-16", TableStatus.Free, "konobar"),
            (17, 6, "VIP", "Separe", "QR-VIP-17", TableStatus.Free, "konobar2"),
            (18, 6, "VIP", "Separe", "QR-VIP-18", TableStatus.Free, "konobar3"),
        ];

        foreach (var t in tables)
        {
            dbContext.DiningTables.Add(new DiningTable
            {
                Number = t.Number,
                Capacity = t.Capacity,
                ZoneId = zones[t.Zone],
                TableTypeId = tableTypes[t.TableType],
                Status = t.Status,
                WaiterId = waiters[t.Waiter],
                QrCode = t.QrCode,
                IsActive = true,
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
