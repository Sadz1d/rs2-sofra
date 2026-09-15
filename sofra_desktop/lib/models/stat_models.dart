class RevenueStatItem {
  const RevenueStatItem({
    required this.periodStart,
    required this.orderCount,
    required this.subtotal,
    required this.tax,
    required this.total,
  });

  final DateTime periodStart;
  final int orderCount;
  final double subtotal;
  final double tax;
  final double total;

  factory RevenueStatItem.fromJson(Map<String, dynamic> json) => RevenueStatItem(
        periodStart: DateTime.parse(json['periodStart'] as String),
        orderCount: json['orderCount'] as int,
        subtotal: (json['subtotal'] as num).toDouble(),
        tax: (json['tax'] as num).toDouble(),
        total: (json['total'] as num).toDouble(),
      );
}

class TopMenuItemStat {
  const TopMenuItemStat({
    required this.menuItemId,
    required this.menuItemName,
    required this.quantitySold,
    required this.revenue,
  });

  final int menuItemId;
  final String menuItemName;
  final int quantitySold;
  final double revenue;

  factory TopMenuItemStat.fromJson(Map<String, dynamic> json) => TopMenuItemStat(
        menuItemId: json['menuItemId'] as int,
        menuItemName: json['menuItemName'] as String,
        quantitySold: json['quantitySold'] as int,
        revenue: (json['revenue'] as num).toDouble(),
      );
}
