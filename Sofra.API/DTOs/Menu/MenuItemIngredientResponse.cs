namespace Sofra.API.DTOs.Menu;

public record MenuItemIngredientResponse(
    int InventoryItemId,
    string InventoryItemName,
    decimal Quantity,
    string UnitAbbreviation);
