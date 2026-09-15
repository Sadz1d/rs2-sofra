import 'package:flutter/material.dart';

import '../theme/app_theme.dart';

/// Validacijske greske ispod kontrole forme - nikad u dijalogu ili odvojenoj poruci.
class FormErrorText extends StatelessWidget {
  const FormErrorText({super.key, this.errors});

  final List<String>? errors;

  @override
  Widget build(BuildContext context) {
    if (errors == null || errors!.isEmpty) {
      return const SizedBox.shrink();
    }
    return Padding(
      padding: const EdgeInsets.only(top: 4, left: 4),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          for (final error in errors!)
            Text(error, style: const TextStyle(color: AppColors.danger, fontSize: 12)),
        ],
      ),
    );
  }
}
