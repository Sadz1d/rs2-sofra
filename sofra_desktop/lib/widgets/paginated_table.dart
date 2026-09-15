import 'package:flutter/material.dart';

class PaginatedTableColumn {
  const PaginatedTableColumn({required this.label, this.numeric = false});

  final String label;
  final bool numeric;
}

/// Generička tabela sa sortiranjem i paginacijom - ekran daje kolone i gradi ćelije
/// za svaku stavku, ova komponenta upravlja samo prikazom stranica i strelicama.
class PaginatedTable<T> extends StatelessWidget {
  const PaginatedTable({
    super.key,
    required this.columns,
    required this.items,
    required this.rowBuilder,
    required this.page,
    required this.pageSize,
    required this.totalCount,
    required this.onPageChanged,
    this.isLoading = false,
    this.sortColumnIndex,
    this.sortAscending = true,
    this.onSort,
  });

  final List<PaginatedTableColumn> columns;
  final List<T> items;
  final List<DataCell> Function(T item) rowBuilder;
  final int page;
  final int pageSize;
  final int totalCount;
  final ValueChanged<int> onPageChanged;
  final bool isLoading;
  final int? sortColumnIndex;
  final bool sortAscending;
  final void Function(int columnIndex, bool ascending)? onSort;

  int get totalPages => pageSize == 0 ? 0 : (totalCount / pageSize).ceil();

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        SizedBox(height: 2, child: isLoading ? const LinearProgressIndicator(minHeight: 2) : null),
        Expanded(
          child: SingleChildScrollView(
            child: SingleChildScrollView(
              scrollDirection: Axis.horizontal,
              child: DataTable(
                sortColumnIndex: sortColumnIndex,
                sortAscending: sortAscending,
                columns: [
                  for (final column in columns)
                    DataColumn(
                      label: Text(column.label, style: const TextStyle(fontWeight: FontWeight.w600)),
                      numeric: column.numeric,
                      onSort: onSort,
                    ),
                ],
                rows: [for (final item in items) DataRow(cells: rowBuilder(item))],
              ),
            ),
          ),
        ),
        const Divider(height: 1),
        Padding(
          padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 8),
          child: Row(
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            children: [
              Text('Ukupno: $totalCount', style: Theme.of(context).textTheme.bodySmall),
              Row(
                children: [
                  IconButton(
                    icon: const Icon(Icons.chevron_left),
                    onPressed: page > 1 ? () => onPageChanged(page - 1) : null,
                  ),
                  Text('Stranica $page / ${totalPages == 0 ? 1 : totalPages}'),
                  IconButton(
                    icon: const Icon(Icons.chevron_right),
                    onPressed: page < totalPages ? () => onPageChanged(page + 1) : null,
                  ),
                ],
              ),
            ],
          ),
        ),
      ],
    );
  }
}
