using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Sofra.API.Entities;

namespace Sofra.API.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<ApplicationUser, IdentityRole<int>, int>(options)
{
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<PasswordResetCode> PasswordResetCodes => Set<PasswordResetCode>();

    public DbSet<MenuCategory> MenuCategories => Set<MenuCategory>();
    public DbSet<Allergen> Allergens => Set<Allergen>();
    public DbSet<DietaryTag> DietaryTags => Set<DietaryTag>();
    public DbSet<Zone> Zones => Set<Zone>();
    public DbSet<TableType> TableTypes => Set<TableType>();
    public DbSet<PaymentMethod> PaymentMethods => Set<PaymentMethod>();
    public DbSet<UnitOfMeasure> UnitsOfMeasure => Set<UnitOfMeasure>();
    public DbSet<InventoryCategory> InventoryCategories => Set<InventoryCategory>();
    public DbSet<Country> Countries => Set<Country>();
    public DbSet<City> Cities => Set<City>();

    public DbSet<MenuItem> MenuItems => Set<MenuItem>();
    public DbSet<MenuItemIngredient> MenuItemIngredients => Set<MenuItemIngredient>();
    public DbSet<MenuItemAllergen> MenuItemAllergens => Set<MenuItemAllergen>();
    public DbSet<MenuItemDietaryTag> MenuItemDietaryTags => Set<MenuItemDietaryTag>();
    public DbSet<Favorite> Favorites => Set<Favorite>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<DiningTable> DiningTables => Set<DiningTable>();
    public DbSet<Reservation> Reservations => Set<Reservation>();
    public DbSet<Review> Reviews => Set<Review>();
    public DbSet<InventoryItem> InventoryItems => Set<InventoryItem>();
    public DbSet<InventoryTransaction> InventoryTransactions => Set<InventoryTransaction>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<Shift> Shifts => Set<Shift>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<News> News => Set<News>();
    public DbSet<Promotion> Promotions => Set<Promotion>();
    public DbSet<UserInteraction> UserInteractions => Set<UserInteraction>();
    public DbSet<MenuItemStats> MenuItemStats => Set<MenuItemStats>();
    public DbSet<MenuItemPair> MenuItemPairs => Set<MenuItemPair>();
    public DbSet<ProcessedEvent> ProcessedEvents => Set<ProcessedEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
