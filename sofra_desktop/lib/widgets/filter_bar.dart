import 'package:flutter/material.dart';

/// Traka filtera iznad liste - polje za pretragu (opciono) plus proizvoljni dodatni filteri.
/// Svaki pregled sa listom mora imati bar jedan parametar pretrage.
class FilterBar extends StatelessWidget {
  const FilterBar({
    super.key,
    this.searchHint,
    this.onSearchChanged,
    this.searchController,
    this.extraFilters = const [],
    this.trailing,
  });

  final String? searchHint;
  final ValueChanged<String>? onSearchChanged;
  final TextEditingController? searchController;
  final List<Widget> extraFilters;
  final Widget? trailing;

  @override
  Widget build(BuildContext context) {
    return Wrap(
      spacing: 12,
      runSpacing: 12,
      crossAxisAlignment: WrapCrossAlignment.center,
      children: [
        if (onSearchChanged != null)
          SizedBox(
            width: 280,
            child: TextField(
              controller: searchController,
              onChanged: onSearchChanged,
              decoration: InputDecoration(
                hintText: searchHint ?? 'Pretraga...',
                prefixIcon: const Icon(Icons.search),
                isDense: true,
              ),
            ),
          ),
        ...extraFilters,
        ?trailing,
      ],
    );
  }
}
