namespace Sofra.API.DTOs.Orders;

public record OrderQuoteItemResponse(int MenuItemId, string MenuItemName, int Quantity, decimal UnitPrice, decimal LineTotal);
