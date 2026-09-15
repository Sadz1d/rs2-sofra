import 'package:fl_chart/fl_chart.dart';
import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../../models/dashboard_data.dart';
import '../../models/reservation_status.dart';
import '../../providers/dashboard_provider.dart';
import '../../theme/app_theme.dart';
import '../../utils/formatting.dart';
import '../../widgets/empty_view.dart';
import '../../widgets/error_view.dart';
import '../../widgets/loading_view.dart';
import '../../widgets/stat_card.dart';

class DashboardScreen extends StatefulWidget {
  const DashboardScreen({super.key});

  @override
  State<DashboardScreen> createState() => _DashboardScreenState();
}

class _DashboardScreenState extends State<DashboardScreen> {
  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addPostFrameCallback((_) {
      context.read<DashboardProvider>().load();
    });
  }

  @override
  Widget build(BuildContext context) {
    final dashboard = context.watch<DashboardProvider>();
    final data = dashboard.data;

    if (dashboard.loading && data == null) {
      return const LoadingView(message: 'Učitavanje pregleda...');
    }

    if (dashboard.error != null && data == null) {
      return ErrorView(
        message: dashboard.error!,
        onRetry: () => context.read<DashboardProvider>().load(),
      );
    }

    if (data == null) {
      return const EmptyView(message: 'Nema podataka za prikaz.');
    }

    return LayoutBuilder(
      builder: (context, constraints) {
        final isWide = constraints.maxWidth >= 1100;
        return SingleChildScrollView(
          padding: const EdgeInsets.all(24),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              _RangeSelector(dashboard: dashboard),
              const SizedBox(height: 16),
              _KpiRow(data: data),
              const SizedBox(height: 20),
              if (isWide)
                IntrinsicHeight(
                  child: Row(
                    crossAxisAlignment: CrossAxisAlignment.stretch,
                    children: [
                      if (data.hasFinancials) ...[
                        Expanded(flex: 3, child: _RevenueChartCard(dashboard: dashboard, data: data)),
                        const SizedBox(width: 20),
                      ],
                      Expanded(
                        flex: 2,
                        child: Column(
                          children: [
                            _ActiveOrdersCard(data: data),
                            const SizedBox(height: 20),
                            _ReservationsCard(data: data),
                          ],
                        ),
                      ),
                      const SizedBox(width: 20),
                      Expanded(
                        flex: 2,
                        child: Column(
                          children: [
                            _LowStockCard(data: data),
                            if (data.hasFinancials) ...[
                              const SizedBox(height: 20),
                              _TopItemsCard(data: data),
                            ],
                          ],
                        ),
                      ),
                    ],
                  ),
                )
              else
                Column(
                  children: [
                    if (data.hasFinancials) ...[
                      _RevenueChartCard(dashboard: dashboard, data: data),
                      const SizedBox(height: 20),
                    ],
                    _ActiveOrdersCard(data: data),
                    const SizedBox(height: 20),
                    _ReservationsCard(data: data),
                    const SizedBox(height: 20),
                    _LowStockCard(data: data),
                    if (data.hasFinancials) ...[
                      const SizedBox(height: 20),
                      _TopItemsCard(data: data),
                    ],
                  ],
                ),
            ],
          ),
        );
      },
    );
  }
}

class _RangeSelector extends StatelessWidget {
  const _RangeSelector({required this.dashboard});

  final DashboardProvider dashboard;

  @override
  Widget build(BuildContext context) {
    return Align(
      alignment: Alignment.centerLeft,
      child: SegmentedButton<DashboardRange>(
        segments: const [
          ButtonSegment(value: DashboardRange.today, label: Text('Danas')),
          ButtonSegment(value: DashboardRange.days7, label: Text('7 dana')),
          ButtonSegment(value: DashboardRange.days30, label: Text('30 dana')),
          ButtonSegment(value: DashboardRange.year, label: Text('Godina')),
        ],
        selected: {dashboard.range},
        showSelectedIcon: false,
        onSelectionChanged: (selection) => context.read<DashboardProvider>().setRange(selection.first),
      ),
    );
  }
}

class _KpiRow extends StatelessWidget {
  const _KpiRow({required this.data});

  final DashboardData data;

  @override
  Widget build(BuildContext context) {
    return LayoutBuilder(
      builder: (context, constraints) {
        final columns = constraints.maxWidth >= 1100
            ? 5
            : constraints.maxWidth >= 700
                ? 3
                : 1;
        final cardWidth = (constraints.maxWidth - (columns - 1) * 16) / columns;

        final cards = [
          if (data.hasFinancials) ...[
            StatCard(
              title: 'Promet',
              value: formatMoney(data.revenue!),
              subtitle: '${formatInt(data.ordersCount)} narudžbi',
              icon: Icons.trending_up,
              iconColor: AppColors.primary,
            ),
            StatCard(
              title: 'Prosječna vrijednost narudžbe',
              value: formatMoney(data.averageOrderValue ?? 0),
              icon: Icons.receipt_long_outlined,
              iconColor: Colors.blue,
            ),
          ] else
            StatCard(
              title: 'Narudžbe',
              value: formatInt(data.ordersCount),
              icon: Icons.receipt_long_outlined,
              iconColor: Colors.blue,
            ),
          StatCard(
            title: 'Rezervacije',
            value: formatInt(data.reservationsCount),
            subtitle: '${data.reservationsPendingCount} na čekanju',
            icon: Icons.event_available_outlined,
            iconColor: AppColors.success,
          ),
          StatCard(
            title: 'Zauzetost stolova',
            value: '${data.tableOccupancyPercent.toStringAsFixed(0)} %',
            subtitle: '${data.tablesOccupied} od ${data.tablesTotal} stolova',
            icon: Icons.grid_view_outlined,
            iconColor: Colors.amber.shade800,
          ),
          StatCard(
            title: 'Zalihe ispod minimuma',
            value: formatInt(data.lowStockCount),
            subtitle: 'artikla',
            icon: Icons.warning_amber_outlined,
            iconColor: AppColors.danger,
          ),
        ];

        return Wrap(
          spacing: 16,
          runSpacing: 16,
          children: [for (final card in cards) SizedBox(width: cardWidth, child: card)],
        );
      },
    );
  }
}

class _RevenueChartCard extends StatelessWidget {
  const _RevenueChartCard({required this.dashboard, required this.data});

  final DashboardProvider dashboard;
  final DashboardData data;

  @override
  Widget build(BuildContext context) {
    final points = data.chart;
    final maxY = points.isEmpty
        ? 100.0
        : (points.map((e) => e.total ?? 0).reduce((a, b) => a > b ? a : b) * 1.2).clamp(1.0, double.infinity);

    return Card(
      child: Padding(
        padding: const EdgeInsets.all(20),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text('Promet', style: Theme.of(context).textTheme.titleMedium),
            Text('u KM, uključujući PDV', style: Theme.of(context).textTheme.bodySmall),
            const SizedBox(height: 20),
            SizedBox(
              height: 260,
              child: points.isEmpty
                  ? const EmptyView(message: 'Nema podataka o prometu za odabrani period.')
                  : BarChart(
                      BarChartData(
                        maxY: maxY.toDouble(),
                        gridData: const FlGridData(show: false),
                        borderData: FlBorderData(show: false),
                        barTouchData: BarTouchData(
                          touchTooltipData: BarTouchTooltipData(
                            getTooltipItem: (group, groupIndex, rod, rodIndex) => BarTooltipItem(
                              formatMoney(rod.toY),
                              const TextStyle(color: Colors.white, fontWeight: FontWeight.w600),
                            ),
                          ),
                        ),
                        titlesData: FlTitlesData(
                          topTitles: const AxisTitles(),
                          rightTitles: const AxisTitles(),
                          leftTitles: const AxisTitles(),
                          bottomTitles: AxisTitles(
                            sideTitles: SideTitles(
                              showTitles: true,
                              reservedSize: 28,
                              getTitlesWidget: (value, meta) {
                                final index = value.toInt();
                                if (index < 0 || index >= points.length) return const SizedBox.shrink();
                                return Padding(
                                  padding: const EdgeInsets.only(top: 6),
                                  child: Text(_axisLabel(points[index].periodStart, dashboard.range),
                                      style: Theme.of(context).textTheme.bodySmall),
                                );
                              },
                            ),
                          ),
                        ),
                        barGroups: [
                          for (var i = 0; i < points.length; i++)
                            BarChartGroupData(
                              x: i,
                              barRods: [
                                BarChartRodData(
                                  toY: points[i].total ?? 0,
                                  width: 22,
                                  borderRadius: BorderRadius.circular(4),
                                  color: i == points.length - 1
                                      ? AppColors.primary
                                      : AppColors.primary.withValues(alpha: 0.28),
                                ),
                              ],
                            ),
                        ],
                      ),
                    ),
            ),
          ],
        ),
      ),
    );
  }

  String _axisLabel(DateTime date, DashboardRange range) {
    if (range == DashboardRange.year) {
      const months = ['Jan', 'Feb', 'Mar', 'Apr', 'Maj', 'Jun', 'Jul', 'Avg', 'Sep', 'Okt', 'Nov', 'Dec'];
      return months[date.month - 1];
    }
    const days = ['Pon', 'Uto', 'Sri', 'Čet', 'Pet', 'Sub', 'Ned'];
    return '${days[date.weekday - 1]} ${date.day}.';
  }
}

class _ActiveOrdersCard extends StatelessWidget {
  const _ActiveOrdersCard({required this.data});

  final DashboardData data;

  @override
  Widget build(BuildContext context) {
    return Card(
      child: Padding(
        padding: const EdgeInsets.all(20),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              children: [
                Text('Aktivne narudžbe', style: Theme.of(context).textTheme.titleMedium),
                TextButton(
                  onPressed: () => Navigator.of(context).pushReplacementNamed('/orders'),
                  child: Text('Sve (${data.activeOrdersCount})'),
                ),
              ],
            ),
            if (data.activeOrders.isEmpty)
              const Padding(
                padding: EdgeInsets.symmetric(vertical: 16),
                child: EmptyView(message: 'Nema aktivnih narudžbi.', icon: Icons.receipt_long_outlined),
              )
            else
              for (final order in data.activeOrders.take(5))
                Padding(
                  padding: const EdgeInsets.symmetric(vertical: 8),
                  child: Row(
                    children: [
                      Expanded(
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            Text('#${order.number}', style: const TextStyle(fontWeight: FontWeight.w600)),
                            Text(
                              order.diningTableNumber != null
                                  ? 'Sto ${order.diningTableNumber} · ${order.itemCount} st.'
                                  : 'Za ponijeti · ${order.itemCount} st.',
                              style: Theme.of(context).textTheme.bodySmall,
                            ),
                          ],
                        ),
                      ),
                      Text(formatMoney(order.total)),
                    ],
                  ),
                ),
          ],
        ),
      ),
    );
  }
}

class _ReservationsCard extends StatelessWidget {
  const _ReservationsCard({required this.data});

  final DashboardData data;

  @override
  Widget build(BuildContext context) {
    return Card(
      child: Padding(
        padding: const EdgeInsets.all(20),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              children: [
                Text('Rezervacije', style: Theme.of(context).textTheme.titleMedium),
                TextButton(
                  onPressed: () => Navigator.of(context).pushReplacementNamed('/reservations'),
                  child: Text('Sve (${data.reservationsCount})'),
                ),
              ],
            ),
            if (data.reservations.isEmpty)
              const Padding(
                padding: EdgeInsets.symmetric(vertical: 16),
                child: EmptyView(message: 'Nema rezervacija za odabrani period.', icon: Icons.event_busy_outlined),
              )
            else
              for (final reservation in data.reservations.take(5))
                Padding(
                  padding: const EdgeInsets.symmetric(vertical: 8),
                  child: Row(
                    children: [
                      CircleAvatar(
                        radius: 16,
                        child: Text(reservation.userName.isNotEmpty ? reservation.userName[0] : '?'),
                      ),
                      const SizedBox(width: 12),
                      Expanded(
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            Text(reservation.userName, style: const TextStyle(fontWeight: FontWeight.w600)),
                            Text(
                              '${formatDateTime(reservation.reservationAt)} · ${reservation.guests} os. · ${reservation.zoneName}',
                              style: Theme.of(context).textTheme.bodySmall,
                            ),
                          ],
                        ),
                      ),
                      _StatusPill(label: reservation.status.label, status: reservation.status),
                    ],
                  ),
                ),
          ],
        ),
      ),
    );
  }
}

class _LowStockCard extends StatelessWidget {
  const _LowStockCard({required this.data});

  final DashboardData data;

  @override
  Widget build(BuildContext context) {
    return Card(
      child: Padding(
        padding: const EdgeInsets.all(20),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              children: [
                const Icon(Icons.warning_amber_outlined, color: AppColors.danger, size: 20),
                const SizedBox(width: 8),
                Text('Upozorenja zaliha', style: Theme.of(context).textTheme.titleMedium),
              ],
            ),
            if (data.lowStockItems.isEmpty)
              const Padding(
                padding: EdgeInsets.symmetric(vertical: 16),
                child: EmptyView(message: 'Sve zalihe su iznad minimuma.', icon: Icons.check_circle_outline),
              )
            else
              for (final item in data.lowStockItems)
                Padding(
                  padding: const EdgeInsets.symmetric(vertical: 6),
                  child: Row(
                    children: [
                      const Icon(Icons.circle, size: 8, color: AppColors.danger),
                      const SizedBox(width: 10),
                      Expanded(child: Text(item.name)),
                      Text(
                        '${_formatQuantity(item.quantity)} ${item.unitOfMeasureAbbreviation} / '
                        'min ${_formatQuantity(item.minQuantity)} ${item.unitOfMeasureAbbreviation}',
                        style: Theme.of(context).textTheme.bodySmall,
                      ),
                    ],
                  ),
                ),
            if (data.lowStockItems.isNotEmpty)
              Align(
                alignment: Alignment.centerLeft,
                child: TextButton(
                  onPressed: () => Navigator.of(context).pushReplacementNamed('/inventory'),
                  child: const Text('Otvori zalihe'),
                ),
              ),
          ],
        ),
      ),
    );
  }

  String _formatQuantity(double value) =>
      value.truncateToDouble() == value ? value.toStringAsFixed(0) : value.toStringAsFixed(1);
}

class _TopItemsCard extends StatelessWidget {
  const _TopItemsCard({required this.data});

  final DashboardData data;

  @override
  Widget build(BuildContext context) {
    final items = data.topItems;
    final maxQty = items.isEmpty ? 1 : items.map((e) => e.quantitySold).reduce((a, b) => a > b ? a : b);

    return Card(
      child: Padding(
        padding: const EdgeInsets.all(20),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text('Top jela', style: Theme.of(context).textTheme.titleMedium),
            const SizedBox(height: 12),
            if (items.isEmpty)
              const EmptyView(message: 'Još nema prodatih jela u ovom periodu.', icon: Icons.restaurant_menu_outlined)
            else
              for (final item in items)
                Padding(
                  padding: const EdgeInsets.symmetric(vertical: 6),
                  child: Row(
                    children: [
                      Expanded(
                        flex: 2,
                        child: Text(item.menuItemName, overflow: TextOverflow.ellipsis),
                      ),
                      Expanded(
                        flex: 3,
                        child: ClipRRect(
                          borderRadius: BorderRadius.circular(4),
                          child: LinearProgressIndicator(
                            value: item.quantitySold / maxQty,
                            minHeight: 8,
                            backgroundColor: AppColors.primary.withValues(alpha: 0.12),
                            valueColor: const AlwaysStoppedAnimation(AppColors.primary),
                          ),
                        ),
                      ),
                      const SizedBox(width: 8),
                      SizedBox(
                        width: 28,
                        child: Text('${item.quantitySold}', textAlign: TextAlign.end),
                      ),
                    ],
                  ),
                ),
          ],
        ),
      ),
    );
  }
}

class _StatusPill extends StatelessWidget {
  const _StatusPill({required this.label, required this.status});

  final String label;
  final ReservationStatus status;

  @override
  Widget build(BuildContext context) {
    final color = switch (status) {
      ReservationStatus.confirmed => AppColors.success,
      ReservationStatus.pending => Colors.amber.shade800,
      ReservationStatus.rejected || ReservationStatus.cancelled || ReservationStatus.noShow => AppColors.danger,
      ReservationStatus.completed => AppColors.dark,
    };
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
      decoration: BoxDecoration(color: color.withValues(alpha: 0.12), borderRadius: BorderRadius.circular(20)),
      child: Text(label, style: TextStyle(color: color, fontSize: 12, fontWeight: FontWeight.w600)),
    );
  }
}
