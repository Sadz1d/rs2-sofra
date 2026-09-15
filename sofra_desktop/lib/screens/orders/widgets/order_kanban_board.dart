import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../../../models/allowed_transition.dart';
import '../../../models/order.dart';
import '../../../models/order_status.dart';
import '../../../providers/auth_provider.dart';
import '../../../providers/orders_provider.dart';
import '../../../theme/app_theme.dart';
import '../../../utils/formatting.dart';
import '../../../widgets/app_toast.dart';
import '../../../widgets/confirm_dialog.dart';
import '../../../widgets/empty_view.dart';
import '../../../widgets/error_view.dart';
import '../../../widgets/loading_view.dart';
import '../../../widgets/reason_dialog.dart';

class _Column {
  const _Column({required this.label, required this.statuses, required this.dotColor});

  final String label;
  final List<OrderStatus> statuses;
  final Color dotColor;
}

const _columns = [
  _Column(label: 'Primljene', statuses: [OrderStatus.pending], dotColor: Colors.blue),
  _Column(label: 'U pripremi', statuses: [OrderStatus.confirmed, OrderStatus.inPreparation], dotColor: Colors.amber),
  _Column(label: 'Spremne', statuses: [OrderStatus.ready], dotColor: AppColors.success),
  _Column(label: 'Isporučene', statuses: [OrderStatus.delivered], dotColor: Colors.blueGrey),
];

class OrderKanbanBoard extends StatelessWidget {
  const OrderKanbanBoard({super.key, required this.onSelect});

  final ValueChanged<int> onSelect;

  @override
  Widget build(BuildContext context) {
    final orders = context.watch<OrdersProvider>();

    if (orders.boardLoading && orders.activeCount == 0 && orders.boardError == null) {
      return const LoadingView(message: 'Učitavanje narudžbi...');
    }

    if (orders.boardError != null) {
      return ErrorView(message: orders.boardError!, onRetry: () => context.read<OrdersProvider>().loadBoard());
    }

    return SingleChildScrollView(
      scrollDirection: Axis.horizontal,
      padding: const EdgeInsets.all(16),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          for (final column in _columns) ...[
            _KanbanColumn(column: column, onSelect: onSelect),
            const SizedBox(width: 16),
          ],
        ],
      ),
    );
  }
}

class _KanbanColumn extends StatelessWidget {
  const _KanbanColumn({required this.column, required this.onSelect});

  final _Column column;
  final ValueChanged<int> onSelect;

  @override
  Widget build(BuildContext context) {
    final orders = context.watch<OrdersProvider>();
    final items = [
      for (final status in column.statuses) ...orders.board[status] ?? const [],
    ]..sort((a, b) => a.createdAt.compareTo(b.createdAt));

    return SizedBox(
      width: 300,
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Padding(
            padding: const EdgeInsets.symmetric(horizontal: 4, vertical: 8),
            child: Row(
              children: [
                Icon(Icons.circle, size: 10, color: column.dotColor),
                const SizedBox(width: 8),
                Text(column.label, style: Theme.of(context).textTheme.titleSmall),
                const Spacer(),
                Text('${items.length}', style: Theme.of(context).textTheme.bodyMedium),
              ],
            ),
          ),
          if (items.isEmpty)
            const Padding(
              padding: EdgeInsets.symmetric(vertical: 24),
              child: EmptyView(message: 'Nema narudžbi.', icon: Icons.inbox_outlined),
            )
          else
            for (final order in items) ...[
              _OrderCard(order: order, onTap: () => onSelect(order.id)),
              const SizedBox(height: 10),
            ],
        ],
      ),
    );
  }
}

class _OrderCard extends StatelessWidget {
  const _OrderCard({required this.order, required this.onTap});

  final OrderDetail order;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    final roles = context.watch<AuthProvider>().currentUser?.roles ?? const <String>[];

    return Card(
      child: InkWell(
        onTap: onTap,
        borderRadius: BorderRadius.circular(12),
        child: Padding(
          padding: const EdgeInsets.all(12),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Row(
                mainAxisAlignment: MainAxisAlignment.spaceBetween,
                children: [
                  Text('#${order.number}', style: const TextStyle(fontWeight: FontWeight.w700)),
                  Row(
                    children: [
                      const Icon(Icons.access_time, size: 14, color: Colors.black54),
                      const SizedBox(width: 4),
                      Text(formatWaitDuration(order.lastStatusChangeAt), style: Theme.of(context).textTheme.bodySmall),
                    ],
                  ),
                ],
              ),
              const SizedBox(height: 4),
              Text(
                order.diningTableNumber != null
                    ? 'Sto ${order.diningTableNumber} · ${order.items.length} st.'
                    : 'Za ponijeti · ${order.items.length} st.',
                style: Theme.of(context).textTheme.bodySmall,
              ),
              if (order.notes.isNotEmpty)
                Container(
                  margin: const EdgeInsets.only(top: 8),
                  padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 6),
                  decoration: BoxDecoration(
                    color: Colors.amber.withValues(alpha: 0.15),
                    borderRadius: BorderRadius.circular(6),
                  ),
                  child: Text(
                    order.notes.join('; '),
                    style: const TextStyle(fontSize: 12),
                    maxLines: 2,
                    overflow: TextOverflow.ellipsis,
                  ),
                ),
              const SizedBox(height: 10),
              Text(formatMoney(order.total), style: const TextStyle(fontWeight: FontWeight.w700, fontSize: 16)),
              const SizedBox(height: 8),
              _CardActions(order: order, roles: roles),
            ],
          ),
        ),
      ),
    );
  }
}

class _CardActions extends StatelessWidget {
  const _CardActions({required this.order, required this.roles});

  final OrderDetail order;
  final List<String> roles;

  @override
  Widget build(BuildContext context) {
    final ordersProvider = context.read<OrdersProvider>();

    switch (order.status) {
      case OrderStatus.pending:
        final confirm = order.allowedTransitions.forStatus(OrderStatus.confirmed.value);
        final cancel = order.allowedTransitions.forStatus(OrderStatus.cancelled.value);
        if (confirm == null && cancel == null) return const SizedBox.shrink();
        return Row(
          children: [
            if (confirm != null)
              Expanded(
                child: FilledButton(
                  onPressed: () async {
                    try {
                      await ordersProvider.transition(order.id, OrderStatus.confirmed);
                      if (context.mounted) showAppToast(context, 'Narudžba #${order.number} je potvrđena.');
                    } catch (e) {
                      if (context.mounted) showAppToast(context, e.toString(), isError: true);
                    }
                  },
                  child: Text(OrderStatus.confirmed.actionVerb),
                ),
              ),
            if (confirm != null && cancel != null) const SizedBox(width: 8),
            if (cancel != null)
              Expanded(
                child: OutlinedButton(
                  onPressed: () => _cancel(context, ordersProvider),
                  child: Text(OrderStatus.cancelled.actionVerb),
                ),
              ),
          ],
        );

      case OrderStatus.confirmed:
      case OrderStatus.inPreparation:
        return const SizedBox.shrink();

      case OrderStatus.ready:
        final option = order.allowedTransitions.forStatus(OrderStatus.delivered.value);
        if (option == null) return const SizedBox.shrink();
        return SizedBox(
          width: double.infinity,
          child: FilledButton(
            style: FilledButton.styleFrom(backgroundColor: AppColors.success),
            onPressed: () async {
              try {
                await ordersProvider.transition(order.id, OrderStatus.delivered);
                if (context.mounted) showAppToast(context, 'Narudžba #${order.number} je isporučena.');
              } catch (e) {
                if (context.mounted) showAppToast(context, e.toString(), isError: true);
              }
            },
            child: Text(OrderStatus.delivered.actionVerb),
          ),
        );

      case OrderStatus.delivered:
        if (order.isPaid) {
          return Container(
            width: double.infinity,
            padding: const EdgeInsets.symmetric(vertical: 8),
            alignment: Alignment.center,
            decoration: BoxDecoration(
              color: AppColors.success.withValues(alpha: 0.12),
              borderRadius: BorderRadius.circular(8),
            ),
            child: const Text('Plaćeno', style: TextStyle(color: AppColors.success, fontWeight: FontWeight.w600)),
          );
        }
        if (!roles.any((r) => r == 'Konobar' || r == 'Admin')) return const SizedBox.shrink();
        return SizedBox(
          width: double.infinity,
          child: OutlinedButton(
            onPressed: () async {
              final confirmed = await showConfirmDialog(
                context,
                title: 'Naplata narudžbe',
                message: 'Naplatiti narudžbu #${order.number} gotovinom u iznosu ${formatMoney(order.total)}?',
                confirmLabel: 'Naplati',
              );
              if (!confirmed || !context.mounted) return;
              try {
                await ordersProvider.payCash(order.id);
                if (context.mounted) showAppToast(context, 'Narudžba #${order.number} je naplaćena.');
              } catch (e) {
                if (context.mounted) showAppToast(context, e.toString(), isError: true);
              }
            },
            child: const Text('Naplati'),
          ),
        );

      case OrderStatus.completed:
      case OrderStatus.cancelled:
        return const SizedBox.shrink();
    }
  }

  Future<void> _cancel(BuildContext context, OrdersProvider ordersProvider) async {
    final reason = await showReasonDialog(
      context,
      title: 'Otkazivanje narudžbe',
      message: 'Otkazati narudžbu #${order.number}? Razlog je obavezan.',
      label: 'Razlog otkazivanja',
      confirmLabel: 'Otkaži narudžbu',
    );
    if (reason == null || !context.mounted) return;
    try {
      await ordersProvider.transition(order.id, OrderStatus.cancelled, cancelReason: reason);
      if (context.mounted) showAppToast(context, 'Narudžba #${order.number} je otkazana.');
    } catch (e) {
      if (context.mounted) showAppToast(context, e.toString(), isError: true);
    }
  }
}
