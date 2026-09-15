import 'package:flutter/material.dart';

import '../../theme/app_theme.dart';

/// Placeholder za module koji jos nisu izgradjeni - ruta postoji i navigacija radi,
/// sadrzaj dolazi u sljedecim koracima Faze 3.
class ComingSoonScreen extends StatelessWidget {
  const ComingSoonScreen({super.key, required this.title});

  final String title;

  @override
  Widget build(BuildContext context) {
    return Center(
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          const Icon(Icons.construction_outlined, size: 48, color: AppColors.primary),
          const SizedBox(height: 12),
          Text('$title — uskoro dostupno', style: Theme.of(context).textTheme.titleMedium),
        ],
      ),
    );
  }
}
