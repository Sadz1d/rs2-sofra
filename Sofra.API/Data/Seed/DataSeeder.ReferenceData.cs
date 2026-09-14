using Microsoft.EntityFrameworkCore;
using Sofra.API.Entities;

namespace Sofra.API.Data.Seed;

/// <summary>
/// Grupa 2: sifarnici. Svaka tabela se provjerava nezavisno (AnyAsync) da djelimican prekid
/// seed-a (npr. pad kontejnera nakon drzava, prije gradova) ne ostavi trajnu prazninu.
/// </summary>
public partial class DataSeeder
{
    private async Task SeedReferenceDataAsync(CancellationToken cancellationToken)
    {
        await SeedCountriesAndCitiesAsync(cancellationToken);
        await SeedMenuCategoriesAsync(cancellationToken);
        await SeedAllergensAsync(cancellationToken);
        await SeedDietaryTagsAsync(cancellationToken);
        await SeedZonesAsync(cancellationToken);
        await SeedTableTypesAsync(cancellationToken);
        await SeedPaymentMethodsAsync(cancellationToken);
        await SeedUnitsOfMeasureAsync(cancellationToken);
        await SeedInventoryCategoriesAsync(cancellationToken);
    }

    private async Task SeedCountriesAndCitiesAsync(CancellationToken cancellationToken)
    {
        if (!await dbContext.Countries.AnyAsync(cancellationToken))
        {
            dbContext.Countries.AddRange(
                new Country { Name = "Bosna i Hercegovina", Code = "BA" },
                new Country { Name = "Hrvatska", Code = "HR" });
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        if (!await dbContext.Cities.AnyAsync(cancellationToken))
        {
            var bih = await dbContext.Countries.FirstAsync(x => x.Code == "BA", cancellationToken);
            var hrvatska = await dbContext.Countries.FirstAsync(x => x.Code == "HR", cancellationToken);

            dbContext.Cities.AddRange(
                new City { Name = "Mostar", CountryId = bih.Id },
                new City { Name = "Sarajevo", CountryId = bih.Id },
                new City { Name = "Banja Luka", CountryId = bih.Id },
                new City { Name = "Tuzla", CountryId = bih.Id },
                new City { Name = "Zenica", CountryId = bih.Id },
                new City { Name = "Široki Brijeg", CountryId = bih.Id },
                new City { Name = "Zagreb", CountryId = hrvatska.Id },
                new City { Name = "Split", CountryId = hrvatska.Id });
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task SeedMenuCategoriesAsync(CancellationToken cancellationToken)
    {
        if (await dbContext.MenuCategories.AnyAsync(cancellationToken))
        {
            return;
        }

        string[] names = ["Predjela", "Supe i čorbe", "Salate", "Glavna jela", "Roštilj", "Riblja jela", "Deserti", "Pića"];
        dbContext.MenuCategories.AddRange(names.Select((name, index) => new MenuCategory
        {
            Name = name,
            SortOrder = index + 1,
            IsActive = true,
        }));
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedAllergensAsync(CancellationToken cancellationToken)
    {
        if (await dbContext.Allergens.AnyAsync(cancellationToken))
        {
            return;
        }

        // Sluzbena EU lista od 14 alergena koje je obavezno oznaciti u ugostiteljstvu.
        string[] names =
        [
            "Gluten", "Rakovi", "Jaja", "Riba", "Kikiriki", "Soja", "Mlijeko",
            "Orašasti plodovi", "Celer", "Slačica", "Sezam", "Sumporni dioksid i sulfiti",
            "Lupina", "Mekušci",
        ];
        dbContext.Allergens.AddRange(names.Select(name => new Allergen { Name = name }));
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedDietaryTagsAsync(CancellationToken cancellationToken)
    {
        if (await dbContext.DietaryTags.AnyAsync(cancellationToken))
        {
            return;
        }

        string[] names = ["Vegetarijansko", "Vegansko", "Bez glutena", "Bez laktoze", "Ljuto"];
        dbContext.DietaryTags.AddRange(names.Select(name => new DietaryTag { Name = name }));
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedZonesAsync(CancellationToken cancellationToken)
    {
        if (await dbContext.Zones.AnyAsync(cancellationToken))
        {
            return;
        }

        dbContext.Zones.AddRange(
            new Zone { Name = "Sala", Capacity = 60 },
            new Zone { Name = "Terasa", Capacity = 40 },
            new Zone { Name = "VIP", Capacity = 16 });
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedTableTypesAsync(CancellationToken cancellationToken)
    {
        if (await dbContext.TableTypes.AnyAsync(cancellationToken))
        {
            return;
        }

        string[] names = ["Standardni", "Visoki", "Separe", "Baštenski"];
        dbContext.TableTypes.AddRange(names.Select(name => new TableType { Name = name }));
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedPaymentMethodsAsync(CancellationToken cancellationToken)
    {
        if (await dbContext.PaymentMethods.AnyAsync(cancellationToken))
        {
            return;
        }

        dbContext.PaymentMethods.AddRange(
            new PaymentMethod { Name = "Kartica", Code = "CARD" },
            new PaymentMethod { Name = "Gotovina", Code = "CASH" });
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedUnitsOfMeasureAsync(CancellationToken cancellationToken)
    {
        if (await dbContext.UnitsOfMeasure.AnyAsync(cancellationToken))
        {
            return;
        }

        (string Name, string Abbreviation)[] units =
        [
            ("Kilogram", "kg"),
            ("Gram", "g"),
            ("Litar", "l"),
            ("Mililitar", "ml"),
            ("Komad", "kom"),
            ("Pakovanje", "pak"),
        ];
        dbContext.UnitsOfMeasure.AddRange(units.Select(u => new UnitOfMeasure { Name = u.Name, Abbreviation = u.Abbreviation }));
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedInventoryCategoriesAsync(CancellationToken cancellationToken)
    {
        if (await dbContext.InventoryCategories.AnyAsync(cancellationToken))
        {
            return;
        }

        string[] names = ["Meso i perad", "Riba i plodovi mora", "Povrće", "Voće", "Mliječni proizvodi", "Začini i dodaci", "Pića"];
        dbContext.InventoryCategories.AddRange(names.Select(name => new InventoryCategory { Name = name }));
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
