using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Sofra.API.Data;
using Sofra.API.DTOs;
using Sofra.API.DTOs.Reservations;
using Sofra.API.Entities;
using Sofra.API.Enums;
using Sofra.API.Exceptions;
using Sofra.API.Options;
using Sofra.API.Requests.Reservations;
using Sofra.API.Services.Interfaces;

namespace Sofra.API.Services;

public class ReservationService(
    AppDbContext dbContext,
    IReservationStateMachine stateMachine,
    IDiningTableStatusService diningTableStatusService,
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

    public async Task<ReservationResponse> GetByIdAsync(int id, int actorUserId, bool isStaff, CancellationToken cancellationToken = default)
    {
        var reservation = await LoadDetailedAsync(id, cancellationToken)
            ?? throw new NotFoundException($"Rezervacija sa Id {id} ne postoji.");

        if (!isStaff && reservation.UserId != actorUserId)
        {
            throw new ForbiddenException("Ne možete pregledati tuđu rezervaciju.");
        }

        return ToResponse(reservation);
    }

    public async Task<IReadOnlyList<ReservationSlotResponse>> GetAvailabilityAsync(ReservationAvailabilityRequest request, CancellationToken cancellationToken = default)
    {
        var restaurant = restaurantOptions.Value;
        var slotInterval = reservationOptions.Value.SlotIntervalMinutes;

        var openAt = request.Date.ToDateTime(TimeOnly.FromTimeSpan(restaurant.OpenTime));
        var closeAt = request.Date.ToDateTime(TimeOnly.FromTimeSpan(restaurant.CloseTime));

        var candidateTables = await dbContext.DiningTables.AsNoTracking()
            .Where(x => x.IsActive && x.Capacity >= request.Guests && x.Zone.Capacity >= request.Guests)
            .Where(x => request.ZoneId == null || x.ZoneId == request.ZoneId)
            .Select(x => new { x.Id })
            .ToListAsync(cancellationToken);

        if (candidateTables.Count == 0)
        {
            return [];
        }

        var candidateTableIds = candidateTables.Select(x => x.Id).ToHashSet();

        var dayReservations = await dbContext.Reservations.AsNoTracking()
            .Where(x => x.DiningTableId != null && candidateTableIds.Contains(x.DiningTableId!.Value))
            .Where(x => ActiveStatuses.Contains(x.Status))
            .Where(x => x.ReservationAt < closeAt && x.ReservationAt.AddMinutes(x.DurationMinutes) > openAt)
            .Select(x => new { x.DiningTableId, x.ReservationAt, x.DurationMinutes })
            .ToListAsync(cancellationToken);

        var now = DateTime.UtcNow;
        var slots = new List<ReservationSlotResponse>();

        for (var slotStart = openAt; slotStart.AddMinutes(request.DurationMinutes) <= closeAt; slotStart = slotStart.AddMinutes(slotInterval))
        {
            if (slotStart <= now)
            {
                continue;
            }

            var slotEnd = slotStart.AddMinutes(request.DurationMinutes);

            var availableTables = candidateTableIds.Count(tableId => !dayReservations.Any(r =>
                r.DiningTableId == tableId &&
                r.ReservationAt < slotEnd &&
                r.ReservationAt.AddMinutes(r.DurationMinutes) > slotStart));

            if (availableTables > 0)
            {
                slots.Add(new ReservationSlotResponse(slotStart, slotEnd, availableTables));
            }
        }

        return slots;
    }

    public async Task<ReservationResponse> CreateAsync(ReservationRequest request, int actorUserId, CancellationToken cancellationToken = default)
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

            await EnsureNoOverlapAsync(table.Id, request.ReservationAt, request.DurationMinutes, excludeReservationId: null, cancellationToken);
        }

        var reservationEnd = request.ReservationAt.AddMinutes(request.DurationMinutes);
        var dayOpen = request.ReservationAt.Date + restaurantOptions.Value.OpenTime;
        var dayClose = request.ReservationAt.Date + restaurantOptions.Value.CloseTime;
        if (request.ReservationAt < dayOpen || reservationEnd > dayClose)
        {
            throw new BusinessException(
                $"Termin mora biti unutar radnog vremena ({restaurantOptions.Value.OpenTime:hh\\:mm}-{restaurantOptions.Value.CloseTime:hh\\:mm}).");
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

        return await GetByIdAsync(reservation.Id, actorUserId, isStaff: true, cancellationToken);
    }

    public async Task<ReservationResponse> TransitionAsync(int id, ReservationTransitionRequest request, int actorUserId, IReadOnlyCollection<string> actorRoles, CancellationToken cancellationToken = default)
    {
        var reservation = await dbContext.Reservations.FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException($"Rezervacija sa Id {id} ne postoji.");

        stateMachine.Apply(reservation, request.Status, actorUserId, actorRoles, request.RejectReason, request.AlternativeAt);

        await dbContext.SaveChangesAsync(cancellationToken);

        if (reservation.DiningTableId.HasValue)
        {
            await diningTableStatusService.RecalculateAsync(reservation.DiningTableId.Value, cancellationToken);
        }

        return await GetByIdAsync(id, actorUserId, isStaff: true, cancellationToken);
    }

    public async Task<ReservationResponse> AssignTableAsync(int id, AssignReservationTableRequest request, CancellationToken cancellationToken = default)
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

        return await GetByIdAsync(id, reservation.UserId, isStaff: true, cancellationToken);
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

    private static ReservationResponse ToResponse(Reservation reservation) => new(
        reservation.Id, reservation.UserId, reservation.User.FirstName + " " + reservation.User.LastName,
        reservation.ReservationAt, reservation.DurationMinutes, reservation.Guests,
        reservation.ZoneId, reservation.Zone.Name,
        reservation.DiningTableId, reservation.DiningTable?.Number,
        reservation.Note, reservation.Status,
        reservation.RejectReason, reservation.AlternativeAt,
        reservation.ProcessedById, reservation.ProcessedBy == null ? null : reservation.ProcessedBy.FirstName + " " + reservation.ProcessedBy.LastName, reservation.ProcessedAt,
        reservation.CancelledById, reservation.CancelledBy == null ? null : reservation.CancelledBy.FirstName + " " + reservation.CancelledBy.LastName, reservation.CancelledAt,
        reservation.CreatedAt);
}
