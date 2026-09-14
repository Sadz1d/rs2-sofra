using Microsoft.EntityFrameworkCore;
using Sofra.API.Entities;
using Sofra.API.Enums;

namespace Sofra.API.Data.Seed;

/// <summary>
/// Grupa 3: jela sa slikama i normativima. Namirnice (InventoryItem) se seed-uju ovdje - prije jela -
/// jer normativ (MenuItemIngredient) zahtijeva da FK ciljevi vec postoje; docs/entiteti.md ih formalno
/// svrstava pod "stolovi i namirnice" ali stvarni redoslijed upisa mora biti obrnut.
/// </summary>
public partial class DataSeeder
{
    private async Task SeedMenuAsync(CancellationToken cancellationToken)
    {
        await SeedInventoryItemsAsync(cancellationToken);
        await SeedMenuItemsAsync(cancellationToken);
    }

    private async Task SeedInventoryItemsAsync(CancellationToken cancellationToken)
    {
        if (await dbContext.InventoryItems.AnyAsync(cancellationToken))
        {
            return;
        }

        var categories = await dbContext.InventoryCategories.ToDictionaryAsync(x => x.Name, x => x.Id, cancellationToken);
        var units = await dbContext.UnitsOfMeasure.ToDictionaryAsync(x => x.Abbreviation, x => x.Id, cancellationToken);

        // Quantity ispod MinQuantity je namjerno za par artikala - realan signal za LowStockDetected (Faza 2).
        (string Name, string Category, string Unit, decimal Quantity, decimal MinQuantity, decimal UnitCost)[] items =
        [
            ("Juneće mljeveno meso", "Meso i perad", "kg", 25m, 8m, 14.5m),
            ("Pileći file", "Meso i perad", "kg", 20m, 6m, 11.0m),
            ("Pileći batak", "Meso i perad", "kg", 18m, 6m, 8.5m),
            ("Teletina", "Meso i perad", "kg", 15m, 5m, 18.0m),
            ("Svinjski but", "Meso i perad", "kg", 12m, 4m, 12.0m),
            ("Kobasice", "Meso i perad", "kg", 10m, 3m, 10.0m),
            ("Pršut", "Meso i perad", "kg", 3m, 5m, 28.0m),
            ("Slanina", "Meso i perad", "kg", 6m, 2m, 9.0m),

            ("Losos", "Riba i plodovi mora", "kg", 8m, 3m, 32.0m),
            ("Oslić", "Riba i plodovi mora", "kg", 10m, 3m, 16.0m),
            ("Škampi", "Riba i plodovi mora", "kg", 5m, 2m, 38.0m),
            ("Lignje", "Riba i plodovi mora", "kg", 6m, 2m, 22.0m),

            ("Krompir", "Povrće", "kg", 60m, 15m, 1.2m),
            ("Crni luk", "Povrće", "kg", 25m, 8m, 1.5m),
            ("Bijeli luk", "Povrće", "kg", 5m, 2m, 6.0m),
            ("Paradajz", "Povrće", "kg", 20m, 6m, 3.5m),
            ("Krastavac", "Povrće", "kg", 15m, 5m, 2.8m),
            ("Paprika", "Povrće", "kg", 18m, 6m, 4.0m),
            ("Kupus", "Povrće", "kg", 20m, 6m, 1.4m),
            ("Šargarepa", "Povrće", "kg", 15m, 5m, 1.6m),
            ("Zelena salata", "Povrće", "kom", 30m, 10m, 1.0m),
            ("Tikvica", "Povrće", "kg", 10m, 3m, 2.6m),
            ("Pečurke", "Povrće", "kg", 1.5m, 5m, 6.5m),
            ("Spanać", "Povrće", "kg", 8m, 3m, 3.2m),

            ("Limun", "Voće", "kom", 40m, 15m, 0.6m),
            ("Jabuka", "Voće", "kg", 15m, 5m, 2.0m),
            ("Banana", "Voće", "kg", 12m, 4m, 2.4m),
            ("Jagode", "Voće", "kg", 6m, 3m, 8.0m),
            ("Narandža", "Voće", "kg", 20m, 6m, 2.2m),

            ("Kajmak", "Mliječni proizvodi", "kg", 8m, 3m, 16.0m),
            ("Bijeli sir", "Mliječni proizvodi", "kg", 12m, 4m, 12.0m),
            ("Pavlaka", "Mliječni proizvodi", "l", 10m, 3m, 5.5m),
            ("Mlijeko", "Mliječni proizvodi", "l", 20m, 6m, 2.0m),
            ("Puter", "Mliječni proizvodi", "kg", 5m, 2m, 13.0m),
            ("Jaja", "Mliječni proizvodi", "kom", 200m, 60m, 0.35m),
            ("Mocarela", "Mliječni proizvodi", "kg", 6m, 2m, 15.0m),

            ("Brašno", "Začini i dodaci", "kg", 40m, 10m, 1.3m),
            ("So", "Začini i dodaci", "kg", 10m, 3m, 0.8m),
            ("Biber", "Začini i dodaci", "kg", 2m, 1m, 24.0m),
            ("Suncokretovo ulje", "Začini i dodaci", "l", 25m, 8m, 2.6m),
            ("Maslinovo ulje", "Začini i dodaci", "l", 8m, 3m, 12.0m),
            ("Šećer", "Začini i dodaci", "kg", 15m, 5m, 1.7m),

            ("Kafa u zrnu", "Pića", "kg", 10m, 3m, 22.0m),
            ("Čaj", "Pića", "pak", 30m, 10m, 3.0m),
        ];

        foreach (var item in items)
        {
            dbContext.InventoryItems.Add(new InventoryItem
            {
                Name = item.Name,
                InventoryCategoryId = categories[item.Category],
                UnitOfMeasureId = units[item.Unit],
                Quantity = item.Quantity,
                MinQuantity = item.MinQuantity,
                UnitCost = item.UnitCost,
                IsActive = true,
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedMenuItemsAsync(CancellationToken cancellationToken)
    {
        if (await dbContext.MenuItems.AnyAsync(cancellationToken))
        {
            return;
        }

        var categories = await dbContext.MenuCategories.ToDictionaryAsync(x => x.Name, x => x.Id, cancellationToken);
        var allergens = await dbContext.Allergens.ToDictionaryAsync(x => x.Name, x => x.Id, cancellationToken);
        var tags = await dbContext.DietaryTags.ToDictionaryAsync(x => x.Name, x => x.Id, cancellationToken);
        var ingredients = await dbContext.InventoryItems.ToDictionaryAsync(x => x.Name, x => x.Id, cancellationToken);

        foreach (var dish in BuildDishes())
        {
            var menuItem = new MenuItem
            {
                Name = dish.Name,
                Description = dish.Description,
                Price = dish.Price,
                ImageUrl = $"/images/seed/menu/{dish.Slug}.png",
                MenuCategoryId = categories[dish.Category],
                ServingPeriods = dish.Periods,
                IsAvailable = true,
            };

            foreach (var allergenName in dish.Allergens)
            {
                menuItem.MenuItemAllergens.Add(new MenuItemAllergen { AllergenId = allergens[allergenName] });
            }

            foreach (var tagName in dish.Tags)
            {
                menuItem.MenuItemDietaryTags.Add(new MenuItemDietaryTag { DietaryTagId = tags[tagName] });
            }

            foreach (var (ingredientName, quantity) in dish.Recipe)
            {
                menuItem.Ingredients.Add(new MenuItemIngredient
                {
                    InventoryItemId = ingredients[ingredientName],
                    Quantity = quantity,
                });
            }

            dbContext.MenuItems.Add(menuItem);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static List<SeedMenuItem> BuildDishes()
    {
        const ServingPeriod LD = ServingPeriod.Lunch | ServingPeriod.Dinner;
        const ServingPeriod AllDay = ServingPeriod.Breakfast | ServingPeriod.Lunch | ServingPeriod.Dinner;

        return
        [
            // Predjela
            new("sir-pane", "Sir pane", "Predjela", "Pohovani sir uz tartar umak.", 8.50m, LD,
                ["Gluten", "Mlijeko", "Jaja"], ["Vegetarijansko"],
                [("Bijeli sir", 0.2m), ("Brašno", 0.05m), ("Jaja", 1m), ("Suncokretovo ulje", 0.1m)]),
            new("prsut-i-sir", "Pršut i sir", "Predjela", "Domaći pršut i mocarela.", 12.00m, LD,
                ["Mlijeko"], ["Bez glutena"],
                [("Pršut", 0.1m), ("Mocarela", 0.1m)]),
            new("punjene-pecurke-sa-sirom", "Punjene pečurke sa sirom", "Predjela", "Pečene pečurke punjene sirom.", 9.00m, LD,
                ["Mlijeko"], ["Vegetarijansko"],
                [("Pečurke", 0.25m), ("Bijeli sir", 0.15m), ("Maslinovo ulje", 0.02m)]),
            new("domaci-ajvar-sa-somunom", "Domaći ajvar sa somunom", "Predjela", "Domaći ajvar uz svježi somun.", 6.00m, LD,
                ["Gluten"], ["Vegansko"],
                [("Paprika", 0.3m), ("Suncokretovo ulje", 0.05m), ("Brašno", 0.1m)]),
            new("prosciutto-carpaccio", "Prosciutto carpaccio", "Predjela", "Tanko sječeni pršut s parmezanom.", 14.00m, LD,
                [], ["Bez glutena"],
                [("Pršut", 0.08m), ("Mocarela", 0.05m), ("Maslinovo ulje", 0.02m)]),
            new("punjene-paprike-hladne", "Punjene paprike (hladne)", "Predjela", "Paprike punjene sirnim namazom.", 8.00m, LD,
                ["Mlijeko"], ["Vegetarijansko"],
                [("Paprika", 0.3m), ("Bijeli sir", 0.2m)]),

            // Supe i čorbe
            new("begova-corba", "Begova čorba", "Supe i čorbe", "Klasična begova čorba s piletinom.", 6.50m, LD,
                ["Mlijeko"], [],
                [("Pileći batak", 0.2m), ("Šargarepa", 0.1m), ("Krompir", 0.1m), ("Pavlaka", 0.05m)]),
            new("pileca-supa", "Pileća supa", "Supe i čorbe", "Domaća pileća supa s povrćem.", 5.50m, LD,
                [], ["Bez laktoze"],
                [("Pileći batak", 0.25m), ("Šargarepa", 0.1m), ("Crni luk", 0.05m)]),
            new("gulas-corba", "Gulaš čorba", "Supe i čorbe", "Gusta gulaš čorba od junetine.", 7.00m, LD,
                [], [],
                [("Juneće mljeveno meso", 0.15m), ("Krompir", 0.15m), ("Paprika", 0.1m)]),
            new("riblja-corba", "Riblja čorba", "Supe i čorbe", "Čorba od oslića i povrća.", 8.00m, LD,
                ["Riba"], [],
                [("Oslić", 0.2m), ("Šargarepa", 0.1m), ("Krompir", 0.1m)]),
            new("krem-supa-od-povrca", "Krem supa od povrća", "Supe i čorbe", "Krem supa od sezonskog povrća.", 6.00m, LD,
                ["Mlijeko"], ["Vegetarijansko"],
                [("Šargarepa", 0.15m), ("Krompir", 0.15m), ("Pavlaka", 0.05m)]),
            new("tarhana-corba", "Tarhana čorba", "Supe i čorbe", "Domaća tarhana s povrćem.", 6.50m, LD,
                ["Gluten", "Mlijeko"], ["Vegetarijansko"],
                [("Brašno", 0.1m), ("Pavlaka", 0.1m), ("Paradajz", 0.1m)]),

            // Salate
            new("sopska-salata", "Šopska salata", "Salate", "Paradajz, krastavac, paprika i sir.", 6.50m, LD,
                ["Mlijeko"], ["Vegetarijansko", "Bez glutena"],
                [("Paradajz", 0.15m), ("Krastavac", 0.1m), ("Paprika", 0.1m), ("Bijeli sir", 0.05m)]),
            new("zelena-salata", "Zelena salata", "Salate", "Svježa zelena salata s uljem.", 4.50m, LD,
                [], ["Vegansko", "Bez glutena"],
                [("Zelena salata", 1m), ("Krastavac", 0.05m), ("Maslinovo ulje", 0.02m)]),
            new("caesar-salata-sa-piletinom", "Caesar salata s piletinom", "Salate", "Piletina, salata, sir i krutoni.", 11.00m, LD,
                ["Gluten", "Mlijeko", "Jaja"], [],
                [("Pileći file", 0.15m), ("Zelena salata", 1m), ("Mocarela", 0.05m)]),
            new("salata-sa-lososom", "Salata sa lososom", "Salate", "Losos na žaru uz svježu salatu.", 13.50m, LD,
                ["Riba"], ["Bez glutena"],
                [("Losos", 0.12m), ("Zelena salata", 1m), ("Krastavac", 0.05m)]),
            new("salata-sa-skampima", "Salata sa škampima", "Salate", "Škampi na žaru uz povrće.", 12.50m, LD,
                ["Rakovi"], ["Bez glutena"],
                [("Škampi", 0.12m), ("Zelena salata", 1m), ("Paradajz", 0.1m)]),
            new("vocna-salata", "Voćna salata", "Salate", "Miks svježeg sezonskog voća.", 5.50m, LD,
                [], ["Vegansko", "Bez glutena"],
                [("Jabuka", 0.1m), ("Banana", 0.1m), ("Narandža", 0.1m), ("Jagode", 0.1m)]),

            // Glavna jela
            new("piletina-sa-povrcem", "Piletina sa povrćem", "Glavna jela", "Piletina wok s povrćem.", 11.00m, LD,
                [], ["Bez glutena"],
                [("Pileći file", 0.25m), ("Tikvica", 0.1m), ("Šargarepa", 0.1m)]),
            new("teleci-gulas", "Teleći gulaš", "Glavna jela", "Gulaš od teletine s prilogom.", 14.00m, LD,
                [], ["Bez glutena"],
                [("Teletina", 0.3m), ("Crni luk", 0.1m), ("Paprika", 0.1m)]),
            new("punjena-paprika-topla", "Punjena paprika", "Glavna jela", "Paprika punjena mesom i pirinčem.", 9.50m, LD,
                [], ["Bez glutena"],
                [("Paprika", 0.3m), ("Juneće mljeveno meso", 0.2m), ("Krompir", 0.1m)]),
            new("musaka", "Musaka", "Glavna jela", "Slojevita musaka od krompira i mesa.", 10.50m, LD,
                ["Mlijeko", "Jaja"], [],
                [("Krompir", 0.3m), ("Juneće mljeveno meso", 0.2m), ("Pavlaka", 0.1m), ("Jaja", 1m)]),
            new("sarma", "Sarma", "Glavna jela", "Sarma u kupusovom listu.", 10.00m, LD,
                [], ["Bez glutena"],
                [("Kupus", 0.3m), ("Juneće mljeveno meso", 0.2m), ("Crni luk", 0.05m)]),
            new("pileci-odrezak-sa-pecurkama", "Pileći odrezak s pečurkama", "Glavna jela", "Odrezak u umaku od pečuraka.", 12.50m, LD,
                ["Mlijeko"], ["Bez glutena"],
                [("Pileći file", 0.25m), ("Pečurke", 0.15m), ("Pavlaka", 0.1m)]),

            // Roštilj
            new("cevapi-u-somunu", "Ćevapi u somunu (10 kom)", "Roštilj", "Domaći ćevapi od junetine, luk, somun.", 12.00m, LD,
                ["Gluten", "Mlijeko"], [],
                [("Juneće mljeveno meso", 0.25m), ("Brašno", 0.1m), ("Crni luk", 0.05m)]),
            new("pljeskavica-sa-kajmakom", "Pljeskavica sa kajmakom", "Roštilj", "Pljeskavica punjena kajmakom.", 13.50m, LD,
                ["Gluten", "Mlijeko"], [],
                [("Juneće mljeveno meso", 0.3m), ("Kajmak", 0.05m), ("Brašno", 0.1m)]),
            new("mijesano-meso", "Miješano meso", "Roštilj", "Ćevapi, ražnjić, pileći file, kobasica.", 18.00m, LD,
                ["Gluten"], [],
                [("Juneće mljeveno meso", 0.15m), ("Pileći batak", 0.15m), ("Kobasice", 0.1m), ("Brašno", 0.05m)]),
            new("pileci-raznjici", "Pileći ražnjići", "Roštilj", "Ražnjići od pilećeg filea.", 11.00m, LD,
                [], ["Bez glutena"],
                [("Pileći file", 0.3m), ("Paprika", 0.1m)]),
            new("vesalica-na-zaru", "Vešalica na žaru", "Roštilj", "Svinjska vešalica s roštilja.", 15.00m, LD,
                [], ["Bez glutena"],
                [("Svinjski but", 0.3m), ("Crni luk", 0.05m)]),
            new("kobasice-sa-rostilja", "Kobasice sa roštilja", "Roštilj", "Domaće kobasice s roštilja.", 10.00m, LD,
                [], ["Bez glutena"],
                [("Kobasice", 0.3m), ("Paprika", 0.1m)]),

            // Riblja jela
            new("losos-na-zaru", "Losos na žaru", "Riblja jela", "Losos s limunom i maslinovim uljem.", 18.50m, LD,
                ["Riba"], ["Bez glutena"],
                [("Losos", 0.3m), ("Maslinovo ulje", 0.03m), ("Limun", 1m)]),
            new("oslic-u-pivskom-tijestu", "Oslić u pivskom tijestu", "Riblja jela", "Oslić pohovan u pivskom tijestu.", 13.00m, LD,
                ["Riba", "Gluten"], [],
                [("Oslić", 0.25m), ("Brašno", 0.1m)]),
            new("skampi-na-buzaru", "Škampi na buzaru", "Riblja jela", "Škampi u umaku od bijelog luka.", 19.00m, LD,
                ["Rakovi"], ["Bez glutena"],
                [("Škampi", 0.3m), ("Bijeli luk", 0.02m), ("Maslinovo ulje", 0.03m)]),
            new("lignje-na-zaru", "Lignje na žaru", "Riblja jela", "Lignje s limunom i uljem.", 16.50m, LD,
                ["Mekušci"], ["Bez glutena"],
                [("Lignje", 0.3m), ("Limun", 1m), ("Maslinovo ulje", 0.02m)]),
            new("riblji-rizoto", "Riblji rižoto", "Riblja jela", "Kremasti rižoto s oslićem.", 14.50m, LD,
                ["Riba", "Mlijeko"], ["Bez glutena"],
                [("Oslić", 0.15m), ("Pavlaka", 0.05m)]),
            new("pohovane-lignje", "Pohovane lignje", "Riblja jela", "Lignje pohovane u brašnu.", 15.50m, LD,
                ["Mekušci", "Gluten"], [],
                [("Lignje", 0.3m), ("Brašno", 0.1m)]),

            // Deserti
            new("tufahija", "Tufahija", "Deserti", "Kuhana jabuka punjena orasima.", 6.50m, LD,
                ["Orašasti plodovi"], ["Vegetarijansko"],
                [("Jabuka", 0.2m), ("Pavlaka", 0.05m)]),
            new("baklava", "Baklava", "Deserti", "Domaća baklava s orasima.", 5.50m, LD,
                ["Gluten", "Orašasti plodovi"], ["Vegetarijansko"],
                [("Brašno", 0.1m), ("Šećer", 0.1m)]),
            new("palacinke-sa-cokoladom", "Palačinke sa čokoladom", "Deserti", "Palačinke punjene čokoladom.", 6.00m, LD,
                ["Gluten", "Mlijeko", "Jaja"], ["Vegetarijansko"],
                [("Brašno", 0.1m), ("Mlijeko", 0.1m), ("Jaja", 1m)]),
            new("cokoladna-torta", "Čokoladna torta", "Deserti", "Bogata čokoladna torta.", 7.00m, LD,
                ["Gluten", "Mlijeko", "Jaja"], ["Vegetarijansko"],
                [("Brašno", 0.1m), ("Puter", 0.1m), ("Jaja", 2m)]),
            new("vocna-torta", "Voćna torta", "Deserti", "Torta sa svježim voćem.", 7.50m, LD,
                ["Gluten", "Mlijeko", "Jaja"], ["Vegetarijansko"],
                [("Brašno", 0.1m), ("Jagode", 0.1m), ("Pavlaka", 0.1m)]),
            new("sladoled-3-kugle", "Sladoled (3 kugle)", "Deserti", "Tri kugle domaćeg sladoleda.", 5.00m, LD,
                ["Mlijeko"], ["Vegetarijansko", "Bez glutena"],
                [("Mlijeko", 0.15m), ("Šećer", 0.05m)]),

            // Pića
            new("espresso", "Espresso", "Pića", "Espresso kafa.", 2.00m, AllDay,
                [], ["Vegansko", "Bez glutena"],
                [("Kafa u zrnu", 0.01m)]),
            new("cappuccino", "Cappuccino", "Pića", "Espresso s mliječnom pjenom.", 2.80m, AllDay,
                ["Mlijeko"], ["Vegetarijansko", "Bez glutena"],
                [("Kafa u zrnu", 0.01m), ("Mlijeko", 0.1m)]),
            new("caj", "Čaj", "Pića", "Izbor crnog ili voćnog čaja.", 2.00m, AllDay,
                [], ["Vegansko", "Bez glutena"],
                [("Čaj", 1m)]),
            new("svjeze-cijedjeni-sok-od-narandze", "Svježe cijeđeni sok", "Pića", "Svježe cijeđeni sok od narandže.", 4.50m, AllDay,
                [], ["Vegansko", "Bez glutena"],
                [("Narandža", 0.3m)]),
            new("limunada", "Limunada", "Pića", "Domaća limunada.", 3.50m, AllDay,
                [], ["Vegansko", "Bez glutena"],
                [("Limun", 2m), ("Šećer", 0.05m)]),
            new("ayran", "Ayran", "Pića", "Domaći ayran.", 2.50m, AllDay,
                ["Mlijeko"], ["Vegetarijansko", "Bez glutena"],
                [("Mlijeko", 0.2m)]),
        ];
    }

    private sealed record SeedMenuItem(
        string Slug,
        string Name,
        string Category,
        string Description,
        decimal Price,
        ServingPeriod Periods,
        string[] Allergens,
        string[] Tags,
        (string Ingredient, decimal Quantity)[] Recipe);
}
