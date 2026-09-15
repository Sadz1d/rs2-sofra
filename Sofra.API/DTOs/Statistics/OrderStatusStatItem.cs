using Sofra.API.Enums;

namespace Sofra.API.DTOs.Statistics;

public record OrderStatusStatItem(OrderStatus Status, int Count);
