import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';

import 'package:sofra_desktop/app.dart';
import 'package:sofra_desktop/providers/auth_provider.dart';
import 'package:sofra_desktop/providers/dashboard_provider.dart';
import 'package:sofra_desktop/providers/notification_provider.dart';
import 'package:sofra_desktop/providers/orders_provider.dart';
import 'package:sofra_desktop/providers/reservations_provider.dart';
import 'package:sofra_desktop/providers/tables_provider.dart';
import 'package:sofra_desktop/services/api_client.dart';
import 'package:sofra_desktop/services/auth_service.dart';
import 'package:sofra_desktop/services/signalr_service.dart';
import 'package:sofra_desktop/services/token_store.dart';

void main() {
  testWidgets('Prikazuje pocetni (splash) ekran prije provjere prijave', (tester) async {
    final tokenStore = TokenStore();
    final apiClient = ApiClient(tokenStore: tokenStore);
    final signalRService = SignalRService(tokenStore: tokenStore);
    final authService = AuthService(apiClient: apiClient, tokenStore: tokenStore);
    final authProvider = AuthProvider(
      authService: authService,
      tokenStore: tokenStore,
      signalRService: signalRService,
    );
    final notificationProvider = NotificationProvider(
      apiClient: apiClient,
      signalRService: signalRService,
    );
    final ordersProvider = OrdersProvider(apiClient: apiClient, signalRService: signalRService);
    final dashboardProvider = DashboardProvider(apiClient: apiClient);
    final tablesProvider = TablesProvider(apiClient: apiClient, signalRService: signalRService);
    final reservationsProvider = ReservationsProvider(apiClient: apiClient, signalRService: signalRService);

    await tester.pumpWidget(
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

    // Status prijave je jos AuthStatus.unknown (bootstrap se ne poziva u testu) - ocekuje se splash ekran.
    expect(find.byType(CircularProgressIndicator), findsOneWidget);
  });
}
