import 'package:flutter/material.dart';

import '../../theme/app_theme.dart';

class AccessDeniedScreen extends StatelessWidget {
  const AccessDeniedScreen({super.key});

  @override
  Widget build(BuildContext context) {
    return Center(
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          const Icon(Icons.lock_outline, size: 48, color: AppColors.danger),
          const SizedBox(height: 12),
          Text('Nemate pristup ovom ekranu.', style: Theme.of(context).textTheme.titleMedium),
        ],
      ),
    );
  }
}
