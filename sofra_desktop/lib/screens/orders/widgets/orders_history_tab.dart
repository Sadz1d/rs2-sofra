import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../../../models/order.dart';
import '../../../models/order_status.dart';
import '../../../models/order_type.dart';
import '../../../providers/orders_provider.dart';
import '../../../theme/app_theme.dart';
import '../../../utils/formatting.dart';
import '../../../widgets/empty_view.dart';
import '../../../widgets/error_view.dart';
import '../../../widgets/filter_bar.dart';
import '../../../widgets/paginated_table.dart';

class OrdersHistoryTab extends StatefulWidget {
  const OrdersHistoryTab({super.key, required this.onSelect});

  final ValueChanged<int> onSelect;

  @override
  State<OrdersHistoryTab> createState() => _OrdersHistoryTabState();
}

class _OrdersHistoryTabState extends State<OrdersHistoryTab> {
  final _searchController = TextEditingController();

  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addPostFrameCallback((_) {
      final orders = context.read<OrdersProvider>();
      if (orders.historyPage == null) {
        orders.loadHistory();
      }
    });
  }

  @override
  void dispose() {
    _searchController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final orders = context.watch<OrdersProvider>();

    return Padding(
      padding: const EdgeInsets.all(16),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          FilterBar(
            searchHint: 'Broj narudžbe...',
            searchController: _searchController,
            onSearchChanged: (value) => context.read<OrdersProvider>().setHistoryFilters(search: value),
            extraFilters: [
              _StatusDropdown(value: orders.historyStatus),
              _TypeDropdown(value: orders.historyType),
              _DateRangeButton(from: orders.historyDateFrom, to: orders.historyDateTo),
            ],
          ),
          const SizedBox(height: 12),
          Expanded(child: _Table(orders: orders, onSelect: widget.onSelect)),
        ],
      ),
    );
  }
}

class _Table extends StatelessWidget {
  const _Table({required this.orders, required this.onSelect});

  final OrdersProvider orders;
  final ValueChanged<int> onSelect;

  @override
  Widget build(BuildContext context) {
    if (orders.historyError != null) {
      return ErrorView(message: orders.historyError!, onRetry: () => orders.loadHistory());
    }

    final page = orders.historyPage;
    if (page == null || (orders.historyLoading && page.items.isEmpty)) {
      return const Center(child: CircularProgressIndicator());
    }

    if (page.items.isEmpty) {
      return const EmptyView(message: 'Nema narudžbi za odabrane filtere.', icon: Icons.receipt_long_outlined);
    }

    return PaginatedTable<OrderListItem>(
      isLoading: orders.historyLoading,
      columns: const [
        PaginatedTableColumn(label: 'Broj'),
        PaginatedTableColumn(label: 'Status'),
        PaginatedTableColumn(label: 'Tip'),
        PaginatedTableColumn(label: 'Sto'),
        PaginatedTableColumn(label: 'Ukupno', numeric: true),
        PaginatedTableColumn(label: 'Plaćeno'),
        PaginatedTableColumn(label: 'Vrijeme'),
      ],
      items: page.items,
      page: page.page,
      pageSize: page.pageSize,
      totalCount: page.totalCount,
      onPageChanged: (p) => orders.setHistoryPage(p),
      rowBuilder: (item) => [
        DataCell(Text('#${item.number}'), onTap: () => onSelect(item.id)),
        DataCell(_StatusText(status: item.status), onTap: () => onSelect(item.id)),
        DataCell(Text(item.type.label), onTap: () => onSelect(item.id)),
        DataCell(
          Text(item.diningTableNumber != null ? '${item.diningTableNumber}' : '—'),
          onTap: () => onSelect(item.id),
        ),
        DataCell(Text(formatMoney(item.total)), onTap: () => onSelect(item.id)),
        DataCell(
          Icon(
            item.isPaid ? Icons.check_circle : Icons.remove_circle_outline,
            size: 18,
            color: item.isPaid ? AppColors.success : Colors.black38,
          ),
          onTap: () => onSelect(item.id),
        ),
        DataCell(Text(formatDateTime(item.createdAt)), onTap: () => onSelect(item.id)),
      ],
    );
  }
}

class _StatusText extends StatelessWidget {
  const _StatusText({required this.status});

  final OrderStatus status;

  @override
  Widget build(BuildContext context) => Text(status.label);
}

class _StatusDropdown extends StatelessWidget {
  const _StatusDropdown({required this.value});

  final OrderStatus? value;

  @override
  Widget build(BuildContext context) {
    return SizedBox(
      width: 180,
      child: DropdownButtonFormField<OrderStatus?>(
        initialValue: value,
        isDense: true,
        decoration: const InputDecoration(labelText: 'Status'),
        items: [
          const DropdownMenuItem(value: null, child: Text('Svi')),
          for (final status in OrderStatus.values) DropdownMenuItem(value: status, child: Text(status.label)),
        ],
        onChanged: (selected) => context.read<OrdersProvider>().setHistoryFilters(
              status: selected,
              clearStatus: selected == null,
            ),
      ),
    );
  }
}

class _TypeDropdown extends StatelessWidget {
  const _TypeDropdown({required this.value});

  final OrderType? value;

  @override
  Widget build(BuildContext context) {
    return SizedBox(
      width: 160,
      child: DropdownButtonFormField<OrderType?>(
        initialValue: value,
        isDense: true,
        decoration: const InputDecoration(labelText: 'Tip'),
        items: [
          const DropdownMenuItem(value: null, child: Text('Svi')),
          for (final type in OrderType.values) DropdownMenuItem(value: type, child: Text(type.label)),
        ],
        onChanged: (selected) => context.read<OrdersProvider>().setHistoryFilters(
              type: selected,
              clearType: selected == null,
            ),
      ),
    );
  }
}

class _DateRangeButton extends StatelessWidget {
  const _DateRangeButton({required this.from, required this.to});

  final DateTime? from;
  final DateTime? to;

  @override
  Widget build(BuildContext context) {
    final label = from == null || to == null
        ? 'Raspon datuma'
        : '${formatDate(from!)} – ${formatDate(to!)}';

    return OutlinedButton.icon(
      icon: const Icon(Icons.date_range_outlined, size: 18),
      label: Text(label),
      onPressed: () async {
        final now = DateTime.now();
        final range = await showDateRangePicker(
          context: context,
          firstDate: DateTime(now.year - 2),
          lastDate: DateTime(now.year + 1),
          initialDateRange: from != null && to != null ? DateTimeRange(start: from!, end: to!) : null,
        );
        if (range == null || !context.mounted) return;
        context.read<OrdersProvider>().setHistoryFilters(
              dateFrom: range.start,
              dateTo: DateTime(range.end.year, range.end.month, range.end.day, 23, 59, 59),
            );
      },
    );
  }
}
