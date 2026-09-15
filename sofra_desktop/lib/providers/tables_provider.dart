import 'dart:async';

import 'package:flutter/foundation.dart';

import '../models/api_exception.dart';
import '../models/dining_table.dart';
import '../models/paged_result.dart';
import '../models/reservation.dart';
import '../models/reservation_status.dart';
import '../models/signalr_event.dart';
import '../models/staff_user.dart';
import '../models/table_type.dart';
import '../models/zone.dart';
import '../services/api_client.dart';
import '../services/signalr_service.dart';

/// Plan sale - svi stolovi (bez paginacije, realan broj je nekoliko desetina), zone i tipovi
/// stola za CRUD formu. Status stola je uvijek posljedican (mijenja ga DiningTableStatusService
/// preko narudzbi/rezervacija) - ovaj provider ga nikad ne postavlja direktno.
class TablesProvider extends ChangeNotifier {
  TablesProvider({required this.apiClient, required this.signalRService}) {
    _subscription = signalRService.rawEvents.listen(_onEvent);
  }

  final ApiClient apiClient;
  final SignalRService signalRService;
  StreamSubscription<SignalREvent>? _subscription;

  bool loading = false;
  String? error;
  List<DiningTable> tables = [];
  List<Zone> zones = [];
  List<TableType> tableTypes = [];

  int? selectedTableId;
  DiningTable? get selectedTable {
    final id = selectedTableId;
    if (id == null) return null;
    for (final table in tables) {
      if (table.id == id) return table;
    }
    return null;
  }

  List<StaffUser> konobarOptions = [];
  bool _konobarOptionsLoaded = false;

  /// Nadolazece rezervacije (Na cekanju/Potvrdjena, sljedecih 30 dana) - izolovano od
  /// ReservationsProvider da izbor filtera na ekranu Rezervacije ne utice na ovaj prikaz
  /// i obrnuto. Koristi ga samo detalj panel stola za "Sljedeca rezervacija".
  List<ReservationListItem> upcomingReservations = [];

  Future<void> loadUpcomingReservations() async {
    try {
      final now = DateTime.now();
      final page = await apiClient.getPaged(
        '/api/reservations',
        ReservationListItem.fromJson,
        query: {
          'dateFrom': now.toIso8601String(),
          'dateTo': now.add(const Duration(days: 30)).toIso8601String(),
          'pageSize': 100,
          'sortBy': 'reservationAt',
        },
      );
      upcomingReservations = page.items
          .where((r) => r.status == ReservationStatus.pending || r.status == ReservationStatus.confirmed)
          .toList();
      notifyListeners();
    } catch (_) {
      // Tiho - "sljedeca rezervacija" je samo prikaz, ne kriticna funkcionalnost.
    }
  }

  ReservationListItem? nextReservationForTable(int tableId) {
    ReservationListItem? next;
    for (final reservation in upcomingReservations) {
      if (reservation.diningTableId != tableId) continue;
      if (next == null || reservation.reservationAt.isBefore(next.reservationAt)) {
        next = reservation;
      }
    }
    return next;
  }

  Future<void> loadAll() async {
    loading = true;
    error = null;
    notifyListeners();
    try {
      final tablesFuture = apiClient.getPaged('/api/tables', DiningTable.fromJson, query: {'pageSize': 100});
      final zonesFuture = apiClient.getPaged('/api/zones', Zone.fromJson, query: {'pageSize': 100});
      final typesFuture = apiClient.getPaged('/api/table-types', TableType.fromJson, query: {'pageSize': 100});
      final results = await Future.wait([tablesFuture, zonesFuture, typesFuture]);
      tables = (results[0] as PagedResult<DiningTable>).items;
      zones = (results[1] as PagedResult<Zone>).items;
      tableTypes = (results[2] as PagedResult<TableType>).items;
    } catch (e) {
      error = e is ApiException ? e.message : 'Došlo je do greške pri učitavanju stolova.';
    } finally {
      loading = false;
      notifyListeners();
    }
  }

  Future<void> loadKonobarOptions() async {
    if (_konobarOptionsLoaded) return;
    try {
      final page = await apiClient.getPaged(
        '/api/users',
        StaffUser.fromJson,
        query: {'role': 'Konobar', 'isActive': true, 'pageSize': 100},
      );
      konobarOptions = page.items;
      _konobarOptionsLoaded = true;
      notifyListeners();
    } catch (_) {
      // Tiho - dropdown ostaje sa samo "Bez konobara" opcijom.
    }
  }

  void selectTable(int id) {
    selectedTableId = id;
    notifyListeners();
  }

  void clearSelection() {
    selectedTableId = null;
    notifyListeners();
  }

  Future<void> createTable({
    required int number,
    required int capacity,
    required int zoneId,
    required int tableTypeId,
    int? waiterId,
    String? qrCode,
  }) async {
    await apiClient.post('/api/tables', body: {
      'number': number,
      'capacity': capacity,
      'zoneId': zoneId,
      'tableTypeId': tableTypeId,
      'waiterId': waiterId,
      'qrCode': qrCode,
    });
    await loadAll();
  }

  Future<void> updateTable(
    int id, {
    required int number,
    required int capacity,
    required int zoneId,
    required int tableTypeId,
    int? waiterId,
    String? qrCode,
  }) async {
    await apiClient.put('/api/tables/$id', body: {
      'number': number,
      'capacity': capacity,
      'zoneId': zoneId,
      'tableTypeId': tableTypeId,
      'waiterId': waiterId,
      'qrCode': qrCode,
    });
    await loadAll();
  }

  Future<void> deleteTable(int id) async {
    await apiClient.delete('/api/tables/$id');
    if (selectedTableId == id) {
      selectedTableId = null;
    }
    await loadAll();
  }

  void _onEvent(SignalREvent event) {
    if (event.hub == 'orders' && event.method == 'tableStatusChanged') {
      loadAll();
    }
  }

  @override
  void dispose() {
    _subscription?.cancel();
    super.dispose();
  }
}
