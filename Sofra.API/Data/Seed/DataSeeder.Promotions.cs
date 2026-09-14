using Microsoft.EntityFrameworkCore;
using Sofra.API.Entities;
using Sofra.API.Enums;

namespace Sofra.API.Data.Seed;

/// <summary>
/// 6 promocija: 2 aktivne, 1 zakazana (buduca), 2 istekle, 1 rucno iskljucena (neaktivna iako bi
/// po datumima jos vazila). Pokriva oba DiscountType i sva tri PromotionScope.
/// </summary>
public partial class DataSeeder
{
    private async Task SeedPromotionsAsync(CancellationToken cancellationToken)
    {
        if (await dbContext.Promotions.AnyAsync(cancellationToken))
        {
            return;
        }

        var rostilj = await dbContext.MenuCategories.FirstAsync(x => x.Name == "Roštilj", cancellationToken);
        var deserti = await dbContext.MenuCategories.FirstAsync(x => x.Name == "Deserti", cancellationToken);
        var now = DateTime.UtcNow;

        dbContext.Promotions.AddRange(
            new Promotion
            {
                Name = "Ljetna akcija -15%",
                Code = "LJETO15",
                DiscountType = DiscountType.Percentage,
                Value = 15,
                Scope = PromotionScope.AllItems,
                ValidFrom = now.AddDays(-20),
                ValidTo = now.AddDays(20),
                MaxUses = 200,
                UsedCount = 47,
                MinOrderAmount = 20,
                IsActive = true,
            },
            new Promotion
            {
                Name = "Popust na roštilj",
                Code = "ROSTILJ10",
                DiscountType = DiscountType.Percentage,
                Value = 10,
                Scope = PromotionScope.Category,
                MenuCategoryId = rostilj.Id,
                ValidFrom = now.AddDays(-10),
                ValidTo = now.AddDays(10),
                UsedCount = 23,
                IsActive = true,
            },
            new Promotion
            {
                Name = "Zimska ponuda",
                Code = "ZIMA2026",
                DiscountType = DiscountType.FixedAmount,
                Value = 5.00m,
                Scope = PromotionScope.AllItems,
                ValidFrom = now.AddDays(15),
                ValidTo = now.AddDays(45),
                MaxUses = 150,
                UsedCount = 0,
                MinOrderAmount = 25,
                IsActive = true,
            },
            new Promotion
            {
                Name = "Black Friday -20%",
                Code = "BLACKFRIDAY",
                DiscountType = DiscountType.Percentage,
                Value = 20,
                Scope = PromotionScope.AllItems,
                ValidFrom = now.AddDays(-90),
                ValidTo = now.AddDays(-85),
                MaxUses = 100,
                UsedCount = 100,
                IsActive = true,
            },
            new Promotion
            {
                Name = "Dobrodošlica -10 KM",
                Code = "DOBRODOSLI",
                DiscountType = DiscountType.FixedAmount,
                Value = 10.00m,
                Scope = PromotionScope.FirstOrder,
                ValidFrom = now.AddDays(-60),
                ValidTo = now.AddDays(-30),
                UsedCount = 34,
                MinOrderAmount = 30,
                IsActive = true,
            },
            new Promotion
            {
                Name = "Desert popust",
                Code = "DESERT20",
                DiscountType = DiscountType.Percentage,
                Value = 20,
                Scope = PromotionScope.Category,
                MenuCategoryId = deserti.Id,
                ValidFrom = now.AddDays(-30),
                ValidTo = now.AddDays(30),
                MaxUses = 50,
                UsedCount = 12,
                IsActive = false,
            });

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
