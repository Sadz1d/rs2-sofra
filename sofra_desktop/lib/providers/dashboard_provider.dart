import 'package:flutter/foundation.dart';

import '../models/api_exception.dart';
import '../models/dashboard_data.dart';
import '../services/api_client.dart';
import '../utils/formatting.dart';

enum DashboardRange { today, days7, days30, year }

/// Jedan poziv (GET /api/statistics/dashboard) daje sve sto Dashboard-u treba za odabrani
/// raspon - KPI, graf, aktivne narudzbe/rezervacije, niske zalihe, top jela.
class DashboardProvider extends ChangeNotifier {
  DashboardProvider({required this.apiClient});

  final ApiClient apiClient;

  bool loading = false;
  String? error;
  DashboardRange range = DashboardRange.today;
  DashboardData? data;

  Future<void> load() async {
    loading = true;
    error = null;
    notifyListeners();
    try {
      final today = DateTime.now();
      final (from, period) = switch (range) {
        DashboardRange.today => (today, 'Day'),
        DashboardRange.days7 => (today.subtract(const Duration(days: 6)), 'Day'),
        DashboardRange.days30 => (today.subtract(const Duration(days: 29)), 'Day'),
        DashboardRange.year => (DateTime(today.year - 1, today.month, today.day), 'Month'),
      };

      final json = await apiClient.get('/api/statistics/dashboard', query: {
        'dateFrom': apiDateOnly(from),
        'dateTo': apiDateOnly(today),
        'period': period,
      }) as JsonMap;

      data = DashboardData.fromJson(json);
    } catch (e) {
      error = e is ApiException ? e.message : 'Došlo je do greške pri učitavanju pregleda.';
    } finally {
      loading = false;
      notifyListeners();
    }
  }

  Future<void> setRange(DashboardRange newRange) async {
    if (range == newRange) return;
    range = newRange;
    await load();
  }
}
