namespace Sofra.API.DTOs.Orders;

public record OrderItemResponse(
    int Id, int MenuItemId, string MenuItemName, int Quantity, decimal UnitPrice, decimal LineTotal, string? Note);
