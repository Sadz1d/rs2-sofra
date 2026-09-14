using Microsoft.EntityFrameworkCore;
using Sofra.API.Constants;
using Sofra.API.Entities;
using Sofra.API.Enums;

namespace Sofra.API.Data.Seed;

/// <summary>
/// Grupa 5: istorijski podaci (narudzbe, placanja, rezervacije, recenzije, interakcije).
/// Svaki gost dobija profil preferencija (2 omiljene kategorije) koji tezinski utice na izbor jela
/// u narudzbama i interakcijama - da recommender (Faza 5) ima stvaran, iskoristiv signal umjesto
/// cisto uniformnog suma. Fiksni random seed radi ponovljivosti; cijela grupa je zasticena jednom
/// idempotency provjerom (Orders), jer su podgrupe medjusobno zavisne (recenzije trebaju vec upisane
/// Completed narudzbe sa stvarnim Id-jevima).
/// </summary>
public partial class DataSeeder
{
    private static readonly Random Rng = new(20260914);

    private async Task SeedHistoricalDataAsync(CancellationToken cancellationToken)
    {
        if (await dbContext.Orders.AnyAsync(cancellationToken))
        {
            return;
        }

        var guests = await userManager.GetUsersInRoleAsync(Roles.Gost);
        var waiters = (await userManager.GetUsersInRoleAsync(Roles.Konobar)).ToList();
        var admins = (await userManager.GetUsersInRoleAsync(Roles.Admin)).ToList();
        var menuItems = await dbContext.MenuItems.AsNoTracking().ToListAsync(cancellationToken);
        var tables = await dbContext.DiningTables.AsNoTracking().ToListAsync(cancellationToken);
        var zones = await dbContext.Zones.AsNoTracking().ToListAsync(cancellationToken);
        var paymentMethods = await dbContext.PaymentMethods.AsNoTracking().ToDictionaryAsync(x => x.Code!, x => x.Id, cancellationToken);

        var guestPreferences = BuildGuestPreferences(guests, menuItems);

        var orders = await SeedOrdersAsync(guests, waiters, menuItems, tables, paymentMethods, guestPreferences, cancellationToken);
        await SeedReservationsAsync(guests, admins, zones, tables, cancellationToken);
        await SeedReviewsAsync(orders, admins, cancellationToken);
        await SeedUserInteractionsAsync(guests, menuItems, guestPreferences, cancellationToken);
    }

    private static Dictionary<int, Dictionary<int, int>> BuildGuestPreferences(IList<ApplicationUser> guests, List<MenuItem> menuItems)
    {
        var categoryIds = menuItems.Select(x => x.MenuCategoryId).Distinct().ToList();
        var result = new Dictionary<int, Dictionary<int, int>>();

        foreach (var guest in guests)
        {
            var preferred = categoryIds.OrderBy(_ => Rng.Next()).Take(2).ToHashSet();
            result[guest.Id] = categoryIds.ToDictionary(id => id, id => preferred.Contains(id) ? 3 : 1);
        }

        return result;
    }

    private static int PickWeightedCategory(Dictionary<int, int> weights)
    {
        var total = weights.Values.Sum();
        var roll = Rng.Next(total);
        var cumulative = 0;
        foreach (var (categoryId, weight) in weights)
        {
            cumulative += weight;
            if (roll < cumulative)
            {
                return categoryId;
            }
        }

        return weights.Keys.First();
    }

    private static MenuItem PickMenuItem(List<MenuItem> menuItems, Dictionary<int, int> guestWeights)
    {
        var categoryId = PickWeightedCategory(guestWeights);
        var candidates = menuItems.Where(x => x.MenuCategoryId == categoryId).ToList();
        return candidates[Rng.Next(candidates.Count)];
    }

    private async Task<List<Order>> SeedOrdersAsync(
        IList<ApplicationUser> guests,
        List<ApplicationUser> waiters,
        List<MenuItem> menuItems,
        List<DiningTable> tables,
        Dictionary<string, int> paymentMethods,
        Dictionary<int, Dictionary<int, int>> guestPreferences,
        CancellationToken cancellationToken)
    {
        var orders = new List<Order>();
        var orderNumber = 1001;

        string[] cancelReasons =
        [
            "Gost je otkazao narudžbu", "Nema traženih sastojaka", "Duplirana narudžba", "Gost nije stigao na vrijeme",
        ];

        void ApplyProgress(Order order, DateTime baseTime)
        {
            order.ConfirmedAt = baseTime.AddMinutes(Rng.Next(2, 6));
            if (order.Status is OrderStatus.Pending)
            {
                return;
            }

            order.PreparationStartedAt = order.ConfirmedAt!.Value.AddMinutes(Rng.Next(2, 8));
            if (order.Status is OrderStatus.Confirmed)
            {
                return;
            }

            order.ReadyAt = order.PreparationStartedAt!.Value.AddMinutes(Rng.Next(10, 20));
            if (order.Status is OrderStatus.InPreparation)
            {
                return;
            }

            order.DeliveredAt = order.ReadyAt!.Value.AddMinutes(Rng.Next(2, 8));
            if (order.Status is OrderStatus.Ready)
            {
                return;
            }

            order.CompletedAt = order.DeliveredAt!.Value.AddMinutes(Rng.Next(5, 15));
        }

        for (var i = 0; i < 300; i++)
        {
            var isLive = i >= 290; // posljednjih 10 su "danasnje" live narudzbe u svim fazama
            var guest = guests[Rng.Next(guests.Count)];
            var weights = guestPreferences[guest.Id];

            DateTime createdAt;
            OrderStatus status;

            if (isLive)
            {
                createdAt = DateTime.UtcNow.AddMinutes(-Rng.Next(5, 300));
                status = (OrderStatus)Rng.Next(0, 7);
            }
            else
            {
                var daysAgo = Rng.Next(1, 90);
                createdAt = DateTime.UtcNow.Date.AddDays(-daysAgo).AddHours(Rng.Next(10, 23)).AddMinutes(Rng.Next(0, 60));
                status = Rng.Next(100) < 85 ? OrderStatus.Completed : OrderStatus.Cancelled;
            }

            var type = Rng.Next(100) < 65 ? OrderType.DineIn : OrderType.Takeaway;
            var table = type == OrderType.DineIn ? tables[Rng.Next(tables.Count)] : null;
            var waiter = type == OrderType.DineIn ? waiters[Rng.Next(waiters.Count)] : null;

            var order = new Order
            {
                Number = orderNumber++.ToString(),
                UserId = guest.Id,
                CreatedAt = createdAt,
                Type = type,
                DiningTableId = table?.Id,
                WaiterId = waiter?.Id,
                Status = status,
            };

            var itemCount = Rng.Next(1, 5);
            decimal subtotal = 0;
            for (var n = 0; n < itemCount; n++)
            {
                var item = PickMenuItem(menuItems, weights);
                var quantity = Rng.Next(1, 4);
                subtotal += item.Price * quantity;
                order.Items.Add(new OrderItem
                {
                    MenuItemId = item.Id,
                    Quantity = quantity,
                    UnitPrice = item.Price,
                });
            }

            order.Subtotal = subtotal;
            order.Tax = Math.Round(subtotal * 0.17m, 2);
            order.Discount = 0;
            order.Total = order.Subtotal + order.Tax;

            if (status == OrderStatus.Cancelled)
            {
                var cancelledAt = createdAt.AddMinutes(Rng.Next(3, 30));
                order.CancelledAt = cancelledAt;
                order.CancelReason = cancelReasons[Rng.Next(cancelReasons.Length)];
                order.CancelledById = Rng.Next(100) < 50 ? guest.Id : waiters[Rng.Next(waiters.Count)].Id;

                if (Rng.Next(100) < 40)
                {
                    var paidAt = createdAt.AddMinutes(Rng.Next(1, 3));
                    order.Payment = new Payment
                    {
                        CreatedAt = paidAt,
                        PaymentMethodId = paymentMethods[Rng.Next(100) < 60 ? "CARD" : "CASH"],
                        Amount = order.Total,
                        Status = PaymentStatus.Refunded,
                        PaidAt = paidAt,
                        RefundedAmount = order.Total,
                        RefundedAt = cancelledAt,
                        StripePaymentIntentId = $"pi_seed_{Guid.NewGuid():N}",
                    };
                }
            }
            else if (status != OrderStatus.Pending)
            {
                ApplyProgress(order, createdAt);

                var paidAt = createdAt.AddMinutes(Rng.Next(1, 3));
                var isCard = Rng.Next(100) < 60;
                order.Payment = new Payment
                {
                    CreatedAt = paidAt,
                    PaymentMethodId = paymentMethods[isCard ? "CARD" : "CASH"],
                    Amount = order.Total,
                    Status = PaymentStatus.Succeeded,
                    PaidAt = paidAt,
                    StripePaymentIntentId = isCard ? $"pi_seed_{Guid.NewGuid():N}" : null,
                };
            }

            dbContext.Orders.Add(order);
            orders.Add(order);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return orders;
    }

    private async Task SeedReservationsAsync(
        IList<ApplicationUser> guests,
        List<ApplicationUser> admins,
        List<Zone> zones,
        List<DiningTable> tables,
        CancellationToken cancellationToken)
    {
        (ReservationStatus Status, int Count)[] plan =
        [
            (ReservationStatus.Completed, 35),
            (ReservationStatus.NoShow, 5),
            (ReservationStatus.Cancelled, 8),
            (ReservationStatus.Rejected, 7),
            (ReservationStatus.Pending, 10),
            (ReservationStatus.Confirmed, 15),
        ];

        string[] rejectReasons =
        [
            "Nema slobodnih stolova za traženi termin", "Prekoračen kapacitet zone", "Restoran je zatvoren tog datuma",
        ];

        var usedActiveSlots = new HashSet<(int UserId, DateTime At)>();

        foreach (var (status, count) in plan)
        {
            for (var i = 0; i < count; i++)
            {
                var guest = guests[Rng.Next(guests.Count)];
                var isPast = status is ReservationStatus.Completed or ReservationStatus.NoShow;
                var isFuture = status is ReservationStatus.Pending or ReservationStatus.Confirmed;
                var isActive = isFuture;

                DateTime reservationAt;
                do
                {
                    reservationAt = isPast
                        ? DateTime.UtcNow.Date.AddDays(-Rng.Next(1, 60)).AddHours(Rng.Next(12, 22)).AddMinutes(Rng.Next(0, 2) * 30)
                        : isFuture
                            ? DateTime.UtcNow.Date.AddDays(Rng.Next(1, 21)).AddHours(Rng.Next(12, 22)).AddMinutes(Rng.Next(0, 2) * 30)
                            : DateTime.UtcNow.Date.AddDays(Rng.Next(-30, 20)).AddHours(Rng.Next(12, 22)).AddMinutes(Rng.Next(0, 2) * 30);
                }
                while (isActive && !usedActiveSlots.Add((guest.Id, reservationAt)));

                var createdAt = reservationAt.AddDays(-Rng.Next(1, 10));
                var zone = zones[Rng.Next(zones.Count)];

                var reservation = new Reservation
                {
                    UserId = guest.Id,
                    CreatedAt = createdAt,
                    ReservationAt = reservationAt,
                    DurationMinutes = 120,
                    Guests = Rng.Next(2, 11),
                    ZoneId = zone.Id,
                    Status = status,
                };

                if (status is ReservationStatus.Confirmed or ReservationStatus.Completed)
                {
                    var zoneTables = tables.Where(t => t.ZoneId == zone.Id).ToList();
                    reservation.DiningTableId = zoneTables[Rng.Next(zoneTables.Count)].Id;
                    reservation.ProcessedById = admins[Rng.Next(admins.Count)].Id;
                    reservation.ProcessedAt = reservationAt.AddDays(-Rng.Next(1, 3));
                }
                else if (status is ReservationStatus.NoShow)
                {
                    reservation.ProcessedById = admins[Rng.Next(admins.Count)].Id;
                    reservation.ProcessedAt = reservationAt.AddDays(-Rng.Next(1, 3));
                }
                else if (status is ReservationStatus.Rejected)
                {
                    reservation.ProcessedById = admins[Rng.Next(admins.Count)].Id;
                    reservation.ProcessedAt = reservationAt.AddDays(-Rng.Next(1, 5));
                    reservation.RejectReason = rejectReasons[Rng.Next(rejectReasons.Length)];
                    if (Rng.Next(100) < 40)
                    {
                        reservation.AlternativeAt = reservationAt.AddHours(Rng.Next(1, 4));
                    }
                }
                else if (status is ReservationStatus.Cancelled)
                {
                    reservation.CancelledById = Rng.Next(100) < 60 ? guest.Id : admins[Rng.Next(admins.Count)].Id;
                    reservation.CancelledAt = reservationAt.AddHours(-Rng.Next(2, 48));
                }

                dbContext.Reservations.Add(reservation);
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedReviewsAsync(List<Order> orders, List<ApplicationUser> admins, CancellationToken cancellationToken)
    {
        var completedOrders = orders.Where(o => o.Status == OrderStatus.Completed && o.CompletedAt.HasValue).ToList();
        if (completedOrders.Count == 0)
        {
            return;
        }

        string[] positiveComments =
        [
            "Odlično jelo, definitivno preporučujem!", "Sve pohvale kuhinji, vraćamo se sigurno.",
            "Brza usluga i ukusna hrana.", "Baš onako kako volimo, domaći ukus.",
        ];
        string[] neutralComments =
        [
            "Solidno, ništa posebno.", "Očekivao sam malo više za tu cijenu.", "Ukusno, ali stiglo malo hladno.",
        ];
        string[] negativeComments =
        [
            "Čekanje je bilo predugo.", "Nije bilo po mom ukusu.", "Očekivao sam bolju porciju.",
        ];
        string[] replies =
        [
            "Hvala vam na utisku, javite nam se opet!", "Žao nam je zbog iskustva, potrudit ćemo se da bude bolje.",
            "Zahvaljujemo na povratnoj informaciji.",
        ];

        var usedFoodReviewKeys = new HashSet<(int OrderId, int MenuItemId)>();
        var usedServiceReviewOrders = new HashSet<int>();
        var menuItemRatings = new Dictionary<int, List<int>>();

        var created = 0;
        var attempts = 0;
        while (created < 150 && attempts < 2000)
        {
            attempts++;
            var order = completedOrders[Rng.Next(completedOrders.Count)];
            var isServiceReview = order.WaiterId.HasValue && Rng.Next(100) < 30;

            int? menuItemId = null;
            if (!isServiceReview)
            {
                if (order.Items.Count == 0)
                {
                    continue;
                }

                var orderItem = order.Items.ElementAt(Rng.Next(order.Items.Count));
                if (!usedFoodReviewKeys.Add((order.Id, orderItem.MenuItemId)))
                {
                    continue;
                }

                menuItemId = orderItem.MenuItemId;
            }
            else if (!usedServiceReviewOrders.Add(order.Id))
            {
                continue;
            }

            var rating = WeightedRating();
            var comment = rating >= 4
                ? positiveComments[Rng.Next(positiveComments.Length)]
                : rating == 3
                    ? neutralComments[Rng.Next(neutralComments.Length)]
                    : negativeComments[Rng.Next(negativeComments.Length)];

            var createdAt = order.CompletedAt!.Value.AddHours(Rng.Next(1, 72));

            var review = new Review
            {
                OrderId = order.Id,
                UserId = order.UserId,
                MenuItemId = menuItemId,
                WaiterId = isServiceReview ? order.WaiterId : null,
                Rating = rating,
                Comment = comment,
                CreatedAt = createdAt,
            };

            if (Rng.Next(100) < 30)
            {
                review.Reply = replies[Rng.Next(replies.Length)];
                review.RepliedById = admins[Rng.Next(admins.Count)].Id;
                review.RepliedAt = createdAt.AddHours(Rng.Next(2, 48));
            }

            dbContext.Reviews.Add(review);

            if (menuItemId.HasValue)
            {
                if (!menuItemRatings.TryGetValue(menuItemId.Value, out var list))
                {
                    list = [];
                    menuItemRatings[menuItemId.Value] = list;
                }

                list.Add(rating);
            }

            created++;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        // AvgRating/ReviewCount su keš na MenuItem - u stvarnom servisu bi ih azurirala recenzija-kreacija;
        // ovdje ih racunamo direktno jer seed zaobilazi servisni sloj.
        if (menuItemRatings.Count > 0)
        {
            var affectedIds = menuItemRatings.Keys.ToList();
            var items = await dbContext.MenuItems.Where(x => affectedIds.Contains(x.Id)).ToListAsync(cancellationToken);
            foreach (var item in items)
            {
                var ratings = menuItemRatings[item.Id];
                item.AvgRating = Math.Round((decimal)ratings.Average(), 2);
                item.ReviewCount = ratings.Count;
            }

            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    private static int WeightedRating()
    {
        var roll = Rng.Next(100);
        return roll switch
        {
            < 3 => 1,
            < 10 => 2,
            < 25 => 3,
            < 65 => 4,
            _ => 5,
        };
    }

    private async Task SeedUserInteractionsAsync(
        IList<ApplicationUser> guests,
        List<MenuItem> menuItems,
        Dictionary<int, Dictionary<int, int>> guestPreferences,
        CancellationToken cancellationToken)
    {
        string[] searchTerms =
        [
            "ćevapi", "pizza", "riba", "vegetarijansko", "salata", "roštilj", "bez glutena", "desert", "pića", "piletina",
        ];

        var favoritePairs = new HashSet<(int UserId, int MenuItemId)>();
        foreach (var guest in guests)
        {
            var count = Rng.Next(1, 4);
            for (var i = 0; i < count; i++)
            {
                var item = PickMenuItem(menuItems, guestPreferences[guest.Id]);
                favoritePairs.Add((guest.Id, item.Id));
            }
        }

        foreach (var (userId, menuItemId) in favoritePairs)
        {
            var favoritedAt = DateTime.UtcNow.AddDays(-Rng.Next(1, 90));
            dbContext.Favorites.Add(new Favorite { UserId = userId, MenuItemId = menuItemId, CreatedAt = favoritedAt });
            dbContext.UserInteractions.Add(new UserInteraction
            {
                UserId = userId,
                MenuItemId = menuItemId,
                Type = InteractionType.Favorite,
                CreatedAt = favoritedAt,
            });
        }

        var remaining = 500 - favoritePairs.Count;
        var viewCount = (int)(remaining * 0.63);
        var searchCount = (int)(remaining * 0.16);
        var shownCount = (int)(remaining * 0.14);
        var clickedCount = remaining - viewCount - searchCount - shownCount;

        void AddInteraction(int userId, InteractionType type, int? menuItemId, string? searchTerm)
        {
            dbContext.UserInteractions.Add(new UserInteraction
            {
                UserId = userId,
                MenuItemId = menuItemId,
                Type = type,
                SearchTerm = searchTerm,
                CreatedAt = DateTime.UtcNow.AddDays(-Rng.Next(0, 90)).AddHours(-Rng.Next(0, 24)),
            });
        }

        for (var i = 0; i < viewCount; i++)
        {
            var guest = guests[Rng.Next(guests.Count)];
            var item = PickMenuItem(menuItems, guestPreferences[guest.Id]);
            AddInteraction(guest.Id, InteractionType.View, item.Id, null);
        }

        for (var i = 0; i < searchCount; i++)
        {
            var guest = guests[Rng.Next(guests.Count)];
            AddInteraction(guest.Id, InteractionType.Search, null, searchTerms[Rng.Next(searchTerms.Length)]);
        }

        for (var i = 0; i < shownCount; i++)
        {
            var guest = guests[Rng.Next(guests.Count)];
            var item = PickMenuItem(menuItems, guestPreferences[guest.Id]);
            AddInteraction(guest.Id, InteractionType.RecommendationShown, item.Id, null);
        }

        for (var i = 0; i < clickedCount; i++)
        {
            var guest = guests[Rng.Next(guests.Count)];
            var item = PickMenuItem(menuItems, guestPreferences[guest.Id]);
            AddInteraction(guest.Id, InteractionType.RecommendationClicked, item.Id, null);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
