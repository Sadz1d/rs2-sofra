import 'package:flutter/foundation.dart';

import '../models/api_exception.dart';
import '../models/dining_table.dart';
import '../models/inventory_item.dart';
import '../models/reservation.dart';
import '../models/reservation_status.dart';
import '../models/stat_models.dart';
import '../models/table_status.dart';
import '../services/api_client.dart';
import '../utils/formatting.dart';

enum RevenueChartRange { days7, days30, year }

/// Agregira nekoliko postojecih endpointa (statistika, rezervacije, stolovi, zalihe) u
/// jedan pregled za Dashboard. Dashboard nema svoj API endpoint - sastavlja se na klijentu.
class DashboardProvider extends ChangeNotifier {
  DashboardProvider({required this.apiClient});

  final ApiClient apiClient;

  bool loading = false;
  String? error;

  double revenueToday = 0;
  int ordersToday = 0;
  double revenueYesterday = 0;
  int ordersYesterday = 0;

  int reservationsTodayCount = 0;
  int reservationsPendingCount = 0;
  List<ReservationListItem> reservationsTodayPreview = [];

  int tablesOccupied = 0;
  int tablesTotal = 0;

  int lowStockCount = 0;
  List<InventoryItem> lowStockPreview = [];

  List<TopMenuItemStat> topItemsToday = [];

  RevenueChartRange chartRange = RevenueChartRange.days7;
  List<RevenueStatItem> chartData = [];
  bool chartLoading = false;

  double get revenueDeltaPercent =>
      revenueYesterday == 0 ? 0 : ((revenueToday - revenueYesterday) / revenueYesterday) * 100;

  int get ordersDelta => ordersToday - ordersYesterday;

  double get tableOccupancyPercent => tablesTotal == 0 ? 0 : (tablesOccupied / tablesTotal) * 100;

  Future<void> loadAll() async {
    loading = true;
    error = null;
    notifyListeners();
    try {
      await Future.wait([
        _loadRevenueKpis(),
        _loadReservationsToday(),
        _loadTableOccupancy(),
        _loadLowStock(),
        _loadTopItemsToday(),
        _loadChart(),
      ]);
    } catch (e) {
      error = e is ApiException ? e.message : 'Došlo je do greške pri učitavanju pregleda.';
    } finally {
      loading = false;
      notifyListeners();
    }
  }

  Future<void> _loadRevenueKpis() async {
    final today = DateTime.now();
    final yesterday = today.subtract(const Duration(days: 1));
    final rows = await apiClient.get('/api/statistics/revenue', query: {
      'dateFrom': apiDateOnly(yesterday),
      'dateTo': apiDateOnly(today),
      'period': 'Day',
    }) as List<dynamic>;

    final items = rows.map((e) => RevenueStatItem.fromJson(e as JsonMap)).toList();
    for (final item in items) {
      final local = item.periodStart.toLocal();
      if (_isSameDate(local, today)) {
        revenueToday = item.total;
        ordersToday = item.orderCount;
      } else if (_isSameDate(local, yesterday)) {
        revenueYesterday = item.total;
        ordersYesterday = item.orderCount;
      }
    }
  }

  Future<void> _loadReservationsToday() async {
    final today = DateTime.now();
    final startOfDay = DateTime(today.year, today.month, today.day);
    final endOfDay = startOfDay.add(const Duration(days: 1));

    final page = await apiClient.getPaged(
      '/api/reservations',
      ReservationListItem.fromJson,
      query: {
        'dateFrom': startOfDay.toIso8601String(),
        'dateTo': endOfDay.toIso8601String(),
        'pageSize': 100,
        'sortBy': 'reservationAt',
      },
    );
    reservationsTodayCount = page.totalCount;
    reservationsPendingCount = page.items.where((r) => r.status == ReservationStatus.pending).length;
    reservationsTodayPreview = page.items.take(3).toList();
  }

  Future<void> _loadTableOccupancy() async {
    final page = await apiClient.getPaged('/api/tables', DiningTable.fromJson, query: {'pageSize': 100});
    tablesTotal = page.items.length;
    tablesOccupied = page.items.where((t) => t.status == TableStatus.occupied).length;
  }

  Future<void> _loadLowStock() async {
    final page = await apiClient.getPaged(
      '/api/inventory-items',
      InventoryItem.fromJson,
      query: {'lowStock': true, 'pageSize': 3},
    );
    lowStockCount = page.totalCount;
    lowStockPreview = page.items;
  }

  Future<void> _loadTopItemsToday() async {
    final today = DateTime.now();
    final rows = await apiClient.get('/api/statistics/top-items', query: {
      'dateFrom': apiDateOnly(today),
      'dateTo': apiDateOnly(today),
      'take': 4,
    }) as List<dynamic>;
    topItemsToday = rows.map((e) => TopMenuItemStat.fromJson(e as JsonMap)).toList();
  }

  Future<void> setChartRange(RevenueChartRange range) async {
    chartRange = range;
    await _loadChart();
    notifyListeners();
  }

  Future<void> _loadChart() async {
    chartLoading = true;
    notifyListeners();
    try {
      final today = DateTime.now();
      final (from, period) = switch (chartRange) {
        RevenueChartRange.days7 => (today.subtract(const Duration(days: 6)), 'Day'),
        RevenueChartRange.days30 => (today.subtract(const Duration(days: 29)), 'Day'),
        RevenueChartRange.year => (DateTime(today.year - 1, today.month, today.day), 'Month'),
      };

      final rows = await apiClient.get('/api/statistics/revenue', query: {
        'dateFrom': apiDateOnly(from),
        'dateTo': apiDateOnly(today),
        'period': period,
      }) as List<dynamic>;
      chartData = rows.map((e) => RevenueStatItem.fromJson(e as JsonMap)).toList();
    } finally {
      chartLoading = false;
      notifyListeners();
    }
  }

  bool _isSameDate(DateTime a, DateTime b) => a.year == b.year && a.month == b.month && a.day == b.day;
}
