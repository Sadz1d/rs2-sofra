import 'package:flutter/material.dart';

import '../theme/app_theme.dart';

/// Dijalog koji uz potvrdu trazi i obavezan tekstualni razlog (npr. otkazivanje narudzbe,
/// odbijanje rezervacije). Vraca uneseni razlog, ili null ako je korisnik odustao.
Future<String?> showReasonDialog(
  BuildContext context, {
  required String title,
  required String message,
  String label = 'Razlog',
  String confirmLabel = 'Potvrdi',
  String cancelLabel = 'Odustani',
}) async {
  final controller = TextEditingController();
  final formKey = GlobalKey<FormState>();

  final result = await showDialog<String>(
    context: context,
    builder: (context) => AlertDialog(
      title: Text(title),
      content: Form(
        key: formKey,
        child: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(message),
            const SizedBox(height: 16),
            TextFormField(
              controller: controller,
              autofocus: true,
              maxLines: 2,
              decoration: InputDecoration(labelText: label),
              validator: (value) =>
                  (value == null || value.trim().isEmpty) ? 'Razlog je obavezan.' : null,
            ),
          ],
        ),
      ),
      actions: [
        TextButton(
          onPressed: () => Navigator.of(context).pop(),
          child: Text(cancelLabel),
        ),
        FilledButton(
          style: FilledButton.styleFrom(backgroundColor: AppColors.danger),
          onPressed: () {
            if (formKey.currentState!.validate()) {
              Navigator.of(context).pop(controller.text.trim());
            }
          },
          child: Text(confirmLabel),
        ),
      ],
    ),
  );

  controller.dispose();
  return result;
}
