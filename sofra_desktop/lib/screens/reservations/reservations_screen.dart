import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../../models/reservation.dart';
import '../../models/reservation_status.dart';
import '../../providers/reservations_provider.dart';
import '../../providers/tables_provider.dart';
import '../../theme/app_theme.dart';
import '../../utils/formatting.dart';
import '../../widgets/empty_view.dart';
import '../../widgets/error_view.dart';
import '../../widgets/filter_bar.dart';
import '../../widgets/paginated_table.dart';
import 'widgets/reservation_detail_panel.dart';

class ReservationsScreen extends StatefulWidget {
  const ReservationsScreen({super.key});

  @override
  State<ReservationsScreen> createState() => _ReservationsScreenState();
}

class _ReservationsScreenState extends State<ReservationsScreen> {
  final _searchController = TextEditingController();

  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addPostFrameCallback((_) {
      context.read<ReservationsProvider>().loadList();
      context.read<TablesProvider>().loadAll();
    });
  }

  @override
  void dispose() {
    _searchController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final reservations = context.watch<ReservationsProvider>();

    return Row(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        Expanded(
          child: Padding(
            padding: const EdgeInsets.all(16),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                FilterBar(
                  searchHint: 'Pretraga gosta...',
                  searchController: _searchController,
                  onSearchChanged: (value) => context.read<ReservationsProvider>().setFilters(search: value),
                  extraFilters: [
                    _StatusDropdown(value: reservations.status),
                    _ZoneDropdown(value: reservations.zoneId),
                    _DateRangeButton(from: reservations.dateFrom, to: reservations.dateTo),
                  ],
                ),
                const SizedBox(height: 12),
                Expanded(child: _Table(reservations: reservations)),
              ],
            ),
          ),
        ),
        if (reservations.selectedReservationId != null) const ReservationDetailPanel(),
      ],
    );
  }
}

class _Table extends StatelessWidget {
  const _Table({required this.reservations});

  final ReservationsProvider reservations;

  @override
  Widget build(BuildContext context) {
    if (reservations.listError != null) {
      return ErrorView(message: reservations.listError!, onRetry: () => reservations.loadList());
    }

    final page = reservations.page;
    if (page == null || (reservations.listLoading && page.items.isEmpty)) {
      return const Center(child: CircularProgressIndicator());
    }

    if (page.items.isEmpty) {
      return const EmptyView(message: 'Nema rezervacija za odabrane filtere.', icon: Icons.event_busy_outlined);
    }

    return PaginatedTable<ReservationListItem>(
      isLoading: reservations.listLoading,
      columns: const [
        PaginatedTableColumn(label: 'Gost'),
        PaginatedTableColumn(label: 'Termin'),
        PaginatedTableColumn(label: 'Osobe', numeric: true),
        PaginatedTableColumn(label: 'Zona'),
        PaginatedTableColumn(label: 'Sto'),
        PaginatedTableColumn(label: 'Status'),
      ],
      items: page.items,
      page: page.page,
      pageSize: page.pageSize,
      totalCount: page.totalCount,
      onPageChanged: (p) => reservations.setPage(p),
      rowBuilder: (item) {
        void onTap() => context.read<ReservationsProvider>().selectReservation(item.id);
        return [
          DataCell(Text(item.userName), onTap: onTap),
          DataCell(Text(formatDateTime(item.reservationAt)), onTap: onTap),
          DataCell(Text('${item.guests}'), onTap: onTap),
          DataCell(Text(item.zoneName), onTap: onTap),
          DataCell(Text(item.diningTableNumber != null ? '${item.diningTableNumber}' : '—'), onTap: onTap),
          DataCell(_StatusPill(status: item.status), onTap: onTap),
        ];
      },
    );
  }
}

class _StatusPill extends StatelessWidget {
  const _StatusPill({required this.status});

  final ReservationStatus status;

  @override
  Widget build(BuildContext context) {
    final color = switch (status) {
      ReservationStatus.pending => Colors.amber.shade800,
      ReservationStatus.confirmed => AppColors.success,
      ReservationStatus.rejected || ReservationStatus.cancelled || ReservationStatus.noShow => AppColors.danger,
      ReservationStatus.completed => AppColors.dark,
    };
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
      decoration: BoxDecoration(color: color.withValues(alpha: 0.12), borderRadius: BorderRadius.circular(20)),
      child: Text(status.label, style: TextStyle(color: color, fontSize: 12, fontWeight: FontWeight.w600)),
    );
  }
}

class _StatusDropdown extends StatelessWidget {
  const _StatusDropdown({required this.value});

  final ReservationStatus? value;

  @override
  Widget build(BuildContext context) {
    return SizedBox(
      width: 180,
      child: DropdownButtonFormField<ReservationStatus?>(
        initialValue: value,
        isDense: true,
        decoration: const InputDecoration(labelText: 'Status'),
        items: [
          const DropdownMenuItem(value: null, child: Text('Svi')),
          for (final status in ReservationStatus.values) DropdownMenuItem(value: status, child: Text(status.label)),
        ],
        onChanged: (selected) => context.read<ReservationsProvider>().setFilters(
              status: selected,
              clearStatus: selected == null,
            ),
      ),
    );
  }
}

class _ZoneDropdown extends StatelessWidget {
  const _ZoneDropdown({required this.value});

  final int? value;

  @override
  Widget build(BuildContext context) {
    final zones = context.watch<TablesProvider>().zones;
    return SizedBox(
      width: 180,
      child: DropdownButtonFormField<int?>(
        initialValue: value,
        isDense: true,
        decoration: const InputDecoration(labelText: 'Zona'),
        items: [
          const DropdownMenuItem(value: null, child: Text('Sve')),
          for (final zone in zones) DropdownMenuItem(value: zone.id, child: Text(zone.name)),
        ],
        onChanged: (selected) => context.read<ReservationsProvider>().setFilters(
              zoneId: selected,
              clearZone: selected == null,
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
    final label = from == null || to == null ? 'Raspon datuma' : '${formatDate(from!)} – ${formatDate(to!)}';

    return OutlinedButton.icon(
      icon: const Icon(Icons.date_range_outlined, size: 18),
      label: Text(label),
      onPressed: () async {
        final now = DateTime.now();
        final range = await showDateRangePicker(
          context: context,
          firstDate: DateTime(now.year - 1),
          lastDate: DateTime(now.year + 2),
          initialDateRange: from != null && to != null ? DateTimeRange(start: from!, end: to!) : null,
        );
        if (range == null || !context.mounted) return;
        context.read<ReservationsProvider>().setFilters(
              dateFrom: range.start,
              dateTo: DateTime(range.end.year, range.end.month, range.end.day, 23, 59, 59),
            );
      },
    );
  }
}
