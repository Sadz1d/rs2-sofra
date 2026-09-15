import 'package:flutter/material.dart';

import '../../../theme/app_theme.dart';
import '../../../utils/formatting.dart';

class RejectReservationResult {
  const RejectReservationResult({required this.reason, this.alternativeAt});

  final String reason;
  final DateTime? alternativeAt;
}

/// Odbijanje rezervacije - obavezan razlog (min. 10 znakova, salje se gostu) i opcioni
/// predlog alternativnog termina.
Future<RejectReservationResult?> showRejectReservationDialog(
  BuildContext context, {
  required String guestName,
  required DateTime reservationAt,
  required int guests,
  required String zoneName,
}) async {
  final formKey = GlobalKey<FormState>();
  final reasonController = TextEditingController();
  DateTime? altDate;
  TimeOfDay? altTime;

  final result = await showDialog<RejectReservationResult>(
    context: context,
    builder: (dialogContext) => StatefulBuilder(
      builder: (context, setState) {
        return AlertDialog(
          title: const Text('Odbij rezervaciju'),
          content: SizedBox(
            width: 420,
            child: Form(
              key: formKey,
              child: SingleChildScrollView(
                child: Column(
                  mainAxisSize: MainAxisSize.min,
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text('$guestName · ${formatDateTime(reservationAt)} · $guests osoba · $zoneName'),
                    const SizedBox(height: 16),
                    TextFormField(
                      controller: reasonController,
                      maxLines: 3,
                      autofocus: true,
                      decoration: const InputDecoration(labelText: 'Razlog odbijanja *'),
                      validator: (value) {
                        if (value == null || value.trim().isEmpty) return 'Razlog je obavezan.';
                        if (value.trim().length < 10) return 'Razlog mora imati najmanje 10 znakova.';
                        return null;
                      },
                    ),
                    Padding(
                      padding: const EdgeInsets.only(top: 4),
                      child: Text(
                        'Razlog je obavezan i biće poslan gostu (najmanje 10 znakova).',
                        style: Theme.of(context).textTheme.bodySmall,
                      ),
                    ),
                    const SizedBox(height: 16),
                    Text('Predloži alternativni termin', style: Theme.of(context).textTheme.titleSmall),
                    const SizedBox(height: 8),
                    Row(
                      children: [
                        Expanded(
                          child: OutlinedButton.icon(
                            icon: const Icon(Icons.calendar_today_outlined, size: 16),
                            label: Text(altDate != null ? formatDate(altDate!) : 'Datum'),
                            onPressed: () async {
                              final picked = await showDatePicker(
                                context: context,
                                initialDate: reservationAt,
                                firstDate: DateTime.now(),
                                lastDate: DateTime.now().add(const Duration(days: 365)),
                              );
                              if (picked != null) setState(() => altDate = picked);
                            },
                          ),
                        ),
                        const SizedBox(width: 8),
                        Expanded(
                          child: OutlinedButton.icon(
                            icon: const Icon(Icons.access_time, size: 16),
                            label: Text(altTime != null ? altTime!.format(context) : 'Vrijeme'),
                            onPressed: () async {
                              final picked = await showTimePicker(
                                context: context,
                                initialTime: TimeOfDay.fromDateTime(reservationAt),
                              );
                              if (picked != null) setState(() => altTime = picked);
                            },
                          ),
                        ),
                      ],
                    ),
                    const SizedBox(height: 16),
                    Container(
                      padding: const EdgeInsets.all(10),
                      decoration: BoxDecoration(
                        color: Colors.blue.withValues(alpha: 0.08),
                        borderRadius: BorderRadius.circular(8),
                      ),
                      child: Row(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          const Icon(Icons.info_outline, size: 18, color: Colors.blue),
                          const SizedBox(width: 8),
                          Expanded(
                            child: Text(
                              'Gost će odmah dobiti notifikaciju u aplikaciji i e-mail. Akcija se bilježi (ko, kada, razlog).',
                              style: Theme.of(context).textTheme.bodySmall,
                            ),
                          ),
                        ],
                      ),
                    ),
                  ],
                ),
              ),
            ),
          ),
          actions: [
            TextButton(
              onPressed: () => Navigator.of(dialogContext).pop(),
              child: const Text('Odustani'),
            ),
            FilledButton.icon(
              style: FilledButton.styleFrom(backgroundColor: AppColors.danger),
              icon: const Icon(Icons.send_outlined, size: 18),
              label: const Text('Odbij i obavijesti gosta'),
              onPressed: () {
                if (!formKey.currentState!.validate()) return;
                DateTime? alternativeAt;
                if (altDate != null && altTime != null) {
                  alternativeAt = DateTime(altDate!.year, altDate!.month, altDate!.day, altTime!.hour, altTime!.minute);
                }
                Navigator.of(dialogContext).pop(
                  RejectReservationResult(reason: reasonController.text.trim(), alternativeAt: alternativeAt),
                );
              },
            ),
          ],
        );
      },
    ),
  );

  reasonController.dispose();
  return result;
}
