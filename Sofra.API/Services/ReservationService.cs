using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Sofra.API.Data;
using Sofra.API.DTOs;
using Sofra.API.DTOs.Reservations;
using Sofra.API.Entities;
using Sofra.API.Enums;
using Sofra.API.Exceptions;
using Sofra.API.Hubs;
using Sofra.API.Hubs.Messages;
using Sofra.API.Options;
using Sofra.API.Requests.Reservations;
using Sofra.API.Services.Interfaces;
using Sofra.Shared.Events;

namespace Sofra.API.Services;

public class ReservationService(
    AppDbContext dbContext,
    IReservationStateMachine stateMachine,
    IDiningTableStatusService diningTableStatusService,
    IEventPublisher eventPublisher,
    IHubContext<OrderHub> orderHub,
    IOptions<RestaurantOptions> restaurantOptions,
    IOptions<ReservationOptions> reservationOptions) : IReservationService
{
    private static readonly ReservationStatus[] ActiveStatuses = [ReservationStatus.Pending, ReservationStatus.Confirmed];

    public async Task<PagedResult<ReservationListItemResponse>> GetListAsync(ReservationListRequest request, int actorUserId, bool isStaff, CancellationToken cancellationToken = default)
    {
        var query = dbContext.Reservations.AsNoTracking().AsQueryable();

        // Gost vidi iskljucivo svoje rezervacije - filter po vlastitom Id-u nadjacava eventualni userId iz upita.
        var effectiveUserId = isStaff ? request.UserId : actorUserId;
        if (effectiveUserId.HasValue)
        {
            query = query.Where(x => x.UserId == effectiveUserId);
        }

        if (request.Status.HasValue)
        {
            query = query.Where(x => x.Status == request.Status);
        }

        if (request.ZoneId.HasValue)
        {
            query = query.Where(x => x.ZoneId == request.ZoneId);
        }

        if (request.DateFrom.HasValue)
        {
            query = query.Where(x => x.ReservationAt >= request.DateFrom);
        }

        if (request.DateTo.HasValue)
        {
            query = query.Where(x => x.ReservationAt <= request.DateTo);
        }

        query = request.SortDesc ? query.OrderByDescending(x => x.ReservationAt) : query.OrderBy(x => x.ReservationAt);

        var projected = query.Select(x => new ReservationListItemResponse(
            x.Id, x.UserId, x.User.FirstName + " " + x.User.LastName,
            x.ReservationAt, x.DurationMinutes, x.Guests,
            x.ZoneId, x.Zone.Name,
            x.DiningTableId, x.DiningTable == null ? null : x.DiningTable.Number,
            x.Status, x.CreatedAt));

        return await projected.ToPagedResultAsync(request, cancellationToken);
    }

    public async Task<ReservationResponse> GetByIdAsync(int id, int actorUserId, IReadOnlyCollection<string> actorRoles, bool isStaff, CancellationToken cancellationToken = default)
    {
        var reservation = await LoadDetailedAsync(id, cancellationToken)
            ?? throw new NotFoundException($"Rezervacija sa Id {id} ne postoji.");

        if (!isStaff && reservation.UserId != actorUserId)
        {
            throw new ForbiddenException("Ne možete pregledati tuđu rezervaciju.");
        }

        return ToResponse(reservation, stateMachine.GetAllowedTransitions(reservation, actorUserId, actorRoles));
    }

    public async Task<IReadOnlyList<ReservationSlotResponse>> GetAvailabilityAsync(ReservationAvailabilityRequest request, CancellationToken cancellationToken = default)
    {
        var restaurant = restaurantOptions.Value;
        var slotInterval = reservationOptions.Value.SlotIntervalMinutes;

        var openAt = request.Date.ToDateTime(TimeOnly.FromTimeSpan(restaurant.OpenTime));
        var closeAt = request.Date.ToDateTime(TimeOnly.FromTimeSpan(restaurant.CloseTime));

        // Zone ciji ukupni kapacitet uopste moze primiti ovoliko gostiju - grubi predfilter prije simulacije po stolu.
        var zoneIds = await dbContext.Zones.AsNoTracking()
            .Where(z => z.Capacity >= request.Guests)
            .Where(z => request.ZoneId == null || z.Id == request.ZoneId)
            .Select(z => z.Id)
            .ToListAsync(cancellationToken);

        if (zoneIds.Count == 0)
        {
            return [];
        }

        // Svi (ne samo dovoljno veliki) stolovi u tim zonama - simulacija mora znati i za male stolove
        // da bi ih ispravno dodijelila manjim nedodijeljenim rezervacijama umjesto da ih "otme" velikim upitima.
        var zoneTables = await dbContext.DiningTables.AsNoTracking()
            .Where(x => x.IsActive && zoneIds.Contains(x.ZoneId))
            .Select(x => new { x.Id, x.ZoneId, x.Capacity })
            .ToListAsync(cancellationToken);

        var tablesByZone = zoneTables
            .GroupBy(x => x.ZoneId)
            .ToDictionary(g => g.Key, g => g.Select(x => (x.Id, x.Capacity)).ToList());

        var dayReservations = await dbContext.Reservations.AsNoTracking()
            .Where(x => zoneIds.Contains(x.ZoneId))
            .Where(x => ActiveStatuses.Contains(x.Status))
            .Where(x => x.ReservationAt < closeAt && x.ReservationAt.AddMinutes(x.DurationMinutes) > openAt)
            .Select(x => new { x.ZoneId, x.DiningTableId, x.Guests, x.ReservationAt, x.DurationMinutes })
            .ToListAsync(cancellationToken);

        var reservationsByZone = dayReservations.GroupBy(x => x.ZoneId).ToDictionary(g => g.Key, g => g.ToList());

        var now = DateTime.UtcNow;
        var slots = new List<ReservationSlotResponse>();

        for (var slotStart = openAt; slotStart.AddMinutes(request.DurationMinutes) <= closeAt; slotStart = slotStart.AddMinutes(slotInterval))
        {
            if (slotStart <= now)
            {
                continue;
            }

            var slotEnd = slotStart.AddMinutes(request.DurationMinutes);
            var totalAvailable = 0;

            foreach (var zoneId in zoneIds)
            {
                if (!tablesByZone.TryGetValue(zoneId, out var tables) || tables.Count == 0)
                {
                    continue;
                }

                var overlapping = reservationsByZone.TryGetValue(zoneId, out var zoneReservations)
                    ? zoneReservations
                        .Where(r => r.ReservationAt < slotEnd && r.ReservationAt.AddMinutes(r.DurationMinutes) > slotStart)
                        .Select(r => (r.DiningTableId, r.Guests))
                        .ToList()
                    : [];

                var occupied = SimulateOccupiedTables(tables, overlapping);
                totalAvailable += tables.Count(t => t.Capacity >= request.Guests && !occupied.Contains(t.Id));
            }

            if (totalAvailable > 0)
            {
                slots.Add(new ReservationSlotResponse(slotStart, slotEnd, totalAvailable));
            }
        }

        return slots;
    }

    public async Task<ReservationResponse> CreateAsync(ReservationRequest request, int actorUserId, IReadOnlyCollection<string> actorRoles, CancellationToken cancellationToken = default)
    {
        var zone = await dbContext.Zones.FirstOrDefaultAsync(x => x.Id == request.ZoneId, cancellationToken)
            ?? throw new Exceptions.ValidationException(new Dictionary<string, string[]>
            {
                ["ZoneId"] = [$"Zona sa Id {request.ZoneId} ne postoji."],
            });

        if (request.Guests > zone.Capacity)
        {
            throw new BusinessException($"Broj gostiju ({request.Guests}) premašuje kapacitet zone '{zone.Name}' ({zone.Capacity}).");
        }

        var reservationEnd = request.ReservationAt.AddMinutes(request.DurationMinutes);
        var dayOpen = request.ReservationAt.Date + restaurantOptions.Value.OpenTime;
        var dayClose = request.ReservationAt.Date + restaurantOptions.Value.CloseTime;
        if (request.ReservationAt < dayOpen || reservationEnd > dayClose)
        {
            throw new BusinessException(
                $"Termin mora biti unutar radnog vremena ({restaurantOptions.Value.OpenTime:hh\\:mm}-{restaurantOptions.Value.CloseTime:hh\\:mm}).");
        }

        DiningTable? table = null;
        if (request.DiningTableId.HasValue)
        {
            table = await dbContext.DiningTables.FirstOrDefaultAsync(x => x.Id == request.DiningTableId, cancellationToken)
                ?? throw new Exceptions.ValidationException(new Dictionary<string, string[]>
                {
                    ["DiningTableId"] = [$"Sto sa Id {request.DiningTableId} ne postoji."],
                });

            if (table.ZoneId != request.ZoneId)
            {
                throw new Exceptions.ValidationException(new Dictionary<string, string[]>
                {
                    ["DiningTableId"] = ["Odabrani sto ne pripada odabranoj zoni."],
                });
            }

            if (request.Guests > table.Capacity)
            {
                throw new BusinessException($"Broj gostiju ({request.Guests}) premašuje kapacitet stola {table.Number} ({table.Capacity}).");
            }

            // Sto je eksplicitno trazen - direktna provjera preklapanja bas na taj sto je dovoljna,
            // jer taj sto vise nije dio "zajednickog fonda" koji simulacija dijeli nedodijeljenim rezervacijama.
            await EnsureNoOverlapAsync(table.Id, request.ReservationAt, request.DurationMinutes, excludeReservationId: null, cancellationToken);
        }
        else
        {
            // Bez dodijeljenog stola: rezervacija i dalje treba zauzeti "jedan sto" u zoni, inace bi neograniceno
            // mnogo nedodijeljenih rezervacija moglo "potvrditi" isti termin. Simuliraj zauzece cijele zone.
            var availableCount = await CountAvailableTablesAsync(request.ZoneId, request.ReservationAt, reservationEnd, request.Guests, excludeReservationId: null, cancellationToken);
            if (availableCount == 0)
            {
                throw new BusinessException($"Nema slobodnih stolova u zoni '{zone.Name}' za taj termin.");
            }
        }

        var hasActiveAtSameTime = await dbContext.Reservations.AnyAsync(x =>
            x.UserId == actorUserId && x.ReservationAt == request.ReservationAt && ActiveStatuses.Contains(x.Status), cancellationToken);
        if (hasActiveAtSameTime)
        {
            throw new Exceptions.ValidationException(new Dictionary<string, string[]>
            {
                ["ReservationAt"] = ["Već imate aktivnu rezervaciju za ovaj termin."],
            });
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var reservation = new Reservation
        {
            CreatedAt = DateTime.UtcNow,
            UserId = actorUserId,
            ReservationAt = request.ReservationAt,
            DurationMinutes = request.DurationMinutes,
            Guests = request.Guests,
            ZoneId = request.ZoneId,
            DiningTableId = table?.Id,
            Note = request.Note,
            Status = ReservationStatus.Pending,
        };

        dbContext.Reservations.Add(reservation);
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        var userName = await dbContext.Users.Where(x => x.Id == actorUserId)
            .Select(x => x.FirstName + " " + x.LastName)
            .FirstAsync(cancellationToken);

        // Nova (Pending) rezervacija - osoblje treba odmah da je vidi bez rucnog refresh-a.
        await orderHub.Clients.Group("staff").SendAsync(
            "reservationCreated",
            new ReservationCreatedMessage(reservation.Id, actorUserId, userName, reservation.ReservationAt, reservation.Guests, reservation.ZoneId, zone.Name),
            cancellationToken);

        return await GetByIdAsync(reservation.Id, actorUserId, actorRoles, isStaff: true, cancellationToken);
    }

    public async Task<ReservationResponse> TransitionAsync(int id, ReservationTransitionRequest request, int actorUserId, IReadOnlyCollection<string> actorRoles, CancellationToken cancellationToken = default)
    {
        var reservation = await dbContext.Reservations.Include(x => x.User).FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException($"Rezervacija sa Id {id} ne postoji.");

        stateMachine.Apply(reservation, request.Status, actorUserId, actorRoles, request.RejectReason, request.AlternativeAt);

        await dbContext.SaveChangesAsync(cancellationToken);

        if (reservation.DiningTableId.HasValue)
        {
            await diningTableStatusService.RecalculateAsync(reservation.DiningTableId.Value, cancellationToken);
        }

        // Objavljivanje ide tek nakon uspjesnog SaveChangesAsync, nikad unutar transakcije.
        await eventPublisher.PublishAsync(
            new ReservationProcessedEvent(
                Guid.NewGuid(), DateTime.UtcNow,
                reservation.Id, reservation.UserId, reservation.User.Email ?? string.Empty, $"{reservation.User.FirstName} {reservation.User.LastName}",
                reservation.ReservationAt, ReservationStateMachine.GetStatusLabel(reservation.Status),
                reservation.RejectReason, reservation.AlternativeAt),
            EventRoutingKeys.ReservationProcessed,
            cancellationToken);

        return await GetByIdAsync(id, actorUserId, actorRoles, isStaff: true, cancellationToken);
    }

    public async Task<ReservationResponse> AssignTableAsync(
        int id, AssignReservationTableRequest request, int actorUserId, IReadOnlyCollection<string> actorRoles, CancellationToken cancellationToken = default)
    {
        var reservation = await dbContext.Reservations.FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException($"Rezervacija sa Id {id} ne postoji.");

        if (reservation.Status is not (ReservationStatus.Pending or ReservationStatus.Confirmed))
        {
            throw new BusinessException($"Rezervaciji u statusu '{reservation.Status}' se ne može dodijeliti sto.");
        }

        var table = await dbContext.DiningTables.FirstOrDefaultAsync(x => x.Id == request.DiningTableId, cancellationToken)
            ?? throw new Exceptions.ValidationException(new Dictionary<string, string[]>
            {
                ["DiningTableId"] = [$"Sto sa Id {request.DiningTableId} ne postoji."],
            });

        if (table.ZoneId != reservation.ZoneId)
        {
            throw new Exceptions.ValidationException(new Dictionary<string, string[]>
            {
                ["DiningTableId"] = ["Odabrani sto ne pripada zoni rezervacije."],
            });
        }

        if (reservation.Guests > table.Capacity)
        {
            throw new BusinessException($"Broj gostiju ({reservation.Guests}) premašuje kapacitet stola {table.Number} ({table.Capacity}).");
        }

        await EnsureNoOverlapAsync(table.Id, reservation.ReservationAt, reservation.DurationMinutes, excludeReservationId: reservation.Id, cancellationToken);

        var previousTableId = reservation.DiningTableId;
        reservation.DiningTableId = table.Id;
        await dbContext.SaveChangesAsync(cancellationToken);

        if (previousTableId.HasValue && previousTableId != table.Id)
        {
            await diningTableStatusService.RecalculateAsync(previousTableId.Value, cancellationToken);
        }

        await diningTableStatusService.RecalculateAsync(table.Id, cancellationToken);

        return await GetByIdAsync(id, actorUserId, actorRoles, isStaff: true, cancellationToken);
    }

    private async Task<int> CountAvailableTablesAsync(int zoneId, DateTime windowStart, DateTime windowEnd, int guests, int? excludeReservationId, CancellationToken cancellationToken)
    {
        var tables = await dbContext.DiningTables.AsNoTracking()
            .Where(x => x.IsActive && x.ZoneId == zoneId)
            .Select(x => new { x.Id, x.Capacity })
            .ToListAsync(cancellationToken);

        var overlapping = await dbContext.Reservations.AsNoTracking()
            .Where(x => x.ZoneId == zoneId)
            .Where(x => ActiveStatuses.Contains(x.Status))
            .Where(x => excludeReservationId == null || x.Id != excludeReservationId)
            .Where(x => x.ReservationAt < windowEnd && x.ReservationAt.AddMinutes(x.DurationMinutes) > windowStart)
            .Select(x => new { x.DiningTableId, x.Guests })
            .ToListAsync(cancellationToken);

        var occupied = SimulateOccupiedTables(
            tables.Select(t => (t.Id, t.Capacity)).ToList(),
            overlapping.Select(r => (r.DiningTableId, r.Guests)).ToList());

        return tables.Count(t => t.Capacity >= guests && !occupied.Contains(t.Id));
    }

    /// <summary>
    /// Simulira koji stolovi ostaju zauzeti u jednoj zoni za dati skup aktivnih rezervacija koje se preklapaju
    /// s posmatranim terminom: rezervacije s dodijeljenim stolom zauzimaju bas taj sto; rezervacije bez stola
    /// se pohlepno dodjeljuju najmanjem preostalom slobodnom stolu koji prima njihov broj gostiju (first-fit
    /// po rastucem kapacitetu, manje rezervacije prve). Ovo sprecava da neograniceno mnogo nedodijeljenih
    /// rezervacija "potvrdi" isti termin - svaka aktivna rezervacija stvarno zauzima jedan sto.
    /// </summary>
    private static HashSet<int> SimulateOccupiedTables(
        List<(int Id, int Capacity)> zoneTables,
        List<(int? DiningTableId, int Guests)> overlappingReservations)
    {
        var occupied = new HashSet<int>();

        foreach (var tableId in overlappingReservations.Where(x => x.DiningTableId.HasValue).Select(x => x.DiningTableId!.Value))
        {
            if (zoneTables.Any(t => t.Id == tableId))
            {
                occupied.Add(tableId);
            }
        }

        var freeTablesAscending = zoneTables
            .Where(t => !occupied.Contains(t.Id))
            .OrderBy(t => t.Capacity)
            .ToList();

        foreach (var guests in overlappingReservations.Where(x => !x.DiningTableId.HasValue).Select(x => x.Guests).OrderBy(g => g))
        {
            var matchIndex = freeTablesAscending.FindIndex(t => t.Capacity >= guests);
            if (matchIndex < 0)
            {
                // Nema odgovarajuceg stola za ovu (hipotetsku) rezervaciju - u stvarnosti se ne bi mogla
                // potvrditi bez dodijeljenog stola, pa je ignorisemo u simulaciji zauzeca.
                continue;
            }

            occupied.Add(freeTablesAscending[matchIndex].Id);
            freeTablesAscending.RemoveAt(matchIndex);
        }

        return occupied;
    }

    private async Task EnsureNoOverlapAsync(int diningTableId, DateTime reservationAt, int durationMinutes, int? excludeReservationId, CancellationToken cancellationToken)
    {
        var newEnd = reservationAt.AddMinutes(durationMinutes);

        var overlaps = await dbContext.Reservations.AnyAsync(x =>
            x.DiningTableId == diningTableId &&
            (excludeReservationId == null || x.Id != excludeReservationId) &&
            ActiveStatuses.Contains(x.Status) &&
            x.ReservationAt < newEnd &&
            x.ReservationAt.AddMinutes(x.DurationMinutes) > reservationAt, cancellationToken);

        if (overlaps)
        {
            throw new BusinessException("Sto je već rezervisan u tom terminu.");
        }
    }

    private Task<Reservation?> LoadDetailedAsync(int id, CancellationToken cancellationToken) =>
        dbContext.Reservations.AsNoTracking()
            .Include(x => x.User)
            .Include(x => x.Zone)
            .Include(x => x.DiningTable)
            .Include(x => x.ProcessedBy)
            .Include(x => x.CancelledBy)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    private static ReservationResponse ToResponse(Reservation reservation, IReadOnlyList<AllowedTransition> allowedTransitions) => new(
        reservation.Id, reservation.UserId, reservation.User.FirstName + " " + reservation.User.LastName,
        reservation.ReservationAt, reservation.DurationMinutes, reservation.Guests,
        reservation.ZoneId, reservation.Zone.Name,
        reservation.DiningTableId, reservation.DiningTable?.Number,
        reservation.Note, reservation.Status,
        reservation.RejectReason, reservation.AlternativeAt,
        reservation.ProcessedById, reservation.ProcessedBy == null ? null : reservation.ProcessedBy.FirstName + " " + reservation.ProcessedBy.LastName, reservation.ProcessedAt,
        reservation.CancelledById, reservation.CancelledBy == null ? null : reservation.CancelledBy.FirstName + " " + reservation.CancelledBy.LastName, reservation.CancelledAt,
        reservation.CreatedAt, allowedTransitions);
}
