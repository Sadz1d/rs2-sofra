import 'dart:async';

import 'package:flutter/foundation.dart';

import '../models/api_exception.dart';
import '../models/paged_result.dart';
import '../models/reservation.dart';
import '../models/reservation_status.dart';
import '../models/signalr_event.dart';
import '../services/api_client.dart';
import '../services/signalr_service.dart';

/// Paginirana lista rezervacija sa filterima i detalj panel. Dugmad za prelaze se crtaju
/// iz ReservationDetail.allowedTransitions - isti princip kao kod narudzbi.
class ReservationsProvider extends ChangeNotifier {
  ReservationsProvider({required this.apiClient, required this.signalRService}) {
    _subscription = signalRService.rawEvents.listen(_onEvent);
  }

  final ApiClient apiClient;
  final SignalRService signalRService;
  StreamSubscription<SignalREvent>? _subscription;

  // --- Lista ---
  PagedResult<ReservationListItem>? page;
  bool listLoading = false;
  String? listError;

  int pageNumber = 1;
  String search = '';
  ReservationStatus? status;
  int? zoneId;
  DateTime? dateFrom;
  DateTime? dateTo;

  Future<void> loadList() async {
    listLoading = true;
    listError = null;
    notifyListeners();
    try {
      page = await apiClient.getPaged(
        '/api/reservations',
        ReservationListItem.fromJson,
        query: {
          'page': pageNumber,
          'pageSize': 20,
          if (search.isNotEmpty) 'search': search,
          if (status != null) 'status': status!.value,
          if (zoneId != null) 'zoneId': zoneId,
          if (dateFrom != null) 'dateFrom': dateFrom!.toIso8601String(),
          if (dateTo != null) 'dateTo': dateTo!.toIso8601String(),
        },
      );
    } catch (e) {
      listError = e is ApiException ? e.message : 'Došlo je do greške pri učitavanju rezervacija.';
    } finally {
      listLoading = false;
      notifyListeners();
    }
  }

  void setPage(int p) {
    pageNumber = p;
    loadList();
  }

  void setFilters({
    String? search,
    ReservationStatus? status,
    bool clearStatus = false,
    int? zoneId,
    bool clearZone = false,
    DateTime? dateFrom,
    bool clearDateFrom = false,
    DateTime? dateTo,
    bool clearDateTo = false,
  }) {
    if (search != null) this.search = search;
    if (clearStatus) {
      this.status = null;
    } else if (status != null) {
      this.status = status;
    }
    if (clearZone) {
      this.zoneId = null;
    } else if (zoneId != null) {
      this.zoneId = zoneId;
    }
    if (clearDateFrom) {
      this.dateFrom = null;
    } else if (dateFrom != null) {
      this.dateFrom = dateFrom;
    }
    if (clearDateTo) {
      this.dateTo = null;
    } else if (dateTo != null) {
      this.dateTo = dateTo;
    }
    pageNumber = 1;
    loadList();
  }

  // --- Detalj ---
  int? selectedReservationId;
  ReservationDetail? selectedReservation;
  bool selectedLoading = false;
  String? selectedError;

  Future<void> selectReservation(int id) async {
    selectedReservationId = id;
    selectedLoading = true;
    selectedError = null;
    notifyListeners();
    try {
      final json = await apiClient.get('/api/reservations/$id') as JsonMap;
      selectedReservation = ReservationDetail.fromJson(json);
    } catch (e) {
      selectedReservation = null;
      selectedError = e is ApiException ? e.message : 'Došlo je do greške pri učitavanju rezervacije.';
    } finally {
      selectedLoading = false;
      notifyListeners();
    }
  }

  void clearSelection() {
    selectedReservationId = null;
    selectedReservation = null;
    selectedError = null;
    notifyListeners();
  }

  // --- Akcije ---
  Future<void> transition(int id, ReservationStatus to, {String? rejectReason, DateTime? alternativeAt}) async {
    await apiClient.post('/api/reservations/$id/status', body: {
      'status': to.value,
      'rejectReason': rejectReason,
      'alternativeAt': alternativeAt?.toIso8601String(),
    });
    await _refreshAfterChange(id);
  }

  Future<void> assignTable(int id, int diningTableId) async {
    await apiClient.put('/api/reservations/$id/table', body: {'diningTableId': diningTableId});
    await _refreshAfterChange(id);
  }

  Future<void> _refreshAfterChange(int id) async {
    if (page != null) {
      await loadList();
    }
    if (selectedReservationId == id) {
      await selectReservation(id);
    }
  }

  void _onEvent(SignalREvent event) {
    if (event.hub == 'orders' && event.method == 'reservationCreated') {
      if (page != null) {
        loadList();
      }
    }
  }

  @override
  void dispose() {
    _subscription?.cancel();
    super.dispose();
  }
}
