import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../../providers/auth_provider.dart';
import '../../providers/notification_provider.dart';

class AppTopBar extends StatelessWidget implements PreferredSizeWidget {
  const AppTopBar({super.key, required this.title});

  final String title;

  @override
  Size get preferredSize => const Size.fromHeight(64);

  @override
  Widget build(BuildContext context) {
    final auth = context.watch<AuthProvider>();
    final notifications = context.watch<NotificationProvider>();
    final user = auth.currentUser;

    return AppBar(
      title: Text(title),
      actions: [
        Stack(
          alignment: Alignment.center,
          children: [
            IconButton(
              icon: const Icon(Icons.notifications_outlined),
              tooltip: 'Notifikacije',
              // Ekran sa listom notifikacija dolazi u kasnijem koraku Faze 3.
              onPressed: () {},
            ),
            if (notifications.unreadCount > 0)
              Positioned(
                right: 8,
                top: 10,
                child: Container(
                  padding: const EdgeInsets.symmetric(horizontal: 5, vertical: 1),
                  decoration: BoxDecoration(
                    color: Theme.of(context).colorScheme.error,
                    borderRadius: BorderRadius.circular(10),
                  ),
                  child: Text(
                    notifications.unreadCount > 99 ? '99+' : '${notifications.unreadCount}',
                    style: const TextStyle(color: Colors.white, fontSize: 10),
                  ),
                ),
              ),
          ],
        ),
        const SizedBox(width: 8),
        PopupMenuButton<String>(
          onSelected: (value) {
            if (value == 'logout') {
              auth.logout();
            }
          },
          itemBuilder: (context) => [
            PopupMenuItem<String>(
              enabled: false,
              child: Text(user?.fullName ?? '', style: const TextStyle(fontWeight: FontWeight.w600)),
            ),
            const PopupMenuDivider(),
            const PopupMenuItem<String>(value: 'logout', child: Text('Odjava')),
          ],
          child: Padding(
            padding: const EdgeInsets.symmetric(horizontal: 12),
            child: Row(
              mainAxisSize: MainAxisSize.min,
              children: [
                CircleAvatar(
                  radius: 16,
                  child: Text(user != null && user.firstName.isNotEmpty ? user.firstName[0] : '?'),
                ),
                const SizedBox(width: 8),
                Text(user?.fullName ?? ''),
                const Icon(Icons.arrow_drop_down),
              ],
            ),
          ),
        ),
        const SizedBox(width: 16),
      ],
    );
  }
}
