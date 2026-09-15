import 'package:flutter/material.dart';

import '../../utils/nav_items.dart';
import 'sidebar.dart';
import 'topbar.dart';

/// Trajni okvir aplikacije (sidebar + topbar) - svaki poslovni ekran se renderuje unutar njega.
class AppShell extends StatelessWidget {
  const AppShell({super.key, required this.currentRoute, required this.child});

  final String currentRoute;
  final Widget child;

  @override
  Widget build(BuildContext context) {
    final matching = kNavItems.where((item) => item.route == currentRoute);
    final title = matching.isNotEmpty ? matching.first.label : 'Sofra';

    return Scaffold(
      body: Row(
        children: [
          AppSidebar(currentRoute: currentRoute),
          Expanded(
            child: Scaffold(
              appBar: AppTopBar(title: title),
              body: child,
            ),
          ),
        ],
      ),
    );
  }
}
