import 'dart:async';

import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../../models/order.dart';
import '../../models/order_status.dart';
import '../../models/order_workflow.dart';
import '../../providers/auth_provider.dart';
import '../../providers/orders_provider.dart';
import '../../theme/app_theme.dart';
import '../../widgets/app_toast.dart';
import '../../widgets/empty_view.dart';
import '../../widgets/error_view.dart';
import '../../widgets/loading_view.dart';
import '../../widgets/stat_card.dart';

/// KDS - ravan grid kartica sortiran po vremenu cekanja, ne kolone po statusu (vidi mockup d03).
/// Prikazuje samo Potvrdjene i U pripremi narudzbe - to su jedina dva prelaza koje Kuhar
/// stvarno smije izvrsiti (Na cekanju je posao konobara, potvrdjuje se na ekranu Narudzbe).
class KitchenScreen extends StatefulWidget {
  const KitchenScreen({super.key});

  @override
  State<KitchenScreen> createState() => _KitchenScreenState();
}

class _KitchenScreenState extends State<KitchenScreen> {
  Timer? _ticker;

  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addPostFrameCallback((_) {
      context.read<OrdersProvider>().loadBoard();
    });
    // Osvjezava izvedene KPI brojeve (prosjecno vrijeme, kasni) i bez novog SignalR dogadjaja.
    _ticker = Timer.periodic(const Duration(seconds: 30), (_) {
      if (mounted) setState(() {});
    });
  }

  @override
  void dispose() {
    _ticker?.cancel();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final orders = context.watch<OrdersProvider>();

    if (orders.boardLoading && orders.activeCount == 0 && orders.boardError == null) {
      return const LoadingView(message: 'Učitavanje kuhinjskih naloga...');
    }
    if (orders.boardError != null) {
      return ErrorView(message: orders.boardError!, onRetry: () => orders.loadBoard());
    }

    final items = [
      ...orders.board[OrderStatus.confirmed] ?? const [],
      ...orders.board[OrderStatus.inPreparation] ?? const [],
    ]..sort((a, b) => _waitSince(a).compareTo(_waitSince(b)));

    final now = DateTime.now();
    final waits = items.map((o) => now.difference(_waitSince(o))).toList();
    final avgMinutes = waits.isEmpty ? 0 : (waits.fold<int>(0, (sum, d) => sum + d.inMinutes) / waits.length).round();
    final lateCount = waits.where((d) => d.inMinutes > 12).length;

    return Padding(
      padding: const EdgeInsets.all(24),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          Row(
            children: [
              Expanded(
                child: StatCard(
                  title: 'Aktivni nalozi',
                  value: '${items.length}',
                  subtitle: '${orders.board[OrderStatus.confirmed]?.length ?? 0} čekaju start',
                  icon: Icons.soup_kitchen_outlined,
                  iconColor: AppColors.primary,
                ),
              ),
              const SizedBox(width: 16),
              Expanded(
                child: StatCard(
                  title: 'Prosječno vrijeme čekanja',
                  value: '$avgMinutes min',
                  icon: Icons.timer_outlined,
                  iconColor: Colors.blue,
                ),
              ),
              const SizedBox(width: 16),
              Expanded(
                child: StatCard(
                  title: 'Kasni (> 12 min)',
                  value: '$lateCount',
                  subtitle: lateCount > 0 ? 'označeno crveno' : null,
                  icon: Icons.warning_amber_outlined,
                  iconColor: AppColors.danger,
                ),
              ),
            ],
          ),
          const SizedBox(height: 20),
          Expanded(
            child: items.isEmpty
                ? const EmptyView(message: 'Nema aktivnih naloga u kuhinji.', icon: Icons.soup_kitchen_outlined)
                : SingleChildScrollView(
                    child: Wrap(
                      spacing: 16,
                      runSpacing: 16,
                      children: [for (final order in items) SizedBox(width: 320, child: _KitchenCard(order: order))],
                    ),
                  ),
          ),
        ],
      ),
    );
  }

  DateTime _waitSince(OrderDetail order) => order.confirmedAt ?? order.createdAt;
}

class _KitchenCard extends StatelessWidget {
  const _KitchenCard({required this.order});

  final OrderDetail order;

  @override
  Widget build(BuildContext context) {
    final since = order.confirmedAt ?? order.createdAt;
    final roles = context.watch<AuthProvider>().currentUser?.roles ?? const <String>[];
    final option = OrderWorkflow.primaryOption(order.status, roles, isPaid: order.isPaid);

    return Card(
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(12),
        side: BorderSide(color: _UrgencyClock.colorFor(since), width: 3),
      ),
      child: Padding(
        padding: const EdgeInsets.all(14),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              children: [
                Text('#${order.number}', style: const TextStyle(fontWeight: FontWeight.w700, fontSize: 16)),
                _UrgencyClock(since: since),
              ],
            ),
            const SizedBox(height: 2),
            Row(
              children: [
                Text(
                  order.diningTableNumber != null ? 'Sto ${order.diningTableNumber}' : 'Za ponijeti',
                  style: Theme.of(context).textTheme.bodySmall,
                ),
                const SizedBox(width: 6),
                _StatusChip(status: order.status),
              ],
            ),
            const SizedBox(height: 10),
            for (final item in order.items)
              Padding(
                padding: const EdgeInsets.symmetric(vertical: 2),
                child: Text('${item.quantity}× ${item.menuItemName}'),
              ),
            if (order.notes.isNotEmpty)
              Container(
                margin: const EdgeInsets.only(top: 8),
                padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 6),
                decoration: BoxDecoration(
                  color: Colors.amber.withValues(alpha: 0.15),
                  borderRadius: BorderRadius.circular(6),
                ),
                child: Text(order.notes.join('; '), style: const TextStyle(fontSize: 12)),
              ),
            const SizedBox(height: 12),
            if (option != null)
              SizedBox(
                width: double.infinity,
                child: FilledButton.icon(
                  style: FilledButton.styleFrom(backgroundColor: AppColors.success),
                  icon: Icon(order.status == OrderStatus.confirmed ? Icons.play_arrow : Icons.check),
                  label: Text(order.status == OrderStatus.confirmed ? 'Započni' : 'Spremno'),
                  onPressed: () async {
                    try {
                      await context.read<OrdersProvider>().transition(order.id, option.to);
                      if (context.mounted) showAppToast(context, 'Narudžba #${order.number} ažurirana.');
                    } catch (e) {
                      if (context.mounted) showAppToast(context, e.toString(), isError: true);
                    }
                  },
                ),
              ),
          ],
        ),
      ),
    );
  }
}

class _StatusChip extends StatelessWidget {
  const _StatusChip({required this.status});

  final OrderStatus status;

  @override
  Widget build(BuildContext context) {
    final color = status == OrderStatus.confirmed ? Colors.blue : Colors.amber.shade800;
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 2),
      decoration: BoxDecoration(color: color.withValues(alpha: 0.12), borderRadius: BorderRadius.circular(12)),
      child: Text(status.label, style: TextStyle(color: color, fontSize: 11, fontWeight: FontWeight.w600)),
    );
  }
}

/// Uzivo tikuci timer (MM:SS) sa bojom po hitnosti - < 5 min zeleno, 5-12 min zuto, > 12 min crveno.
class _UrgencyClock extends StatefulWidget {
  const _UrgencyClock({required this.since});

  final DateTime since;

  @override
  State<_UrgencyClock> createState() => _UrgencyClockState();

  static Color colorFor(DateTime since) {
    final minutes = DateTime.now().difference(since).inMinutes;
    if (minutes > 12) return AppColors.danger;
    if (minutes >= 5) return Colors.amber.shade700;
    return AppColors.success;
  }
}

class _UrgencyClockState extends State<_UrgencyClock> {
  Timer? _timer;

  @override
  void initState() {
    super.initState();
    _timer = Timer.periodic(const Duration(seconds: 1), (_) {
      if (mounted) setState(() {});
    });
  }

  @override
  void dispose() {
    _timer?.cancel();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final elapsed = DateTime.now().difference(widget.since);
    final color = _UrgencyClock.colorFor(widget.since);

    // MM:SS dok traje uzivo tikanje ima smisla samo unutar jednog sata cekanja;
    // preko toga prelazi na Xh Ymin da ne prikazuje zavaravajuce "omotane" vrijednosti.
    final label = elapsed.inHours >= 1
        ? '${elapsed.inHours}h ${elapsed.inMinutes.remainder(60)}min'
        : '${elapsed.inMinutes.toString().padLeft(2, '0')}:${elapsed.inSeconds.remainder(60).toString().padLeft(2, '0')}';

    return Row(
      mainAxisSize: MainAxisSize.min,
      children: [
        Icon(Icons.access_time, size: 14, color: color),
        const SizedBox(width: 4),
        Text(label, style: TextStyle(color: color, fontWeight: FontWeight.w600, fontSize: 13)),
      ],
    );
  }
}
