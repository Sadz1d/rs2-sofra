import 'dart:async';

import 'package:signalr_netcore/signalr_client.dart';

import '../models/notification_message.dart';
import '../models/signalr_event.dart';
import 'api_client.dart';
import 'token_store.dart';

/// Spaja se na /hubs/notifications i /hubs/orders i izlaze tokove dogadjaja preko
/// stream-ova. Ekrani se pretplacuju na ove tokove - ne spajaju se sami na hub.
class SignalRService {
  SignalRService({required this.tokenStore});

  final TokenStore tokenStore;

  HubConnection? _notificationHub;
  HubConnection? _orderHub;

  final _notificationsController = StreamController<NotificationMessage>.broadcast();
  Stream<NotificationMessage> get notifications => _notificationsController.stream;

  final _rawEventsController = StreamController<SignalREvent>.broadcast();
  /// Sirovi tok sa oba huba - koristi ga privremeni ekran koji dokazuje da veza radi.
  Stream<SignalREvent> get rawEvents => _rawEventsController.stream;

  static const _orderHubMethods = ['orderStatusChanged', 'reservationCreated', 'tableStatusChanged'];

  Future<void> connect() async {
    await disconnect();

    _notificationHub = _build('/hubs/notifications');
    _notificationHub!.on('notificationReceived', (arguments) {
      if (arguments == null || arguments.isEmpty) return;
      final message = NotificationMessage.fromJson(arguments[0] as Map<String, dynamic>);
      _notificationsController.add(message);
      _rawEventsController.add(SignalREvent(hub: 'notifications', method: 'notificationReceived', data: message));
    });

    _orderHub = _build('/hubs/orders');
    for (final method in _orderHubMethods) {
      _orderHub!.on(method, (arguments) {
        final data = (arguments != null && arguments.isNotEmpty) ? arguments[0] : null;
        _rawEventsController.add(SignalREvent(hub: 'orders', method: method, data: data));
      });
    }

    await Future.wait([
      _notificationHub!.start() ?? Future.value(),
      _orderHub!.start() ?? Future.value(),
    ]);
  }

  HubConnection _build(String hubPath) {
    return HubConnectionBuilder()
        .withUrl(
          '${ApiClient.baseUrl}$hubPath',
          options: HttpConnectionOptions(
            accessTokenFactory: () async => tokenStore.accessToken ?? '',
          ),
        )
        .withAutomaticReconnect()
        .build();
  }

  Future<void> disconnect() async {
    await _notificationHub?.stop();
    await _orderHub?.stop();
    _notificationHub = null;
    _orderHub = null;
  }

  void dispose() {
    disconnect();
    _notificationsController.close();
    _rawEventsController.close();
  }
}
