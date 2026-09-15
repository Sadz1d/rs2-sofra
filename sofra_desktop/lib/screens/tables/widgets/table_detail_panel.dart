import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../../../constants/roles.dart';
import '../../../models/dining_table.dart';
import '../../../models/order.dart';
import '../../../models/order_status.dart';
import '../../../models/reservation.dart';
import '../../../models/table_status.dart';
import '../../../providers/auth_provider.dart';
import '../../../providers/orders_provider.dart';
import '../../../providers/tables_provider.dart';
import '../../../theme/app_theme.dart';
import '../../../utils/formatting.dart';
import '../../../widgets/app_toast.dart';
import '../../../widgets/confirm_dialog.dart';
import 'table_form_dialog.dart';

class TableDetailPanel extends StatelessWidget {
  const TableDetailPanel({super.key});

  @override
  Widget build(BuildContext context) {
    final tablesProvider = context.watch<TablesProvider>();
    final table = tablesProvider.selectedTable;
    final isAdmin = context.watch<AuthProvider>().currentUser?.roles.contains(Roles.admin) ?? false;

    return Container(
      width: 380,
      decoration: const BoxDecoration(
        color: Colors.white,
        border: Border(left: BorderSide(color: Color(0x1A000000))),
      ),
      child: table == null
          ? const Center(child: Text('Odaberite sto.'))
          : Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                Padding(
                  padding: const EdgeInsets.fromLTRB(20, 20, 12, 12),
                  child: Row(
                    children: [
                      Expanded(
                        child: Text('Sto ${table.number} · ${table.zoneName}', style: Theme.of(context).textTheme.titleLarge),
                      ),
                      IconButton(
                        icon: const Icon(Icons.close),
                        onPressed: () => context.read<TablesProvider>().clearSelection(),
                      ),
                    ],
                  ),
                ),
                const Divider(height: 1),
                Expanded(
                  child: SingleChildScrollView(
                    padding: const EdgeInsets.all(20),
                    child: _Body(table: table, isAdmin: isAdmin),
                  ),
                ),
              ],
            ),
    );
  }
}

class _Body extends StatelessWidget {
  const _Body({required this.table, required this.isAdmin});

  final DiningTable table;
  final bool isAdmin;

  @override
  Widget build(BuildContext context) {
    final activeOrder = _findActiveOrder(context);
    final nextReservation = _findNextReservation(context);

    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Wrap(
          spacing: 8,
          runSpacing: 8,
          children: [
            _Pill(label: table.status.label, color: _statusColor(table.status)),
            _Pill(label: '${table.capacity} osoba', color: AppColors.dark),
            _Pill(label: table.tableTypeName, color: AppColors.dark),
          ],
        ),
        const SizedBox(height: 20),
        Text(
          'Status se mijenja automatski kroz narudžbe i rezervacije.',
          style: Theme.of(context).textTheme.bodySmall,
        ),
        const SizedBox(height: 16),
        if (isAdmin) ...[
          Text('Dodijeljeni konobar', style: Theme.of(context).textTheme.titleSmall),
          const SizedBox(height: 8),
          Text(table.waiterName ?? 'Bez konobara'),
          const SizedBox(height: 20),
        ],
        if (activeOrder != null) ...[
          Text('Aktivna narudžba', style: Theme.of(context).textTheme.titleSmall),
          const SizedBox(height: 8),
          _ActiveOrderTile(order: activeOrder),
          const SizedBox(height: 20),
        ],
        if (nextReservation != null) ...[
          Text('Sljedeća rezervacija', style: Theme.of(context).textTheme.titleSmall),
          const SizedBox(height: 8),
          _NextReservationTile(reservation: nextReservation),
          const SizedBox(height: 20),
        ],
        if (isAdmin) ...[
          const Divider(height: 32),
          SizedBox(
            width: double.infinity,
            child: FilledButton.icon(
              icon: const Icon(Icons.edit_outlined),
              label: const Text('Uredi sto'),
              onPressed: () => showDialog<void>(
                context: context,
                builder: (_) => TableFormDialog(existing: table),
              ),
            ),
          ),
          const SizedBox(height: 8),
          SizedBox(
            width: double.infinity,
            child: OutlinedButton.icon(
              style: OutlinedButton.styleFrom(foregroundColor: AppColors.danger),
              icon: const Icon(Icons.delete_outline),
              label: const Text('Obriši sto'),
              onPressed: () => _delete(context),
            ),
          ),
        ],
      ],
    );
  }

  OrderDetail? _findActiveOrder(BuildContext context) {
    final orders = context.watch<OrdersProvider>();
    for (final list in orders.board.values) {
      for (final order in list) {
        if (order.diningTableId == table.id && order.status != OrderStatus.completed && order.status != OrderStatus.cancelled) {
          return order;
        }
      }
    }
    return null;
  }

  ReservationListItem? _findNextReservation(BuildContext context) =>
      context.watch<TablesProvider>().nextReservationForTable(table.id);

  Future<void> _delete(BuildContext context) async {
    final confirmed = await showConfirmDialog(
      context,
      title: 'Brisanje stola',
      message: 'Obrisati sto ${table.number}? Ova radnja se ne može poništiti.',
      confirmLabel: 'Obriši',
      danger: true,
    );
    if (!confirmed || !context.mounted) return;
    try {
      await context.read<TablesProvider>().deleteTable(table.id);
      if (context.mounted) showAppToast(context, 'Sto ${table.number} je obrisan.');
    } catch (e) {
      if (context.mounted) showAppToast(context, e.toString(), isError: true);
    }
  }

  Color _statusColor(TableStatus status) => switch (status) {
        TableStatus.free => AppColors.success,
        TableStatus.occupied => AppColors.danger,
        TableStatus.reserved => Colors.amber.shade800,
      };
}

class _ActiveOrderTile extends StatelessWidget {
  const _ActiveOrderTile({required this.order});

  final OrderDetail order;

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.all(12),
      decoration: BoxDecoration(
        color: AppColors.background,
        borderRadius: BorderRadius.circular(8),
      ),
      child: Row(
        children: [
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text('#${order.number} · ${order.items.length} st.', style: const TextStyle(fontWeight: FontWeight.w600)),
                Text(order.status.label, style: Theme.of(context).textTheme.bodySmall),
              ],
            ),
          ),
          Text(formatMoney(order.total)),
        ],
      ),
    );
  }
}

class _NextReservationTile extends StatelessWidget {
  const _NextReservationTile({required this.reservation});

  final ReservationListItem reservation;

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.all(12),
      decoration: BoxDecoration(
        color: AppColors.background,
        borderRadius: BorderRadius.circular(8),
      ),
      child: Row(
        children: [
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(reservation.userName, style: const TextStyle(fontWeight: FontWeight.w600)),
                Text(
                  '${formatDateTime(reservation.reservationAt)} · ${reservation.guests} os.',
                  style: Theme.of(context).textTheme.bodySmall,
                ),
              ],
            ),
          ),
          Text(reservation.status.label, style: Theme.of(context).textTheme.bodySmall),
        ],
      ),
    );
  }
}

class _Pill extends StatelessWidget {
  const _Pill({required this.label, required this.color});

  final String label;
  final Color color;

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
      decoration: BoxDecoration(color: color.withValues(alpha: 0.12), borderRadius: BorderRadius.circular(20)),
      child: Text(label, style: TextStyle(color: color, fontSize: 12, fontWeight: FontWeight.w600)),
    );
  }
}
