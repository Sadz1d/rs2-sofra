import 'dart:async';

import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../../models/signalr_event.dart';
import '../../providers/auth_provider.dart';
import '../../services/signalr_service.dart';
import '../../theme/app_theme.dart';
import '../../widgets/empty_view.dart';

/// Privremeni ekran koji dokazuje da su auth, API klijent i SignalR spojeni -
/// prikazuje prijavljenog korisnika, njegove uloge i uzivo dogadjaje sa huba.
/// Uklanja se kad prvi pravi ekran (Dashboard) dobije svoj korak u Fazi 3.
class HomeScreen extends StatefulWidget {
  const HomeScreen({super.key});

  @override
  State<HomeScreen> createState() => _HomeScreenState();
}

class _HomeScreenState extends State<HomeScreen> {
  final List<SignalREvent> _events = [];
  StreamSubscription<SignalREvent>? _subscription;

  @override
  void initState() {
    super.initState();
    final signalRService = context.read<SignalRService>();
    _subscription = signalRService.rawEvents.listen((event) {
      setState(() {
        _events.insert(0, event);
        if (_events.length > 30) {
          _events.removeLast();
        }
      });
    });
  }

  @override
  void dispose() {
    _subscription?.cancel();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final user = context.watch<AuthProvider>().currentUser;

    return Padding(
      padding: const EdgeInsets.all(24),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Card(
            child: Padding(
              padding: const EdgeInsets.all(20),
              child: Row(
                children: [
                  CircleAvatar(
                    radius: 28,
                    child: Text(
                      user != null && user.firstName.isNotEmpty ? user.firstName[0] : '?',
                      style: const TextStyle(fontSize: 20),
                    ),
                  ),
                  const SizedBox(width: 16),
                  Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(user?.fullName ?? '', style: Theme.of(context).textTheme.titleLarge),
                      Text(user?.email ?? '', style: Theme.of(context).textTheme.bodyMedium),
                      const SizedBox(height: 4),
                      Wrap(
                        spacing: 6,
                        children: [
                          for (final role in user?.roles ?? const <String>[])
                            Chip(
                              label: Text(role),
                              backgroundColor: AppColors.primary.withValues(alpha: 0.12),
                            ),
                        ],
                      ),
                    ],
                  ),
                ],
              ),
            ),
          ),
          const SizedBox(height: 24),
          Text('SignalR — uživo dogadjaji', style: Theme.of(context).textTheme.titleMedium),
          const SizedBox(height: 8),
          Expanded(
            child: _events.isEmpty
                ? const EmptyView(
                    icon: Icons.podcasts_outlined,
                    message: 'Još nema primljenih dogadjaja.\n'
                        'Napravite akciju u API-ju (npr. promjenu statusa narudžbe) da vidite prijem ovdje.',
                  )
                : ListView.separated(
                    itemCount: _events.length,
                    separatorBuilder: (_, _) => const Divider(height: 1),
                    itemBuilder: (context, index) {
                      final event = _events[index];
                      return ListTile(
                        dense: true,
                        leading: Icon(
                          event.hub == 'notifications' ? Icons.notifications_outlined : Icons.receipt_long_outlined,
                        ),
                        title: Text('${event.hub}.${event.method}'),
                        subtitle: Text(event.data?.toString() ?? ''),
                        trailing: Text(TimeOfDay.fromDateTime(event.receivedAt).format(context)),
                      );
                    },
                  ),
          ),
        ],
      ),
    );
  }
}
