import 'package:flutter/material.dart';

import 'screens/dashboard/dashboard_screen.dart';
import 'screens/error/not_found_screen.dart';
import 'screens/kitchen/kitchen_screen.dart';
import 'screens/orders/orders_screen.dart';
import 'screens/reservations/reservations_screen.dart';
import 'screens/shell/auth_gate.dart';
import 'screens/tables/tables_screen.dart';
import 'screens/shell/coming_soon_screen.dart';
import 'theme/app_theme.dart';
import 'utils/nav_items.dart';

class SofraApp extends StatelessWidget {
  const SofraApp({super.key});

  @override
  Widget build(BuildContext context) {
    return MaterialApp(
      title: 'Sofra',
      debugShowCheckedModeBanner: false,
      theme: AppTheme.light,
      initialRoute: '/',
      onGenerateRoute: _onGenerateRoute,
    );
  }

  static Route<dynamic> _onGenerateRoute(RouteSettings settings) {
    final name = settings.name ?? '/';

    NavItem? navItem;
    for (final item in kNavItems) {
      if (item.route == name) {
        navItem = item;
        break;
      }
    }

    if (navItem == null) {
      return MaterialPageRoute(
        builder: (_) => AuthGate(route: name, child: const NotFoundScreen()),
        settings: settings,
      );
    }

    final content = switch (name) {
      '/' => const DashboardScreen(),
      '/orders' => const OrdersScreen(),
      '/kitchen' => const KitchenScreen(),
      '/tables' => const TablesScreen(),
      '/reservations' => const ReservationsScreen(),
      _ => ComingSoonScreen(title: navItem.label),
    };

    return MaterialPageRoute(
      builder: (_) => AuthGate(route: name, allowedRoles: navItem!.allowedRoles, child: content),
      settings: settings,
    );
  }
}
