import 'package:flutter/material.dart';

import 'screens/error/not_found_screen.dart';
import 'screens/home/home_screen.dart';
import 'screens/shell/auth_gate.dart';
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

    if (name == '/') {
      return MaterialPageRoute(
        builder: (_) => const AuthGate(route: '/', child: HomeScreen()),
        settings: settings,
      );
    }

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

    final resolvedTitle = navItem.label;
    return MaterialPageRoute(
      builder: (_) => AuthGate(route: name, child: ComingSoonScreen(title: resolvedTitle)),
      settings: settings,
    );
  }
}
