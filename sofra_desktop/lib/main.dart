import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import 'app.dart';
import 'providers/auth_provider.dart';
import 'providers/dashboard_provider.dart';
import 'providers/notification_provider.dart';
import 'providers/orders_provider.dart';
import 'providers/reservations_provider.dart';
import 'providers/tables_provider.dart';
import 'services/api_client.dart';
import 'services/auth_service.dart';
import 'services/signalr_service.dart';
import 'services/token_store.dart';

void main() {
  WidgetsFlutterBinding.ensureInitialized();

  final tokenStore = TokenStore();
  final apiClient = ApiClient(tokenStore: tokenStore);
  final signalRService = SignalRService(tokenStore: tokenStore);
  final authService = AuthService(apiClient: apiClient, tokenStore: tokenStore);

  final authProvider = AuthProvider(
    authService: authService,
    tokenStore: tokenStore,
    signalRService: signalRService,
  );
  // Postavljeno nakon konstrukcije da se izbjegne kruzna zavisnost izmedju ApiClient-a i AuthProvider-a.
  apiClient.onSessionExpired = authProvider.handleSessionExpired;

  final notificationProvider = NotificationProvider(
    apiClient: apiClient,
    signalRService: signalRService,
  );
  authProvider.addListener(() {
    if (authProvider.status == AuthStatus.authenticated) {
      notificationProvider.refreshUnreadCount();
    } else if (authProvider.status == AuthStatus.unauthenticated) {
      notificationProvider.reset();
    }
  });

  final ordersProvider = OrdersProvider(apiClient: apiClient, signalRService: signalRService);
  final dashboardProvider = DashboardProvider(apiClient: apiClient);
  final tablesProvider = TablesProvider(apiClient: apiClient, signalRService: signalRService);
  final reservationsProvider = ReservationsProvider(apiClient: apiClient, signalRService: signalRService);

  authProvider.bootstrap();

  runApp(
    MultiProvider(
      providers: [
        Provider<ApiClient>.value(value: apiClient),
        Provider<SignalRService>.value(value: signalRService),
        ChangeNotifierProvider<AuthProvider>.value(value: authProvider),
        ChangeNotifierProvider<NotificationProvider>.value(value: notificationProvider),
        ChangeNotifierProvider<OrdersProvider>.value(value: ordersProvider),
        ChangeNotifierProvider<DashboardProvider>.value(value: dashboardProvider),
        ChangeNotifierProvider<TablesProvider>.value(value: tablesProvider),
        ChangeNotifierProvider<ReservationsProvider>.value(value: reservationsProvider),
      ],
      child: const SofraApp(),
    ),
  );
}
