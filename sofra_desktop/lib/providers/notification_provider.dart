import 'dart:async';

import 'package:flutter/foundation.dart';

import '../models/notification_message.dart';
import '../services/api_client.dart';
import '../services/signalr_service.dart';

/// Brojac neprocitanih notifikacija za zvono u topbaru, plus zadnjih par
/// notifikacija primljenih uzivo preko SignalR-a.
class NotificationProvider extends ChangeNotifier {
  NotificationProvider({required this.apiClient, required this.signalRService}) {
    _subscription = signalRService.notifications.listen(_onPush);
  }

  final ApiClient apiClient;
  final SignalRService signalRService;
  StreamSubscription<NotificationMessage>? _subscription;

  int unreadCount = 0;
  final List<NotificationMessage> recent = [];

  Future<void> refreshUnreadCount() async {
    try {
      final result = await apiClient.get('/api/notifications', query: {
        'isRead': false,
        'pageSize': 1,
      }) as JsonMap;
      unreadCount = result['totalCount'] as int;
      notifyListeners();
    } catch (_) {
      // Brojac nije kritican za rad ekrana - tiha greska.
    }
  }

  void _onPush(NotificationMessage message) {
    unreadCount++;
    recent.insert(0, message);
    if (recent.length > 20) {
      recent.removeLast();
    }
    notifyListeners();
  }

  void reset() {
    unreadCount = 0;
    recent.clear();
    notifyListeners();
  }

  @override
  void dispose() {
    _subscription?.cancel();
    super.dispose();
  }
}
