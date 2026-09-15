import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../../constants/roles.dart';
import '../../models/dining_table.dart';
import '../../models/table_status.dart';
import '../../models/zone.dart';
import '../../providers/auth_provider.dart';
import '../../providers/tables_provider.dart';
import '../../theme/app_theme.dart';
import '../../widgets/empty_view.dart';
import '../../widgets/error_view.dart';
import '../../widgets/loading_view.dart';
import 'widgets/table_detail_panel.dart';
import 'widgets/table_form_dialog.dart';

class TablesScreen extends StatefulWidget {
  const TablesScreen({super.key});

  @override
  State<TablesScreen> createState() => _TablesScreenState();
}

class _TablesScreenState extends State<TablesScreen> {
  int? _zoneFilter;
  String? _waiterFilter;

  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addPostFrameCallback((_) {
      context.read<TablesProvider>().loadAll();
      context.read<TablesProvider>().loadUpcomingReservations();
    });
  }

  @override
  Widget build(BuildContext context) {
    final tablesProvider = context.watch<TablesProvider>();
    final isAdmin = context.watch<AuthProvider>().currentUser?.roles.contains(Roles.admin) ?? false;

    if (tablesProvider.loading && tablesProvider.tables.isEmpty) {
      return const LoadingView(message: 'Učitavanje stolova...');
    }
    if (tablesProvider.error != null) {
      return ErrorView(message: tablesProvider.error!, onRetry: () => tablesProvider.loadAll());
    }

    final filtered = tablesProvider.tables.where((t) {
      if (_zoneFilter != null && t.zoneId != _zoneFilter) return false;
      if (_waiterFilter != null && t.waiterName != _waiterFilter) return false;
      return true;
    }).toList();

    final waiterNames = tablesProvider.tables
        .map((t) => t.waiterName)
        .whereType<String>()
        .toSet()
        .toList()
      ..sort();

    return Row(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        Expanded(
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              Padding(
                padding: const EdgeInsets.fromLTRB(16, 16, 16, 8),
                child: _ZoneTabs(
                  zoneFilter: _zoneFilter,
                  onSelect: (zoneId) => setState(() => _zoneFilter = zoneId),
                ),
              ),
              Padding(
                padding: const EdgeInsets.symmetric(horizontal: 16),
                child: Wrap(
                  spacing: 16,
                  runSpacing: 8,
                  crossAxisAlignment: WrapCrossAlignment.center,
                  children: [
                    const _Legend(),
                    SizedBox(
                      width: 200,
                      child: DropdownButtonFormField<String?>(
                        initialValue: _waiterFilter,
                        isDense: true,
                        decoration: const InputDecoration(labelText: 'Konobar'),
                        items: [
                          const DropdownMenuItem(value: null, child: Text('Svi')),
                          for (final name in waiterNames) DropdownMenuItem(value: name, child: Text(name)),
                        ],
                        onChanged: (value) => setState(() => _waiterFilter = value),
                      ),
                    ),
                    if (isAdmin)
                      FilledButton.icon(
                        icon: const Icon(Icons.add),
                        label: const Text('Novi sto'),
                        onPressed: () => showDialog<void>(context: context, builder: (_) => const TableFormDialog()),
                      ),
                  ],
                ),
              ),
              const SizedBox(height: 8),
              Expanded(
                child: filtered.isEmpty
                    ? const EmptyView(message: 'Nema stolova za odabrane filtere.', icon: Icons.table_bar_outlined)
                    : SingleChildScrollView(
                        padding: const EdgeInsets.all(16),
                        child: _zoneFilter == null
                            ? _GroupedByZone(tables: filtered, zones: tablesProvider.zones)
                            : _TableGrid(tables: filtered),
                      ),
              ),
            ],
          ),
        ),
        if (tablesProvider.selectedTableId != null) const TableDetailPanel(),
      ],
    );
  }
}

class _ZoneTabs extends StatelessWidget {
  const _ZoneTabs({required this.zoneFilter, required this.onSelect});

  final int? zoneFilter;
  final ValueChanged<int?> onSelect;

  @override
  Widget build(BuildContext context) {
    final tablesProvider = context.watch<TablesProvider>();
    return Wrap(
      spacing: 8,
      children: [
        _ZoneTabButton(
          label: 'Sve',
          count: tablesProvider.tables.length,
          selected: zoneFilter == null,
          onTap: () => onSelect(null),
        ),
        for (final zone in tablesProvider.zones)
          _ZoneTabButton(
            label: zone.name,
            count: tablesProvider.tables.where((t) => t.zoneId == zone.id).length,
            selected: zoneFilter == zone.id,
            onTap: () => onSelect(zone.id),
          ),
      ],
    );
  }
}

class _ZoneTabButton extends StatelessWidget {
  const _ZoneTabButton({required this.label, required this.count, required this.selected, required this.onTap});

  final String label;
  final int count;
  final bool selected;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    return Material(
      color: selected ? AppColors.primary : Colors.white,
      borderRadius: BorderRadius.circular(20),
      child: InkWell(
        borderRadius: BorderRadius.circular(20),
        onTap: onTap,
        child: Padding(
          padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 10),
          child: Text(
            '$label ($count)',
            style: TextStyle(
              color: selected ? Colors.white : AppColors.dark,
              fontWeight: FontWeight.w600,
            ),
          ),
        ),
      ),
    );
  }
}

class _Legend extends StatelessWidget {
  const _Legend();

  @override
  Widget build(BuildContext context) {
    final tables = context.watch<TablesProvider>().tables;
    final free = tables.where((t) => t.status == TableStatus.free).length;
    final occupied = tables.where((t) => t.status == TableStatus.occupied).length;
    final reserved = tables.where((t) => t.status == TableStatus.reserved).length;

    return Row(
      mainAxisSize: MainAxisSize.min,
      children: [
        _LegendDot(color: AppColors.success, label: 'Slobodan $free'),
        const SizedBox(width: 12),
        _LegendDot(color: AppColors.danger, label: 'Zauzet $occupied'),
        const SizedBox(width: 12),
        _LegendDot(color: Colors.amber.shade800, label: 'Rezervisan $reserved'),
      ],
    );
  }
}

class _LegendDot extends StatelessWidget {
  const _LegendDot({required this.color, required this.label});

  final Color color;
  final String label;

  @override
  Widget build(BuildContext context) {
    return Row(
      mainAxisSize: MainAxisSize.min,
      children: [
        Icon(Icons.circle, size: 10, color: color),
        const SizedBox(width: 6),
        Text(label, style: Theme.of(context).textTheme.bodySmall),
      ],
    );
  }
}

class _GroupedByZone extends StatelessWidget {
  const _GroupedByZone({required this.tables, required this.zones});

  final List<DiningTable> tables;
  final List<Zone> zones;

  @override
  Widget build(BuildContext context) {
    final zoneOrder = zones.map((z) => z.id).toList();
    final grouped = <int, List<DiningTable>>{};
    for (final table in tables) {
      grouped.putIfAbsent(table.zoneId, () => []).add(table);
    }

    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        for (final zoneId in zoneOrder)
          if (grouped[zoneId] != null) ...[
            Padding(
              padding: const EdgeInsets.symmetric(vertical: 8),
              child: Text(
                grouped[zoneId]!.first.zoneName.toUpperCase(),
                style: Theme.of(context).textTheme.titleSmall?.copyWith(color: Colors.black54),
              ),
            ),
            _TableGrid(tables: grouped[zoneId]!),
          ],
      ],
    );
  }
}

class _TableGrid extends StatelessWidget {
  const _TableGrid({required this.tables});

  final List<DiningTable> tables;

  @override
  Widget build(BuildContext context) {
    return Wrap(
      spacing: 16,
      runSpacing: 16,
      children: [for (final table in tables) SizedBox(width: 220, child: _TableCard(table: table))],
    );
  }
}

class _TableCard extends StatelessWidget {
  const _TableCard({required this.table});

  final DiningTable table;

  @override
  Widget build(BuildContext context) {
    final selected = context.watch<TablesProvider>().selectedTableId == table.id;
    final color = switch (table.status) {
      TableStatus.free => AppColors.success,
      TableStatus.occupied => AppColors.danger,
      TableStatus.reserved => Colors.amber.shade800,
    };

    return Card(
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(12),
        side: BorderSide(color: selected ? AppColors.primary : color.withValues(alpha: 0.4), width: selected ? 2 : 1),
      ),
      color: color.withValues(alpha: 0.06),
      child: InkWell(
        borderRadius: BorderRadius.circular(12),
        onTap: () => context.read<TablesProvider>().selectTable(table.id),
        child: Padding(
          padding: const EdgeInsets.all(14),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Row(
                mainAxisAlignment: MainAxisAlignment.spaceBetween,
                children: [
                  Text('Sto ${table.number}', style: const TextStyle(fontWeight: FontWeight.w700)),
                  Container(
                    padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 2),
                    decoration: BoxDecoration(color: color.withValues(alpha: 0.15), borderRadius: BorderRadius.circular(12)),
                    child: Text(table.status.label, style: TextStyle(color: color, fontSize: 11, fontWeight: FontWeight.w600)),
                  ),
                ],
              ),
              const SizedBox(height: 6),
              Row(
                children: [
                  const Icon(Icons.people_outline, size: 14, color: Colors.black54),
                  const SizedBox(width: 4),
                  Text('${table.capacity} osoba', style: Theme.of(context).textTheme.bodySmall),
                ],
              ),
              const SizedBox(height: 6),
              Row(
                children: [
                  CircleAvatar(
                    radius: 10,
                    backgroundColor: AppColors.primary.withValues(alpha: 0.15),
                    child: Text(
                      table.waiterName != null && table.waiterName!.isNotEmpty ? table.waiterName![0] : '?',
                      style: const TextStyle(fontSize: 10, color: AppColors.primary),
                    ),
                  ),
                  const SizedBox(width: 6),
                  Expanded(
                    child: Text(
                      table.waiterName ?? 'bez konobara',
                      style: Theme.of(context).textTheme.bodySmall,
                      overflow: TextOverflow.ellipsis,
                    ),
                  ),
                ],
              ),
            ],
          ),
        ),
      ),
    );
  }
}
