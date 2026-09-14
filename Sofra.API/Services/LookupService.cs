using Microsoft.EntityFrameworkCore;
using Sofra.API.Data;
using Sofra.API.DTOs;
using Sofra.API.Entities;
using Sofra.API.Exceptions;
using Sofra.API.Requests;

namespace Sofra.API.Services;

/// <summary>Broj zapisa koji trenutno koriste sifarnik, sa gramatickim oblicima imenice za poruku.</summary>
public readonly record struct LookupUsage(int Count, string Singular, string Few, string Many)
{
    public static readonly LookupUsage None = new(0, string.Empty, string.Empty, string.Empty);
}

/// <summary>
/// Zajednicka CRUD implementacija za sifarnike (MenuCategory, Allergen, DietaryTag, Zone, TableType,
/// PaymentMethod, UnitOfMeasure, InventoryCategory, Country, City). Izvedeni servisi definisu samo
/// ono sto je stvarno specificno: DbSet, mapiranje polja, unique provjeru i provjeru upotrebe prije
/// brisanja. Pretraga/sortiranje po Name i standardna "nije pronadjeno"/"u upotrebi" poruka su ovdje.
/// </summary>
public abstract class LookupService<TEntity, TResponse, TRequest>(AppDbContext dbContext)
    where TEntity : class, INamedEntity
{
    protected AppDbContext DbContext { get; } = dbContext;

    protected abstract DbSet<TEntity> Set { get; }

    /// <summary>Naziv sifarnika na bosanskom, za poruke ("Kategorija", "Alergen", "Zona"...).</summary>
    protected abstract string EntityLabel { get; }

    protected abstract System.Linq.Expressions.Expression<Func<TEntity, TResponse>> ProjectToResponse { get; }

    protected abstract TEntity CreateEntity(TRequest request);

    protected abstract void UpdateEntity(TEntity entity, TRequest request);

    /// <summary>Baca Exceptions.ValidationException (polje-kljuc -> poruka) ako zahtjev krsi unique ogranicenje.</summary>
    protected abstract Task EnsureUniqueAsync(TRequest request, int? excludeId, CancellationToken cancellationToken);

    /// <summary>Broj zapisa koji koriste ovaj sifarnik (0 = moze se obrisati).</summary>
    protected abstract Task<LookupUsage> GetUsageAsync(int id, CancellationToken cancellationToken);

    protected virtual string NotFoundMessage(int id) => $"{EntityLabel} sa Id {id} ne postoji.";

    protected virtual IQueryable<TEntity> ApplySearch(IQueryable<TEntity> query, string? search) =>
        string.IsNullOrWhiteSpace(search) ? query : query.Where(x => x.Name.Contains(search));

    protected virtual IQueryable<TEntity> ApplySort(IQueryable<TEntity> query, string? sortBy, bool descending) =>
        descending ? query.OrderByDescending(x => x.Name) : query.OrderBy(x => x.Name);

    /// <summary>
    /// Kuka za filtere koje ima samo pojedini sifarnik (npr. City.CountryId, MenuCategory.IsActive).
    /// Podrazumijevano ne radi nista; izvedeni servis prepozna svoj konkretni PagedRequest podtip
    /// preko pattern-matcha (npr. "request is CityListRequest { CountryId: not null } r").
    /// </summary>
    protected virtual IQueryable<TEntity> ApplyExtraFilters(IQueryable<TEntity> query, PagedRequest request) => query;

    public virtual async Task<PagedResult<TResponse>> GetListAsync(PagedRequest request, CancellationToken cancellationToken = default)
    {
        var query = ApplySearch(Set.AsNoTracking(), request.Search);
        query = ApplyExtraFilters(query, request);
        query = ApplySort(query, request.SortBy, request.SortDesc);
        return await query.Select(ProjectToResponse).ToPagedResultAsync(request, cancellationToken);
    }

    public virtual async Task<TResponse> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var response = await Set.AsNoTracking()
            .Where(x => x.Id == id)
            .Select(ProjectToResponse)
            .FirstOrDefaultAsync(cancellationToken);

        return response ?? throw new NotFoundException(NotFoundMessage(id));
    }

    public virtual async Task<TResponse> CreateAsync(TRequest request, CancellationToken cancellationToken = default)
    {
        await EnsureUniqueAsync(request, excludeId: null, cancellationToken);

        var entity = CreateEntity(request);
        Set.Add(entity);
        await DbContext.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(entity.Id, cancellationToken);
    }

    public virtual async Task<TResponse> UpdateAsync(int id, TRequest request, CancellationToken cancellationToken = default)
    {
        var entity = await Set.FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException(NotFoundMessage(id));

        await EnsureUniqueAsync(request, excludeId: id, cancellationToken);

        UpdateEntity(entity, request);
        await DbContext.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(id, cancellationToken);
    }

    public virtual async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await Set.FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException(NotFoundMessage(id));

        var usage = await GetUsageAsync(id, cancellationToken);
        if (usage.Count > 0)
        {
            var noun = BosnianPluralizer.Pluralize(usage.Count, usage.Singular, usage.Few, usage.Many);
            throw new BusinessException($"{EntityLabel} '{entity.Name}' se ne može obrisati jer je koristi {usage.Count} {noun}.");
        }

        Set.Remove(entity);
        await DbContext.SaveChangesAsync(cancellationToken);
    }
}
