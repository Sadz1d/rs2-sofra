import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../../providers/auth_provider.dart';
import '../../theme/app_theme.dart';
import '../../utils/nav_items.dart';

class AppSidebar extends StatelessWidget {
  const AppSidebar({super.key, required this.currentRoute});

  final String currentRoute;

  @override
  Widget build(BuildContext context) {
    final roles = context.watch<AuthProvider>().currentUser?.roles ?? const <String>[];
    final items = kNavItems.where((item) => item.allowedRoles.any(roles.contains)).toList();

    return Container(
      width: 240,
      color: AppColors.dark,
      child: SafeArea(
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            const Padding(
              padding: EdgeInsets.fromLTRB(20, 24, 20, 16),
              child: Text(
                'Sofra',
                style: TextStyle(color: Colors.white, fontSize: 24, fontWeight: FontWeight.bold),
              ),
            ),
            Expanded(
              child: ListView(
                padding: EdgeInsets.zero,
                children: [
                  for (final item in items)
                    _SidebarTile(item: item, selected: item.route == currentRoute),
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }
}

class _SidebarTile extends StatelessWidget {
  const _SidebarTile({required this.item, required this.selected});

  final NavItem item;
  final bool selected;

  @override
  Widget build(BuildContext context) {
    return Material(
      color: selected ? AppColors.primary.withValues(alpha: 0.15) : Colors.transparent,
      child: ListTile(
        leading: Icon(item.icon, color: selected ? AppColors.primary : Colors.white70),
        title: Text(
          item.label,
          style: TextStyle(
            color: selected ? AppColors.primary : Colors.white70,
            fontWeight: selected ? FontWeight.w600 : FontWeight.normal,
          ),
        ),
        selected: selected,
        onTap: selected ? null : () => Navigator.of(context).pushReplacementNamed(item.route),
      ),
    );
  }
}
