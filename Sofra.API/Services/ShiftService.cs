using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Sofra.API.Data;
using Sofra.API.DTOs;
using Sofra.API.DTOs.Shifts;
using Sofra.API.Entities;
using Sofra.API.Exceptions;
using Sofra.API.Requests.Shifts;
using Sofra.API.Services.Interfaces;

namespace Sofra.API.Services;

public class ShiftService(AppDbContext dbContext) : IShiftService
{
    private static readonly Expression<Func<Shift, ShiftResponse>> ProjectToResponse = x => new ShiftResponse(
        x.Id, x.EmployeeId, x.Employee.UserId, x.Employee.User.FirstName + " " + x.Employee.User.LastName,
        x.Date, x.StartTime, x.EndTime, x.Note);

    public async Task<PagedResult<ShiftResponse>> GetListAsync(ShiftListRequest request, CancellationToken cancellationToken = default)
    {
        var query = dbContext.Shifts.AsNoTracking().AsQueryable();

        if (request.UserId.HasValue)
        {
            query = query.Where(x => x.Employee.UserId == request.UserId);
        }

        if (request.DateFrom.HasValue)
        {
            query = query.Where(x => x.Date >= request.DateFrom);
        }

        if (request.DateTo.HasValue)
        {
            query = query.Where(x => x.Date <= request.DateTo);
        }

        query = request.SortDesc ? query.OrderByDescending(x => x.Date).ThenByDescending(x => x.StartTime) : query.OrderBy(x => x.Date).ThenBy(x => x.StartTime);

        return await query.Select(ProjectToResponse).ToPagedResultAsync(request, cancellationToken);
    }

    public async Task<ShiftResponse> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var response = await dbContext.Shifts.AsNoTracking().Where(x => x.Id == id).Select(ProjectToResponse).FirstOrDefaultAsync(cancellationToken);
        return response ?? throw new NotFoundException($"Smjena sa Id {id} ne postoji.");
    }

    public async Task<ShiftResponse> CreateAsync(ShiftRequest request, CancellationToken cancellationToken = default)
    {
        var employeeId = await ResolveEmployeeIdAsync(request.UserId, cancellationToken);
        await EnsureNoOverlapAsync(employeeId, request, excludeId: null, cancellationToken);

        var entity = new Shift
        {
            EmployeeId = employeeId,
            Date = request.Date,
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            Note = request.Note,
        };

        dbContext.Shifts.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(entity.Id, cancellationToken);
    }

    public async Task<ShiftResponse> UpdateAsync(int id, ShiftRequest request, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.Shifts.FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException($"Smjena sa Id {id} ne postoji.");

        var employeeId = await ResolveEmployeeIdAsync(request.UserId, cancellationToken);
        await EnsureNoOverlapAsync(employeeId, request, excludeId: id, cancellationToken);

        entity.EmployeeId = employeeId;
        entity.Date = request.Date;
        entity.StartTime = request.StartTime;
        entity.EndTime = request.EndTime;
        entity.Note = request.Note;

        await dbContext.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.Shifts.FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException($"Smjena sa Id {id} ne postoji.");

        dbContext.Shifts.Remove(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<int> ResolveEmployeeIdAsync(int userId, CancellationToken cancellationToken)
    {
        var employeeId = await dbContext.Employees.Where(x => x.UserId == userId).Select(x => x.Id).FirstOrDefaultAsync(cancellationToken);
        if (employeeId == 0)
        {
            throw new Exceptions.ValidationException(new Dictionary<string, string[]>
            {
                ["UserId"] = [$"Korisnik sa Id {userId} nije evidentiran kao zaposlenik."],
            });
        }

        return employeeId;
    }

    private async Task EnsureNoOverlapAsync(int employeeId, ShiftRequest request, int? excludeId, CancellationToken cancellationToken)
    {
        var overlaps = await dbContext.Shifts.AnyAsync(x =>
            x.EmployeeId == employeeId &&
            x.Date == request.Date &&
            (excludeId == null || x.Id != excludeId) &&
            x.StartTime < request.EndTime && x.EndTime > request.StartTime, cancellationToken);

        if (overlaps)
        {
            throw new BusinessException("Zaposlenik već ima smjenu koja se preklapa s ovim terminom.");
        }
    }
}
