using Microsoft.EntityFrameworkCore;
using Sofra.API.Constants;
using Sofra.API.Entities;

namespace Sofra.API.Data.Seed;

/// <summary>
/// 12 obavijesti: 11 objavljenih kroz zadnja 3 mjeseca (rastuci prema danas), 1 nacrt zakazan
/// za buducnost (IsPublished = false). Slike su u wwwroot/images/seed/news/{slug}.png.
/// </summary>
public partial class DataSeeder
{
    private async Task SeedNewsAsync(CancellationToken cancellationToken)
    {
        if (await dbContext.News.AnyAsync(cancellationToken))
        {
            return;
        }

        var admins = await userManager.GetUsersInRoleAsync(Roles.Admin);
        var createdById = admins[0].Id;
        var now = DateTime.UtcNow;

        // DaysAgo < 0 znaci "za X dana u buducnosti" -> nacrt.
        (string Slug, string Title, string Text, int DaysAgo, bool SendPush)[] items =
        [
            ("nova-ljetna-ponuda", "Nova ljetna ponuda jela", "Predstavljamo novu ljetnu ponudu sa svježim salatama i laganim jelima idealnim za tople dane.", 85, true),
            ("radno-vrijeme-praznici", "Radno vrijeme za praznike", "Obavještavamo goste o izmijenjenom radnom vremenu tokom predstojećih praznika.", 75, false),
            ("nova-terasa", "Otvaramo novu terasu", "Sa zadovoljstvom najavljujemo otvaranje nove terase sa pogledom na rijeku.", 65, false),
            ("cevapi-nova-receptura", "Ćevapi kao u Mostaru - nova receptura", "Naši kuhari su usavršili recepturu za ćevape po tradicionalnom mostarskom receptu.", 55, true),
            ("vikend-akcija-rostilj", "Vikend akcija na roštilj", "Ovog vikenda 10% popusta na sva jela sa roštilja uz kod ROSTILJ10.", 45, true),
            ("nova-vinska-karta", "Nova vinska karta", "Proširili smo ponudu domaćih i regionalnih vina.", 38, false),
            ("degustacija-specijaliteta", "Degustacija domaćih specijaliteta", "Pridružite nam se na večeri degustacije tradicionalnih bosanskih specijaliteta.", 30, false),
            ("nagrada-najbolji-cevap", "Sofra osvojila nagradu za najbolji ćevap u gradu", "Ponosni smo što je naš restoran osvojio lokalno priznanje za najbolje ćevape.", 22, true),
            ("dostava-na-adresu", "Uvodimo dostavu na kućnu adresu", "Od sada možete naručiti omiljena jela i za dostavu na kućnu adresu.", 14, true),
            ("novi-meni-dorucak", "Novi meni za doručak", "Uveli smo novi jutarnji meni dostupan radnim danima od 8h.", 7, false),
            ("rezervacije-bajram", "Rezervišite sto za Bajram", "Bajramske rezervacije su otvorene - osigurajte svoj sto na vrijeme.", 2, true),
            ("najava-jesenjeg-menija", "Najava jesenjeg menija", "Uskoro stiže novi jesenji meni sa sezonskim namirnicama.", -5, false),
        ];

        foreach (var item in items)
        {
            var isDraft = item.DaysAgo < 0;
            dbContext.News.Add(new News
            {
                Title = item.Title,
                Text = item.Text,
                ImageUrl = $"/images/seed/news/{item.Slug}.png",
                PublishAt = now.AddDays(-item.DaysAgo),
                IsPublished = !isDraft,
                CreatedById = createdById,
                SendPush = item.SendPush,
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
