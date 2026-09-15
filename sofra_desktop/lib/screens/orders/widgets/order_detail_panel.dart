import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../../../models/order.dart';
import '../../../models/order_status.dart';
import '../../../models/order_workflow.dart';
import '../../../providers/auth_provider.dart';
import '../../../providers/orders_provider.dart';
import '../../../theme/app_theme.dart';
import '../../../utils/formatting.dart';
import '../../../widgets/app_toast.dart';
import '../../../widgets/error_view.dart';
import '../../../widgets/loading_view.dart';
import '../../../widgets/reason_dialog.dart';

/// Slide-in panel sa detaljima jedne narudzbe - dijeli ga kanban tabla i historija.
class OrderDetailPanel extends StatelessWidget {
  const OrderDetailPanel({super.key});

  @override
  Widget build(BuildContext context) {
    final orders = context.watch<OrdersProvider>();

    return Container(
      width: 380,
      decoration: const BoxDecoration(
        color: Colors.white,
        border: Border(left: BorderSide(color: Color(0x1A000000))),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          Padding(
            padding: const EdgeInsets.fromLTRB(20, 20, 12, 12),
            child: Row(
              children: [
                Expanded(
                  child: Text(
                    orders.selectedOrder != null ? 'Narudžba #${orders.selectedOrder!.number}' : 'Narudžba',
                    style: Theme.of(context).textTheme.titleLarge,
                  ),
                ),
                IconButton(
                  icon: const Icon(Icons.close),
                  onPressed: () => context.read<OrdersProvider>().clearSelection(),
                ),
              ],
            ),
          ),
          const Divider(height: 1),
          Expanded(child: _Body(orders: orders)),
        ],
      ),
    );
  }
}

class _Body extends StatelessWidget {
  const _Body({required this.orders});

  final OrdersProvider orders;

  @override
  Widget build(BuildContext context) {
    if (orders.selectedLoading) {
      return const LoadingView();
    }
    if (orders.selectedError != null) {
      return ErrorView(
        message: orders.selectedError!,
        onRetry: () => orders.selectOrder(orders.selectedOrderId!),
      );
    }
    final order = orders.selectedOrder;
    if (order == null) {
      return const Center(child: Text('Odaberite narudžbu.'));
    }

    final roles = context.watch<AuthProvider>().currentUser?.roles ?? const <String>[];

    return SingleChildScrollView(
      padding: const EdgeInsets.all(20),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Wrap(
            spacing: 8,
            runSpacing: 8,
            children: [
              _Pill(label: order.status.label, color: _statusColor(order.status)),
              if (order.isPaid)
                _Pill(
                  label: '✓ Plaćeno${order.paymentMethodName != null ? ' ${order.paymentMethodName!.toLowerCase()}' : ''}',
                  color: AppColors.success,
                ),
            ],
          ),
          const SizedBox(height: 20),
          _InfoRow(
            label: order.diningTableNumber != null ? 'Sto' : 'Tip',
            value: order.diningTableNumber != null ? '${order.diningTableNumber}' : order.type.label,
          ),
          if (order.waiterName != null) _InfoRow(label: 'Konobar', value: order.waiterName!),
          _InfoRow(label: 'Gost', value: order.userName),
          _InfoRow(
            label: 'Vrijeme',
            value: '${formatTime(order.createdAt)} · čeka ${formatWaitDuration(order.lastStatusChangeAt)}',
          ),
          if (order.notes.isNotEmpty) ...[
            const SizedBox(height: 12),
            for (final note in order.notes)
              Container(
                margin: const EdgeInsets.only(bottom: 6),
                padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 8),
                decoration: BoxDecoration(
                  color: Colors.amber.withValues(alpha: 0.15),
                  borderRadius: BorderRadius.circular(8),
                ),
                child: Text(note, style: const TextStyle(fontSize: 13)),
              ),
          ],
          const SizedBox(height: 20),
          for (final item in order.items)
            Padding(
              padding: const EdgeInsets.symmetric(vertical: 4),
              child: Row(
                children: [
                  Expanded(child: Text('${item.quantity}× ${item.menuItemName}')),
                  Text(formatMoney(item.lineTotal)),
                ],
              ),
            ),
          if (order.promotionCode != null)
            Padding(
              padding: const EdgeInsets.symmetric(vertical: 4),
              child: Row(
                children: [
                  Expanded(child: Text('Popust ${order.promotionCode}')),
                  Text('-${formatMoney(order.discount)}', style: const TextStyle(color: AppColors.success)),
                ],
              ),
            ),
          const Divider(height: 24),
          _TotalRow(label: 'Osnovica', value: order.subtotal),
          _TotalRow(label: 'PDV', value: order.tax),
          _TotalRow(label: 'Ukupno', value: order.total, isBold: true),
          const SizedBox(height: 24),
          Text('Promjena statusa', style: Theme.of(context).textTheme.titleSmall),
          const SizedBox(height: 12),
          for (final option in OrderWorkflow.nextOptions(order.status, roles, isPaid: order.isPaid))
            Padding(
              padding: const EdgeInsets.only(bottom: 8),
              child: _TransitionButton(order: order, option: option),
            ),
          if (OrderWorkflow.nextOptions(order.status, roles, isPaid: order.isPaid).isEmpty)
            Text(
              'Nema dostupnih akcija za vašu ulogu na ovom statusu.',
              style: Theme.of(context).textTheme.bodySmall,
            ),
          const SizedBox(height: 20),
          Text(_historyLine(order), style: Theme.of(context).textTheme.bodySmall),
        ],
      ),
    );
  }

  String _historyLine(OrderDetail order) {
    final parts = <String>['Primljena ${formatTime(order.createdAt)}'];
    if (order.confirmedAt != null) parts.add('Potvrđena ${formatTime(order.confirmedAt!)}');
    if (order.preparationStartedAt != null) parts.add('U pripremi ${formatTime(order.preparationStartedAt!)}');
    if (order.readyAt != null) parts.add('Spremna ${formatTime(order.readyAt!)}');
    if (order.deliveredAt != null) parts.add('Isporučena ${formatTime(order.deliveredAt!)}');
    if (order.completedAt != null) parts.add('Završena ${formatTime(order.completedAt!)}');
    if (order.cancelledAt != null) {
      parts.add('Otkazana ${formatTime(order.cancelledAt!)}${order.cancelReason != null ? ' (${order.cancelReason})' : ''}');
    }
    return 'Historija: ${parts.join(' · ')}';
  }

  Color _statusColor(OrderStatus status) => switch (status) {
        OrderStatus.pending => Colors.blue,
        OrderStatus.confirmed || OrderStatus.inPreparation => Colors.amber.shade800,
        OrderStatus.ready => AppColors.success,
        OrderStatus.delivered => Colors.blueGrey,
        OrderStatus.completed => AppColors.dark,
        OrderStatus.cancelled => AppColors.danger,
      };
}

class _TransitionButton extends StatelessWidget {
  const _TransitionButton({required this.order, required this.option});

  final OrderDetail order;
  final OrderTransitionOption option;

  @override
  Widget build(BuildContext context) {
    final isCancel = option.to == OrderStatus.cancelled;

    if (!option.allowed) {
      return Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          SizedBox(
            width: double.infinity,
            child: OutlinedButton(onPressed: null, child: Text(option.label)),
          ),
          if (option.disabledReason != null)
            Padding(
              padding: const EdgeInsets.only(top: 4),
              child: Text(
                option.disabledReason!,
                style: Theme.of(context).textTheme.bodySmall?.copyWith(color: AppColors.danger),
              ),
            ),
        ],
      );
    }

    return SizedBox(
      width: double.infinity,
      child: isCancel
          ? OutlinedButton(
              style: OutlinedButton.styleFrom(foregroundColor: AppColors.danger),
              onPressed: () => _handle(context),
              child: Text(option.label),
            )
          : FilledButton(
              style: option.to == OrderStatus.ready || option.to == OrderStatus.delivered
                  ? FilledButton.styleFrom(backgroundColor: AppColors.success)
                  : null,
              onPressed: () => _handle(context),
              child: Text(option.label),
            ),
    );
  }

  Future<void> _handle(BuildContext context) async {
    final ordersProvider = context.read<OrdersProvider>();
    String? cancelReason;
    if (option.to == OrderStatus.cancelled) {
      cancelReason = await showReasonDialog(
        context,
        title: 'Otkazivanje narudžbe',
        message: 'Otkazati narudžbu #${order.number}? Razlog je obavezan.',
        label: 'Razlog otkazivanja',
        confirmLabel: 'Otkaži narudžbu',
      );
      if (cancelReason == null || !context.mounted) return;
    }
    try {
      await ordersProvider.transition(order.id, option.to, cancelReason: cancelReason);
      if (context.mounted) showAppToast(context, 'Status narudžbe #${order.number} je ažuriran.');
    } catch (e) {
      if (context.mounted) showAppToast(context, e.toString(), isError: true);
    }
  }
}

class _InfoRow extends StatelessWidget {
  const _InfoRow({required this.label, required this.value});

  final String label;
  final String value;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 4),
      child: Row(
        children: [
          SizedBox(width: 90, child: Text(label, style: Theme.of(context).textTheme.bodySmall)),
          Expanded(child: Text(value, style: const TextStyle(fontWeight: FontWeight.w500))),
        ],
      ),
    );
  }
}

class _TotalRow extends StatelessWidget {
  const _TotalRow({required this.label, required this.value, this.isBold = false});

  final String label;
  final double value;
  final bool isBold;

  @override
  Widget build(BuildContext context) {
    final style = TextStyle(fontWeight: isBold ? FontWeight.w700 : FontWeight.w400, fontSize: isBold ? 16 : 14);
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 4),
      child: Row(
        mainAxisAlignment: MainAxisAlignment.spaceBetween,
        children: [
          Text(label, style: style),
          Text(formatMoney(value), style: style),
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
