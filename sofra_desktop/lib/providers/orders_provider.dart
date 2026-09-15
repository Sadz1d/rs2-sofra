import 'dart:async';

import 'package:flutter/foundation.dart';

import '../models/api_exception.dart';
import '../models/order.dart';
import '../models/order_status.dart';
import '../models/order_type.dart';
import '../models/paged_result.dart';
import '../models/signalr_event.dart';
import '../services/api_client.dart';
import '../services/signalr_service.dart';

/// Aktivna kanban tabla (5 statusa, bez Zavrsene/Otkazane), detalj panel i paginirana
/// historija svih narudzbi. Board se osvjezava na svaki SignalR dogadjaj sa OrderHub-a.
class OrdersProvider extends ChangeNotifier {
  OrdersProvider({required this.apiClient, required this.signalRService}) {
    _subscription = signalRService.rawEvents.listen(_onEvent);
  }

  final ApiClient apiClient;
  final SignalRService signalRService;
  StreamSubscription<SignalREvent>? _subscription;

  static const activeStatuses = [
    OrderStatus.pending,
    OrderStatus.confirmed,
    OrderStatus.inPreparation,
    OrderStatus.ready,
    OrderStatus.delivered,
  ];

  // --- Aktivna tabla ---
  bool boardLoading = false;
  String? boardError;
  Map<OrderStatus, List<OrderDetail>> board = {for (final s in activeStatuses) s: []};

  int get activeCount => board.values.fold(0, (sum, list) => sum + list.length);

  Future<void> loadBoard() async {
    boardLoading = true;
    boardError = null;
    notifyListeners();
    try {
      final results = await Future.wait(activeStatuses.map(_fetchStatusDetailed));
      board = {for (var i = 0; i < activeStatuses.length; i++) activeStatuses[i]: results[i]};
    } catch (e) {
      boardError = e is ApiException ? e.message : 'Došlo je do greške pri učitavanju narudžbi.';
    } finally {
      boardLoading = false;
      notifyListeners();
    }
  }

  Future<List<OrderDetail>> _fetchStatusDetailed(OrderStatus status) async {
    final page = await apiClient.getPaged(
      '/api/orders',
      OrderListItem.fromJson,
      query: {'status': status.value, 'pageSize': 50, 'sortBy': 'createdAt'},
    );
    final details = await Future.wait(page.items.map((item) => fetchDetail(item.id)));
    details.sort((a, b) => a.createdAt.compareTo(b.createdAt));
    return details;
  }

  Future<OrderDetail> fetchDetail(int id) async {
    final json = await apiClient.get('/api/orders/$id') as JsonMap;
    return OrderDetail.fromJson(json);
  }

  // --- Detalj panel ---
  int? selectedOrderId;
  OrderDetail? selectedOrder;
  bool selectedLoading = false;
  String? selectedError;

  Future<void> selectOrder(int id) async {
    selectedOrderId = id;
    final cached = _findInBoard(id);
    if (cached != null) {
      selectedOrder = cached;
      selectedError = null;
      notifyListeners();
      return;
    }

    selectedLoading = true;
    selectedError = null;
    notifyListeners();
    try {
      selectedOrder = await fetchDetail(id);
    } catch (e) {
      selectedOrder = null;
      selectedError = e is ApiException ? e.message : 'Došlo je do greške pri učitavanju narudžbe.';
    } finally {
      selectedLoading = false;
      notifyListeners();
    }
  }

  void clearSelection() {
    selectedOrderId = null;
    selectedOrder = null;
    selectedError = null;
    notifyListeners();
  }

  OrderDetail? _findInBoard(int id) {
    for (final list in board.values) {
      for (final order in list) {
        if (order.id == id) return order;
      }
    }
    return null;
  }

  // --- Akcije ---
  Future<void> transition(int orderId, OrderStatus to, {String? cancelReason}) async {
    await apiClient.post('/api/orders/$orderId/status', body: {
      'status': to.value,
      'cancelReason': cancelReason,
    });
    await _refreshAfterChange(orderId);
  }

  Future<void> payCash(int orderId) async {
    await apiClient.post('/api/payments/cash', body: {'orderId': orderId});
    await _refreshAfterChange(orderId);
  }

  Future<void> _refreshAfterChange(int orderId) async {
    await loadBoard();
    if (historyPage != null) {
      await loadHistory();
    }
    if (selectedOrderId == orderId) {
      await selectOrder(orderId);
    }
  }

  // --- Historija (paginirana tabela) ---
  PagedResult<OrderListItem>? historyPage;
  bool historyLoading = false;
  String? historyError;

  int historyPageNumber = 1;
  String historySearch = '';
  OrderStatus? historyStatus;
  OrderType? historyType;
  DateTime? historyDateFrom;
  DateTime? historyDateTo;

  Future<void> loadHistory() async {
    historyLoading = true;
    historyError = null;
    notifyListeners();
    try {
      historyPage = await apiClient.getPaged(
        '/api/orders',
        OrderListItem.fromJson,
        query: {
          'page': historyPageNumber,
          'pageSize': 20,
          if (historySearch.isNotEmpty) 'search': historySearch,
          if (historyStatus != null) 'status': historyStatus!.value,
          if (historyType != null) 'type': historyType!.value,
          if (historyDateFrom != null) 'dateFrom': historyDateFrom!.toIso8601String(),
          if (historyDateTo != null) 'dateTo': historyDateTo!.toIso8601String(),
        },
      );
    } catch (e) {
      historyError = e is ApiException ? e.message : 'Došlo je do greške pri učitavanju historije narudžbi.';
    } finally {
      historyLoading = false;
      notifyListeners();
    }
  }

  void setHistoryPage(int page) {
    historyPageNumber = page;
    loadHistory();
  }

  void setHistoryFilters({
    String? search,
    OrderStatus? status,
    bool clearStatus = false,
    OrderType? type,
    bool clearType = false,
    DateTime? dateFrom,
    bool clearDateFrom = false,
    DateTime? dateTo,
    bool clearDateTo = false,
  }) {
    if (search != null) historySearch = search;
    if (clearStatus) {
      historyStatus = null;
    } else if (status != null) {
      historyStatus = status;
    }
    if (clearType) {
      historyType = null;
    } else if (type != null) {
      historyType = type;
    }
    if (clearDateFrom) {
      historyDateFrom = null;
    } else if (dateFrom != null) {
      historyDateFrom = dateFrom;
    }
    if (clearDateTo) {
      historyDateTo = null;
    } else if (dateTo != null) {
      historyDateTo = dateTo;
    }
    historyPageNumber = 1;
    loadHistory();
  }

  void _onEvent(SignalREvent event) {
    if (event.hub == 'orders' && (event.method == 'orderStatusChanged' || event.method == 'orderCreated')) {
      loadBoard();
      if (historyPage != null) {
        loadHistory();
      }
      if (selectedOrderId != null) {
        selectOrder(selectedOrderId!);
      }
    }
  }

  @override
  void dispose() {
    _subscription?.cancel();
    super.dispose();
  }
}
