import 'inventory_item.dart';
import 'order.dart';
import 'reservation.dart';

/// Total je null za ne-Admin osoblje - finansijski podatak koji backend ne racuna za njih.
class DashboardChartPoint {
  const DashboardChartPoint({required this.periodStart, required this.orderCount, this.total});

  final DateTime periodStart;
  final int orderCount;
  final double? total;

  factory DashboardChartPoint.fromJson(Map<String, dynamic> json) => DashboardChartPoint(
        periodStart: DateTime.parse(json['periodStart'] as String),
        orderCount: json['orderCount'] as int,
        total: (json['total'] as num?)?.toDouble(),
      );
}

/// Revenue je null za ne-Admin osoblje - finansijski podatak.
class DashboardTopItem {
  const DashboardTopItem({
    required this.menuItemId,
    required this.menuItemName,
    required this.quantitySold,
    this.revenue,
  });

  final int menuItemId;
  final String menuItemName;
  final int quantitySold;
  final double? revenue;

  factory DashboardTopItem.fromJson(Map<String, dynamic> json) => DashboardTopItem(
        menuItemId: json['menuItemId'] as int,
        menuItemName: json['menuItemName'] as String,
        quantitySold: json['quantitySold'] as int,
        revenue: (json['revenue'] as num?)?.toDouble(),
      );
}

/// Sve sto Dashboard-u treba, iz jednog poziva GET /api/statistics/dashboard. Revenue/Tax/
/// AverageOrderValue (i finansijski dio Chart/TopItems) su null za ne-Admin osoblje.
class DashboardData {
  const DashboardData({
    required this.ordersCount,
    required this.activeOrdersCount,
    required this.reservationsCount,
    required this.reservationsPendingCount,
    required this.tablesOccupied,
    required this.tablesTotal,
    required this.lowStockCount,
    required this.chart,
    required this.activeOrders,
    required this.reservations,
    required this.lowStockItems,
    required this.topItems,
    this.revenue,
    this.tax,
    this.averageOrderValue,
  });

  final int ordersCount;
  final double? revenue;
  final double? tax;
  final double? averageOrderValue;
  final int activeOrdersCount;
  final int reservationsCount;
  final int reservationsPendingCount;
  final int tablesOccupied;
  final int tablesTotal;
  final int lowStockCount;
  final List<DashboardChartPoint> chart;
  final List<OrderListItem> activeOrders;
  final List<ReservationListItem> reservations;
  final List<InventoryItem> lowStockItems;
  final List<DashboardTopItem> topItems;

  bool get hasFinancials => revenue != null;

  double get tableOccupancyPercent => tablesTotal == 0 ? 0 : (tablesOccupied / tablesTotal) * 100;

  factory DashboardData.fromJson(Map<String, dynamic> json) => DashboardData(
        ordersCount: json['ordersCount'] as int,
        revenue: (json['revenue'] as num?)?.toDouble(),
        tax: (json['tax'] as num?)?.toDouble(),
        averageOrderValue: (json['averageOrderValue'] as num?)?.toDouble(),
        activeOrdersCount: json['activeOrdersCount'] as int,
        reservationsCount: json['reservationsCount'] as int,
        reservationsPendingCount: json['reservationsPendingCount'] as int,
        tablesOccupied: json['tablesOccupied'] as int,
        tablesTotal: json['tablesTotal'] as int,
        lowStockCount: json['lowStockCount'] as int,
        chart: (json['chart'] as List<dynamic>)
            .map((e) => DashboardChartPoint.fromJson(e as Map<String, dynamic>))
            .toList(),
        activeOrders: (json['activeOrders'] as List<dynamic>)
            .map((e) => OrderListItem.fromJson(e as Map<String, dynamic>))
            .toList(),
        reservations: (json['reservations'] as List<dynamic>)
            .map((e) => ReservationListItem.fromJson(e as Map<String, dynamic>))
            .toList(),
        lowStockItems: (json['lowStockItems'] as List<dynamic>)
            .map((e) => InventoryItem.fromJson(e as Map<String, dynamic>))
            .toList(),
        topItems: (json['topItems'] as List<dynamic>)
            .map((e) => DashboardTopItem.fromJson(e as Map<String, dynamic>))
            .toList(),
      );
}
