import 'package:fl_chart/fl_chart.dart';
import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../../constants/roles.dart';
import '../../models/reservation_status.dart';
import '../../providers/auth_provider.dart';
import '../../providers/dashboard_provider.dart';
import '../../providers/orders_provider.dart';
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
  bool get _isAdmin =>
      context.read<AuthProvider>().currentUser?.roles.contains(Roles.admin) ?? false;

  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addPostFrameCallback((_) {
      context.read<DashboardProvider>().loadAll(isAdmin: _isAdmin);
      context.read<OrdersProvider>().loadBoard();
    });
  }

  @override
  Widget build(BuildContext context) {
    final dashboard = context.watch<DashboardProvider>();

    if (dashboard.loading && dashboard.error == null && dashboard.reservationsTodayPreview.isEmpty) {
      return const LoadingView(message: 'Učitavanje pregleda...');
    }

    if (dashboard.error != null) {
      return ErrorView(
        message: dashboard.error!,
        onRetry: () => context.read<DashboardProvider>().loadAll(isAdmin: _isAdmin),
      );
    }

    final showRevenue = dashboard.canSeeRevenue;

    return LayoutBuilder(
      builder: (context, constraints) {
        final isWide = constraints.maxWidth >= 1100;
        return SingleChildScrollView(
          padding: const EdgeInsets.all(24),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              _KpiRow(dashboard: dashboard, showRevenue: showRevenue),
              const SizedBox(height: 20),
              if (isWide)
                IntrinsicHeight(
                  child: Row(
                    crossAxisAlignment: CrossAxisAlignment.stretch,
                    children: [
                      if (showRevenue) ...[
                        Expanded(flex: 3, child: _RevenueChartCard(dashboard: dashboard)),
                        const SizedBox(width: 20),
                      ],
                      Expanded(
                        flex: 2,
                        child: Column(
                          children: [
                            const _ActiveOrdersCard(),
                            const SizedBox(height: 20),
                            _ReservationsTodayCard(dashboard: dashboard),
                          ],
                        ),
                      ),
                      const SizedBox(width: 20),
                      Expanded(
                        flex: 2,
                        child: Column(
                          children: [
                            _LowStockCard(dashboard: dashboard),
                            if (showRevenue) ...[
                              const SizedBox(height: 20),
                              _TopItemsTodayCard(dashboard: dashboard),
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
                    if (showRevenue) ...[
                      _RevenueChartCard(dashboard: dashboard),
                      const SizedBox(height: 20),
                    ],
                    const _ActiveOrdersCard(),
                    const SizedBox(height: 20),
                    _ReservationsTodayCard(dashboard: dashboard),
                    const SizedBox(height: 20),
                    _LowStockCard(dashboard: dashboard),
                    if (showRevenue) ...[
                      const SizedBox(height: 20),
                      _TopItemsTodayCard(dashboard: dashboard),
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

class _KpiRow extends StatelessWidget {
  const _KpiRow({required this.dashboard, required this.showRevenue});

  final DashboardProvider dashboard;
  final bool showRevenue;

  @override
  Widget build(BuildContext context) {
    final deltaSign = dashboard.revenueDeltaPercent >= 0 ? '+' : '';
    final ordersDeltaSign = dashboard.ordersDelta >= 0 ? '+' : '';

    return LayoutBuilder(
      builder: (context, constraints) {
        final columns = constraints.maxWidth >= 1100
            ? 5
            : constraints.maxWidth >= 700
                ? 3
                : 1;
        final cardWidth = (constraints.maxWidth - (columns - 1) * 16) / columns;

        final cards = [
          if (showRevenue) ...[
            StatCard(
              title: 'Promet danas',
              value: formatMoney(dashboard.revenueToday),
              subtitle: '$deltaSign${dashboard.revenueDeltaPercent.toStringAsFixed(0)} % vs jučer',
              icon: Icons.trending_up,
              iconColor: AppColors.primary,
            ),
            StatCard(
              title: 'Narudžbe danas',
              value: formatInt(dashboard.ordersToday),
              subtitle: '$ordersDeltaSign${dashboard.ordersDelta}',
              icon: Icons.receipt_long_outlined,
              iconColor: Colors.blue,
            ),
          ],
          StatCard(
            title: 'Rezervacije danas',
            value: formatInt(dashboard.reservationsTodayCount),
            subtitle: '${dashboard.reservationsPendingCount} na čekanju',
            icon: Icons.event_available_outlined,
            iconColor: AppColors.success,
          ),
          StatCard(
            title: 'Zauzetost stolova',
            value: '${dashboard.tableOccupancyPercent.toStringAsFixed(0)} %',
            subtitle: '${dashboard.tablesOccupied} od ${dashboard.tablesTotal} stolova',
            icon: Icons.grid_view_outlined,
            iconColor: Colors.amber.shade800,
          ),
          StatCard(
            title: 'Zalihe ispod minimuma',
            value: formatInt(dashboard.lowStockCount),
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
  const _RevenueChartCard({required this.dashboard});

  final DashboardProvider dashboard;

  @override
  Widget build(BuildContext context) {
    final data = dashboard.chartData;
    final maxY = data.isEmpty ? 100.0 : (data.map((e) => e.total).reduce((a, b) => a > b ? a : b) * 1.2);

    return Card(
      child: Padding(
        padding: const EdgeInsets.all(20),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              children: [
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(_titleFor(dashboard.chartRange), style: Theme.of(context).textTheme.titleMedium),
                      Text('u KM, uključujući PDV', style: Theme.of(context).textTheme.bodySmall),
                    ],
                  ),
                ),
                SegmentedButton<RevenueChartRange>(
                  segments: const [
                    ButtonSegment(value: RevenueChartRange.days7, label: Text('7 dana')),
                    ButtonSegment(value: RevenueChartRange.days30, label: Text('30 dana')),
                    ButtonSegment(value: RevenueChartRange.year, label: Text('Godina')),
                  ],
                  selected: {dashboard.chartRange},
                  showSelectedIcon: false,
                  onSelectionChanged: (selection) =>
                      context.read<DashboardProvider>().setChartRange(selection.first),
                ),
              ],
            ),
            const SizedBox(height: 20),
            SizedBox(
              height: 260,
              child: dashboard.chartLoading
                  ? const LoadingView()
                  : data.isEmpty
                      ? const EmptyView(message: 'Nema podataka o prometu za odabrani period.')
                      : BarChart(
                          BarChartData(
                            maxY: maxY,
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
                                    if (index < 0 || index >= data.length) return const SizedBox.shrink();
                                    return Padding(
                                      padding: const EdgeInsets.only(top: 6),
                                      child: Text(_axisLabel(data[index].periodStart, dashboard.chartRange),
                                          style: Theme.of(context).textTheme.bodySmall),
                                    );
                                  },
                                ),
                              ),
                            ),
                            barGroups: [
                              for (var i = 0; i < data.length; i++)
                                BarChartGroupData(
                                  x: i,
                                  barRods: [
                                    BarChartRodData(
                                      toY: data[i].total,
                                      width: 22,
                                      borderRadius: BorderRadius.circular(4),
                                      color: i == data.length - 1
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

  String _titleFor(RevenueChartRange range) => switch (range) {
        RevenueChartRange.days7 => 'Promet posljednjih 7 dana',
        RevenueChartRange.days30 => 'Promet posljednjih 30 dana',
        RevenueChartRange.year => 'Promet posljednjih godinu dana',
      };

  String _axisLabel(DateTime date, RevenueChartRange range) {
    if (range == RevenueChartRange.year) {
      const months = ['Jan', 'Feb', 'Mar', 'Apr', 'Maj', 'Jun', 'Jul', 'Avg', 'Sep', 'Okt', 'Nov', 'Dec'];
      return months[date.month - 1];
    }
    const days = ['Pon', 'Uto', 'Sri', 'Čet', 'Pet', 'Sub', 'Ned'];
    return '${days[date.weekday - 1]} ${date.day}.';
  }
}

class _ActiveOrdersCard extends StatelessWidget {
  const _ActiveOrdersCard();

  @override
  Widget build(BuildContext context) {
    final orders = context.watch<OrdersProvider>();
    final all = orders.board.values.expand((e) => e).toList()
      ..sort((a, b) => b.createdAt.compareTo(a.createdAt));
    final preview = all.take(5).toList();

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
                  child: Text('Sve (${orders.activeCount})'),
                ),
              ],
            ),
            if (orders.boardLoading && preview.isEmpty)
              const Padding(padding: EdgeInsets.symmetric(vertical: 24), child: LoadingView())
            else if (preview.isEmpty)
              const Padding(
                padding: EdgeInsets.symmetric(vertical: 16),
                child: EmptyView(message: 'Nema aktivnih narudžbi.', icon: Icons.receipt_long_outlined),
              )
            else
              for (final order in preview)
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
                                  ? 'Sto ${order.diningTableNumber} · ${order.items.length} st.'
                                  : 'Za ponijeti · ${order.items.length} st.',
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

class _ReservationsTodayCard extends StatelessWidget {
  const _ReservationsTodayCard({required this.dashboard});

  final DashboardProvider dashboard;

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
                Text('Rezervacije danas', style: Theme.of(context).textTheme.titleMedium),
                TextButton(
                  onPressed: () => Navigator.of(context).pushReplacementNamed('/reservations'),
                  child: Text('Sve (${dashboard.reservationsTodayCount})'),
                ),
              ],
            ),
            if (dashboard.reservationsTodayPreview.isEmpty)
              const Padding(
                padding: EdgeInsets.symmetric(vertical: 16),
                child: EmptyView(message: 'Nema rezervacija za danas.', icon: Icons.event_busy_outlined),
              )
            else
              for (final reservation in dashboard.reservationsTodayPreview)
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
                              '${formatTime(reservation.reservationAt)} · ${reservation.guests} os. · ${reservation.zoneName}',
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
  const _LowStockCard({required this.dashboard});

  final DashboardProvider dashboard;

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
            if (dashboard.lowStockPreview.isEmpty)
              const Padding(
                padding: EdgeInsets.symmetric(vertical: 16),
                child: EmptyView(message: 'Sve zalihe su iznad minimuma.', icon: Icons.check_circle_outline),
              )
            else
              for (final item in dashboard.lowStockPreview)
                Padding(
                  padding: const EdgeInsets.symmetric(vertical: 6),
                  child: Row(
                    children: [
                      const Icon(Icons.circle, size: 8, color: AppColors.danger),
                      const SizedBox(width: 10),
                      Expanded(child: Text(item.name)),
                      Text(
                        '${item.quantity.toStringAsFixed(item.quantity.truncateToDouble() == item.quantity ? 0 : 1)} '
                        '${item.unitOfMeasureAbbreviation} / min ${item.minQuantity.toStringAsFixed(item.minQuantity.truncateToDouble() == item.minQuantity ? 0 : 1)} ${item.unitOfMeasureAbbreviation}',
                        style: Theme.of(context).textTheme.bodySmall,
                      ),
                    ],
                  ),
                ),
            if (dashboard.lowStockPreview.isNotEmpty)
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
}

class _TopItemsTodayCard extends StatelessWidget {
  const _TopItemsTodayCard({required this.dashboard});

  final DashboardProvider dashboard;

  @override
  Widget build(BuildContext context) {
    final items = dashboard.topItemsToday;
    final maxQty = items.isEmpty ? 1 : items.map((e) => e.quantitySold).reduce((a, b) => a > b ? a : b);

    return Card(
      child: Padding(
        padding: const EdgeInsets.all(20),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text('Top jela danas', style: Theme.of(context).textTheme.titleMedium),
            const SizedBox(height: 12),
            if (items.isEmpty)
              const EmptyView(message: 'Još nema prodatih jela danas.', icon: Icons.restaurant_menu_outlined)
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
